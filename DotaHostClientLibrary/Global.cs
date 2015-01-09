
namespace DotaHostClientLibrary
{
    public static class Global
    {
        // Web root
        public const string ROOT = "http://dotahost.net/";

        // Where this executable is run from
        public static readonly string BASE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\";

        // Server init download location
        public const string SERVERINIT_DOWNLOAD = "http://dotahost.net/files/serverinit.zip";
        // AppData temporary folder
        public static readonly string TEMP = System.IO.Path.GetTempPath() + @"dotahost\";

        // URL to download the app from
        public const string DOWNLOAD_PATH_APP = "https://github.com/Jexah/DotaHostReleases/releases/download/{0}-mm/DotaHostManager.exe";

        // URL to download the updater from
        public const string DOWNLOAD_PATH_UPDATER = "https://github.com/Jexah/DotaHostReleases/releases/download/{0}-mmu/DotaHostManagerUpdater.exe";

        // URL to download version.txt from
        public const string DOWNLOAD_PATH_VERSION = ROOT + "version.txt";

        // URL to download addon updates from ({0} = addonID, {1} = version)
        public const string DOWNLOAD_PATH_ADDONS = "https://github.com/Jexah/DotaHostReleases/releases/download/{1}/{0}.zip";

        // URL to get addon info from ({0} = addonID)
        public const string DOWNLOAD_PATH_ADDON_INFO = ROOT + "addons/{0}.txt";

        // Client addon install location ({0} = dotaPath)
        public const string CLIENT_ADDON_INSTALL_LOCATION = @"{0}\dota\addons_dotahost\";

        // Lobby Manager Port
        public const int LOBBY_MANAGER_PORT = 2075;

        // Character used to seperate values in strings
        public const char MSG_SEP = '\0';

        // A user failed to connect to our server
        public const int PLAYER_STATUS_NOT_CONNECTED = 1;

        // A user connected to our server successfully
        public const int PLAYER_STATUS_CONNECTED = 2;

        // A user disconnected part way through a match
        public const int PLAYER_STATUS_DISCONNECTED = 3;
    }
}
