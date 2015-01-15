

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DotaHostClientLibrary
{
    public static class Helpers
    {
        // Full path and exe name of execuing assembly
        public static readonly string FullExePath = String.Join("\\", RemoveIndex(RemoveIndex(RemoveIndex(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Split('/'), 0), 0), 0));

        // Directory of executing assembly
        public static readonly string BasePath = Path.GetDirectoryName(FullExePath) + "\\";

        // Used to generate random numbers
        private static readonly Random Random = new Random((int)DateTime.Now.Ticks);

        // Used to track the order of logs
        private static int _count;

        // Generates a random string of the given size
        public static string RandomString(int size)
        {
            var sb = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Random.NextDouble() + 65)));
                sb.Append(ch);
            }

            return sb.ToString();
        }

        // Helper function to log all outputs
        public static void Log(string str)
        {
            Console.WriteLine(_count + ": " + str);
            try
            {
                File.AppendAllText(BasePath + "log.txt", _count + ": " + str + Environment.NewLine);
            }
            catch
            {
                Timers.SetTimeout(100, Timers.Milliseconds, () => { LogRetry(str, _count); });
            }
            _count++;
        }

        // Helpers.log retry function to log all outputs
        public static void LogRetry(string str, int retryCount)
        {
            Console.WriteLine(_count + ": " + str);
            try
            {
                File.AppendAllText(BasePath + "log.txt", _count + ": " + str + Environment.NewLine);
            }
            catch
            {
                LogRetry(str, retryCount);
            }
        }

        // Another helper function, stolen from somewhere else. StackExchange I believe
        public static string[] RemoveIndex(string[] indicesArray, int removeAt)
        {
            var newIndicesArray = new string[indicesArray.Length - 1];
            int i = 0;
            int j = 0;
            while (i < indicesArray.Length)
            {
                if (i != removeAt)
                {
                    newIndicesArray[j] = indicesArray[i];
                    j++;
                }
                i++;
            }
            return newIndicesArray;
        }

        // Helper function to calculate CRC, stolen from somewhere
        public static string CalculateCrc(string fileName)
        {
            var crc32 = new Crc32();
            var hash = String.Empty;
            using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                hash = crc32.ComputeHash(fs).Aggregate(hash, (current, b) => current + b.ToString("x2").ToLower());
            }
            return hash;
        }

        // Delete the folder (and it's contents!) at the given path
        // Use this with care!!!
        public static void DeleteFolder(string path, bool recursive)
        {
            try
            {
                Directory.Delete(path, recursive);
            }
            catch
            {
                // ignored
            }
        }

        // Deletes a file with no chance of exceptions
        public static void DeleteSafe(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignored
            }
        }

        // Copies a directory from one place to another
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            try
            {
                // Get the subdirectories for the specified directory.
                var dir = new DirectoryInfo(sourceDirName);
                var dirs = dir.GetDirectories();

                if (!dir.Exists)
                {
                    return;
                }

                // If the destination directory doesn't exist, create it. 
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }

                // Get the files in the directory and copy them to the new location.
                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    var temppath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(temppath, true);
                }

                // If copying subdirectories, copy them and their contents to new location. 
                if (!copySubDirs) return;

                foreach (var subdir in dirs)
                {
                    var temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
            catch
            {
                // ignored
            }
        }

        // Packs arguments nicely for you
        public static string PackArguments(params string[] arguments)
        {
            return String.Join("" + Global.MsgSep, arguments);
        }
    }
}
