using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace FarmTypeManager
{
    public partial class ModEntry : Mod
    {
        /// <summary>Methods used repeatedly by other sections of this mod, e.g. to locate tiles.</summary>
        private static partial class Utility
        {
            /// <summary>Validates a single instance of farm data, correcting obsolete/invalid settings automatically.</summary>
            /// <param name="config">The contents of a single config file to be validated.</param>
            /// <param name="pack">The content pack associated with this config data; null if the file was from this mod's own data folder.</param>
            public static void ValidateFarmData(FarmConfig config, IContentPack pack)
            {
                if (pack != null)
                {
                    Monitor.VerboseLog($"Validating data from content pack: {pack.Manifest.Name}");
                }
                else
                {
                    Monitor.VerboseLog("Validating data from FarmTypeManager/data");
                }

                List<SpawnArea[]> allAreas = new List<SpawnArea[]>(); //a unified list of each "Areas" array in this config file
                //add each group of spawn areas to the list (unless its config section is null)
                if (config.Forage_Spawn_Settings != null)
                {
                    allAreas.Add(config.Forage_Spawn_Settings.Areas);
                }
                if (config.Large_Object_Spawn_Settings != null)
                {
                    allAreas.Add(config.Large_Object_Spawn_Settings.Areas);
                }
                if (config.Ore_Spawn_Settings != null)
                {
                    allAreas.Add(config.Ore_Spawn_Settings.Areas);
                }
                if (config.Monster_Spawn_Settings != null)
                {
                    allAreas.Add(config.Monster_Spawn_Settings.Areas);
                }

                Monitor.VerboseLog("Checking for duplicate UniqueAreaIDs...");
                HashSet<string> IDs = new HashSet<string>(); //a record of all unique IDs encountered during this process

                //erase any duplicate IDs and record the others in the "IDs" hashset
                foreach (SpawnArea[] areas in allAreas) //for each "Areas" array in allAreas
                {
                    foreach (SpawnArea area in areas) //for each area in the current array
                    {
                        if (String.IsNullOrWhiteSpace(area.UniqueAreaID) || area.UniqueAreaID.ToLower() == "null") //if the area ID is null, blank, or the string "null" (to account for user confusion)
                        {
                            continue; //this name will be replaced later, so ignore it for now
                        }

                        if (IDs.Contains(area.UniqueAreaID)) //if this area's ID was already encountered
                        {
                            Monitor.VerboseLog($"Duplicate UniqueAreaID found: \"{area.UniqueAreaID}\" will be renamed.");
                            if (pack != null) //if this config is from a content pack
                            {
                                Monitor.VerboseLog($"Content pack: {pack.Manifest.Name}");
                                Monitor.VerboseLog($"If this happened after updating another mod, it might cause certain conditions (such as one-time-only spawns) to reset in that area.");
                            }

                            area.UniqueAreaID = ""; //erase this area's ID, marking it for replacement
                        }
                        else //if this ID is unique so far
                        {
                            IDs.Add(area.UniqueAreaID); //add the area to the ID set
                        }
                    }
                }

                Monitor.VerboseLog("Assigning new UniqueAreaIDs to any blanks or duplicates...");
                string newName; //temp storage for a new ID while it's created/tested
                int newNumber; //temp storage for the numeric part of a new ID

                //create new IDs for any empty ones
                foreach (SpawnArea[] areas in allAreas) //for each "Areas" array in allAreas
                {
                    foreach (SpawnArea area in areas) //for each area in the current array
                    {
                        if (String.IsNullOrWhiteSpace(area.UniqueAreaID) || area.UniqueAreaID.ToLower() == "null") //if the area ID is null, blank, or the string "null" (to account for user confusion)
                        {
                            //create a new name, based on which type of area this is
                            newName = area.MapName;
                            if (area is ForageSpawnArea) { newName += " forage area "; }
                            else if (area is LargeObjectSpawnArea) { newName += " large object area "; }
                            else if (area is OreSpawnArea) { newName += " ore area "; }
                            else if (area is MonsterSpawnArea) { newName += " monster area "; }
                            else { newName += " area "; }

                            newNumber = 1;

                            while (IDs.Contains(newName + newNumber)) //if this ID wouldn't be unique
                            {
                                newNumber++; //increment and try again
                            }

                            area.UniqueAreaID = newName + newNumber; //apply the new unique ID
                            Monitor.VerboseLog($"New UniqueAreaID assigned: {area.UniqueAreaID}");
                        }

                        IDs.Add(area.UniqueAreaID); //the ID is finalized, so add it to the set of encountered IDs
                    }
                }

                //confirm that any paired min/max settings are in the correct order
                foreach (SpawnArea[] areas in allAreas) //for each "Areas" array in allAreas
                {
                    foreach (SpawnArea area in areas) //for each area in the current array
                    {

                        if (area.MinimumSpawnsPerDay > area.MaximumSpawnsPerDay) //if the min and max are in the wrong order
                        {
                            //swap min and max
                            int temp = area.MinimumSpawnsPerDay;
                            area.MinimumSpawnsPerDay = area.MaximumSpawnsPerDay;
                            area.MaximumSpawnsPerDay = temp;
                            Monitor.VerboseLog($"Swapping minimum and maximum spawns per day for this area: {area.UniqueAreaID}");
                        }

                        if (area.SpawnTiming.StartTime > area.SpawnTiming.EndTime) //if start and end are in the wrong order
                        {
                            //swap start and end
                            StardewTime temp = area.SpawnTiming.StartTime;
                            area.SpawnTiming.StartTime = area.SpawnTiming.EndTime;
                            area.SpawnTiming.EndTime = temp;
                            Monitor.VerboseLog($"Swapping StartTime and EndTime in the SpawnTiming settings for this area: {area.UniqueAreaID}");
                        }
                    }
                }

                //detect spawn areas with no included spawn tiles
                foreach (SpawnArea[] areas in allAreas) //for each "Areas" array in allAreas
                {
                    foreach (SpawnArea area in areas) //for each area in the current array
                    {
                        if (area.IncludeCoordinates == null || area.IncludeCoordinates.Length < 1) //if this area doesn't spawn on any coordinates
                        {
                            if (area.IncludeTerrainTypes == null || area.IncludeTerrainTypes.Length < 1) //if this area doesn't spawn on any terrain types
                            {
                                if ((area is LargeObjectSpawnArea lobjArea && lobjArea.FindExistingObjectLocations) == false) //if this area is NOT using existing large object locations
                                {
                                    Monitor.Log($"This spawn area's IncludeCoordinates and IncludeTerrainTypes are both empty, which means it has no valid spawn tiles.", LogLevel.Debug);
                                    Monitor.Log($"Area: {area.UniqueAreaID}", LogLevel.Debug);

                                    if (pack != null)
                                        Monitor.Log($"Content pack: {pack.Manifest.Name}", LogLevel.Debug);
                                    else
                                        Monitor.Log($"File: data/{Constants.SaveFolderName}.json", LogLevel.Debug);
                                }
                            }
                        }
                    }
                }

                //detect invalid sound names and warn the user
                //NOTE: this will not remove the invalid name, in case the problem is related to custom sound loading
                foreach (SpawnArea[] areas in allAreas) //for each "Areas" array in allAreas
                {
                    foreach (SpawnArea area in areas) //for each area in the current array
                    {
                        if (area.SpawnTiming.SpawnSound != null && area.SpawnTiming.SpawnSound.Trim() != "") //if a SpawnSound has been provided for this area
                        {
                            try
                            {
                                Game1.soundBank.GetCue(area.SpawnTiming.SpawnSound); //test whether this sound exists by retrieving it from the game's soundbank
                            }
                            catch //if an exception is thrown while retrieving the sound
                            {
                                Monitor.Log($"This spawn sound could not be found: {area.SpawnTiming.SpawnSound}", LogLevel.Debug);
                                Monitor.Log($"Please make sure the sound's name is spelled and capitalized correctly. Sound names are case-sensitive.", LogLevel.Debug);
                                Monitor.Log($"Area: {area.UniqueAreaID}", LogLevel.Debug);

                                if (pack != null)
                                    Monitor.Log($"Content pack: {pack.Manifest.Name}", LogLevel.Debug);
                                else
                                    Monitor.Log($"File: data/{Constants.SaveFolderName}.json", LogLevel.Debug);
                            }
                        }
                    }
                }

                if (pack != null)
                    Monitor.VerboseLog($"Validation complete for content pack: {pack.Manifest.Name}");
                else
                    Monitor.VerboseLog($"Validation complete for file: data/{Constants.SaveFolderName}.json");

                return;
            }
        }
    }
}