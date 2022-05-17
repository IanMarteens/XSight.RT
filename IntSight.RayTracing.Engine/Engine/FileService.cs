using System;
using System.IO;

namespace IntSight.RayTracing.Engine
{
    public static class FileService
    {
        public static string SourceFolder { get; set; } = "";

        public static string FindFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                if (!string.IsNullOrEmpty(SourceFolder))
                {
                    string fn1 = Path.Combine(SourceFolder, fileName);
                    if (File.Exists(fn1))
                        return fn1;
                }
                string fn = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures), fileName);
                if (File.Exists(fn))
                    return fn;
                fn = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments), "scenes", fileName);
                if (File.Exists(fn))
                    return fn;
                fn = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments), fileName);
                if (File.Exists(fn))
                    return fn;
            }
            return fileName;
        }
    }
}
