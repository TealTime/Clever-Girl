using System;
using XRL.UI;

namespace XRL.World.CleverGirl
{
    public class ManageGear {
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction{
            Name = "Clever Girl - Manage Gear",
            Display = "manage g{{inventoryhotkey|e}}ar",
            Command = "CleverGirl_ManageGear",
            Key = 'e',
        };

        public static bool Manage(GameObject follower) {
            IScreen screen = new EquipmentScreen();
            screen.Show(follower);
            return false;
        }
    }
}