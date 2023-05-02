namespace CleverGirl.Menus {
    using System.Collections.Generic;
    using System.Linq;
    using XRL;
    using XRL.World;
    using CleverGirl.Parts;

    public class CleverGirl_BehaviorsMenu {
        public static readonly Utility.OptionAction ACTION = new Utility.OptionAction {
            Name = "Clever Girl - Manage Behaviors",
            Display = "manage {{hotkey|b}}ehaviors",
            Command = "CleverGirl_ManageBehaviors",
            Key = 'b',
            Valid = Utility.OptionAction.Adjacent,
        };
        public static readonly Utility.OptionAction ENABLE_PICKUP = new Utility.OptionAction {
            Name = "Clever Girl - Enable Auto Gear Pickup",
            Display = "{{y|[ ]}} toggle {{hotkey|a}}uto pickup",
            Command = "CleverGirl_EnableAutoGearPickup",
            Key = 'a',
            Valid = (leader, companion) => leader == The.Player && !companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction DISABLE_PICKUP = new Utility.OptionAction {
            Name = "Clever Girl - Disable Auto Gear Pickup",
            Display = "{{W|[þ]}} toggle {{hotkey|a}}uto pickup",
            Command = "CleverGirl_DisableAutoGearPickup",
            Key = 'a',
            Valid = (leader, companion) => leader == The.Player && companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_ENABLE_PICKUP = new Utility.OptionAction {
            Name = "Clever Girl - Enable Follower Auto Gear Pickup",
            Display = "{{y|[ ]}} toggle {{hotkey|f}}ollower auto pickup",
            Command = "CleverGirl_EnableFollowerAutoGearPickup",
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => !obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_DISABLE_PICKUP = new Utility.OptionAction {
            Name = "Clever Girl - Disable Follower Auto Gear Pickup",
            Display = "{{W|[þ]}} toggle {{hotkey|f}}ollower auto pickup",
            Command = "CleverGirl_DisableFollowerAutoGearPickup",
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };

        public static readonly List<Utility.OptionAction> OPTIONS = new List<Utility.OptionAction> {
            ENABLE_PICKUP,
            DISABLE_PICKUP,
            FOLLOWER_ENABLE_PICKUP,
            FOLLOWER_DISABLE_PICKUP,
        };

        public static bool Start(GameObject leader, GameObject companion) {
            return CleverGirl_BasicMenu.Start(leader, companion, OPTIONS,
                                              Title: companion.ShortDisplayName,
                                              centerIntro: true,
                                              IntroIcon: companion.RenderForUI(),
                                              AllowEscape: true);
        }
    }
}
