namespace XRL.World.Parts {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.UI;
    using XRL.World.Skills;
    using CleverGirl;

    [Serializable]
    public class CleverGirl_AIManageSkills : IPart {
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Manage Skills",
            Display = "manage s{{inventoryhotkey|k}}ills",
            Command = "CleverGirl_ManageSkills",
            Key = 'k',
            Valid = E => E.Object.PartyLeader == The.Player,
        };
        public static string PROPERTY => "CleverGirl_AIManageSkills";
        public static string LEARNINGSKILLS_PROPERTY => PROPERTY + "_LearningSkills";
        public override void Register(GameObject Object, IEventRegistrar Registrar) {
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
        public static HashSet<string> IgnoreSkills = new HashSet<string>{
            "Cooking and Gathering",
            "Customs and Folklore",
            "Tinkering",
            "Wayfaring",
            "Set Limb",
            "Fasting Way",
            "Mind over Body",
        };

        public static HashSet<string> CombatSkills = new HashSet<string>{
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
                    toDrop.Add(skillName);
                }
            }
            // drop skills that are already complete
            LearningSkills = LearningSkills.Except(toDrop).ToList();

            if (0 < pool.Count) {
                var which = pool.GetRandomElement(Utility.SeededRandom(this));
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

        public bool Manage() {
            var changed = false;
            var skills = new List<string>(SkillFactory.Factory.SkillList.Count);
            var strings = new List<string>(SkillFactory.Factory.SkillList.Count);
            var keys = new List<char>(SkillFactory.Factory.SkillList.Count);
            foreach (var Skill in SkillFactory.Factory.SkillList.Values) {
                if (IgnoreSkills.Contains(Skill.Name)) {
                    continue;
                }
                if (!ParentObject.IsCombatObject() && CombatSkills.Contains(Skill.Name)) {
                    continue;
                }
                skills.Add(Skill.Name);
                var canLearnSkill = Skill.MeetsRequirements(ParentObject);
                var havePowers = 0;
                var lockedPowers = 0;
                var totalPowers = 0;
                foreach (var Power in Skill.Powers.Values) {
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
                        ++havePowers;
                    } else if (!Power.MeetsRequirements(ParentObject)) {
                        ++lockedPowers;
                    }
                    ++totalPowers;
                }
                if (!canLearnSkill && !ParentObject.HasSkill(Skill.Class)) {
                    lockedPowers = totalPowers - havePowers;
                }
                var unlockedPowers = totalPowers - lockedPowers;
                var prefix = havePowers == totalPowers ? "*" : LearningSkills.Contains(Skill.Name) ? "+" : "-";
                var suffix = lockedPowers == 0 ? "" : "{{r| (" + lockedPowers + " locked)}}";
                strings.Add(prefix + " " + Skill.Name + ": " + havePowers + "/" + unlockedPowers + suffix);
                keys.Add(keys.Count >= 26 ? ' ' : (char)('a' + keys.Count));
            }

            while (true) {
                var index = Popup.ShowOptionList(Options: strings.ToArray(),
                                                Hotkeys: keys.ToArray(),
                                                Intro: "What skills should " + ParentObject.the + ParentObject.ShortDisplayName + " learn?",
                                                AllowEscape: true);
                if (index < 0) {
                    if (LearningSkills.Count == 0) {
                        // don't bother listening if there's nothing to hear
                        ParentObject.RemovePart<CleverGirl_AIManageSkills>();
                        foreach (var follower in Utility.CollectFollowersOf(ParentObject)) {
                            follower.RemovePart<CleverGirl_AIManageSkills>();
                        }
                    } else {
                        // spend any skill points we have saved up
                        SpendSP();
                        foreach (var follower in Utility.CollectFollowersOf(ParentObject)) {
                            var part = follower.RequirePart<CleverGirl_AIManageSkills>();
                            part.LearningSkills = LearningSkills;
                            part.SpendSP();
                        }
                    }
                    return changed;
                }
                if (strings[index][0] == '*') {
                    // ignore
                } else if (strings[index][0] == '-') {
                    // start learning this skill
                    var working = LearningSkills;
                    working.Add(skills[index]);
                    LearningSkills = working;

                    strings[index] = '+' + strings[index].Substring(1);
                    changed = true;
                } else if (strings[index][0] == '+') {
                    // stop learning this skill
                    var working = LearningSkills;
                    _ = working.Remove(skills[index]);
                    LearningSkills = working;

                    strings[index] = '-' + strings[index].Substring(1);
                    changed = true;
                }
            }
        }
    }
}
