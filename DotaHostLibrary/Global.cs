﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public static class Global
    {
        // Web root
        public const string ROOT = "https://dl.dropboxusercontent.com/u/25095474/dotahost/";

        // Where this executable is run from
        public static readonly string BASE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // GitHub download root
        private const string GITHUB = "https://codeload.github.com/ash47/";
    }
}