using System;
using System.IO;
using IOTools;
using ProtoBuf;
using BL3Tools.GVAS;
using BL3Tools.Decryption;
using OakSave;
using System.Linq;
using BL3Tools.GameData.Items;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace BL3Tools
{

    public static class BL3Tools
    {
        public class BL3Exceptions
        {
            public class InvalidSaveException : Exception
            {
                public InvalidSaveException() : base("Invalid TTW Save") { }
                public InvalidSaveException(string saveGameType) : base(String.Format("Invalid TTW Save Game Type: {0}", saveGameType)) { }
                public InvalidSaveException(Platform platform) : base(String.Format("Incorrectly decrypted save game using the {0} platform; Are you sure you're using the right one?", platform)) { }
            }


            public class SerialParseException : Exception
            {
                public bool knowCause = false;

                public SerialParseException() : base("Invalid TTW Serial...") { }
                public SerialParseException(string serial) : base(String.Format("Invalid Serial: {0}", serial)) { }
                public SerialParseException(string serial, int version) : base(String.Format("Invalid Serial: \"{0}\"; Version: {1}", serial, version)) { knowCause = true; }
                public SerialParseException(string serial, int version, uint originalChecksum, uint calculatedChecksum) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Checksum Difference: {2} vs {3}", serial, version, originalChecksum, calculatedChecksum)) { knowCause = true; }

                public SerialParseException(string serial, int version, int databaseVersion) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Database Version: {2}", serial, version, databaseVersion)) { knowCause = true; }

                public SerialParseException(string serial, int version, int databaseVersion, string oddity) : base(String.Format("Invalid Serial: \"{0}\"; Serial Version: {1}; Database Version: {2}; Error: {3}", serial, version, databaseVersion, oddity)) { knowCause = true; }

            }
        }

        /// <summary>
        /// This function writes a <c>UE3Save</c> instance to the drive, deserializes it to the respective classes of <c>BL3Profile</c> or <c>BL3Save</c>
        /// </summary>
        /// <param name="filePath">A file path for which to load the file from</param>
        /// <param name="bBackup">Whether or not to backup the save on reading (Default: False)</param>
        /// <returns>An instance of the respective type, all subclassed by a <c>UE3Save</c> instance</returns>
        public static UE3Save LoadFileFromDisk(string filePath, Platform platform = Platform.PC, bool bBackup = false)
        {
            if (platform == Platform.JSON)
            {
                
                try
                {
                    UE3Save saveGame = null;
                    Console.WriteLine("Reading new file: \"{0}\"", filePath);
                    string[] saveData = File.ReadAllLines(filePath);

                    if (filePath.Contains("profile"))
                    {
                        throw new BL3Exceptions.InvalidSaveException("Not supported PS4 Save Wizard profile.");
                    }
                    else
                    {
                        for (int x = 0; x < saveData.Length; x++)
                        {
                            Match match = Regex.Match(saveData[x], "\"([a-z_]+)\"");

                            if (match.Success)
                            {
                                string[] values = match.Value.Split('_');

                                for (int i = 0; i < values.Length; i++)
                                {
                                    values[i] = char.ToUpper(values[i][i == 0 ? 1 : 0]) +
                                                values[i].Substring(i == 0 ? 2 : 1);
                                }

                                saveData[x] = saveData[x].Replace(match.Value, $" \"{String.Join("", values)}");
                            }
                            
                            if (saveData[x].Contains("NicknameMappings"))
                            {
                                // change Save Wizard format from {petNickname: value} to
                                // [{ key: petNickname, value: name}],

                                if (!saveData[x].Contains("["))
                                {
                                   
                                        // JSON format matches Save Wizard output

                                        saveData[x] = "\"NicknameMappings\": [{";

                                        x++;

                                        var nickTemp = saveData[x].Split(':');

                                        var nickKey = nickTemp[0];
                                        var nickValue = nickTemp[1];

                                        if (nickKey.Contains("petNicknameLich") ||
                                            nickKey.Contains("petNicknameMushroom") ||
                                            nickKey.Contains("petNicknameWyvern"))
                                        {
                                            saveData[x] = " \"key\": " + nickKey + ",";

                                            x++;

                                            saveData[x] = " \"value\": " + nickValue + "}],";
                                        }
                                        else
                                        {
                                            x--;

                                        //saveData[x] = "\"NicknameMappings\": [{\"key\": \"\",\"value\": \"\"}],";
                                        saveData[x] = "\"NicknameMappings\": [],";

                                        /*
                                        saveData[x] = " \"key\": \"\",";

                                            x++;

                                            saveData[x] = " \"value\": \"\"}],";
                                        */
                                    }
                                }
                                // JSON format does not match Save Wizard output
                            }
                            
                            if (saveData[x].Contains("GameStatsData"))
                            {
                                saveData[x] = saveData[x].Replace("GameStatsData", "GameStatsDatas");
                            }

                            if (saveData[x].Contains("InventoryCategoryList"))
                            {
                                saveData[x] = saveData[x].Replace("InventoryCategoryList", "InventoryCategoryLists");
                            }

                            if (saveData[x].Contains("EquippedInventoryList"))
                            {
                                saveData[x] = saveData[x].Replace("EquippedInventoryList", "EquippedInventoryLists");
                            }

                            if (saveData[x].Contains("ActiveWeaponList"))
                            {
                                saveData[x] = saveData[x].Replace("ActiveWeaponList", "ActiveWeaponLists");
                            }

                            if (saveData[x].Contains("MissionPlaythroughsData"))
                            {
                                saveData[x] = saveData[x].Replace("MissionPlaythroughsData",
                                    "MissionPlaythroughsDatas");
                            }

                            if (saveData[x].Contains("LastActiveTravelStationForPlaythrough"))
                            {
                                saveData[x] = saveData[x].Replace("LastActiveTravelStationForPlaythrough",
                                    "LastActiveTravelStationForPlaythroughs");
                            }

                            if (saveData[x].Contains("GameStateSaveDataForPlaythrough"))
                            {
                                saveData[x] = saveData[x].Replace("GameStateSaveDataForPlaythrough",
                                    "GameStateSaveDataForPlaythroughs");
                            }

                            if (saveData[x].Contains("ActiveTravelStationsForPlaythrough"))
                            {
                                saveData[x] = saveData[x].Replace("ActiveTravelStationsForPlaythrough",
                                    "ActiveTravelStationsForPlaythroughs");
                            }

                            if (saveData[x].Contains("ChallengeData"))
                            {
                                saveData[x] = saveData[x].Replace("ChallengeData", "ChallengeDatas");
                            }

                            if (saveData[x].Contains("SduList"))
                            {
                                saveData[x] = saveData[x].Replace("SduList", "SduLists");
                            }

                            if (saveData[x].Contains("LastOverworldTravelStationForPlaythrough"))
                            {
                                saveData[x] = saveData[x].Replace("LastOverworldTravelStationForPlaythrough",
                                    "LastOverworldTravelStationForPlaythroughs");
                            }

                            if (saveData[x].Contains("CustomizationLinkData"))
                            {
                                saveData[x] = saveData[x].Replace("CustomizationLinkData", "CustomizationLinkDatas");
                            }

                            if (saveData[x].Contains("MissionList"))
                            {
                                saveData[x] = saveData[x].Replace("MissionList", "MissionLists");
                            }

                            if (saveData[x].Contains("MS_NotStarted"))
                            {
                                saveData[x] = saveData[x].Replace("MS_NotStarted", "0");
                            }

                            if (saveData[x].Contains("MS_Active"))
                            {
                                saveData[x] = saveData[x].Replace("MS_Active", "1");
                            }

                            if (saveData[x].Contains("MS_Complete"))
                            {
                                saveData[x] = saveData[x].Replace("MS_Complete", "2");
                            }

                            if (saveData[x].Contains("MS_Failed"))
                            {
                                saveData[x] = saveData[x].Replace("MS_Failed", "3");
                            }

                            if (saveData[x].Contains("MS_Unknown"))
                            {
                                saveData[x] = saveData[x].Replace("MS_Unknown", "4");
                            }

                            if (saveData[x].Contains("ObjectivesProgress"))
                            {
                                saveData[x] = saveData[x].Replace("ObjectivesProgress", "ObjectivesProgresses");
                            }

                            if (saveData[x].Contains("DiscoveredLevelInfo"))
                            {
                                saveData[x] = saveData[x].Replace("DiscoveredLevelInfo", "DiscoveredLevelInfoes");
                            }

                            if (saveData[x].Contains("LevelData"))
                            {
                                saveData[x] = saveData[x].Replace("LevelData", "LevelDatas");
                            }

                            if (saveData[x].Contains("PlanetCycleInfo"))
                            {
                                saveData[x] = saveData[x].Replace("PlanetCycleInfo", "PlanetCycleInfoes");
                            }
                        }

                        Character character = JsonConvert.DeserializeObject<Character>(String.Join("", saveData));

                        // original saveGame object with invalid header information
                        // which is fine if user sticks to JSON export
                        saveGame = new BL3Save(new GVASSave(-1, -1, -1, -1, -1, 0, null, -1, -1, new Dictionary<byte[], int>(), "BPSaveGame_Default_C"), character);
                        
                        // a very bad solution to faking buffer data saving as PC save
                        // more than likely breaks something
                        /*
                        Dictionary<byte[], int> dictbuf = new Dictionary<byte[], int>();
                        
                        for (int i = 0; i < 236; i++)
                        {
                            dictbuf.Add(new byte[] {0x0}, 0);
                        }

                        saveGame = new BL3Save(new GVASSave(2, 516, 4, 20, 3, 2150344073, "OAK-PATCHWIN641-118\0O", 3, 59, dictbuf, "BPSaveGame_Default_C"), character);
                        */
                        (saveGame as BL3Save).Platform = platform;
                        saveGame.filePath = filePath;
                    }

                    return saveGame;
                }
                catch (ProtoBuf.ProtoException ex)
                {
                    throw ex;
                }
            }
            else
            {
                UE3Save saveGame = null;
                Console.WriteLine("Reading new file: \"{0}\"", filePath);
                FileStream fs = new FileStream(filePath, FileMode.Open);

                IOWrapper io = new IOWrapper(fs, Endian.Little, 0x0000000);
                try
                {
                    if (bBackup)
                    {
                        // Gonna use this byte array for backing up the save file
                        byte[] originalBytes = io.ReadAll();
                        io.Seek(0);

                        // Backup the file
                        File.WriteAllBytes(filePath + ".bak", originalBytes);
                    }

                    GVASSave saveData = Helpers.ReadGVASSave(io);

                    // Throw an exception if the save is null somehow
                    if (saveData == null)
                    {
                        throw new BL3Exceptions.InvalidSaveException();
                    }

                    // Read in the save data itself now
                    string saveGameType = saveData.sgType;
                    int remainingData = io.ReadInt32();
                    Console.WriteLine("Length of data: {0}", remainingData);
                    byte[] buffer = io.ReadBytes(remainingData);

                    switch (saveGameType)
                    {
                        // Decrypt a profile
                        case "OakProfile":
                            ProfileBogoCrypt.Decrypt(buffer, 0, remainingData, platform);
                            saveGame = new BL3Profile(saveData, Serializer.Deserialize<Profile>(new MemoryStream(buffer)));
                            (saveGame as BL3Profile).Platform = platform;
                            break;
                        // Decrypt a save game
                        case "BPSaveGame_Default_C":
                            SaveBogoCrypt.Decrypt(buffer, 0, remainingData, platform);
                            saveGame = new BL3Save(saveData, Serializer.Deserialize<Character>(new MemoryStream(buffer)));
                            (saveGame as BL3Save).Platform = platform;
                            break;
                        default:
                            throw new BL3Exceptions.InvalidSaveException(saveGameType);
                    }
                }
                catch (ProtoBuf.ProtoException ex)
                {
                    // Typically this exception means that the user didn't properly give in the platform for their save
                    if (ex.Message.StartsWith("Invalid wire-type (7);"))
                    {
                        throw new BL3Exceptions.InvalidSaveException(platform);
                    }

                    // Raise all other exceptions
                    throw ex;
                }
                finally
                {
                    // Close the buffer
                    io.Close();
                }
                saveGame.filePath = filePath;
                return saveGame;
            }
        }

        /// <summary>
        /// Writes a <c>UE3Save</c> instance to disk, serializing it to the respective protobuf type.
        /// </summary>
        /// <param name="saveGame">An instance of a UE3Save for which to write out</param>
        /// <param name="bBackup">Whether or not to backup on writing (Default: True)</param>
        /// <returns>Whether or not the file writing succeeded</returns>
        public static bool WriteFileToDisk(UE3Save saveGame, bool bBackup = true)
        {
            return WriteFileToDisk(saveGame.filePath, saveGame, bBackup);
        }

        /// <summary>
        /// Writes a <c>UE3Save</c> instance to disk, serializing it to the respective protobuf type.
        /// </summary>
        /// <param name="filePath">Filepath for which to write the <paramref name="saveGame"/> out to</param>
        /// <param name="saveGame">An instance of a UE3Save for which to write out</param>
        /// <param name="bBackup">Whether or not to backup on writing (Default: True)</param>
        /// <returns>Whether or not the file writing succeeded</returns>

        public static bool WriteFileToDisk(string filePath, UE3Save saveGame, bool bBackup = true)
        {
            Console.WriteLine("Writing file to disk...");
            FileStream fs = new FileStream(filePath, FileMode.Create);
            IOWrapper io = new IOWrapper(fs, Endian.Little, 0x0000000);
            try
            {
                bool isJson = false;

                Helpers.WriteGVASSave(io, saveGame.GVASData);
                byte[] result;

                Console.WriteLine("Writing profile of type: {0}", saveGame.GVASData.sgType);

                using (var stream = new MemoryStream())
                {
                    switch (saveGame.GVASData.sgType)
                    {
                        case "OakProfile":
                            // This is probably a little bit unsafe and costly but *ehh*?
                            BL3Profile vx = (BL3Profile)saveGame;

                            vx.Profile.BankInventoryLists.Clear();
                            vx.Profile.BankInventoryLists.AddRange(vx.BankItems.Select(x => x.InventoryKey == null ? x.OriginalData : new OakInventoryItemSaveGameData()
                            {
                                DevelopmentSaveData = null,
                                Flags = 0x00,
                                ItemSerialNumber = x.EncryptSerialToBytes(),
                                PickupOrderIndex = -1
                            }));

                            vx.Profile.LostLootInventoryLists.Clear();
                            vx.Profile.LostLootInventoryLists.AddRange(vx.LostLootItems.Select(x => x.InventoryKey == null ? x.OriginalData.ItemSerialNumber : x.EncryptSerialToBytes()));

                            Serializer.Serialize(stream, vx.Profile);
                            result = stream.ToArray();
                            ProfileBogoCrypt.Encrypt(result, 0, result.Length, vx.Platform);
                            break;
                        case "BPSaveGame_Default_C":
                            BL3Save save = (BL3Save)saveGame;

                            if (save.Platform == Platform.JSON)
                            {
                                io.Close();
                                isJson = true;

                                foreach (WonderlandsSerial serial in save.InventoryItems)
                                {
                                    var protobufItem = save.Character.InventoryItems.FirstOrDefault(x => ReferenceEquals(x, serial.OriginalData));
                                    if (protobufItem == default)
                                    {
                                        throw new BL3Exceptions.SerialParseException(serial.EncryptSerial(), serial.SerialVersion, serial.SerialDatabaseVersion);
                                    }
                                    protobufItem.ItemSerialNumber = serial.EncryptSerialToBytes();
                                }

                                string[] saveData = (JsonConvert.SerializeObject(save.Character, Formatting.Indented)).Split('\n');

                                for (int x = 0; x < saveData.Length; x++)
                                {
                                    Match match = Regex.Match(saveData[x], "\"([aA-zZ_]+)\":");

                                    if (match.Success)
                                    {
                                        string[] split = Regex.Split(match.Value, @"(?<!^)(?=[A-Z])");

                                        for (int i = 1; i < split.Length; i++)
                                        {
                                            split[i] = split[i].ToLower();

                                            if (i != split.Length - 1)
                                            {
                                                split[i] += "_";
                                            }
                                        }

                                        saveData[x] = saveData[x].Replace(match.Value, $"{String.Join("", split)}");
                                    }

                                    if (saveData[x].Contains("nickname_mapping"))
                                    {
                                        // really bad fix for json nicknames writer, but it seems to work
                                        // saved here only for reference in case it's needed.

                                        if (!saveData[x].Contains("[]"))
                                        {
                                            saveData[x] = " \"nickname_mappings\": {";

                                            x++;

                                            saveData[x] = "";

                                            x++;

                                            var nickKeyTemp = saveData[x].Split(':');
                                            string nickKeySave = nickKeyTemp[1];
                                            nickKeySave = nickKeySave.Substring(0, nickKeySave.Length - 2);

                                            x++;

                                            var nickValueTemp = saveData[x].Split(':');
                                            string nickValueSave = nickValueTemp[1];
                                            nickValueSave = nickValueSave.Substring(0, nickValueSave.Length - 1);

                                            x -= 2;

                                            saveData[x] = nickKeySave + ": " + nickValueSave;

                                            x++;

                                            saveData[x] = "},";

                                            x++;

                                            saveData[x] = "";

                                            x++;

                                            saveData[x] = "";

                                            x++;

                                            saveData[x] = "";
                                        }
                                        else
                                        {
                                            saveData[x] = " \"nickname_mappings\": {},";
                                        }

                                    }

                                    if (saveData[x].Contains("game_stats_datas"))
                                    {
                                        saveData[x] = saveData[x].Replace("game_stats_datas", "game_stats_data");
                                    }

                                    if (saveData[x].Contains("inventory_category_lists"))
                                    {
                                        saveData[x] = saveData[x].Replace("inventory_category_lists", "inventory_category_list");
                                    }

                                    if (saveData[x].Contains("equipped_inventory_lists"))
                                    {
                                        saveData[x] = saveData[x].Replace("equipped_inventory_lists", "equipped_inventory_list");
                                    }

                                    if (saveData[x].Contains("active_weapon_lists"))
                                    {
                                        saveData[x] = saveData[x].Replace("active_weapon_lists", "active_weapon_list");
                                    }

                                    if (saveData[x].Contains("mission_playthroughs_datas"))
                                    {
                                        saveData[x] = saveData[x].Replace("mission_playthroughs_datas",
                                            "mission_playthroughs_data");
                                    }

                                    if (saveData[x].Contains("last_active_travel_station_for_playthroughs"))
                                    {
                                        saveData[x] = saveData[x].Replace("last_active_travel_station_for_playthroughs",
                                            "last_active_travel_station_for_playthrough");
                                    }

                                    if (saveData[x].Contains("game_state_save_data_for_playthroughs"))
                                    {
                                        saveData[x] = saveData[x].Replace("game_state_save_data_for_playthroughs",
                                            "game_state_save_data_for_playthrough");
                                    }

                                    if (saveData[x].Contains("active_travel_stations_for_playthroughs"))
                                    {
                                        saveData[x] = saveData[x].Replace("active_travel_stations_for_playthroughs",
                                            "active_travel_stations_for_playthrough");
                                    }

                                    if (saveData[x].Contains("challenge_datas"))
                                    {
                                        saveData[x] = saveData[x].Replace("challenge_datas", "challenge_data");
                                    }

                                    if (saveData[x].Contains("sdu_lists"))
                                    {
                                        saveData[x] = saveData[x].Replace("sdu_lists", "sdu_list");
                                    }

                                    if (saveData[x].Contains("last_overworld_travel_station_for_playthroughs"))
                                    {
                                        saveData[x] = saveData[x].Replace("last_overworld_travel_station_for_playthroughs",
                                            "last_overworld_travel_station_for_playthrough");
                                    }

                                    if (saveData[x].Contains("customization_link_datas"))
                                    {
                                        saveData[x] = saveData[x].Replace("customization_link_datas", "customization_link_data");
                                    }

                                    if (saveData[x].Contains("mission_lists"))
                                    {
                                        saveData[x] = saveData[x].Replace("mission_lists", "mission_list");
                                    }

                                    if (saveData[x].Contains("\"status\": 0"))
                                    {
                                        saveData[x] = saveData[x].Replace("\"status\": 0", "\"status\": \"MS_NotStarted\"");
                                    }

                                    if (saveData[x].Contains("\"status\": 1"))
                                    {
                                        saveData[x] = saveData[x].Replace("\"status\": 1", "\"status\": \"MS_Active\"");
                                    }

                                    if (saveData[x].Contains("\"status\": 2"))
                                    {
                                        saveData[x] = saveData[x].Replace("\"status\": 2", "\"status\": \"MS_Complete\"");
                                    }

                                    if (saveData[x].Contains("\"status\": 3"))
                                    {
                                        saveData[x] = saveData[x].Replace("\"status\": 3", "\"status\": \"MS_Failed\"");
                                    }

                                    if (saveData[x].Contains("\"status\": 4"))
                                    {
                                        saveData[x] = saveData[x].Replace("\"status\": 4", "\"status\": \"MS_Unknown\"");
                                    }

                                    if (saveData[x].Contains("objectives_progresses"))
                                    {
                                        saveData[x] = saveData[x].Replace("objectives_progresses", "objectives_progress");
                                    }

                                    if (saveData[x].Contains("discovered_level_infoes"))
                                    {
                                        saveData[x] = saveData[x].Replace("discovered_level_infoes", "discovered_level_info");
                                    }

                                    if (saveData[x].Contains("level_datas"))
                                    {
                                        saveData[x] = saveData[x].Replace("level_datas", "level_data");
                                    }

                                    if (saveData[x].Contains("PlanetCycleInfo"))
                                    {
                                        saveData[x] = saveData[x].Replace("planet_cycle_infoes", "planet_cycle_info");
                                    }
                                }

                                File.WriteAllText(save.filePath, String.Join("\n", saveData));

                                result = null;
                                break;
                            }
                            else
                            {
                                // Now we've got to update the underlying protobuf data's serial...
                                foreach (WonderlandsSerial serial in save.InventoryItems)
                                {
                                    var protobufItem = save.Character.InventoryItems.FirstOrDefault(x => ReferenceEquals(x, serial.OriginalData));
                                    if (protobufItem == default)
                                    {
                                        throw new BL3Exceptions.SerialParseException(serial.EncryptSerial(), serial.SerialVersion, serial.SerialDatabaseVersion);
                                    }
                                    protobufItem.ItemSerialNumber = serial.EncryptSerialToBytes();
                                }

                                Serializer.Serialize(stream, save.Character);
                                result = stream.ToArray();
                                SaveBogoCrypt.Encrypt(result, 0, result.Length, save.Platform);
                                break;
                            }
                        default:
                            throw new BL3Exceptions.InvalidSaveException(saveGame.GVASData.sgType);
                    }
                }

                if (!isJson)
                {
                    io.WriteInt32(result.Length);
                    io.WriteBytes(result);
                }
            }
            finally
            {
                if (io.CurrentStream != null) io.Close();
            }

            Console.WriteLine("Completed writing file...");
            return true;
        }
    }

}
