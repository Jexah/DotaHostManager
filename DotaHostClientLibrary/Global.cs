
namespace DotaHostClientLibrary
{
    public static class Global
    {
        // Web root
        public const string Root = "http://dotahost.net/";

        // Where this executable is run from
        public static readonly string BasePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\";

        // Server init download location
        public const string ServerinitDownload = "http://dotahost.net/files/serverinit.zip";

        // AppData temporary folder
        public static readonly string Temp = System.IO.Path.GetTempPath() + @"dotahost\";

        // URL to download the app from
        public const string DownloadPathApp = "https://github.com/Jexah/DotaHostReleases/releases/download/{0}/DotaHostManager.exe";

        // URL to download the updater from
        public const string DownloadPathUpdater = "https://github.com/Jexah/DotaHostReleases/releases/download/{0}/DotaHostManagerUpdater.exe";

        // URL to download addon updates from ({0} = addonID, {1} = version)
        public const string DownloadPathAddons = "https://github.com/Jexah/DotaHostReleases/releases/download/{1}/{0}.zip";

        // URL to get addon info from ({0} = addonID)
        public const string DownloadPathAddonInfo = Root + "addons/{0}.txt";

        // Client addon install location ({0} = dotaPath)
        public const string ClientAddonInstallLocation = @"{0}dota\addons_dotahost\";

        // Lobby Manager Port
        public const int LobbyManagerPort = 2075;

        // Character used to seperate values in strings
        public const char MsgSep = '\0';

        // A user failed to connect to our server
        public const int PlayerStatusNotConnected = 1;

        // A user connected to our server successfully
        public const int PlayerStatusConnected = 2;

        // A user disconnected part way through a match
        public const int PlayerStatusDisconnected = 3;
    }
}
