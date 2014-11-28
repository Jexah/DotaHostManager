﻿

using System;
using System.IO;
using System.Text;

namespace DotaHostClientLibrary
{
    public static class Helpers
    {
        public static readonly string BASE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // Used to generate random numbers
        private static Random random = new Random((int)DateTime.Now.Ticks);

        // Generates a random string of the given size
        public static string randomString(int size)
        {
            StringBuilder sb = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                sb.Append(ch);
            }

            return sb.ToString();
        }

        // Helper function to log all outputs
        public static void log(string str)
        {
            Console.WriteLine(str);
            File.AppendAllText(BASE_PATH + "log.txt", str + Environment.NewLine);
        }

        // Another helper function, stolen from somewhere else. StackExchange I believe
        public static string[] RemoveIndex(string[] IndicesArray, int RemoveAt)
        {
            string[] newIndicesArray = new string[IndicesArray.Length - 1];
            int i = 0;
            int j = 0;
            while (i < IndicesArray.Length)
            {
                if (i != RemoveAt)
                {
                    newIndicesArray[j] = IndicesArray[i];
                    j++;
                }
                i++;
            }
            return newIndicesArray;
        }

        // Helper function to calculate CRC, stolen from somewhere
        public static string calculateCRC(string fileName)
        {
            Crc32 crc32 = new Crc32();
            String hash = String.Empty;
            using (FileStream fs = File.Open(fileName, FileMode.Open))
                foreach (byte b in crc32.ComputeHash(fs))
                {
                    hash += b.ToString("x2").ToLower();
                }
            return hash;
        }

        // Delete the folder (and it's contents!) at the given path
        // Use this with care!!!
        public static void deleteFolder(string path)
        {
            // Ensure the directory exists
            if (Directory.Exists(path))
            {
                DirectoryInfo dir = new DirectoryInfo(path);

                foreach (FileInfo fi in dir.GetFiles())
                {
                    fi.IsReadOnly = false;
                    fi.Delete();
                }

                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    deleteFolder(di.FullName);
                    di.Delete();
                }
            }
        }

        // Copies a directory from one place to another
        public static void directoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

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
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    directoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}