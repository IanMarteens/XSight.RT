using IntSight.RayTracing.Engine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace RayEd
{
    public partial class SceneTree : UserControl
    {
        private sealed class ShapeInfo
        {
            public static int Current;

            public ShapeInfo(string boundsStr)
            {
                AbsoluteIndex = Current++;
                BoundsStr = boundsStr ?? string.Empty;
            }

            public int AbsoluteIndex;
            public string BoundsStr;
        }

        private IScene scene;
        private readonly StringBuilder strBuilder = new();

        private static readonly HashSet<string> unionNames =
            new(StringComparer.InvariantCultureIgnoreCase)
            {
                "union",
                "union2",
                "union2f",
                "union3",
                "union4f",
                "sunion",
                "sunion2",
                "merge",
            };
        private static readonly Dictionary<string, int> imageIndexes =
            new(StringComparer.InvariantCultureIgnoreCase)
            {
                { "union2f", 1 },
                { "sunion", 1 },
                { "sunion2", 1 },
                { "merge", 1 },
                { "union", 1 },
                { "union2", 1 },
                { "union4f", 1 },
                { "repeater", 3 },
                { "inter", 4 },
                { "inter2", 4 },
                { "inter2convex", 4 },
                { "interconvex", 4 },
                { "difference", 4 },
                { "diff2", 4 },
                { "diff2convex", 4 },
                { "rotate", 5 },
                { "transform", 5 },
                { "box", 6 },
                { "orthobox", 6 },
                { "alignedbox", 6 },
                { "sphere", 7 },
                { "psphere", 7 },
                { "cylinder", 8 },
                { "ycylinder", 8 },
                { "zcylinder", 8 },
                { "xcylinder", 9 },
                { "scale", 10 },
                { "sleaf", 11 },
                { "bleaf", 11 },
                { "uleaf", 11 },
                { "bfork", 11 },
                { "sfork", 11 },
                { "torus", 12 },
                { "xtorus", 12 },
                { "ytorus", 12 },
                { "ztorus", 12 },
                { "cone", 13 },
                { "blob", 14 },
                { "plane", 15 },
                { "xplane", 16 },
                { "yplane", 17 },
                { "zplane", 18 },
                { "quart", 19 },
                { "ball", 20 },
                { "cap", 21 },
                { "pipe", 22 },
                { "xpipe", 22 },
                { "ypipe", 22 },
                { "zpipe", 22 },
                { "ellipse", 23 },
                { "hyperboloid", 24 },
            };

        public SceneTree() => InitializeComponent();

        public void LoadScene(IScene scene)
        {
            this.scene = scene;
            bnSave.Enabled = true;
            LoadScene(scene.Root);
            if (treeView.Nodes.Count > 0)
            {
                TreeNode root = treeView.Nodes[0];
                var sb = new StringBuilder(root.ToolTipText);
                if (sb.Length > 0)
                    sb.AppendLine();
                root.ToolTipText = sb
                    .Append("sampler: ").AppendLine(scene.Sampler.GetType().Name)
                    .Append("camera: ").AppendLine(scene.Camera.GetType().Name).ToString();
            }
        }

        private void LoadScene(IShape root)
        {
            treeView.BeginUpdate();
            try
            {
                treeView.Nodes.Clear();
                ShapeInfo.Current = 0;
                RecurseShape(root, treeView.Nodes);
                treeView.ExpandAll();
                treeView.TopNode = treeView.Nodes[0];
                treeView.SelectedNode = treeView.TopNode;
            }
            finally
            {
                treeView.EndUpdate();
            }
        }

        /// <summary>Load tree from an XML document.</summary>
        /// <param name="document">An XML document.</param>
        private void LoadScene(XmlDocument document)
        {
            treeView.BeginUpdate();
            try
            {
                treeView.Nodes.Clear();
                ShapeInfo.Current = 0;
                foreach (XmlElement element in document.SelectNodes("/*"))
                    RecurseNode(element, treeView.Nodes);
                treeView.ExpandAll();
                treeView.TopNode = treeView.Nodes[0];
                treeView.SelectedNode = treeView.TopNode;
            }
            finally
            {
                treeView.EndUpdate();
            }
        }

        private static bool IsVector(XmlElement child, string name) =>
            string.Compare(child.Name, name, true) == 0 &&
                child.HasAttribute("x") &&
                child.HasAttribute("y") &&
                child.HasAttribute("z");

        private static string FormatCoord(string value) =>
            double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double result)
                ? result.ToString("F3", CultureInfo.InvariantCulture)
                : value;

        private static void GetVector(XmlElement child, StringBuilder sb) =>
            sb.Append(child.Name).Append('=')
                .Append(FormatCoord(child.GetAttribute("x"))).Append(',')
                .Append(FormatCoord(child.GetAttribute("y"))).Append(',')
                .Append(FormatCoord(child.GetAttribute("z")));

        const BindingFlags bf =
            BindingFlags.IgnoreCase | BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;

        private void RecurseShape(IBounded shape, TreeNodeCollection list)
        {
            var itemType = shape.GetType();
            TreeNode node = list.Add(itemType.Name.ToLowerInvariant());
            node.Tag = new ShapeInfo("[" + shape.Bounds.ToString() + "]");
            if (shape is IUnion u)
                node.ImageIndex = u.IsChecking ? 1 : 2;
            else
            {
                imageIndexes.TryGetValue(node.Text, out int idx);
                node.ImageIndex = idx;
            }
            node.SelectedImageIndex = node.ImageIndex;
            strBuilder.Length = 0;
            ExtractProperties(shape, itemType);
            if (strBuilder.Length > 0)
                node.ToolTipText = strBuilder.ToString();
            foreach (ChildrenAttribute attribute in itemType.
                GetCustomAttributes(typeof(ChildrenAttribute), false))
                foreach (string propName in attribute.Names)
                {
                    object child = null;
                    PropertyInfo propInfo = itemType.GetProperty(propName, bf);
                    if (propInfo != null)
                        child = propInfo.GetValue(shape);
                    else
                    {
                        FieldInfo fieldInfo = itemType.GetField(propName, bf);
                        if (fieldInfo != null)
                            child = fieldInfo.GetValue(shape);
                    }
                    switch (child)
                    {
                        case IBounded[] bnds:
                            foreach (IBounded shp in bnds)
                                RecurseShape(shp, node.Nodes);
                            break;
                        case IBounded bnd:
                            RecurseShape(bnd, node.Nodes);
                            break;
                    }
                }
        }

        private void ExtractProperties(IBounded shape, Type itemType)
        {
            foreach (PropertiesAttribute attribute in itemType.
                GetCustomAttributes(typeof(PropertiesAttribute), false))
                foreach (string propName in attribute.Names)
                {
                    if (propName == "bounds" || propName == "checkBounds")
                        continue;
                    object value = null;
                    PropertyInfo propInfo = itemType.GetProperty(propName, bf);
                    if (propInfo != null)
                    {
                        if (propInfo.PropertyType.IsValueType)
                            value = propInfo.GetValue(shape);
                    }
                    else
                    {
                        FieldInfo fieldInfo = itemType.GetField(propName, bf);
                        if (fieldInfo != null && fieldInfo.FieldType.IsValueType)
                            value = fieldInfo.GetValue(shape);
                    }
                    if (value != null)
                    {
                        if (strBuilder.Length > 0)
                            strBuilder.AppendLine();
                        strBuilder.Append(propName.ToLowerInvariant()).
                            Append('=').Append(value);
                    }
                }
        }

        private void RecurseNode(XmlElement element, TreeNodeCollection list)
        {
            if (!element.HasAttribute("bounds"))
                return;
            TreeNode node = list.Add(element.Name);
            string nodeText = "[" + element.Attributes["bounds"].Value + "]";
            if (element.HasAttribute("checkBounds") && !element.Name.EndsWith("torus"))
                node.ImageIndex =
                    string.Compare(element.Attributes["checkBounds"].Value, "true") == 0
                    ? 1 : 2;
            else
            {
                imageIndexes.TryGetValue(element.Name, out int idx);
                node.ImageIndex = idx;
            }
            node.SelectedImageIndex = node.ImageIndex;
            strBuilder.Length = 0;
            foreach (XmlAttribute attr in element.Attributes)
                if (string.Compare(attr.Name, "bounds", true) != 0 &&
                    string.Compare(attr.Name, "checkBounds", true) != 0)
                {
                    if (strBuilder.Length > 0)
                        strBuilder.AppendLine();
                    strBuilder.Append(attr.Name).Append('=').Append(attr.Value);
                }
            foreach (XmlElement child in element.ChildNodes)
                if (IsVector(child, "center") || IsVector(child, "normal") ||
                    IsVector(child, "bottom") || IsVector(child, "top") ||
                    IsVector(child, "from") || IsVector(child, "to") ||
                    IsVector(child, "scale") || IsVector(child, "delta") ||
                    IsVector(child, "a") || IsVector(child, "b") ||
                    IsVector(child, "c") || IsVector(child, "centroid") ||
                    IsVector(child, "axes") || IsVector(child, "angles"))
                {
                    if (strBuilder.Length > 0)
                        strBuilder.AppendLine();
                    GetVector(child, strBuilder);
                }
            if (strBuilder.Length > 0)
                node.ToolTipText = strBuilder.ToString();
            node.Tag = new ShapeInfo(nodeText);
            foreach (XmlElement elem in element.SelectNodes("./*"))
                RecurseNode(elem, node.Nodes);
        }

        private void TreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            e.DrawDefault = true;
            if (e.Node.Tag != null)
                e.Graphics.DrawString(((ShapeInfo)e.Node.Tag).BoundsStr, treeView.Font,
                    Brushes.Black, e.Node.Bounds.Right + 8, e.Bounds.Top);
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e) =>
            statusLabel.Text = string.Format("Shape {0} of {1}.",
                ((ShapeInfo)e.Node.Tag).AbsoluteIndex + 1, ShapeInfo.Current);

        private void Close_Click(object sender, EventArgs e) =>
            MainForm.CloseSceneTree();

        private void SaveXml_Click(object sender, EventArgs e)
        {
            if (saveDlg.ShowDialog() == DialogResult.OK)
                scene.Write(saveDlg.FileName);
        }

        /// <summary>Loads the tree from a saved scene XML.</summary>
        private void Open_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                XmlDocument document = new();
                document.Load(openFileDialog.FileName);
                scene = null;
                bnSave.Enabled = false;
                LoadScene(document);
            }
        }

        private void ExpandAll_Click(object sender, EventArgs e) => treeView.ExpandAll();

        private void CollapseAll_Click(object sender, EventArgs e) => treeView.CollapseAll();

        private void ExpandUnions_Click(object sender, EventArgs e)
        {
            treeView.BeginUpdate();
            try
            {
                treeView.CollapseAll();
                TopExpand(treeView.Nodes[0]);
            }
            finally
            {
                treeView.EndUpdate();
            }
        }

        private bool TopExpand(TreeNode treeNode)
        {
            if (!unionNames.Contains(treeNode.Text))
                return false;
            bool expand = false;
            foreach (TreeNode t in treeNode.Nodes)
                expand |= TopExpand(t);
            if (expand)
                treeNode.Expand();
            return true;
        }
    }
}
