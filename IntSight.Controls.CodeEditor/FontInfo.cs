using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IntSight.Controls
{
    /// <summary>Retrieves information about text metrics.</summary>
    public static class FontInfo
    {
        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct TEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextMetrics(IntPtr hdc, out TEXTMETRIC lptm);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        /// <summary>
        /// Retrieves a list with the names of the installed monospaced fonts.
        /// </summary>
        /// <param name="formHandle">A window handle.</param>
        /// <returns>A list with monospaced font names.</returns>
        public static string[] GetMonospacedFonts(this IntPtr formHandle)
        {
            List<string> result = new List<string>();
            IntPtr dc = GetDC(formHandle);
            try
            {
                foreach (FontFamily family in FontFamily.Families)
                    if (family.IsStyleAvailable(FontStyle.Regular))
                        using (Font f = new Font(family, 10.0F))
                        {
                            IntPtr oldObj = SelectObject(dc, f.ToHfont());
                            GetTextMetrics(dc, out TEXTMETRIC metric);
                            if ((metric.tmPitchAndFamily & 0x01) == 0)
                                result.Add(family.Name);
                            SelectObject(dc, oldObj);
                        }
            }
            finally
            {
                ReleaseDC(formHandle, dc);
            }
            return result.ToArray();
        }

        public static bool IsMonospaced(Form form, string fontName)
        {
            using Font f = new Font(fontName, 10.0F);
            if (!f.FontFamily.IsStyleAvailable(FontStyle.Regular))
                return false;
            IntPtr dc = GetDC(form.Handle);
            try
            {
                SelectObject(dc, f.ToHfont());
                GetTextMetrics(dc, out TEXTMETRIC metric);
                return (metric.tmPitchAndFamily & 0x01) == 0;
            }
            finally
            {
                ReleaseDC(form.Handle, dc);
            }
        }
    }
}
