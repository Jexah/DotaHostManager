﻿
namespace DotaHostClientLibrary
{
    public static class Global
    {
        // Web root
        //public const string ROOT = "https://dl.dropboxusercontent.com/u/25095474/dotahost/";
        public const string ROOT = "http://dotahost.net/";

        // Where this executable is run from
        public static readonly string BASE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // AppData temporary folder
        public static readonly string TEMP = System.IO.Path.GetTempPath() + @"dotahost\";

        // URL to download the app from
        public static readonly string DOWNLOAD_PATH_APP = ROOT + "DotaHostManager.exe";

        // URL to download the updater from
        public static readonly string DOWNLOAD_PATH_UPDATER = ROOT + "DotaHostManagerUpdater.exe";

        // URL to download version.txt from
        public static readonly string DOWNLOAD_PATH_VERSION = ROOT + "version.txt";

        // URL to download addon updates from ({0} = addonID, {1} = version)
        public static readonly string DOWNLOAD_PATH_ADDONS = "https://github.com/ash47/DotaHostAddons/releases/download/{1}/{0}.zip";

        // URL to get addon info from ({0} = addonID)
        public static readonly string DOWNLOAD_PATH_ADDON_INFO = ROOT + "addons/{0}.txt";

        // Client addon install location ({0} = dotaPath)
        public static readonly string CLIENT_ADDON_INSTALL_LOCATION = @"{0}\dota\addons_dotahost\";
    }
}
