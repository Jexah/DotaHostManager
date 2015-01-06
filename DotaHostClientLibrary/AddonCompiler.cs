using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace DotaHostClientLibrary
{
    public static class AddonCompiler
    {
        // Path to store temp addons
        private static string TEMP_DIR = Global.TEMP + @"dotahost_addon_temp\";

        // Compiles the given list of addons into the given path
        // if generateRandomPath is true, it will append a random folder to the end
        // NOTE: Path NEEDS to lead in a slash
        // ASSUMPTION: This function CAN NOT be run in parrellel! Wait for it to finish before running again!
        public static string compileAddons(KV lobbyKV, string sourcePath, string outputPath, bool generateRandomPath = false)
        {
            // Validate input
            Helpers.log("1");
            if (lobbyKV == null) return null;
            if (outputPath == null) return null;

            // Load lobby
            Helpers.log("2");
            Lobby lobby = new Lobby(lobbyKV);

            // Attempt to grab the addons
            Helpers.log("3");
            Addons addons = lobby.Addons;
            if (addons == null) return null;

            // Grab the install location for addons
            Helpers.log("4");
            string searchPath = sourcePath;

            // Add the random path if needed
            Helpers.log("5");
            if (generateRandomPath)
            {
                outputPath += Helpers.randomString(8) + "\\";
            }

            // Clear the output path if it already exists
            Helpers.log("");
            Helpers.deleteFolder(outputPath, true);

            // Create the folder
            Helpers.log("6");
            Directory.CreateDirectory(outputPath);

            // Will store the name of archies to extract
            Helpers.log("7");
            string zipName;

            // Build list of loaded addons with scripts
            Helpers.log("8");
            List<string> addonScriptList = new List<string>();

            // Build settings KV
            Helpers.log("9");
            GenericKV settings = new GenericKV();
            GenericKV options = new GenericKV();
            settings.setGenericKey("options", options);

            // Add the teams
            Helpers.log("10");
            settings.setGenericKey("teams", lobby.Teams);

            // Compile each addon in
            Helpers.log("11");
            foreach (Addon addon in addons.getAddons())
            {
                // Get ID of addon
                Helpers.log("12");
                string addonID = addon.Id;

                // Store the options
                Helpers.log("13");
                options.setGenericKey(addonID, addon.Options);

                // The name of the archive
                Helpers.log("14");
                zipName = searchPath + addonID + ".zip";

                // Ensure the addon exists
                Helpers.log("15");
                if (!File.Exists(zipName))
                {
                    Helpers.log("WARNING: Could not find " + zipName);
                    continue;
                }

                // Cleanup temp folder
                Helpers.log("16");
                Helpers.deleteFolder(TEMP_DIR, true);

                // Extract the archive
                Helpers.log("17");
                ZipFile.ExtractToDirectory(zipName, TEMP_DIR);

                // Read the manifest file
                Helpers.log("18");
                KV manifest = KV.read(TEMP_DIR + "manifest.kv");

                // Ensure a valid manifest file
                Helpers.log("19");
                if (manifest == null)
                {
                    Helpers.log("Manifest file for " + addonID + " is missing or invalid!");
                    continue;
                }

                // Grab the compile information for the given mod
                Helpers.log("20");
                manifest = manifest.getKV(addonID);
                if (manifest == null)
                {
                    Helpers.log("Manifest file for " + addonID + " doesn't contain any compile information!");
                    continue;
                }

                // See if there is an addon directory
                Helpers.log("21");
                string addonDir = fixAddonDir(manifest.getValue("directory"));

                // Patch vscripts folder
                Helpers.log("22");
                if (Directory.Exists(addonDir + @"scripts\vscripts"))
                {
                    // Attempt to do the move
                    Helpers.log("23");
                    try
                    {
                        // Attempt to mod the loader
                        Helpers.log("24");
                        try
                        {
                            // Attempt to patch the script
                            Helpers.log("25");
                            string data = File.ReadAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua");

                            // Replace the file
                            Helpers.log("26");
                            File.WriteAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua", data + Environment.NewLine + "return {Precache = Precache,Activate = Activate}");
                        }
                        catch
                        {
                            Helpers.log("No addon_game_mode found for " + addonID);
                        }

                        // Rename the directory
                        Helpers.log("27");
                        Directory.Move(addonDir + @"scripts\vscripts", addonDir + @"scripts\vscripts_moving");

                        // Create new vscripts folder
                        Helpers.log("28");
                        Directory.CreateDirectory(addonDir + @"scripts\vscripts");

                        // Move the directory
                        Helpers.log("29");
                        Directory.Move(addonDir + @"scripts\vscripts_moving", addonDir + @"scripts\vscripts\" + addonID);

                        // Add this to our list of script addons
                        Helpers.log("30");
                        addonScriptList.Add('"' + addonID + '"');
                    }
                    catch (Exception e)
                    {
                        Helpers.log(e.Message);
                        Helpers.log("Failed to patch vscripts for " + addonID);
                    }
                }

                // Process the commands
                Helpers.log("31");
                List<string> commands;

                // Copy directory commands
                Helpers.log("32");
                commands = manifest.getValues("copyDirectory");
                if (commands != null)
                {
                    foreach (String command in commands)
                    {
                        // Remove double dots
                        Helpers.log("33");
                        string dir = command.Replace("..", "");

                        // Copy directory in
                        Helpers.log("34");
                        Helpers.directoryCopy(addonDir + dir, outputPath + dir, true);
                    }
                }

                // Copy file commands
                Helpers.log("35");
                commands = manifest.getValues("copyFile");
                if (commands != null)
                {
                    foreach (String command in commands)
                    {
                        // Remove double dots
                        Helpers.log("36");
                        string file = command.Replace("..", "");

                        // Copy the file
                        Helpers.log("37");
                        if (File.Exists(addonDir + file))
                        {
                            File.Copy(addonDir + file, outputPath + file, true);
                        }
                    }
                }

                // Language file merging
                Helpers.log("38");
                commands = manifest.getValues("mergeKV");
                if (commands != null)
                {
                    foreach (String command in commands)
                    {
                        // Remove double dots
                        Helpers.log("39");
                        string file = command.Replace("..", "");

                        // Ensure the file we want to merge exists
                        Helpers.log("40");
                        if (File.Exists(addonDir + file))
                        {
                            // Do we need to actually merge?
                            Helpers.log("41");
                            if (File.Exists(outputPath + file))
                            {
                                // File exists, merge :(
                                Helpers.log("42");
                                KV kv1 = KV.read(outputPath + file);
                                KV kv2 = KV.read(addonDir + file);

                                // Ensure both are valid (if not, keep the original)
                                Helpers.log("43");
                                if (kv1 != null && kv2 != null)
                                {
                                    // Perform the merge
                                    Helpers.log("44");
                                    kv1.merge(kv2);

                                    // Save the new file
                                    Helpers.log("45");
                                    File.WriteAllText(outputPath + file, kv1.toString());
                                }
                                else
                                {
                                    Helpers.log("Failed to merge " + file);
                                }
                            }
                            else
                            {
                                // File doesn't exist, just copy
                                Helpers.log("46");
                                File.Copy(addonDir + file, outputPath + file, true);
                            }

                        }
                    }
                }
            }

            // Install custom server scripts
            Helpers.log("47");
            zipName = searchPath + "serverinit.zip";

            // Ensure the addon exists
            Helpers.log("48");
            if (File.Exists(zipName))
            {
                // Cleanup temp folder
                Helpers.log("49");
                Helpers.deleteFolder(TEMP_DIR, true);

                // Extract the archive
                Helpers.log("50");
                ZipFile.ExtractToDirectory(zipName, TEMP_DIR);

                // Read the manifest file
                Helpers.log("51");
                KV manifest = KV.read(TEMP_DIR + "manifest.kv");

                // Ensure a valid manifest file
                Helpers.log("52");
                if (manifest == null)
                {
                    Helpers.log("Manifest file for serverinit is missing or invalid!");
                }
                else
                {
                    // Grab the compile information for the given mod
                    Helpers.log("53");
                    manifest = manifest.getKV("serverinit");
                    if (manifest == null)
                    {
                        Helpers.log("Manifest file for serverinit doesn't contain any compile information!");
                    }

                    // See if there is an addon directory
                    Helpers.log("54");
                    string addonDir = fixAddonDir(manifest.getValue("directory"));

                    // Copy directory in
                    Helpers.log("55");
                    Helpers.directoryCopy(addonDir, outputPath, true);
                }
            }
            else
            {
                // Oh god, this can't end well!
                Helpers.log("56");
                Helpers.log("WARNING: serverinit.zip not found! No vscripts will load!");
            }

            // Output the settings KV
            Helpers.log("57");
            File.WriteAllText(outputPath + "settings.kv", settings.toString("settings"));

            // Cleanup temp folder
            Helpers.log("58");
            Helpers.deleteFolder(TEMP_DIR, true);

            // Return the path to the compiled addons
            Helpers.log("59");
            return outputPath;
        }

        // Fixes adddon diretory paths
        private static string fixAddonDir(string addonDir)
        {
            // Check what kind of addon dir was specified
            if (addonDir == null)
            {
                // None at all, assume root
                addonDir = TEMP_DIR;
            }
            else if (addonDir == "")
            {
                // Blank, assume root
                addonDir = TEMP_DIR;
            }
            else
            {
                // Something, append it to the path (and removed HAAAAX)
                addonDir = TEMP_DIR + addonDir.Replace("..", "") + @"\";
            }

            return addonDir;
        }
    }
}
