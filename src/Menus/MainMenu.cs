
namespace XRL.World.CleverGirl {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.Parts;

    public class CleverGirl_MainMenu {

        // TODO: Make these remarks more immersive by adding conditions and priority system. Also account for
        //       lack of mouths and/or telepathy.
        //       (IE: "Remember that [dmg_source] you hit me with [number] turns ago? Cause I sure do.")
        public static readonly List<Func<GameObject, GameObject, string>> REMARKS = new List<Func<GameObject, GameObject, string>> {
            // (leader, companion)
            (l, c) => "How's it hanging, leader?",
            (l, c) => "Where'd my sunglasses go?",
            (l, c) => "What's up boss?",
            (l, c) => "I swear I saw another " + c.GetApparentSpecies() + " earlier that looked just like me!",
            (l, c) => "Are we stopping any time soon?",
            (l, c) => "When was the last time you slept!?",
            (l, c) => "What now?",
            (l, c) => "Live and drink!",
            (l, c) => "Your thirst is yours, my water is MINE.",
        };
        private static readonly Random rng = new Random();
        private static List<int> rngSequence = null;
        private static int rngIndex = 0;

        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Main Menu",
            Display = "\"{{inventoryhotkey|c}}lever girl\"",
            Command = "CleverGirl_MainMenu",
            Key = 'c',
            Valid = Utility.InventoryAction.Adjacent,
        };

        public static readonly List<Utility.OptionAction> OPTIONS = new List<Utility.OptionAction> {
            CleverGirl_Feed.ACTION,
            CleverGirl_ManageGear.ACTION,
            CleverGirl_AutoPickupEquipMenu.ACTION,
            CleverGirl_AIManageSkills.ACTION,
            CleverGirl_AIManageAttributes.ACTION,
            CleverGirl_AIManageMutations.ACTION,
        };

        public static bool Start(GameObject leader, GameObject companion) {
            // Shuffle remarks instead of selecting randomly each time. Because pseudo-random is only pseudo-fun.
            if (rngSequence == null || rngIndex >= rngSequence.Count) {
                rngSequence = Enumerable.Range(0, REMARKS.Count).OrderBy(a => rng.Next()).ToList();
                rngIndex = 0;
            }

            Utility.MaybeLog(string.Join(", ", rngSequence) + " [" + rngIndex + "] ");
            string remark = REMARKS[rngSequence[rngIndex]](leader, companion);
            rngIndex++;

            bool result = false;
            result = CleverGirl_BasicMenu.Start(leader, companion, OPTIONS,
                                                Title: companion.the + companion.ShortDisplayName,
                                                Intro: remark,
                                                centerIntro: true,
                                                IntroIcon: companion.RenderForUI(),
                                                AllowEscape: true);
            return result;
        }
    }
}
