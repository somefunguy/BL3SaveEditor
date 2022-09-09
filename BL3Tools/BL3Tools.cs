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
                                   
                                    // non-standard JSON format found, possible matches Save Wizard output

                                    saveData[x] = "\"NicknameMappings\": [{";

                                    x++;

                                    var nickTemp = saveData[x].Split(':');

                                    var nickKey = nickTemp[0];
                                    var nickValue = nickTemp[1];

                                    // check for companion key
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
                                        // no companion key found, import as empty

                                        x--;

                                        saveData[x] = "\"NicknameMappings\": [],";
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
                        //saveGame = new BL3Save(new GVASSave(-1, -1, -1, -1, -1, 0, null, -1, -1, new Dictionary<byte[], int>(), "BPSaveGame_Default_C"), character);
                        
                        // static save header. guid and most values from multiple saves are the same.
                        // the proper way to have built this would have been to read from a guid array and value array
                        // then pair up into a byte[] array.
                        // since the dictionary is byte array and int value, the entire header is written as one byte[] array
                        // with an int value of 0. this works because the last int value for the paired guid is 0.
                        Dictionary<byte[], int> dictbuf = new Dictionary<byte[], int>();

                        dictbuf.Add(new byte[] { 
                            0xB2, 0x94, 0x83, 0xB9, 0xD0, 0x49, 0x06, 0x51, 0x6B, 0xEB, 0x0C, 0x99, 0x42, 0x10, 0x5E, 0x4A,
                            0x00, 0x00, 0x00, 0x00, 0xD7, 0xBA, 0x3C, 0x60, 0x3A, 0x40, 0x9C, 0xE0, 0x93, 0x15, 0x64, 0x8B,
                            0xD6, 0x12, 0xED, 0x84, 0x00, 0x00, 0x00, 0x00, 0xB8, 0x02, 0xF4, 0x77, 0x4A, 0x42, 0xD9, 0x69,
                            0x09, 0xEB, 0x17, 0xB1, 0x4B, 0xC6, 0x0B, 0x32, 0x00, 0x00, 0x00, 0x00, 0xCC, 0x1D, 0x02, 0x95,
                            0x7C, 0x40, 0xC3, 0x1B, 0x0B, 0xD8, 0xA2, 0x9E, 0xB0, 0x07, 0x70, 0x03, 0x01, 0x00, 0x00, 0x00,
                            0x1C, 0x98, 0xE5, 0x99, 0xDB, 0x4C, 0x44, 0x2C, 0x3F, 0xF6, 0x4A, 0xB9, 0xA3, 0xD1, 0x3C, 0xD5,
                            0x00, 0x00, 0x00, 0x00, 0x02, 0xD0, 0xF1, 0x76, 0x2F, 0x43, 0x57, 0xEC, 0xA0, 0x02, 0x81, 0xB2,
                            0x59, 0x8B, 0x87, 0xCB, 0x01, 0x00, 0x00, 0x00, 0x12, 0xE4, 0x26, 0xFB, 0x4D, 0x4B, 0x15, 0x1F,
                            0x0A, 0x55, 0x72, 0x93, 0x70, 0x2F, 0x1D, 0x96, 0x03, 0x00, 0x00, 0x00, 0x12, 0x08, 0xB9, 0x1F,
                            0xA0, 0x4B, 0xBB, 0x2C, 0x5B, 0xCF, 0x7A, 0xBD, 0x68, 0xF4, 0x85, 0x56, 0x02, 0x00, 0x00, 0x00,
                            0xC6, 0x02, 0x70, 0xB8, 0x04, 0x49, 0x38, 0x1D, 0x76, 0xA1, 0x35, 0xA8, 0x37, 0x03, 0x1B, 0xDB,
                            0x02, 0x00, 0x00, 0x00, 0x62, 0x67, 0x2C, 0x2E, 0x38, 0x43, 0x14, 0x47, 0x73, 0x23, 0xC9, 0x99,
                            0x92, 0x42, 0x4B, 0xEE, 0x00, 0x00, 0x00, 0x00, 0x22, 0xD5, 0x54, 0x9C, 0xBE, 0x4F, 0x26, 0xA8,
                            0x46, 0x07, 0x21, 0x94, 0xD0, 0x82, 0xB4, 0x61, 0x11, 0x00, 0x00, 0x00, 0xE4, 0x32, 0xD8, 0xB0,
                            0x0D, 0x4F, 0x89, 0x1F, 0xB7, 0x7E, 0xCF, 0xAC, 0xA2, 0x4A, 0xFD, 0x36, 0x0A, 0x00, 0x00, 0x00,
                            0x28, 0x43, 0xC6, 0xE1, 0x53, 0x4D, 0x2C, 0xA2, 0x86, 0x8E, 0x6C, 0xA3, 0x8C, 0xBD, 0x17, 0x64,
                            0x00, 0x00, 0x00, 0x00, 0x3C, 0xC1, 0x5E, 0x37, 0xFB, 0x48, 0xE4, 0x06, 0xF0, 0x84, 0x00, 0xB5,
                            0x7E, 0x71, 0x2A, 0x26, 0x02, 0x00, 0x00, 0x00, 0xED, 0x68, 0xB0, 0xE4, 0xE9, 0x42, 0x94, 0xF4,
                            0x0B, 0xDA, 0x31, 0xA2, 0x41, 0xBB, 0x46, 0x2E, 0x18, 0x00, 0x00, 0x00, 0x3F, 0x74, 0xFC, 0xCF,
                            0x80, 0x44, 0xB0, 0x43, 0xDF, 0x14, 0x91, 0x93, 0x73, 0x20, 0x1D, 0x17, 0x22, 0x00, 0x00, 0x00,
                            0xB5, 0x49, 0x2B, 0xB0, 0xE9, 0x44, 0x20, 0xBB, 0xB7, 0x32, 0x04, 0xA3, 0x60, 0x03, 0xE4, 0x52,
                            0x02, 0x00, 0x00, 0x00, 0x5C, 0x10, 0xE4, 0xA4, 0xB5, 0x49, 0xA1, 0x59, 0xC4, 0x40, 0xC5, 0xA7,
                            0xEE, 0xDF, 0x7E, 0x54, 0x00, 0x00, 0x00, 0x00, 0xC9, 0x31, 0xC8, 0x39, 0xDC, 0x47, 0xE6, 0x5A,
                            0x17, 0x9C, 0x44, 0x9A, 0x7C, 0x8E, 0x1C, 0x3E, 0x00, 0x00, 0x00, 0x00, 0x33, 0x1B, 0xF0, 0x78,
                            0x98, 0x4F, 0xEA, 0xEB, 0xEA, 0x84, 0xB4, 0xB9, 0xA2, 0x5A, 0xB9, 0xCC, 0x00, 0x00, 0x00, 0x00,
                            0x0F, 0x38, 0x31, 0x66, 0xE0, 0x43, 0x4D, 0x2D, 0x27, 0xCF, 0x09, 0x80, 0x5A, 0xA9, 0x56, 0x69,
                            0x00, 0x00, 0x00, 0x00, 0x9F, 0x8B, 0xF8, 0x12, 0xFC, 0x4A, 0x75, 0x88, 0x0C, 0xD9, 0x7C, 0xA6,
                            0x29, 0xBD, 0x3A, 0x38, 0x1A, 0x00, 0x00, 0x00, 0x4C, 0xE7, 0x5A, 0x7B, 0x10, 0x4C, 0x70, 0xD2,
                            0x98, 0x57, 0x58, 0xA9, 0x5A, 0x2A, 0x21, 0x0B, 0x09, 0x00, 0x00, 0x00, 0x18, 0x69, 0x29, 0xD7,
                            0xDD, 0x4B, 0xD6, 0x1D, 0xA8, 0x64, 0xE2, 0x9D, 0x84, 0x38, 0xC1, 0x3C, 0x02, 0x00, 0x00, 0x00,
                            0x78, 0x52, 0xA1, 0xC2, 0xFE, 0x4A, 0xE7, 0xBF, 0xFF, 0x90, 0x17, 0x6C, 0x55, 0xF7, 0x1D, 0x53,
                            0x01, 0x00, 0x00, 0x00, 0xD4, 0xA3, 0xAC, 0x6E, 0xC1, 0x4C, 0xEC, 0x40, 0xED, 0x8B, 0x86, 0xB7,
                            0xC5, 0x8F, 0x42, 0x09, 0x03, 0x00, 0x00, 0x00, 0x4C, 0xFB, 0x16, 0x87, 0x99, 0x46, 0xF4, 0x2F,
                            0x16, 0xD4, 0x0C, 0xAB, 0xD3, 0x47, 0x86, 0x30, 0x02, 0x00, 0x00, 0x00, 0xDD, 0x75, 0xE5, 0x29,
                            0x27, 0x46, 0xA3, 0xE0, 0x76, 0xD2, 0x10, 0x9D, 0xEA, 0xDC, 0x2C, 0x23, 0x11, 0x00, 0x00, 0x00,
                            0xEC, 0x6C, 0x26, 0x6B, 0x8F, 0x4B, 0xC7, 0x1E, 0xD9, 0xE4, 0x0B, 0xA3, 0x07, 0xFC, 0x42, 0x09,
                            0x01, 0x00, 0x00, 0x00, 0x61, 0x3D, 0xF7, 0x0D, 0xEA, 0x47, 0x3F, 0xA2, 0xE9, 0x89, 0x27, 0xB7,
                            0x9A, 0x49, 0x41, 0x0C, 0x01, 0x00, 0x00, 0x00, 0x86, 0x18, 0x1D, 0x60, 0x84, 0x4F, 0x64, 0xAC,
                            0xDE, 0xD3, 0x16, 0xAA, 0xD6, 0xC7, 0xEA, 0x0D, 0x0C, 0x00, 0x00, 0x00, 0xD6, 0xBC, 0xFF, 0x9D,
                            0x58, 0x01, 0x4F, 0x49, 0x82, 0x12, 0x21, 0xE2, 0x88, 0xA8, 0x92, 0x3C, 0x01, 0x00, 0x00, 0x00,
                            0xC2, 0x01, 0x97, 0xF2, 0xC7, 0x47, 0x86, 0x84, 0x04, 0x1C, 0xF1, 0xB3, 0x6B, 0xC7, 0x21, 0xAC,
                            0x0B, 0x00, 0x00, 0x00, 0x18, 0xF0, 0x54, 0xB9, 0xD6, 0x4D, 0x62, 0xC9, 0xB1, 0x79, 0x4E, 0xA7,
                            0xC2, 0x13, 0xA1, 0x8E, 0x10, 0x00, 0x00, 0x00, 0x48, 0xEB, 0xD4, 0x03, 0xC3, 0x4C, 0x0B, 0xB5,
                            0x41, 0xDE, 0x98, 0xA5, 0x93, 0xC9, 0x6C, 0x5C, 0x00, 0x00, 0x00, 0x00, 0x19, 0x4D, 0x0C, 0x43,
                            0x70, 0x49, 0x54, 0x71, 0x69, 0x9B, 0x69, 0x87, 0xE5, 0xB0, 0x90, 0xDF, 0x0B, 0x00, 0x00, 0x00,
                            0xBD, 0x32, 0xFE, 0xAA, 0x14, 0x4C, 0x95, 0x53, 0x25, 0x5E, 0x6A, 0xB6, 0xDD, 0xD1, 0x32, 0x10,
                            0x01, 0x00, 0x00, 0x00, 0x8E, 0xE1, 0xAF, 0x23, 0x58, 0x4E, 0xE1, 0x4C, 0x52, 0xC2, 0x61, 0x8D,
                            0xB7, 0xBE, 0x53, 0xB9, 0x09, 0x00, 0x00, 0x00, 0xEA, 0xB7, 0x62, 0xA4, 0x3A, 0x4E, 0x99, 0xF4,
                            0x1F, 0xEC, 0xC1, 0x99, 0xB2, 0xE1, 0x24, 0x82, 0x02, 0x00, 0x00, 0x00, 0xBD, 0xFD, 0xB5, 0x2E,
                            0x10, 0x4D, 0xAC, 0x01, 0x8F, 0xF3, 0x36, 0x81, 0xDA, 0xA5, 0x93, 0x33, 0x05, 0x00, 0x00, 0x00,
                            0x4F, 0x35, 0x9D, 0x50, 0x2F, 0x49, 0xE6, 0xF6, 0xB2, 0x85, 0x49, 0xA7, 0x1C, 0x63, 0x3C, 0x07,
                            0x00, 0x00, 0x00, 0x00, 0xEC, 0xDB, 0xDC, 0x17, 0x03, 0x43, 0xD7, 0xAF, 0x5F, 0xF5, 0x52, 0xA0,
                            0xEA, 0x3C, 0x9B, 0xF9, 0x05, 0x00, 0x00, 0x00, 0x8E, 0x7C, 0x00, 0x03, 0xE8, 0x11, 0x3F, 0x29,
                            0x65, 0x50, 0x66, 0xBF, 0x14, 0x5D, 0x2C, 0xF3, 0x01, 0x00, 0x00, 0x00, 0xEC, 0xAC, 0xA5, 0x84,
                            0xE7, 0x11, 0x43, 0x42, 0x85, 0x0C, 0x99, 0xB2, 0x1E, 0x60, 0x19, 0xFC, 0x01, 0x00, 0x00, 0x00,
                            0x17, 0x41, 0x2A, 0x48, 0xBA, 0x27, 0x72, 0x94, 0x24, 0x7D, 0xB8, 0xB7, 0x02, 0xA3, 0x3E, 0xCA,
                            0x03, 0x00, 0x00, 0x00, 0x40, 0xEB, 0x56, 0x4A, 0xDC, 0x11, 0xF5, 0x10, 0x7E, 0x34, 0xD3, 0x92,
                            0xE7, 0x6A, 0xC9, 0xB2, 0x02, 0x00, 0x00, 0x00, 0xA0, 0x38, 0x40, 0x3A, 0xA6, 0x45, 0x21, 0x2B,
                            0x7A, 0x3A, 0xCE, 0x87, 0x96, 0x93, 0x04, 0x6B, 0x01, 0x00, 0x00, 0x00, 0x00, 0x4A, 0x8A, 0xD7,
                            0x97, 0x46, 0x58, 0xE8, 0xB5, 0x19, 0xA8, 0xBA, 0xB4, 0x46, 0x7D, 0x48, 0x10, 0x00, 0x00, 0x00,
                            0x0A, 0x5F, 0x53, 0x48, 0x10, 0x45, 0x17, 0xE4, 0xEA, 0xF1, 0xE1, 0x94, 0xC1, 0x47, 0x3A, 0xA0,
                            0x06, 0x00, 0x00, 0x00, 0x86, 0xF8, 0x79, 0x55, 0x1F, 0x4C, 0x3A, 0x93, 0x7B, 0x08, 0xBA, 0x83,
                            0x2F, 0xB9, 0x61, 0x63, 0x01, 0x00, 0x00, 0x00, 0x52, 0xBE, 0x2F, 0x61, 0x0B, 0x40, 0x53, 0xDA,
                            0x91, 0x4F, 0x0D, 0x91, 0x7C, 0x85, 0xB1, 0x9F, 0x01, 0x00, 0x00, 0x00, 0x36, 0x7A, 0x23, 0xA4,
                            0xC9, 0x41, 0xEA, 0xCA, 0xF8, 0x18, 0xA2, 0x8F, 0xF3, 0x1B, 0x68, 0x58, 0x04, 0x00, 0x00, 0x00,
                            0xF2, 0x0A, 0x68, 0xFB, 0xA3, 0x4B, 0xEF, 0x59, 0xB5, 0x19, 0xA8, 0xBA, 0x3D, 0x44, 0xC8, 0x73,
                            0x01, 0x00, 0x00, 0x00, 0xB9, 0x22, 0xE3, 0xB4, 0xF9, 0x47, 0x7E, 0xDA, 0x57, 0x6F, 0x7F, 0xBF,
                            0xBD, 0x1E, 0x37, 0xF4, 0x01, 0x00, 0x00, 0x00, 0x19, 0x04, 0x70, 0xFA, 0x69, 0x47, 0x18, 0xDA,
                            0xF1, 0x25, 0x5D, 0x8D, 0xDD, 0x07, 0xC5, 0x1A, 0x00, 0x00, 0x00, 0x00, 0x08, 0x7F, 0x08, 0xA6,
                            0x07, 0x44, 0x33, 0x89, 0xFA, 0xE8, 0x8A, 0x8E, 0xC2, 0xBC, 0xB3, 0x50, 0x00, 0x00, 0x00, 0x00,
                            0x2C, 0x5D, 0xC0, 0xCC, 0xB2, 0x43, 0x9F, 0xAF, 0xC6, 0x22, 0xAE, 0xA6, 0x9A, 0x03, 0xD9, 0x10,
                            0x00, 0x00, 0x00, 0x00, 0xEF, 0xA6, 0x3A, 0x63, 0xD5, 0x44, 0xC9, 0x94, 0xE0, 0xEA, 0x37, 0x99,
                            0x0E, 0xE4, 0x4E, 0xFE, 0x01, 0x00, 0x00, 0x00, 0x28, 0xFC, 0x22, 0x7E, 0xF9, 0x67, 0x2C, 0x0B,
                            0x6D, 0x9D, 0xF0, 0x1D, 0x10, 0x02, 0x97, 0xD4 }, 0);

                        saveGame = new BL3Save(new GVASSave(2, 516, 4, 20, 3, 2150344073, "DAFFODIL-PATCHDIESEL1-68", 3, 59, dictbuf, "BPSaveGame_Default_C"), character);
                        
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
