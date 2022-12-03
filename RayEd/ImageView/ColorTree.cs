using System.Linq;

namespace RayEd;

/// <summary>A kd-tree for colors.</summary>
public class ColorTree
{
    /// <summary>Represents a kd-tree node.</summary>
    private class ColorNode
    {
        /// <summary>Color name.</summary>
        public string name;
        /// <summary>Color components.</summary>
        public byte[] color;
        /// <summary>Splitting axis.</summary>
        public byte axis;
        /// <summary>Reference to the left subtree.</summary>
        public ColorNode left;
        /// <summary>Reference to the right subtree.</summary>
        public ColorNode right;
    }

    private struct ColorRec
    {
        /// <summary>Distance to the searched color.</summary>
        public int distance;
        /// <summary>Best node, so far.</summary>
        public ColorNode node;
    }

    /// <summary>Singleton instance of the color tree.</summary>
    public static readonly ColorTree Instance = new();

    /// <summary>The kd-tree root node.</summary>
    private ColorNode root;

    /// <summary>Creates a color tree and initializes it with all known colors.</summary>
    public ColorTree()
    {
        foreach (var color in from KnownColor kc in Enum.GetValues(typeof(KnownColor))
                              where typeof(Color).GetProperty(kc.ToString()) != null
                              let c = Color.FromKnownColor(kc)
                              where c.A == 255
                              select c)
            Add(color);
    }

    private static string GetColorClass(Color c)
    {
        if (c.GetSaturation() < 0.001F)
            return "Gray";
        int hue = (int)Math.Round(c.GetHue());
        return 
            hue < 16 ? "Red"
            : hue < 39 ? "Orange"
            : hue < 80 ? "Yellow"
            : hue < 159 ? "Green"
            : hue < 195 ? "Cyan"
            : hue < 259 ? "Blue"
            : hue < 348 ? "Violet" : "Red";
    }

    public static IEnumerable<IGrouping<string, Color>> GetColorList() =>
        from KnownColor kc in Enum.GetValues(typeof(KnownColor))
        where typeof(Color).GetProperty(kc.ToString()) != null
        let c = Color.FromKnownColor(kc)
        where c.A == 255
        group c by GetColorClass(c) into classes
        orderby classes.Key
        select classes;

    /// <summary>Adds a named color to the tree.</summary>
    /// <param name="color">Color to add.</param>
    public void Add(Color color) =>
        Add(new[] { color.R, color.G, color.B }, 0, ref root, color.Name);

    private void Add(byte[] color, byte axis, ref ColorNode node, string name)
    {
        if (node == null)
            node = new ColorNode() { color = color, axis = axis, name = name };
        else
        {
            byte v1 = color[axis];
            byte v2 = node.color[axis];
            if (v1 < v2)
                Add(color, (byte)((axis + 1) % 3), ref node.left, name);
            else if (v1 > v2 ||
                color[0] != node.color[0] ||
                color[1] != node.color[1] ||
                color[2] != node.color[2])
                Add(color, (byte)((axis + 1) % 3), ref node.right, name);
        }
    }

    /// <summary>Finds the nearest named color.</summary>
    /// <param name="color">Color to identify.</param>
    /// <param name="exact">Was it an exact match?</param>
    /// <returns>The found name, or an empty string if the search failed.</returns>
    public string FindNeighbor(Color color, out bool exact)
    {
        ColorRec cr = Find(root, new byte[] { color.R, color.G, color.B }, 8);
        if (cr.node == null)
        {
            exact = false;
            return string.Empty;
        }
        else
        {
            exact = cr.distance == 0;
            return cr.node.name;
        }
    }

    private static ColorRec Find(ColorNode node, byte[] color, int tolerance)
    {
        ColorRec cr1 = new();
        if (node == null)
            return cr1;
        int d = Math.Max(Math.Max(
            Math.Abs(node.color[0] - color[0]),
            Math.Abs(node.color[1] - color[1])),
            Math.Abs(node.color[2] - color[2]));
        if (d <= tolerance)
        {
            cr1.node = node;
            cr1.distance = d;
            if (d == 0)
                return cr1;
        }
        int b1 = color[node.axis];
        int b2 = node.color[node.axis];
        if (b1 <= b2 + tolerance)
        {
            ColorRec cr2 = Find(node.left, color, tolerance);
            if (cr1.node == null || cr2.node != null && cr2.distance < cr1.distance)
                cr1 = cr2;
        }
        if (b1 >= b2 - tolerance)
        {
            ColorRec cr2 = Find(node.right, color, tolerance);
            if (cr1.node == null || cr2.node != null && cr2.distance < cr1.distance)
                cr1 = cr2;
        }
        return cr1;
    }
}
