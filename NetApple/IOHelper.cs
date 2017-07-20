using System.IO;

namespace NetApple
{
    public static class IOHelper
    {
        public static void CloneDirectory(string source, string target)
        {
            foreach (var directory in Directory.GetDirectories(source))
            {
                var dirName = Path.GetFileName(directory);
                if (!Directory.Exists(Path.Combine(target, dirName)))
                    Directory.CreateDirectory(Path.Combine(target, dirName));
                CloneDirectory(directory, Path.Combine(target, dirName));
            }
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
    }
}