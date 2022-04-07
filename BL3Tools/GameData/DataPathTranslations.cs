using System;
using System.Linq;
using System.Collections.Generic;
using BL3Tools.Decryption;
using System.Reflection;
using System.IO;
using Newtonsoft.Json.Linq;
using BL3Tools.GameData.Items;
using System.Text.RegularExpressions;

namespace BL3Tools.GameData
{
    public static class DataPathTranslations
    {
        /// <summary>
        /// A list of all of the hashes of the weapon skins
        /// </summary>
        public static readonly List<uint> weaponSkinHashes = new List<uint>();

        /// <summary>
        /// A list of all of the hashes of the weapon trinkets
        /// </summary>
        public static readonly List<uint> trinketHashes = new List<uint>();

        public static readonly string BankSDUAssetPath = "/Game/Pickups/SDU/SDU_Bank.SDU_Bank";
        public static readonly string LLSDUAssetPath = "/Game/Pickups/SDU/SDU_LostLoot.SDU_LostLoot";

        // /-Extract\OakGame\Content\Gear\_Shared\_Design\InventoryCategories
        public static readonly string GoldenKeyCurrencyPath = "/Game/Gear/_Shared/_Design/InventoryCategories/InventoryCategory_GoldenKey.InventoryCategory_GoldenKey";
        public static readonly string DiamondKeyCurrencyPath = "/Game/Gear/_Shared/_Design/InventoryCategories/InventoryCategory_DiamondKey.InventoryCategory_DiamondKey";
        public static readonly string MoneyCurrencyPath = "/Game/Gear/_Shared/_Design/InventoryCategories/InventoryCategory_Money.InventoryCategory_Money";
        public static readonly string EridiumCurrencyPath = "/Game/Gear/_Shared/_Design/InventoryCategories/InventoryCategory_Eridium.InventoryCategory_Eridium";
        public static readonly string VaultCard1Path = "/Game/Gear/_Shared/_Design/InventoryCategories/InventoryCategory_VaultCard1Key";
        public static readonly string VaultCard2Path = "/Game/Gear/_Shared/_Design/InventoryCategories/InventoryCategory_VaultCard2Key";

        public static uint GoldenKeyHash { get; private set; }
        public static uint DiamondKeyHash { get; private set; }
        public static uint VaultCard1Hash { get; private set; }
        public static uint VaultCard2Hash { get; private set; }
        public static uint MoneyHash { get; private set; }
        public static uint EridiumHash { get; private set; }

        private static readonly string embeddedFastTravelDatabasePath = "BL3Tools.GameData.Mappings.fast_travel_to_name.json";

        public static void Initialize()
        {
            GoldenKeyHash = CRC32.Get(GoldenKeyCurrencyPath);
            DiamondKeyHash = CRC32.Get(DiamondKeyCurrencyPath);
            VaultCard1Hash = CRC32.Get(VaultCard1Path);
            VaultCard2Hash = CRC32.Get(VaultCard2Path);
            MoneyHash = CRC32.Get(MoneyCurrencyPath);
            EridiumHash = CRC32.Get(EridiumCurrencyPath);

            Console.WriteLine("Initialized CRC32 hashes of customization & currency data");

            Assembly me = typeof(BL3Tools).Assembly;
            using (Stream stream = me.GetManifestResourceStream(embeddedFastTravelDatabasePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                JObject db = JObject.FromObject(Newtonsoft.Json.JsonConvert.DeserializeObject(result));
                FastTravelTranslations = db.ToObject<Dictionary<string, string>>();
            }
        }

        static DataPathTranslations()
        {
            Initialize();
        }

        #region Translation Dictionaries

        #region Fast Travels

        public static List<string> UnobtainableFastTravels { get; private set; } = new List<string>() {
            "/Game/GameData/FastTravel/FTS_OrbitalPlatform.FTS_OrbitalPlatform",
            "/Game/PatchDLC/Raid1/GameData/FastTravel/LevelTravelData/FTS_MaliwanTD_SendOnly.FTS_MaliwanTD_SendOnly",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds01.FTS_ProvingGrounds01",
            "/Game/PatchDLC/Alisma/GameData/FastTravel/FTS_ALI_Chase_Boss.FTS_ALI_Chase_Boss",
            "/Game/GameData/FastTravel/FTS_FinalBossPortal.FTS_FinalBossPortal",
            "/Game/GameData/FastTravel/FTS_Prison_SendOnly.FTS_Prison_SendOnly",
            "/Game/PatchDLC/Ixora2/GameData/FastTravel/LevelTravel/FTS_Ixora2_NekroMystery_OneWay.FTS_Ixora2_NekroMystery_OneWay",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds02.FTS_ProvingGrounds02",
            "/Game/GameData/FastTravel/FTS_TechSlaughter.FTS_TechSlaughter",
            "/Game/PatchDLC/Dandelion/GameData/FastTravel/FTS_Strip_DLC1_TricksyNickArea.FTS_Strip_DLC1_TricksyNickArea",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds04.FTS_ProvingGrounds04",
            "/Game/PatchDLC/Takedown2/GameData/LevelTravel/FTS_GuardianTD_SendOnly.FTS_GuardianTD_SendOnly",
            "/Game/GameData/FastTravel/FTS_AtlasHQ_SendOnly.FTS_AtlasHQ_SendOnly",
            "/Game/GameData/FastTravel/FTS_Grotto.FTS_Grotto",
            "/Game/PatchDLC/Alisma/GameData/FastTravel/FTS_ALI_Eldorado_Boss.FTS_ALI_Eldorado_Boss",
            "/Game/GameData/FastTravel/FTS_Raid.FTS_Raid",
            "/Game/PatchDLC/Raid1/GameData/FastTravel/LevelTravelData/FTS_Raid1.FTS_Raid1",
            "/Game/GameData/FastTravel/FTS_MotorcadeInterior_SendOnly.FTS_MotorcadeInterior_SendOnly",
            "/Game/PatchDLC/Alisma/GameData/FastTravel/FTS_ALI_Experiment_Boss.FTS_ALI_Experiment_Boss",
            "/Game/PatchDLC/Ixora/GameData/FastTravel/LevelTravel/FTS_GearUpMap_SendOnly.FTS_GearUpMap_SendOnly",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds03.FTS_ProvingGrounds03",
            "/Game/PatchDLC/Alisma/GameData/FastTravel/FTS_ALI_Anger_Boss.FTS_ALI_Anger_Boss",
            "/Game/PatchDLC/Ixora2/GameData/FastTravel/LevelTravel/FTS_Ixora2_SacrificeBoss_One.FTS_Ixora2_SacrificeBoss_One",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds07.FTS_ProvingGrounds07",
            "/Game/PatchDLC/Takedown2/GameData/LevelTravel/FTS_GuardianTD_SendOnly_2.FTS_GuardianTD_SendOnly_2",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds00.FTS_ProvingGrounds00",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds08.FTS_ProvingGrounds08",
            "/Game/GameData/FastTravel/FTS_ZoneMapTest.FTS_ZoneMapTest",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds05.FTS_ProvingGrounds05",
            "/Game/PatchDLC/Hibiscus/GameData/FastTravel/FTS_DLC2_Lake_Excavation.FTS_DLC2_Lake_Excavation",
            "/Game/PatchDLC/Ixora2/GameData/FastTravel/LevelTravel/FTS_Ixora2_Promethea.FTS_Ixora2_Promethea",
            "/Game/GameData/FastTravel/FTS_ZoneMapTest2.FTS_ZoneMapTest2",
            "/Game/PatchDLC/Takedown2/GameData/LevelTravel/FTS_TD2.FTS_TD2",
            "/Game/GameData/FastTravel/FTS_Monastery.FTS_Monastery",
            "/Game/GameData/FastTravel/FTS_Marshfields_SendOnly.FTS_Marshfields_SendOnly",
            "/Game/GameData/FastTravel/FTS_WetlandsBoss_SendOnly.FTS_WetlandsBoss_SendOnly",
            "/Game/GameData/FastTravel/FTS_PlayableIntro_SendOnly.FTS_PlayableIntro_SendOnly",
            "/Game/GameData/FastTravel/FTS_ProvingGrounds06.FTS_ProvingGrounds06",
            "/Game/GameData/FastTravel/FTS_CityBoss_SendOnly.FTS_CityBoss_SendOnly"
        };

        public static Dictionary<string, string> FastTravelTranslations { get; private set; } = new Dictionary<string, string>();
        #endregion

        #region Myth Rank

        public static readonly string[] MythRankAttributes = new string[]
        {
            "Intelligence",
            "Spell Critical Damage",
            "Spell Critical Chance",
            "Fire Damage",
            "Frost Damage",
            "Status Effect Chance",
            "Spell Damage",
            "Strength",
            "Constitution",
            "Dark Magic Damage",
            "Melee Critical Chance",
            "Melee Swing Speed",
            "Melee Critical Damage",
            "Melee Damage",
            "Dexterity",
            "Gun Handling",
            "Move Speed",
            "Reload Speed",
            "Magazine Size",
            "Fire Rate",
            "Gun Damage",
            "Wisdom",
            "Companion Damage",
            "Attunement",
            "Lightning Damage",
            "Loot Luck",
            "Poison Damage",
            "Ability Damage"
        };
        #endregion

        #region Items etc
        public static Dictionary<string, string> SlotToPathDictionary = new Dictionary<string, string>() {
            {"Weapon1", "/Game/Gear/Weapons/_Shared/_Design/InventorySlots/BPInvSlot_Weapon1.BPInvSlot_Weapon1" },
            {"Weapon2", "/Game/Gear/Weapons/_Shared/_Design/InventorySlots/BPInvSlot_Weapon2.BPInvSlot_Weapon2" },
            {"Weapon3", "/Game/Gear/Weapons/_Shared/_Design/InventorySlots/BPInvSlot_Weapon3.BPInvSlot_Weapon3" },
            {"Weapon4", "/Game/Gear/Weapons/_Shared/_Design/InventorySlots/BPInvSlot_Weapon4.BPInvSlot_Weapon4" },
            {"Amulet", "/Game/Gear/Amulets/_Shared/_Design/A_Data/InvSlot_Amulet.InvSlot_Amulet"},
            {"Spell", "/Game/Gear/SpellMods/_Shared/_Design/A_Data/BPInvSlot_SpellMod.BPInvSlot_SpellMod"},
            {"SecondSpell", "/Game/Gear/SpellMods/_Shared/_Design/A_Data/BPInvSlot_SecondSpellMod.BPInvSlot_SecondSpellMod"},
            {"Ring1", "/Game/Gear/Rings/_Shared/Design/A_Data/InvSlot_Ring_1.InvSlot_Ring_1"},
            {"Ring2", "/Game/Gear/Rings/_Shared/Design/A_Data/InvSlot_Ring_2.InvSlot_Ring_2"},
            {"Pauldron", "/Game/Gear/Pauldrons/_Shared/_Design/A_Data/InvSlot_Pauldron.InvSlot_Pauldron"}
        };
        #endregion

        #region Customizations

        #endregion

        #endregion
    }
}
