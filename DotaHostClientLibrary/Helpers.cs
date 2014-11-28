

using System;
using System.IO;

namespace DotaHostClientLibrary
{
    public static class Helpers
    {
        public static readonly string BASE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

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

    }
}
