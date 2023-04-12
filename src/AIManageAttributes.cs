namespace CleverGirl.Parts {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CleverGirl;
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
            var optionNames = new List<string>(Attributes.Count);
            var optionHotkeys = new List<char>(Attributes.Count);
            var initiallySelectedOptions = new List<int>(Attributes.Count);

            foreach (var attr in Attributes) {
                var value = ParentObject.Statistics[attr].Value;
                var bucket = value <= 6 ? 0 :
                             value <= 12 ? 1 :
                             value <= 17 ? 2 :
                             value <= 25 ? 3 :
                             value <= 35 ? 4 :
                                           5;

                optionNames.Add(attr + ": {{" + CategoryColors[bucket] + "|" + Categories[attr][bucket] + "}}");
                optionHotkeys.Add(optionHotkeys.Count >= 26 ? ' ' : (char)('a' + optionHotkeys.Count));
                if (HoningAttributes.Contains(attr)) {
                    int optionIndex = optionNames.Count - 1;  // index that this skill will have in the menu
                    initiallySelectedOptions.Add(optionIndex);
                }
            }

            // Start the menu
            var yieldedResults = CleverGirl_Popup.YieldSeveral(
                Title: ParentObject.the + ParentObject.ShortDisplayName,
                Intro: Options.ShowSillyText ? "Which attributes should invest my points in?" : "Select attribute focus.",
                Options: optionNames.ToArray(),
                Hotkeys: optionHotkeys.ToArray(),
                CenterIntro: true,
                IntroIcon: ParentObject.RenderForUI(),
                AllowEscape: true,
                InitialSelections: initiallySelectedOptions
            );

            // Process selections as they happen until menu is closed
            var changed = false;
            foreach (CleverGirl_Popup.YieldResult result in yieldedResults) {
                changed |= ModifyProperty(Attributes[result.Index], result.Value);
            }

            return changed;
        }

        /// <summary>
        /// Add or remove an element from a list property
        /// Probably be done in a type generic fashion but properties are being kinda nasty to me right now.
        /// </summary
        private bool ModifyProperty(string element, bool add) {
            // TODO: Make this generic as it's duplicated across 4 classes
            List<string> property = HoningAttributes;
            bool existedPrior = property.Contains(element);

            if (add && !existedPrior) {
                property.Add(element);
                HoningAttributes = property;
                return true;
            } else if (!add && existedPrior) {
                _ = property.Remove(element);
                HoningAttributes = property;
                return true;
            }

            return false;
        }
    }
}
