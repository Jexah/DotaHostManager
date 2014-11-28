using DotaHostClientLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public static class AddonCompiler
    {
        // Path to store temp addons
        private static string TEMP_DIR = Global.BASE_PATH + @"dotahost_addon_temp\";

        // Compiles the given list of addons into the given path
        // if generateRandomPath is true, it will append a random folder to the end
        // NOTE: Path NEEDS to lead in a slash
        // ASSUMPTION: This function CAN NOT be run in parrellel! Wait for it to finish before running again!
        public static string compileAddons(List<Addon> addons, string searchPath, string outputPath, bool generateRandomPath=false)
        {
            // Validate input
            if (addons == null) return null;
            if (searchPath == null) return null;
            if (outputPath == null) return null;

            // Add the random path if needed
            if (generateRandomPath)
            {
                outputPath += Helpers.randomString(8) + "\\";
            }

            // Clear the output path if it already exists
            Helpers.deleteFolder(outputPath);

            // Create the folder
            Directory.CreateDirectory(outputPath);

            // Compile each addon in
            foreach(Addon addon in addons)
            {
                // The name of the archive
                string zipName = searchPath + addon.Id + ".zip";

                // Ensure the addon exists
                if (!File.Exists(zipName))
                {
                    Helpers.log("WARNING: Could not find " + zipName);
                    continue;
                }

                // Cleanup temp folder
                Helpers.deleteFolder(TEMP_DIR);

                // Extract the archive
                ZipFile.ExtractToDirectory(zipName, TEMP_DIR);

                // Read the manifest file
                KV manifest = KV.read(TEMP_DIR + "manifest.kv");

                // Ensure a valid manifest file
                if (manifest == null)
                {
                    Helpers.log("Manifest file for " + addon.Id + " is missing or invalid!");
                    continue;
                }

                // Grab the compile information for the given mod
                manifest = manifest.getKV(addon.Id);
                if (manifest == null)
                {
                    Helpers.log("Manifest file for " + addon.Id + " doesn't contain any compile information!");
                    continue;
                }

                // See if there is an addon directory
                string addonDir = manifest.getValue("directory");

                // Check what kind of addon dir was specified
                if(addonDir == null)
                {
                    // None at all, assume root
                    addonDir = TEMP_DIR;
                }
                else if(addonDir == "")
                {
                    // Blank, assume root
                    addonDir = TEMP_DIR;
                }
                else
                {
                    // Something, append it to the path
                    addonDir = TEMP_DIR + addonDir + @"\";
                }

                // Process the commands
                List<string> commands;

                // Copy directory commands
                commands = manifest.getValues("copyDirectory");
                if (commands != null)
                {
                    foreach (String command in commands)
                    {
                        // Remove double dots
                        string dir = command.Replace("..", "");

                        // Copy directory in
                        Helpers.directoryCopy(addonDir + dir, outputPath + dir, true);
                    }
                }

                // Copy file commands
                commands = manifest.getValues("copyFile");
                if (commands != null)
                {
                    foreach (String command in commands)
                    {
                        // Remove double dots
                        string file = command.Replace("..", "");

                        // Copy the file
                        if (File.Exists(addonDir + file))
                        {
                            File.Copy(addonDir + file, outputPath + file, true);
                        }
                    }
                }

                // Language file merging
                commands = manifest.getValues("mergeKV");
                if (commands != null)
                {
                    foreach (String command in commands)
                    {
                        // Remove double dots
                        string file = command.Replace("..", "");

                        // Ensure the file we want to merge exists
                        if (File.Exists(addonDir + file))
                        {
                            // Do we need to actually merge?
                            if (File.Exists(outputPath + file))
                            {
                                // File exists, merge :(
                                KV kv1 = KV.read(outputPath + file);
                                KV kv2 = KV.read(addonDir + file);

                                // Ensure both are valid (if not, keep the original)
                                if(kv1 != null && kv2 != null)
                                {
                                    // Perform the merge
                                    kv1.merge(kv2);
                                }
                            }
                            else
                            {
                                // File doesn't exist, just copy
                                File.Copy(addonDir + file, outputPath + file, true);
                            }
                            
                        }
                    }
                }
            }

            // Cleanup temp folder
            Helpers.deleteFolder(TEMP_DIR);

            // Return the path to the compiled addons
            return outputPath;
        }
    }
}
