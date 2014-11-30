﻿using DotaHostClientLibrary;
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
        public static string compileAddons(List<Addon> addons, string outputPath, bool generateRandomPath=false, string serverSettings=null)
        {
            // Validate input
            if (addons == null) return null;
            if (outputPath == null) return null;

            // Grab the install location for addons
            string searchPath = AddonDownloader.getAddonInstallLocation();

            // Add the random path if needed
            if (generateRandomPath)
            {
                outputPath += Helpers.randomString(8) + "\\";
            }

            // Clear the output path if it already exists
            Helpers.deleteFolder(outputPath, true);

            // Create the folder
            Directory.CreateDirectory(outputPath);

            // Will store the name of archies to extract
            string zipName;

            // Build list of loaded addons with scripts
            List<string> addonScriptList = new List<string>();

            // Compile each addon in
            foreach(Addon addon in addons)
            {
                // The name of the archive
                zipName = searchPath + addon.Id + ".zip";

                // Ensure the addon exists
                if (!File.Exists(zipName))
                {
                    Helpers.log("WARNING: Could not find " + zipName);
                    continue;
                }

                // Cleanup temp folder
                Helpers.deleteFolder(TEMP_DIR, true);

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
                string addonDir = fixAddonDir(manifest.getValue("directory"));

                // Patch vscripts folder
                if (Directory.Exists(addonDir + @"scripts\vscripts"))
                {
                    // Attempt to do the move
                    try
                    {
                        // Attempt to mod the loader
                        try
                        {
                            // Attempt to patch the script
                            string data = File.ReadAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua");

                            // Replace the file
                            File.WriteAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua", data + Environment.NewLine + "return {Precache = Precache,Activate = Activate}");
                        }
                        catch
                        {
                            Helpers.log("No addon_game_mode found for " + addon.Id);
                        }

                        // Rename the directory
                        Directory.Move(addonDir + @"scripts\vscripts", addonDir + @"scripts\vscripts_moving");

                        // Create new vscripts folder
                        Directory.CreateDirectory(addonDir + @"scripts\vscripts");

                        // Move the directory
                        Directory.Move(addonDir + @"scripts\vscripts_moving", addonDir + @"scripts\vscripts\" + addon.Id);

                        // Add this to our list of script addons
                        addonScriptList.Add('"' + addon.Id + '"');
                    }
                    catch (Exception e)
                    {
                        Helpers.log(e.Message);
                        Helpers.log("Failed to patch vscripts for " + addon.Id);
                    }
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

                                    // Save the new file
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
                                File.Copy(addonDir + file, outputPath + file, true);
                            }
                            
                        }
                    }
                }
            }

            // Install custom server scripts
            zipName = searchPath + "serverinit.zip";

            // Ensure the addon exists
            if (File.Exists(zipName))
            {
                // Cleanup temp folder
                Helpers.deleteFolder(TEMP_DIR, true);

                // Extract the archive
                ZipFile.ExtractToDirectory(zipName, TEMP_DIR);

                // Read the manifest file
                KV manifest = KV.read(TEMP_DIR + "manifest.kv");

                // Ensure a valid manifest file
                if (manifest == null)
                {
                    Helpers.log("Manifest file for serverinit is missing or invalid!");
                }
                else
                {
                    // Grab the compile information for the given mod
                    manifest = manifest.getKV("serverinit");
                    if (manifest == null)
                    {
                        Helpers.log("Manifest file for serverinit doesn't contain any compile information!");
                    }

                    // See if there is an addon directory
                    string addonDir = fixAddonDir(manifest.getValue("directory"));

                    // Patch addon_init_gamemode
                    try
                    {
                        // Read in the data
                        string data = File.ReadAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua");

                        // Replace in the addon list
                        data = data.Replace("--[[$addons]]", string.Join(",", addonScriptList.ToArray()));

                        // Replace in the server settings
                        if (serverSettings != null)
                        {
                            data = data.Replace("--[[$serverSettings]]nil", serverSettings);
                        }

                        // Replace the file
                        File.WriteAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua", data);
                    }
                    catch
                    {
                        Helpers.log("WARNING: Failed to patch serverinit::addon_game_mode.lua");
                    }

                    // Copy directory in
                    Helpers.directoryCopy(addonDir, outputPath, true);
                }
            }
            else
            {
                // Oh god, this can't end well!
                Helpers.log("WARNING: serverinit.zip not found! No vscripts will load!");
            }

            // Cleanup temp folder
            Helpers.deleteFolder(TEMP_DIR, true);

            // Return the path to the compiled addons
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
