
namespace CleverGirl.Menus {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World;
    using CleverGirl.Parts;
    using Options = Globals.Options;


    public class CleverGirl_MainMenu {
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Main Menu",
            Display = "clever command menu",
            Command = "CleverGirl_MainMenu",
            Key = 'c',
            PreferToHighlight = "clever",
            Priority = 1,  // Since this is a somewhat high frequency command, give it a slight bump in priority to preserve hotkey
            Valid = Utility.InventoryAction.Adjacent,
        };

        public static readonly List<Utility.OptionAction> OPTIONS = new List<Utility.OptionAction> {
            CleverGirl_Feed.ACTION,
            CleverGirl_ManageGear.ACTION,
            CleverGirl_AIManageSkills.ACTION,
            CleverGirl_AIManageAttributes.ACTION,
            CleverGirl_AIManageMutations.ACTION,
            CleverGirl_BehaviorsMenu.ACTION,
        };

        // TODO: Make these remarks more immersive by adding conditions and priority system. Also account for
        //       lack of mouths and/or telepathy.
        //       (IE: "Remember that [dmg_source] you hit me with [number] turns ago? Cause I sure do.")
        public static readonly List<Func<GameObject, GameObject, string>> REMARKS = new List<Func<GameObject, GameObject, string>> {
            // (leader, companion)
            (l, c) => "How's it hanging, friend?",
            (l, c) => "Where'd my shades go?",
            (l, c) => "What's up boss?",
            (l, c) => "Are we stopping any time soon?",
            (l, c) => "When was the last time you slept!?",
            (l, c) => "What now?",
            (l, c) => "Live and drink!",
            (l, c) => "Your thirst is yours, my water is MINE.",
        };
        private static readonly Random rng = new Random();
        private static List<int> rngSequence = null;
        private static int rngIndex = 0;

        public static bool Start(GameObject leader, GameObject companion) {
            // Shuffle remarks instead of selecting randomly each time. Because pseudo-random is only pseudo-fun.
            if (rngSequence == null || rngIndex >= rngSequence.Count) {
                rngSequence = Enumerable.Range(0, REMARKS.Count).OrderBy(a => rng.Next()).ToList();
                rngIndex = 0;
            }

            Utility.MaybeLog(string.Join(", ", rngSequence) + " [" + rngIndex + "] ");
            string remark = Options.ShowSillyText ? REMARKS[rngSequence[rngIndex]](leader, companion) : "";
            rngIndex++;


            return CleverGirl_BasicMenu.Start(leader, companion, OPTIONS,
                                              Title: companion.ShortDisplayName,
                                              Intro: remark,
                                              centerIntro: true,
                                              defaultSelected: 1,  // I like having manage gear as default option
                                              IntroIcon: companion.RenderForUI(),
                                              AllowEscape: true);
        }
    }
}
