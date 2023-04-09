
namespace XRL.World.CleverGirl {
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.Parts;

    public class CleverGirl_AutoPickupEquipMenu {
        /** InventoryAction Options **/
        public static readonly Utility.OptionAction ACTION = new Utility.OptionAction {
            Name = "Clever Girl - Manage Gear Pickup",
            Display = "manage gear auto {{inventoryhotkey|p}}ickup/equip behavior",
            Command = "CleverGirl_ManageGearPickup",
            Key = 'E',
            Valid = Utility.OptionAction.Adjacent,
        };

        /** OptionAction Options **/
        public static readonly Utility.OptionAction ENABLE = new Utility.OptionAction {
            Name = "Clever Girl - Enable Gear Pickup",
            Display = "{{y|[ ]}} auto {{hotkey|p}}ickup/equip gear",
            Command = "CleverGirl_EnableGearPickup",
            Key = 'p',
            Valid = (leader, companion) => leader == The.Player && !companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction DISABLE = new Utility.OptionAction {
            Name = "Clever Girl - Disable Gear Pickup",
            Display = "{{W|[þ]}} auto {{hotkey|p}}ickup/equip gear",
            Command = "CleverGirl_DisableGearPickup",
            Key = 'p',
            Valid = (leader, companion) => leader == The.Player && companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_ENABLE = new Utility.OptionAction {
            Name = "Clever Girl - Enable Follower Gear Pickup",
            Display = "{{y|[ ]}} auto pickup/equip gear ({{hotkey|f}}ollowers)",
            Command = "CleverGirl_EnableFollowerGearPickup",
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => !obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_DISABLE = new Utility.OptionAction {
            Name = "Clever Girl - Disable Follower Gear Pickup",
            Display = "{{W|[þ]}} auto pickup/equip gear ({{hotkey|f}}ollowers)",
            Command = "CleverGirl_DisableFollowerGearPickup",
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction AUTO_EQUIP_BEHAVIOR = new Utility.OptionAction {
            Name = "Clever Girl - Specify Forbidden Equipment Slots",
            Display = "{{hotkey|s}}pecify forbidden equipment slots",
            Command = "CleverGirl_SpecifyForbiddenEquipmentSlots",
            Key = 's',
            Valid = (leader, companion) => leader == The.Player && companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.DARKEN,
        };

        public static readonly List<Utility.OptionAction> OPTIONS = new List<Utility.OptionAction> {
            ENABLE,
            DISABLE,
            FOLLOWER_ENABLE,
            FOLLOWER_DISABLE,
            AUTO_EQUIP_BEHAVIOR,
        };

        //TODO: 
        /*
        public static readonly Utility.OptionAction WEAPON_TYPE_PREFERENCE = new Utility.OptionAction {
            Name = "Clever Girl - Set Weapon Type Preference",
            Display = "Set weapon {{inventoryhotkey|t}}ype preference",
            ActionCall = (leader, companion) => SetWeaponTypePreference(companion),
            Key = 't',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            InvalidBehavior = Utility.InvalidOptionBehavior.DARKEN,
        };
        */
        public static bool Start(GameObject leader, GameObject companion) {
            return CleverGirl_BasicMenu.Start(leader, companion, OPTIONS);
        }
    }
}