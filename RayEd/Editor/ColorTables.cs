using System;
using System.Drawing;
using System.Windows.Forms;

namespace RayEd
{
    internal class TanColorTable : ProfessionalColorTable
    {
        public TanColorTable() { }

        public override Color ButtonCheckedGradientBegin => Color.FromArgb(0xE1, 0xE6, 0xE8);

        public override Color ButtonCheckedGradientEnd => Color.FromArgb(0xE1, 0xE6, 0xE8);

        public override Color ButtonCheckedGradientMiddle => Color.FromArgb(0xE1, 0xE6, 0xE8);

        public override Color ButtonPressedBorder => Color.FromArgb(0x31, 0x6A, 0xC5);

        public override Color ButtonPressedGradientBegin => Color.FromArgb(0x98, 0xB5, 0xE2);

        public override Color ButtonPressedGradientEnd => Color.FromArgb(0x98, 0xB5, 0xE2);

        public override Color ButtonPressedGradientMiddle => Color.FromArgb(0x98, 0xB5, 0xE2);

        public override Color ButtonSelectedBorder => Color.FromArgb(0x31, 0x6A, 0xC5);

        public override Color ButtonSelectedGradientBegin => Color.FromArgb(0xC1, 0xD2, 0xEE);

        public override Color ButtonSelectedGradientEnd => Color.FromArgb(0xC1, 0xD2, 0xEE);

        public override Color ButtonSelectedGradientMiddle => Color.FromArgb(0xC1, 0xD2, 0xEE);

        public override Color CheckBackground => Color.FromArgb(0xE1, 0xE6, 0xE8);

        public override Color CheckPressedBackground => Color.FromArgb(0x31, 0x6A, 0xC5);

        public override Color CheckSelectedBackground => Color.FromArgb(0x31, 0x6A, 0xC5);

        public override Color GripDark => Color.FromArgb(0xC1, 0xBE, 0xB3);

        public override Color GripLight => Color.FromArgb(0xFF, 0xFF, 0xFF);

        public override Color ImageMarginGradientBegin => Color.FromArgb(0xFE, 0xFE, 0xFB);

        public override Color ImageMarginGradientEnd => Color.FromArgb(0xBD, 0xBD, 0xA3);

        public override Color ImageMarginGradientMiddle => Color.FromArgb(0xEC, 0xE7, 0xE0);

        public override Color ImageMarginRevealedGradientBegin => Color.FromArgb(0xF7, 0xF6, 0xEF);

        public override Color ImageMarginRevealedGradientEnd => Color.FromArgb(0xE6, 0xE3, 0xD2);

        public override Color ImageMarginRevealedGradientMiddle => Color.FromArgb(0xF2, 0xF0, 0xE4);

        public override Color MenuBorder => Color.FromArgb(0x8A, 0x86, 0x7A);

        public override Color MenuItemBorder
        { get { return Color.FromArgb(0x31, 0x6A, 0xC5); } }

        public override Color MenuItemPressedGradientBegin
        { get { return Color.FromArgb(0xFC, 0xFC, 0xF9); } }

        public override Color MenuItemPressedGradientEnd
        { get { return Color.FromArgb(0xF6, 0xF4, 0xEC); } }

        public override Color MenuItemPressedGradientMiddle
        { get { return Color.FromArgb(0xF2, 0xF0, 0xE4); } }

        public override Color MenuItemSelected
        { get { return Color.FromArgb(0xC1, 0xD2, 0xEE); } }

        public override Color MenuItemSelectedGradientBegin
        { get { return Color.FromArgb(0xC1, 0xD2, 0xEE); } }

        public override Color MenuItemSelectedGradientEnd
        { get { return Color.FromArgb(0xC1, 0xD2, 0xEE); } }

        public override Color MenuStripGradientBegin
        { get { return Color.FromArgb(0xE5, 0xE5, 0xD7); } }

        public override Color MenuStripGradientEnd
        { get { return Color.FromArgb(0xF4, 0xF2, 0xE8); } }

        public override Color OverflowButtonGradientBegin
        { get { return Color.FromArgb(0xF3, 0xF2, 0xF0); } }

        public override Color OverflowButtonGradientEnd
        { get { return Color.FromArgb(0x92, 0x92, 0x76); } }

        public override Color OverflowButtonGradientMiddle
        { get { return Color.FromArgb(0xE2, 0xE1, 0xDB); } }

        public override Color RaftingContainerGradientBegin
        { get { return Color.FromArgb(0xE5, 0xE5, 0xD7); } }

        public override Color RaftingContainerGradientEnd
        { get { return Color.FromArgb(0xF4, 0xF2, 0xE8); } }

        public override Color SeparatorDark
        { get { return Color.FromArgb(0xC5, 0xC2, 0xB8); } }

        public override Color SeparatorLight
        { get { return Color.FromArgb(0xFF, 0xFF, 0xFF); } }

        public override Color ToolStripBorder
        { get { return Color.FromArgb(0xA3, 0xA3, 0x7C); } }

        public override Color ToolStripDropDownBackground
        { get { return Color.FromArgb(0xFC, 0xFC, 0xF9); } }

        public override Color ToolStripGradientBegin
        { get { return Color.FromArgb(0xFE, 0xFE, 0xFB); } }

        public override Color ToolStripGradientEnd
        { get { return Color.FromArgb(0xBD, 0xBD, 0xA3); } }

        public override Color ToolStripGradientMiddle
        { get { return Color.FromArgb(0xEC, 0xE7, 0xE0); } }
    }

    internal class SilverColorTable : ProfessionalColorTable
    {
        public SilverColorTable() { }

        public override Color ButtonCheckedGradientBegin
        { get { return Color.FromArgb(0xE1, 0xE6, 0xE8); } }

        public override Color ButtonCheckedGradientEnd
        { get { return Color.FromArgb(0xE1, 0xE6, 0xE8); } }

        public override Color ButtonCheckedGradientMiddle
        { get { return Color.FromArgb(0xE1, 0xE6, 0xE8); } }

        public override Color ButtonPressedBorder
        { get { return Color.FromArgb(0x31, 0x6A, 0xC5); } }

        public override Color ButtonPressedGradientBegin
        { get { return Color.FromArgb(0x98, 0xB5, 0xE2); } }

        public override Color ButtonPressedGradientEnd
        { get { return Color.FromArgb(0x98, 0xB5, 0xE2); } }

        public override Color ButtonPressedGradientMiddle
        { get { return Color.FromArgb(0x98, 0xB5, 0xE2); } }

        public override Color ButtonSelectedBorder
        { get { return Color.FromArgb(0x31, 0x6A, 0xC5); } }

        public override Color ButtonSelectedGradientBegin
        { get { return Color.FromArgb(0xE4, 0xEA, 0xF2); } }

        public override Color ButtonSelectedGradientEnd
        { get { return Color.FromArgb(0xE4, 0xEA, 0xF2); } }

        public override Color ButtonSelectedGradientMiddle
        { get { return Color.FromArgb(0xE4, 0xEA, 0xF2); } }

        public override Color CheckBackground
        { get { return Color.FromArgb(0xE1, 0xE6, 0xE8); } }

        public override Color CheckPressedBackground
        { get { return Color.FromArgb(0x31, 0x6A, 0xC5); } }

        public override Color CheckSelectedBackground
        { get { return Color.FromArgb(0x31, 0x6A, 0xC5); } }

        public override Color GripDark
        { get { return Color.FromArgb(0xA0, 0xA0, 0xB4); } }

        public override Color GripLight
        { get { return Color.FromArgb(0xFF, 0xFF, 0xFF); } }

        public override Color ImageMarginGradientBegin
        { get { return Color.FromArgb(0xF4, 0xF7, 0xFC); } }

        public override Color ImageMarginGradientEnd
        { get { return Color.FromArgb(0xD4, 0xD8, 0xE6); } }

        public override Color ImageMarginGradientMiddle
        { get { return Color.FromArgb(0xE9, 0xEC, 0xFA); } }

        public override Color ImageMarginRevealedGradientBegin
        { get { return Color.FromArgb(0xF7, 0xF6, 0xEF); } }

        public override Color ImageMarginRevealedGradientEnd
        { get { return Color.FromArgb(0xE6, 0xe3, 0xD2); } }

        public override Color ImageMarginRevealedGradientMiddle
        { get { return Color.FromArgb(0xF2, 0xF0, 0xE4); } }

        public override Color MenuBorder
        { get { return Color.FromArgb(0x69, 0x68, 0x6A); } }

        public override Color MenuItemBorder
        { get { return Color.FromArgb(0x31, 0x6A, 0xC5); } }

        public override Color MenuItemPressedGradientBegin
        { get { return Color.FromArgb(0xFC, 0xFC, 0xF9); } }

        public override Color MenuItemPressedGradientEnd
        { get { return Color.FromArgb(0xF6, 0xF4, 0xEC); } }

        public override Color MenuItemPressedGradientMiddle
        { get { return Color.FromArgb(0xF2, 0xF0, 0xE4); } }

        public override Color MenuItemSelected
        { get { return Color.FromArgb(0xE4, 0xEA, 0xF2); } }

        public override Color MenuItemSelectedGradientBegin
        { get { return Color.FromArgb(0xE4, 0xEA, 0xF2); } }

        public override Color MenuItemSelectedGradientEnd
        { get { return Color.FromArgb(0xE4, 0xEA, 0xF2); } }

        public override Color MenuStripGradientBegin
        { get { return Color.FromArgb(0xE9, 0xEC, 0xFA); } }

        public override Color MenuStripGradientEnd
        { get { return Color.FromArgb(0xF4, 0xF7, 0xFC); } }

        public override Color OverflowButtonGradientBegin
        { get { return Color.FromArgb(0xF3, 0xF2, 0xF0); } }

        public override Color OverflowButtonGradientEnd
        { get { return Color.FromArgb(0x92, 0x92, 0x76); } }

        public override Color OverflowButtonGradientMiddle
        { get { return Color.FromArgb(0xE2, 0xE1, 0xDB); } }

        public override Color RaftingContainerGradientBegin
        { get { return Color.FromArgb(0xE9, 0xEC, 0xFA); } }

        public override Color RaftingContainerGradientEnd
        { get { return Color.FromArgb(0xF4, 0xF7, 0xFC); } }

        public override Color SeparatorDark
        { get { return Color.FromArgb(0xA0, 0xA0, 0xB4); } }

        public override Color SeparatorLight
        { get { return Color.FromArgb(0xFF, 0xFF, 0xFF); } }

        public override Color ToolStripBorder
        { get { return Color.FromArgb(0x7C, 0x7D, 0x7E); } }

        public override Color ToolStripDropDownBackground
        { get { return Color.FromArgb(0xFC, 0xFC, 0xFC); } }

        public override Color ToolStripGradientBegin
        { get { return Color.FromArgb(0xFA, 0xFA, 0xFD); } }

        public override Color ToolStripGradientEnd
        { get { return Color.FromArgb(0xC4, 0xCB, 0xDB); } }

        public override Color ToolStripGradientMiddle
        { get { return Color.FromArgb(0xE9, 0xEC, 0xFA); } }
    }

    internal class BlueColorTable : ProfessionalColorTable
    {
        public BlueColorTable() { }

        public override Color ButtonCheckedGradientBegin
        { get { return Color.FromArgb(255, 213, 140); } }

        public override Color ButtonCheckedGradientEnd
        { get { return Color.FromArgb(255, 173, 86); } }

        public override Color ButtonCheckedGradientMiddle
        { get { return Color.FromArgb(255, 197, 118); } }

        public override Color ButtonPressedBorder
        { get { return Color.FromArgb(0x31, 0x6A, 0xC5); } }

        public override Color ButtonPressedGradientBegin
        { get { return Color.FromArgb(254, 145, 78); } }

        public override Color ButtonPressedGradientEnd
        { get { return Color.FromArgb(255, 211, 142); } }

        public override Color ButtonPressedGradientMiddle
        { get { return Color.FromArgb(255, 177, 109); } }

        public override Color ButtonPressedHighlightBorder
        { get { return Color.FromArgb(0, 0, 128); } }

        public override Color ButtonSelectedBorder
        { get { return Color.FromArgb(0, 0, 128); } }

        public override Color ButtonSelectedGradientBegin
        { get { return Color.FromArgb(255, 244, 204); } }

        public override Color ButtonSelectedGradientEnd
        { get { return Color.FromArgb(255, 208, 145); } }

        public override Color ButtonSelectedGradientMiddle
        { get { return Color.FromArgb(255, 224, 172); } }

        public override Color ButtonSelectedHighlight
        { get { return Color.FromArgb(255, 238, 194); } }

        public override Color CheckBackground
        { get { return Color.FromArgb(255, 192, 111); } }

        public override Color CheckPressedBackground
        { get { return Color.FromArgb(254, 128, 62); } }

        public override Color CheckSelectedBackground
        { get { return Color.FromArgb(254, 128, 62); } }

        public override Color GripDark
        { get { return Color.FromArgb(39, 65, 118); } }

        public override Color GripLight
        { get { return Color.FromArgb(255, 255, 255); } }

        public override Color ImageMarginGradientBegin
        { get { return Color.FromArgb(227, 239, 255); } }

        public override Color ImageMarginGradientEnd
        { get { return Color.FromArgb(134, 172, 228); } }

        public override Color ImageMarginGradientMiddle
        { get { return Color.FromArgb(203, 224, 252); } }

        public override Color ImageMarginRevealedGradientBegin
        { get { return Color.FromArgb(0xF7, 0xF6, 0xEF); } }

        public override Color ImageMarginRevealedGradientEnd
        { get { return Color.FromArgb(0xE6, 0xe3, 0xD2); } }

        public override Color ImageMarginRevealedGradientMiddle
        { get { return Color.FromArgb(0xF2, 0xF0, 0xE4); } }

        public override Color MenuBorder
        { get { return Color.FromArgb(0, 45, 150); } }

        public override Color MenuItemBorder
        { get { return Color.FromArgb(0, 0, 128); } }

        public override Color MenuItemPressedGradientBegin
        { get { return Color.FromArgb(227, 238, 255); } }

        public override Color MenuItemPressedGradientEnd
        { get { return Color.FromArgb(137, 174, 228); } }

        public override Color MenuItemPressedGradientMiddle
        { get { return Color.FromArgb(0xF2, 0xF0, 0xE4); } }

        public override Color MenuItemSelected
        { get { return Color.FromArgb(255, 238, 194); } }

        public override Color MenuItemSelectedGradientBegin
        { get { return Color.FromArgb(255, 255, 255); } }

        public override Color MenuItemSelectedGradientEnd
        { get { return Color.FromArgb(255, 238, 194); } }

        public override Color MenuStripGradientBegin
        { get { return Color.FromArgb(158, 190, 245); } }

        public override Color MenuStripGradientEnd
        { get { return Color.FromArgb(196, 217, 249); } }

        public override Color OverflowButtonGradientBegin
        { get { return Color.FromArgb(0xF3, 0xF2, 0xF0); } }

        public override Color OverflowButtonGradientEnd
        { get { return Color.FromArgb(0x92, 0x92, 0x76); } }

        public override Color OverflowButtonGradientMiddle
        { get { return Color.FromArgb(0xE2, 0xE1, 0xDB); } }

        public override Color RaftingContainerGradientBegin
        { get { return Color.FromArgb(0xE9, 0xEC, 0xFA); } }

        public override Color RaftingContainerGradientEnd
        { get { return Color.FromArgb(0xF4, 0xF7, 0xFC); } }

        public override Color SeparatorDark
        { get { return Color.FromArgb(106, 140, 203); } }

        public override Color SeparatorLight
        { get { return Color.FromArgb(255, 255, 255); } }

        public override Color ToolStripBorder
        { get { return Color.FromArgb(59, 97, 156); } }

        public override Color ToolStripDropDownBackground
        { get { return Color.FromArgb(0xFC, 0xFC, 0xFC); } }

        public override Color ToolStripGradientBegin
        { get { return Color.FromArgb(221, 236, 254); } }

        public override Color ToolStripGradientEnd
        { get { return Color.FromArgb(129, 169, 226); } }

        public override Color ToolStripGradientMiddle
        { get { return Color.FromArgb(203, 224, 251); } }
    }
}