using BL3Tools.GameData.Items;
using BL3Tools.GVAS;
using System.Linq;
using OakSave;
using System.Collections.Generic;

namespace BL3Tools
{

    /// <summary>
    /// A simple underlying class that's used to store both <see cref="BL3Save"/> and <see cref="BL3Profile"/>
    /// </summary>
    public class UE3Save
    {
        /// <summary>
        /// The file path associated with this path; does not need to be set
        /// </summary>
        public string filePath { get; set; } = null;

        public GVASSave GVASData { get; set; }
    }


    /// <summary>
    /// A class that represents a Borderlands 3 save.
    /// <para>This stores all of the data about it; Including the underlying protobuf data as well as all of the loaded/serializable items from the save.</para>
    /// </summary>
    public class BL3Save : UE3Save
    {
        public BL3Save(GVASSave saveData, Character character)
        {
            GVASData = saveData;
            Character = character;

            InventoryItems = Character.InventoryItems.Select(x => WonderlandsSerial.DecryptSerial(x.ItemSerialNumber)).ToList();

            for (int i = 0; i < InventoryItems.Count; i++)
            {
                InventoryItems[i].OriginalData = Character.InventoryItems[i];
            }
        }

        // Unlike the profiles, we can't just remove all of the data from the save's inventory and then readd it
        // Saves store other data in the items as well so we can't do that

        /// <summary>
        /// Deletes the given item from the save
        /// </summary>
        /// <param name="serialToDelete">A <see cref="WonderlandsSerial"/> object representing the item to delete</param>
        public void DeleteItem(WonderlandsSerial serialToDelete)
        {

            InventoryItems.Remove(serialToDelete);
            if (serialToDelete.OriginalData != null)
            {
                Character.InventoryItems.RemoveAll(x => ReferenceEquals(x, serialToDelete.OriginalData));
            }
        }

        /// <summary>
        /// Adds the given item to the save
        /// </summary>
        /// <param name="serialToAdd">A <see cref="WonderlandsSerial"/> object representing the item to add</param>
        public void AddItem(WonderlandsSerial serialToAdd)
        {
            InventoryItems.Add(serialToAdd);
            var oakItem = new OakInventoryItemSaveGameData()
            {
                DevelopmentSaveData = null,
                Flags = 0x01, // "NEW" Flag
                PickupOrderIndex = 8008,
                ItemSerialNumber = serialToAdd.EncryptSerialToBytes()
            };
            // Properly add in the item onto the save
            Character.InventoryItems.Add(oakItem);
            serialToAdd.OriginalData = oakItem;
        }

        /// <summary>
        /// The underlying protobuf data representing this save
        /// </summary>
        public Character Character { get; set; } = null;

        /// <summary>
        ///  The respective platform for the given save
        ///  <para>Used for encryption/decryption of the save files</para>
        /// </summary>
        public Platform Platform { get; set; } = Platform.PC;

        /// <summary>
        /// A list containing all of the inventory items of this file
        /// <para>If you want to add items to the save, please use <see cref="AddItem(WonderlandsSerial)"/></para>
        /// <para>If you want to delete items from the save, please use <see cref="DeleteItem(WonderlandsSerial)"/></para>
        /// </summary>
        public List<WonderlandsSerial> InventoryItems { get; set; } = null;

        public static Dictionary<string, PlayerClassSaveGameData> ValidClasses = new Dictionary<string, PlayerClassSaveGameData>() {
            { "Hero", new PlayerClassSaveGameData() {
                DlcPackageId = 0,
                PlayerClassPath="/Game/PlayerCharacters/PlayerClassId_Player.PlayerClassId_Player" } }
        };

        public static Dictionary<string, string> ValidAbilityBranches = new Dictionary<string, string>() {
            { "None", "" },
            { "Brr-zerker", "/Game/PlayerCharacters/Barbarian/_Shared/_Design/SkillTree/AbilityTree_Branch_Barbarian.AbilityTree_Branch_Barbarian" },
            { "Spellshot", "/Game/PlayerCharacters/GunMage/_Shared/_Design/SkillTree/AbilityTree_Branch_GunMage.AbilityTree_Branch_GunMage" },
            { "Clawbringer", "/Game/PlayerCharacters/KnightOfTheClaw/_Shared/_Design/SkillTree/AbilityTree_Branch_DragonCleric.AbilityTree_Branch_DragonCleric" },
            { "Graveborn", "/Game/PlayerCharacters/Necromancer/_Shared/_Design/SkillTree/AbilityTree_Branch_Necromancer.AbilityTree_Branch_Necromancer" },
            { "Spore Warden", "/Game/PlayerCharacters/Ranger/_Shared/_Design/SkillTree/AbilityTree_Branch_Ranger.AbilityTree_Branch_Ranger" },
            { "Stabbomancer", "/Game/PlayerCharacters/Rogue/_Shared/_Design/SkillTree/AbilityTree_Branch_Rogue.AbilityTree_Branch_Rogue" },
        };

        public static Dictionary<string, string> CharacterToClassPair = new Dictionary<string, string>() {
            { "FL4K", "Beastmaster" },
            { "Moze", "Gunner" },
            { "Zane", "Operative" },
            { "Amara", "Siren" }
        };

        public static Dictionary<string, string> ValidPlayerAspect = new Dictionary<string, string>() {
            { "None", "" },
            { "Village Idiot", "/Game/PlayerCharacters/_Shared/_Design/Aspects/Aspect_01.Aspect_01" },
            { "Raised By Elves", "/Game/PlayerCharacters/_Shared/_Design/Aspects/Aspect_02.Aspect_02" },
            { "Failed Monk", "/Game/PlayerCharacters/_Shared/_Design/Aspects/Aspect_03.Aspect_03" },
            { "Recovering Inventory Hoarder", "/Game/PlayerCharacters/_Shared/_Design/Aspects/Aspect_04.Aspect_04" },
            { "Rogue Alchemist", "/Game/PlayerCharacters/_Shared/_Design/Aspects/Aspect_05.Aspect_05" }
        };

        public static Dictionary<string, string> ValidPlayerPronouns = new Dictionary<string, string>() {
            { "Neutral", "/Game/PlayerCharacters/_Shared/_Design/PlayerPronouns/PlayerPronouns_Neutral.PlayerPronouns_Neutral" },
            { "Masculine", "/Game/PlayerCharacters/_Shared/_Design/PlayerPronouns/PlayerPronouns_Masculine.PlayerPronouns_Masculine" },
            { "Feminine", "/Game/PlayerCharacters/_Shared/_Design/PlayerPronouns/PlayerPronouns_Feminine.PlayerPronouns_Feminine" },
        };
    }

    /// <summary>
    /// A simple class that represents the Borderlands 3 profile structure.
    /// </summary>
    public class BL3Profile : UE3Save
    {
        public BL3Profile(GVASSave gvasSave, Profile profile)
        {
            this.GVASData = gvasSave;
            Profile = profile;

            BankItems = Profile.BankInventoryLists.Select(x => WonderlandsSerial.DecryptSerial(x.ItemSerialNumber)).ToList();
            LostLootItems = Profile.LostLootInventoryLists.Select(x => WonderlandsSerial.DecryptSerial(x)).ToList();

            for (int i = 0; i < BankItems.Count - 1; i++)
            {
                WonderlandsSerial item = BankItems[i];
                item.OriginalData = new OakInventoryItemSaveGameData()
                {
                    DevelopmentSaveData = null,
                    Flags = 0x00,
                    ItemSerialNumber = Profile.BankInventoryLists[i].ItemSerialNumber,
                    PickupOrderIndex = -1
                };
            }

            for (int i = 0; i < LostLootItems.Count - 1; i++)
            {
                WonderlandsSerial item = LostLootItems[i];
                item.OriginalData = new OakInventoryItemSaveGameData()
                {
                    DevelopmentSaveData = null,
                    Flags = 0x00,
                    ItemSerialNumber = Profile.LostLootInventoryLists[i],
                    PickupOrderIndex = -1
                };
            }
        }

        /// <summary>
        /// The underlying protobuf data representing this profile
        /// </summary>
        public Profile Profile { get; set; }


        /// <summary>
        /// A list representing all of the items stored in the current profile's bank.
        /// </summary>
        public List<WonderlandsSerial> BankItems { get; set; } = null;

        /// <summary>
        /// A list representing all of the items stored in the current profile's lost loot.
        /// </summary>
        public List<WonderlandsSerial> LostLootItems { get; set; } = null;


        /// <summary>
        ///  The respective platform for the profile
        ///  <para>Used for encryption/decryption</para>
        /// </summary>
        public Platform Platform { get; set; } = Platform.PC;
    }

}
