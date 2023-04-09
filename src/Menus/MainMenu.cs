
namespace XRL.World.CleverGirl {
    using System.Collections.Generic;
    using XRL.World.Parts;

    public class CleverGirl_MainMenu {
        /** InventoryAction Options **/
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Main Menu",
            Display = "\"{{inventoryhotkey|c}}lever girl\"",
            Command = "CleverGirl_MainMenu",
            Key = 'c',
            Valid = Utility.InventoryAction.Adjacent,
        };

        /** OptionAction Options **/
        public static readonly List<Utility.OptionAction> OPTIONS = new List<Utility.OptionAction> {
            CleverGirl_Feed.ACTION,
            CleverGirl_ManageGear.ACTION,
            CleverGirl_AutoPickupEquipMenu.ACTION,
            CleverGirl_AIManageSkills.ACTION,
            CleverGirl_AIManageAttributes.ACTION,
            CleverGirl_AIManageMutations.ACTION,
        };

        public static bool Start(GameObject leader, GameObject companion) {
            return CleverGirl_BasicMenu.Start(leader, companion, OPTIONS);
        }
    }
}