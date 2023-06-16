namespace CleverGirl.Parts {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL;
    using XRL.World;
    using XRL.World.Skills;
    using CleverGirl;
    using CleverGirl.Menus;
    using CleverGirl.Menus.Overloads;
    using Options = Globals.Options;

    [Serializable]
    public class CleverGirl_AIManageSkills : CleverGirl_INoSavePart {
        public static readonly Utility.OptionAction ACTION = new Utility.OptionAction {
            Name = "Clever Girl - Manage Skills",
            Display = "manage {{hotkey|s}}kills",
            Command = "CleverGirl_ManageSkills",
            Key = 's',
            Valid = (leader, companion) => companion.PartyLeader == The.Player,
        };
        public static string PROPERTY => "CleverGirl_AIManageSkills";
        public static string LEARNINGSKILLS_PROPERTY => PROPERTY + "_LearningSkills";
        public override void Register(GameObject Object) {
            _ = Object.SetIntProperty(PROPERTY, 1);
            if (!Object.HasStringProperty(LEARNINGSKILLS_PROPERTY)) {
                Object.SetStringProperty(LEARNINGSKILLS_PROPERTY, "");
            }
        }
        public override void Remove() {
            ParentObject.RemoveIntProperty(PROPERTY);
            ParentObject.RemoveStringProperty(LEARNINGSKILLS_PROPERTY);
        }
        public List<string> LearningSkills {
            get => ParentObject.GetStringProperty(LEARNINGSKILLS_PROPERTY).Split(',').Where(s => !s.IsNullOrEmpty()).ToList();
            set => ParentObject.SetStringProperty(LEARNINGSKILLS_PROPERTY, string.Join(",", value));
        }

        /// <summary>
        /// these skills don't make sense for companions
        /// </summary>
        public static readonly HashSet<string> IgnoreSkills = new HashSet<string>{
            "Cooking and Gathering",
            "Customs and Folklore",
            "Tinkering",
            "Wayfaring",
            "Set Limb",
            "Fasting Way",
            "Mind over Body",
            "Nonlinearity",
        };

        public static readonly HashSet<string> CombatSkills = new HashSet<string>{
            "Axe",
            "Bow and Rifle",
            "Cudgel",
            "Dual Wield",
            "Heavy Weapon",
            "Long Blade",
            "Pistol",
            "Short Blade",
            "Menacing Stare",
            "Intimidate",
            "Berate",
            "Shield Slam",
            "Deft Throwing",
            "Charge",
        };

        public override bool WantEvent(int ID, int cascade) => ID == StatChangeEvent.ID;

        public override bool HandleEvent(StatChangeEvent E) {
            if (E.Name == "SP") {
                SpendSP();
            }
            return true;
        }

        public void SpendSP() {
            var stat = ParentObject.Statistics["SP"];
            var budget = stat.Value;
            var pool = new List<Tuple<string, int, string>>();
            var toDrop = new List<string>();
            foreach (var skillName in LearningSkills) {
                var skill = SkillFactory.Factory.SkillList[skillName];
                var hasAllPowers = true;
                if (ParentObject.HasSkill(skill.Class)) {
                    foreach (var power in skill.Powers.Values) {
                        if (!ParentObject.HasSkill(power.Class) && !IgnoreSkills.Contains(power.Name)) {
                            if (!ParentObject.IsCombatObject() && CombatSkills.Contains(power.Name)) {
                                continue;
                            }
                            hasAllPowers = false;
                            if (power.Cost > budget) {
                                continue;
                            }
                            if (!power.MeetsRequirements(ParentObject)) {
                                continue;
                            }
                            pool.Add(Tuple.Create(power.Class, power.Cost, power.Name));
                        }
                    }
                } else {
                    hasAllPowers = false;
                    if (skill.Cost <= budget) {
                        var canLearnSkill = skill.MeetsRequirements(ParentObject);
                        foreach (var power in skill.Powers.Values.Where(p => p.Cost == 0)) {
                            if (!power.MeetsRequirements(ParentObject)) {
                                canLearnSkill = false;
                            }
                        }
                        if (canLearnSkill) {
                            pool.Add(Tuple.Create(skill.Class, skill.Cost, skill.Name));
                        }
                    }
                }
                if (hasAllPowers) {
                    // Dont set 'changed' for this as it shouldn't punish the player
                    _ = Utility.EditStringPropertyCollection(ParentObject, LEARNINGSKILLS_PROPERTY, skillName, false);
                }
            }

            if (0 < pool.Count) {
                var which = pool.GetRandomElement(Utility.Random(this));
                ParentObject.AddSkill(which.Item1);

                DidX("learn", which.Item3, "!", ColorAsGoodFor: ParentObject);
                if (LearningSkills.Contains(which.Item1)) {
                    // learned the skill, will also automatically learns free powers
                    var skill = SkillFactory.Factory.SkillList[which.Item1];
                    foreach (var power in skill.Powers.Values.Where(p => p.Cost == 0)) {
                        DidX("learn", power.Name, "!", ColorAsGoodFor: ParentObject);
                    }
                }

                stat.Penalty += which.Item2; // triggers a StatChangeEvent which will call this again until all points are spent
            }
        }

        /// <summary>
        /// Start the Manage Skills Menu.
        ///
        /// <example>
        /// Note to future maintainers who may or may not be as confused as me on this subject: 
        /// The difference between a Skill and a Power is illustrated as such:
        ///
        /// [100] Skill      <-- Skill == the TOP level thing you'd buy with SP
        ///     [150] Power  <-- Power == the SUB level thing you'd buy with SP
        ///     [250] Power
        /// </example>
        /// </summary
        public bool ManageSkillsMenu() {
            var learnableSkills = new List<string>(SkillFactory.Factory.SkillList.Count);
            var options = new List<MenuComponents.Option>(SkillFactory.Factory.SkillList.Count);

            // Traverse all top-level skills (IE: Axe, Tactics, Acrobatics) 
            var suffixes = new List<string>(SkillFactory.Factory.SkillList.Values.Count);
            foreach (var skill in SkillFactory.Factory.SkillList.Values) {
                if (IgnoreSkills.Contains(skill.Name)) {
                    continue;
                }
                if (!ParentObject.IsCombatObject() && CombatSkills.Contains(skill.Name)) {
                    continue;
                }
                learnableSkills.Add(skill.Name);
                var canLearnSkill = skill.MeetsRequirements(ParentObject);
                int learnedPowers = 0;
                int unavailablePowers = 0;
                int totalPowers = 0;
                // Traverse all powers within a skill (IE: Hurdle, Charge, Shield Slam) 
                foreach (var Power in skill.Powers.Values) {
                    if (Power.Cost == 0 && !Power.MeetsRequirements(ParentObject)) {
                        canLearnSkill = false;
                    }
                    if (IgnoreSkills.Contains(Power.Name)) {
                        continue;
                    }
                    if (!ParentObject.IsCombatObject() && CombatSkills.Contains(Power.Name)) {
                        continue;
                    }
                    if (ParentObject.HasSkill(Power.Class)) {
                        ++learnedPowers;
                    } else if (!Power.MeetsRequirements(ParentObject)) {
                        ++unavailablePowers;
                    }
                    ++totalPowers;
                }

                if (!canLearnSkill && !ParentObject.HasSkill(skill.Class)) {
                    unavailablePowers = totalPowers - learnedPowers;
                }

                // Do some quick maths and save generated string token into list for future alignment/formatting
                int availablePowers = totalPowers - unavailablePowers;
                string text = string.Format("[{0}/{1}] {2}", learnedPowers, availablePowers, "{{Y|" + skill.Name + "}}");
                string need = (unavailablePowers > 1) ? "need" : "needs";
                suffixes.Add(unavailablePowers == 0 ? "" : "{{r|[-" + unavailablePowers + "] (stats unmet)}}");

                options.Add(new MenuComponents.Option(Name: text,
                                                      Hotkey: Utility.GetCharInAlphabet(options.Count),
                                                      Locked: learnedPowers >= totalPowers,
                                                      Selected: LearningSkills.Contains(skill.Name)));
            }

            // Pad spacing for string tokens to align vertically to the right of longest name.
            if (Utility.PadTwoCollections(options.Select(o => o.Name).ToList(), suffixes, out List<string> paddedNames)) {
                for (int i = 0; i < options.Count; i++) {
                    options[i].Name = paddedNames[i];
                }
            }

            // Start the menu
            var yieldedResults = CleverGirl_Popup.YieldSeveral(
                Title: ParentObject.the + ParentObject.ShortDisplayName,
                Intro: Options.ShowSillyText ? "Which skills should I learn?" : "Select skill focus.",
                Options: options.Select(o => o.Name).ToArray(),
                Hotkeys: options.Select(o => o.Hotkey).ToArray(),
                CenterIntro: true,
                IntroIcon: ParentObject.RenderForUI(),
                AllowEscape: true,
                InitialSelections: Enumerable.Range(0, options.Count).Where(i => options[i].Selected).ToArray(),
                LockedOptions: Enumerable.Range(0, options.Count).Where(i => options[i].Locked).ToArray()
            );

            // Process selections as they happen until menu is closed
            var changed = false;
            foreach (CleverGirl_Popup.YieldResult result in yieldedResults) {
                changed |= Utility.EditStringPropertyCollection(ParentObject,
                                                                LEARNINGSKILLS_PROPERTY,
                                                                learnableSkills[result.Index],
                                                                result.Value);
            }

            // If not learning any skills, stop listening for events
            if (LearningSkills.Count == 0) {
                ParentObject.RemovePart<CleverGirl_AIManageSkills>();
                foreach (var follower in Utility.CollectFollowersOf(ParentObject)) {
                    follower.RemovePart<CleverGirl_AIManageSkills>();
                }
                return changed;
            }

            // spend any skill points we have saved up
            SpendSP();
            foreach (var follower in Utility.CollectFollowersOf(ParentObject)) {
                var part = follower.RequirePart<CleverGirl_AIManageSkills>();
                part.LearningSkills = LearningSkills;
                part.SpendSP();
            }

            return changed;
        }
    }
}
