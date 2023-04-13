namespace CleverGirl.Parts {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CleverGirl;
    using CleverGirl.Menus;
    using CleverGirl.Menus.Overloads;
    using XRL;
    using XRL.World;
    using Options = Globals.Options;

    [Serializable]
    public class CleverGirl_AIManageAttributes : CleverGirl_INoSavePart {
        public static readonly Utility.OptionAction ACTION = new Utility.OptionAction {
            Name = "Clever Girl - Manage Attributes",
            Display = "manage {{hotkey|a}}ttributes",
            Command = "CleverGirl_ManageAttributes",
            Key = 'a',
            Valid = (leader, companion) => companion.PartyLeader == The.Player,
        };
        public static readonly Dictionary<string, string> Comparatives = new Dictionary<string, string>{
            {"Strength", "stronger"},
            {"Agility", "quicker"},
            {"Toughness", "tougher"},
            {"Intelligence", "smarter"},
            {"Willpower", "stronger-willed"},
            {"Ego", "more compelling"}
        };

        public static readonly Dictionary<string, string[]> Categories = new Dictionary<string, string[]>{
            {"Strength", new string[]{"feeble", "weak", "average", "strong", "beefy", "heckin' swole"}},
            {"Agility", new string[]{"ponderous", "slow", "average", "quick", "olympian", "sonic fast"}},
            {"Toughness", new string[]{"frail", "vulnerable", "average", "tough", "tanky", "slug sponge"}},
            {"Intelligence", new string[]{"incompetent", "dull", "average", "smart", "brilliant", "galaxy brain"}},
            {"Willpower", new string[]{"pushover", "gullible", "average", "strong-willed", "stalwart", "indefatigable"}},
            {"Ego", new string[]{"intolerable", "abrasive", "average", "compelling", "magnificent", "deific"}}
        };

        public static readonly string[] CategoryColors = new string[] { "dark red", "red", "gray", "green", "orange", "extradimensional" };

        public static readonly List<string> Attributes = new List<string> { "Strength", "Agility", "Toughness", "Intelligence", "Willpower", "Ego" };

        public static string PROPERTY => "CleverGirl_AIManageAttributes";
        public static string HONINGATTRIBUTES_PROPERTY => PROPERTY + "_HoningAttributes";
        public override void Register(GameObject Object) {
            _ = Object.SetIntProperty(PROPERTY, 1);
            if (!Object.HasStringProperty(HONINGATTRIBUTES_PROPERTY)) {
                Object.SetStringProperty(HONINGATTRIBUTES_PROPERTY, "");
            }
        }
        public override void Remove() {
            ParentObject.RemoveIntProperty(PROPERTY);
            ParentObject.RemoveStringProperty(HONINGATTRIBUTES_PROPERTY);
        }
        public List<string> HoningAttributes {
            get => ParentObject.GetStringProperty(HONINGATTRIBUTES_PROPERTY).Split(',').Where(s => !s.IsNullOrEmpty()).ToList();
            set => ParentObject.SetStringProperty(HONINGATTRIBUTES_PROPERTY, string.Join(",", value));
        }

        public override bool WantEvent(int ID, int cascade) => ID == StatChangeEvent.ID;

        public override bool HandleEvent(StatChangeEvent E) {
            if (E.Name == "AP") {
                SpendAP();
            }
            return true;
        }

        public void SpendAP() {
            var apStat = ParentObject.Statistics["AP"];

            if (apStat.Value > 0 && HoningAttributes.Count > 0) {
                var which = HoningAttributes.GetRandomElement(Utility.Random(this));
                ++ParentObject.Statistics[which].BaseValue;
                ++apStat.Penalty;

                DidX("become", Comparatives[which], "!", ColorAsGoodFor: ParentObject);
            }
        }

        public bool ManageAttributesMenu() {
            var menuOptions = new List<MenuOption>(Attributes.Count);
            var initiallySelectedOptions = new List<int>(Attributes.Count);


            var suffixes = new List<string>();
            foreach (var attr in Attributes) {
                var value = ParentObject.Statistics[attr].Value;
                var bucket = value <= 6 ? 0 :
                             value <= 12 ? 1 :
                             value <= 17 ? 2 :
                             value <= 25 ? 3 :
                             value <= 35 ? 4 :
                                           5;

                suffixes.Add(" {{" + CategoryColors[bucket] + "|" + Categories[attr][bucket] + "}}");
                menuOptions.Add(new MenuOption(Name: "{{Y|" + attr + "}}",
                                               Hotkey: Utility.GetCharInAlphabet(menuOptions.Count),
                                               Selected: HoningAttributes.Contains(attr)));
            }

            // Pad spacing for string tokens to align vertically to the right of longest name.
            var paddedNames = Utility.PadTwoCollections(menuOptions.Select(o => o.Name).ToList(), suffixes);
            for (int i = 0; i < menuOptions.Count; i++) {
                menuOptions[i].Name = paddedNames[i];
            }

            // Start the menu
            var yieldedResults = CleverGirl_Popup.YieldSeveral(
                Title: ParentObject.the + ParentObject.ShortDisplayName,
                Intro: Options.ShowSillyText ? "Which attributes should I train to improve?" : "Select attribute focus.",
                Options: menuOptions.Select(o => o.Name).ToArray(),
                Hotkeys: menuOptions.Select(o => o.Hotkey).ToArray(),
                CenterIntro: true,
                IntroIcon: ParentObject.RenderForUI(),
                AllowEscape: true,
                InitialSelections: Enumerable.Range(0, menuOptions.Count).Where(i => menuOptions[i].Selected).ToArray()
            );

            // Process selections as they happen until menu is closed
            var changed = false;
            foreach (CleverGirl_Popup.YieldResult result in yieldedResults) {
                changed |= Utility.EditStringPropertyCollection(ParentObject,
                                                                HONINGATTRIBUTES_PROPERTY,
                                                                Attributes[result.Index],
                                                                result.Value);
            }

            return changed;
        }
    }
}
