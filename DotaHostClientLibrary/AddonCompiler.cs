using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace DotaHostClientLibrary
{
    public static class AddonCompiler
    {
        // Path to store temp addons
        private static readonly string TempDir = Global.Temp + @"dotahost_addon_temp\";

        // Compiles the given list of addons into the given path
        // if generateRandomPath is true, it will append a random folder to the end
        // NOTE: Path NEEDS to lead in a slash
        // ASSUMPTION: This function CAN NOT be run in parrellel! Wait for it to finish before running again!
        public static string CompileAddons(Kv lobbyKv, string sourcePath, string outputPath, bool generateRandomPath = false)
        {
            // Validate input
            Helpers.Log("1");
            if (lobbyKv == null) return null;
            if (outputPath == null) return null;

            // Load lobby
            Helpers.Log("2");
            var lobby = new Lobby(lobbyKv);

            // Attempt to grab the addons
            Helpers.Log("3");
            var addons = lobby.Addons;
            if (addons == null) return null;

            // Grab the install location for addons
            Helpers.Log("4");
            var searchPath = sourcePath;

            // Add the random path if needed
            Helpers.Log("5");
            if (generateRandomPath)
            {
                outputPath += Helpers.RandomString(8) + "\\";
            }

            // Clear the output path if it already exists
            Helpers.Log("");
            Helpers.DeleteFolder(outputPath, true);

            // Create the folder
            Helpers.Log("6");
            Directory.CreateDirectory(outputPath);

            // Will store the name of archies to extract
            Helpers.Log("7");
            string zipName;

            // Build list of loaded addons with scripts
            Helpers.Log("8");
            var addonScriptList = new List<string>();

            // Build settings KV
            Helpers.Log("9");
            var settings = new GenericKv();
            var options = new GenericKv();
            settings.SetGenericKey("options", options);

            // Add the teams
            Helpers.Log("10");
            settings.SetGenericKey("teams", lobby.Teams);

            // Compile each addon in
            Helpers.Log("11");
            foreach (var addon in addons.GetAddons())
            {
                // Get ID of addon
                Helpers.Log("12");
                var addonId = addon.Id;

                // Store the options
                Helpers.Log("13");
                options.SetGenericKey(addonId, addon.Options);

                // The name of the archive
                Helpers.Log("14");
                zipName = searchPath + addonId + ".zip";

                // Ensure the addon exists
                Helpers.Log("15");
                if (!File.Exists(zipName))
                {
                    Helpers.Log("WARNING: Could not find " + zipName);
                    continue;
                }

                // Cleanup temp folder
                Helpers.Log("16");
                Helpers.DeleteFolder(TempDir, true);

                // Extract the archive
                Helpers.Log("17");
                ZipFile.ExtractToDirectory(zipName, TempDir);

                // Read the manifest file
                Helpers.Log("18");
                Kv manifest = Kv.Read(TempDir + "manifest.kv");

                // Ensure a valid manifest file
                Helpers.Log("19");
                if (manifest == null)
                {
                    Helpers.Log("Manifest file for " + addonId + " is missing or invalid!");
                    continue;
                }

                // Grab the compile information for the given mod
                Helpers.Log("20");
                manifest = manifest.GetKv(addonId);
                if (manifest == null)
                {
                    Helpers.Log("Manifest file for " + addonId + " doesn't contain any compile information!");
                    continue;
                }

                // See if there is an addon directory
                Helpers.Log("21");
                string addonDir = FixAddonDir(manifest.GetValue("directory"));

                // Patch vscripts folder
                Helpers.Log("22");
                if (Directory.Exists(addonDir + @"scripts\vscripts"))
                {
                    // Attempt to do the move
                    Helpers.Log("23");
                    try
                    {
                        // Attempt to mod the loader
                        Helpers.Log("24");
                        try
                        {
                            // Attempt to patch the script
                            Helpers.Log("25");
                            string data = File.ReadAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua");

                            // Replace the file
                            Helpers.Log("26");
                            File.WriteAllText(addonDir + @"scripts\vscripts\addon_game_mode.lua", data + Environment.NewLine + "return {Precache = Precache,Activate = Activate}");
                        }
                        catch
                        {
                            Helpers.Log("No addon_game_mode found for " + addonId);
                        }

                        // Rename the directory
                        Helpers.Log("27");
                        Directory.Move(addonDir + @"scripts\vscripts", addonDir + @"scripts\vscripts_moving");

                        // Create new vscripts folder
                        Helpers.Log("28");
                        Directory.CreateDirectory(addonDir + @"scripts\vscripts");

                        // Move the directory
                        Helpers.Log("29");
                        Directory.Move(addonDir + @"scripts\vscripts_moving", addonDir + @"scripts\vscripts\" + addonId);

                        // Add this to our list of script addons
                        Helpers.Log("30");
                        addonScriptList.Add('"' + addonId + '"');
                    }
                    catch (Exception e)
                    {
                        Helpers.Log(e.Message);
                        Helpers.Log("Failed to patch vscripts for " + addonId);
                    }
                }

                // Process the commands
                Helpers.Log("31");

                // Copy directory commands
                Helpers.Log("32");
                var commands = manifest.GetValues("copyDirectory");
                if (commands != null)
                {
                    foreach (var command in commands)
                    {
                        // Remove double dots
                        Helpers.Log("33");
                        string dir = command.Replace("..", "");

                        // Copy directory in
                        Helpers.Log("34");
                        Helpers.DirectoryCopy(addonDir + dir, outputPath + dir, true);
                    }
                }

                // Copy file commands
                Helpers.Log("35");
                commands = manifest.GetValues("copyFile");
                if (commands != null)
                {
                    foreach (var command in commands)
                    {
                        // Remove double dots
                        Helpers.Log("36");
                        var file = command.Replace("..", "");

                        // Copy the file
                        Helpers.Log("37");
                        if (File.Exists(addonDir + file))
                        {
                            File.Copy(addonDir + file, outputPath + file, true);
                        }
                    }
                }

                // Language file merging
                Helpers.Log("38");
                commands = manifest.GetValues("mergeKV");
                if (commands == null) continue;
                foreach (var command in commands)
                {
                    // Remove double dots
                    Helpers.Log("39");
                    var file = command.Replace("..", "");

                    // Ensure the file we want to merge exists
                    Helpers.Log("40");
                    if (!File.Exists(addonDir + file)) continue;
                    // Do we need to actually merge?
                    Helpers.Log("41");
                    if (File.Exists(outputPath + file))
                    {
                        // File exists, merge :(
                        Helpers.Log("42");
                        var kv1 = Kv.Read(outputPath + file);
                        var kv2 = Kv.Read(addonDir + file);

                        // Ensure both are valid (if not, keep the original)
                        Helpers.Log("43");
                        if (kv1 != null && kv2 != null)
                        {
                            // Perform the merge
                            Helpers.Log("44");
                            kv1.Merge(kv2);

                            // Save the new file
                            Helpers.Log("45");
                            File.WriteAllText(outputPath + file, kv1.ToString());
                        }
                        else
                        {
                            Helpers.Log("Failed to merge " + file);
                        }
                    }
                    else
                    {
                        // File doesn't exist, just copy
                        Helpers.Log("46");
                        File.Copy(addonDir + file, outputPath + file, true);
                    }
                }
            }

            // Install custom server scripts
            Helpers.Log("47");
            zipName = searchPath + "serverinit.zip";

            // Ensure the addon exists
            Helpers.Log("48");
            if (File.Exists(zipName))
            {
                // Cleanup temp folder
                Helpers.Log("49");
                Helpers.DeleteFolder(TempDir, true);

                // Extract the archive
                Helpers.Log("50");
                ZipFile.ExtractToDirectory(zipName, TempDir);

                // Read the manifest file
                Helpers.Log("51");
                var manifest = Kv.Read(TempDir + "manifest.kv");

                // Ensure a valid manifest file
                Helpers.Log("52");
                if (manifest == null)
                {
                    Helpers.Log("Manifest file for serverinit is missing or invalid!");
                }
                else
                {
                    // Grab the compile information for the given mod
                    Helpers.Log("53");
                    manifest = manifest.GetKv("serverinit");
                    if (manifest == null)
                    {
                        Helpers.Log("Manifest file for serverinit doesn't contain any compile information!");
                    }

                    // See if there is an addon directory
                    Helpers.Log("54");
                    if (manifest != null)
                    {
                        var addonDir = FixAddonDir(manifest.GetValue("directory"));

                        // Copy directory in
                        Helpers.Log("55");
                        Helpers.DirectoryCopy(addonDir, outputPath, true);
                    }
                }
            }
            else
            {
                // Oh god, this can't end well!
                Helpers.Log("56");
                Helpers.Log("WARNING: serverinit.zip not found! No vscripts will load!");
            }

            // Output the settings KV
            Helpers.Log("57");
            File.WriteAllText(outputPath + "settings.kv", settings.ToString("settings"));

            // Cleanup temp folder
            Helpers.Log("58");
            Helpers.DeleteFolder(TempDir, true);

            // Return the path to the compiled addons
            Helpers.Log("59");
            return outputPath;
        }

        // Fixes adddon diretory paths
        private static string FixAddonDir(string addonDir)
        {
            // Check what kind of addon dir was specified
            switch (addonDir)
            {
                case null:
                    // None at all, assume root
                    addonDir = TempDir;
                    break;
                case "":
                    // Blank, assume root
                    addonDir = TempDir;
                    break;
                default:
                    addonDir = TempDir + addonDir.Replace("..", "") + @"\";
                    break;
            }

            return addonDir;
        }
    }
}
