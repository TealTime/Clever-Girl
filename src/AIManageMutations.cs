namespace CleverGirl.Parts {
    using HarmonyLib;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL;
    using XRL.UI;
    using XRL.World;
    using XRL.World.Parts;
    using XRL.World.Parts.Mutation;
    using CleverGirl;
    using CleverGirl.Menus;
    using CleverGirl.Menus.Overloads;
    using Options = Globals.Options;

    [Serializable]
    [HarmonyPatch]
    public class CleverGirl_AIManageMutations : CleverGirl_INoSavePart {
        public static readonly Utility.OptionAction ACTION = new Utility.OptionAction {
            Name = "Clever Girl - Manage Mutations",
            Display = "manage {{hotkey|m}}utations",
            Command = "CleverGirl_ManageMutations",
            Key = 'm',
            Valid = (leader, companion) => companion.PartyLeader == The.Player,
        };
        public static string PROPERTY => "CleverGirl_AIManageMutations";
        public static string FOCUSINGMUTATIONS_PROPERTY => PROPERTY + "_FocusingMutations";
        public static string WANTNEWMUTATIONS_PROPERTY => PROPERTY + "_WantNewMutations";
        public static string FOLLOWERSWANTNEWMUTATIONS_PROPERTY => PROPERTY + "_FollowersWantNewMutations";
        public static string NEWMUTATIONSAVINGS_PROPERTY => PROPERTY + "_NewMutationSavings";

        public override void Register(GameObject Object) {
            _ = Object.SetIntProperty(PROPERTY, 1);
            if (!Object.HasStringProperty(FOCUSINGMUTATIONS_PROPERTY)) {
                Object.SetStringProperty(FOCUSINGMUTATIONS_PROPERTY, "");
            }
            if (!Object.HasIntProperty(WANTNEWMUTATIONS_PROPERTY)) {
                _ = Object.SetIntProperty(WANTNEWMUTATIONS_PROPERTY, 0);
            }
            if (!Object.HasIntProperty(FOLLOWERSWANTNEWMUTATIONS_PROPERTY)) {
                _ = Object.SetIntProperty(FOLLOWERSWANTNEWMUTATIONS_PROPERTY, 0);
            }
            if (!Object.HasIntProperty(NEWMUTATIONSAVINGS_PROPERTY)) {
                _ = Object.SetIntProperty(NEWMUTATIONSAVINGS_PROPERTY, 0);
            }
        }
        public override void Remove() {
            ParentObject.RemoveIntProperty(PROPERTY);
            ParentObject.RemoveStringProperty(FOCUSINGMUTATIONS_PROPERTY);
            ParentObject.RemoveIntProperty(WANTNEWMUTATIONS_PROPERTY);
            ParentObject.RemoveIntProperty(FOLLOWERSWANTNEWMUTATIONS_PROPERTY);
            ParentObject.RemoveIntProperty(NEWMUTATIONSAVINGS_PROPERTY);
        }
        public List<string> FocusingMutations {
            get => ParentObject.GetStringProperty(FOCUSINGMUTATIONS_PROPERTY).Split(',').Where(s => !s.IsNullOrEmpty()).ToList();
            set => ParentObject.SetStringProperty(FOCUSINGMUTATIONS_PROPERTY, string.Join(",", value));
        }

        public bool WantNewMutations {
            get => ParentObject.GetIntProperty(WANTNEWMUTATIONS_PROPERTY) == 1;
            set => ParentObject.SetIntProperty(WANTNEWMUTATIONS_PROPERTY, value ? 1 : 0);
        }
        public bool FollowersWantNewMutations {
            get => ParentObject.GetIntProperty(FOLLOWERSWANTNEWMUTATIONS_PROPERTY) == 1;
            set => ParentObject.SetIntProperty(FOLLOWERSWANTNEWMUTATIONS_PROPERTY, value ? 1 : 0);
        }
        public int NewMutationSavings {
            get => ParentObject.GetIntProperty(NEWMUTATIONSAVINGS_PROPERTY);
            set => ParentObject.SetIntProperty(NEWMUTATIONSAVINGS_PROPERTY, value);
        }

        public static HashSet<string> CombatMutations = new HashSet<string>{
            "Corrosive Gas Generation",
            "Electromagnetic Pulse",
            "Flaming Ray",
            "Freezing Ray",
            "Horns",
            "Quills",
            "Sleep Gas Generation",
            "Slime Glands",
            "Spinnerets",
            "Stinger (Confusing Venom)",
            "Stinger (Paralyzing Venom)",
            "Stinger (Poisoning Venom)",
            "Burgeoning",
            "Confusion",
            "Cryokinesis",
            "Disintegration",
            "Force Wall",
            "Pyrokinesis",
            "Space-Time Vortex",
            "Stunning Force",
            "Sunder Mind",
            "Syphon Vim",
            "Teleport Other",
            "Time Dilation",
            "Temporal Fugue",
        };

        public override bool WantEvent(int ID, int cascade) => ID == StatChangeEvent.ID;

        public override bool HandleEvent(StatChangeEvent E) {
            if (E.Name == "MP") {
                SpendMP();
            }
            return true;
        }

        public void SpendMP() {
            var stat = ParentObject.Statistics["MP"];
            var budget = stat.Value - NewMutationSavings;
            if (budget <= 0) {
                // nothing to do
                return;
            }

            var canLevelMutations = new List<BaseMutation>();
            var maxLevelMutations = new List<string>();
            foreach (var mutationName in FocusingMutations) {
                var mutation = ParentObject.GetPart<Mutations>().GetMutation(mutationName);
                // TODO: Determine how this could. Maybe it's a mutation from another mod that was disabled? Check logs.
                if (mutation == null) {
                    Utility.MaybeLog(ParentObject.DisplayName + " is focusing " + mutationName + " but it doesn't own the mutation anymore?");
                    continue;
                }

                if (mutation.CanIncreaseLevel()) {
                    canLevelMutations.Add(mutation);
                } else if (!mutation.IsPhysical() && mutation.BaseLevel == mutation.GetMaxLevel()) {
                    maxLevelMutations.Add(mutationName);
                }
            }

            if (WantNewMutations) {
                // use null as a placeholder to save the MP
                canLevelMutations.Add(null);
            }

            // drop mutations that are fully leveled
            FocusingMutations = FocusingMutations.Except(maxLevelMutations).ToList();

            if (canLevelMutations.Count == 0) {
                // nothing to learn
                return;
            }

            var Random = Utility.Random(this);
            var targetMutation = canLevelMutations.GetRandomElement(Random);
            if (targetMutation == null) {
                ++NewMutationSavings;
                if (NewMutationSavings < 4) {
                    SpendMP(); // spend any additional MP if relevant
                } else {
                    Utility.MaybeLog("Learning a new mutation");
                    // learn a new mutation
                    var mutations = ParentObject.GetPart<Mutations>();
                    List<MutationEntry> possibleMutations;
                    var isFollower = ParentObject.PartyLeader.HasPart(nameof(CleverGirl_AIManageMutations));
                    if (isFollower) {
                        var myMutations = ParentObject.GetPart<Mutations>()
                                                      .MutationList
                                                      .ConvertAll(m => m.GetMutationEntry());
                        possibleMutations = ParentObject.PartyLeader.GetPart<Mutations>()
                                                                    .MutationList
                                                                    .Select(m => m.GetMutationEntry())
                                                                    .Where(m => !myMutations.Contains(m))
                                                                    .ToList()
                                                                    .Shuffle(Random);
                    } else {
                        possibleMutations = mutations.GetMutatePool()
                                                     .Shuffle(Random);
                    }
                    if (!ParentObject.IsCombatObject()) {
                        // don't offer combat mutations to NoCombat companions
                        possibleMutations = possibleMutations.Where(m => !CombatMutations.Contains(m.DisplayName))
                                                             .ToList();
                    }
                    var valuableMutations = possibleMutations.Where(m => m.Cost > 1);
                    var cheapMutations = possibleMutations.Where(m => m.Cost <= 1);
                    const int baseChoiceCount = 3;
                    var choiceCount = isFollower ? 1 : baseChoiceCount;
                    var choices = new List<BaseMutation>(choiceCount);
                    var optionNames = new List<string>(choiceCount);
                    var newPartIndex = ParentObject.IsChimera() ? Random.Next(baseChoiceCount) : -1;
                    // only offer valuable mutations if possible, but backfill with cheap ones
                    foreach (var mutationType in valuableMutations.Concat(cheapMutations)) {
                        var mutation = mutationType.CreateInstance();
                        var newPartString = choices.Count != newPartIndex ? "" : "{{G|+ grow a new body part}}";
                        choices.Add(mutation);
                        optionNames.Add("{{W|" + mutation.DisplayName + "}} " + newPartString +
                                        " {{y|- " + mutation.GetDescription() + "}}\n" + mutation.GetLevelText(1));
                        if (choices.Count == choiceCount) {
                            break;
                        }
                    }
                    if (choices.Count == 0) {
                        if (!isFollower) {
                            WantNewMutations = false;
                            NewMutationSavings = 0;
                            // spend our points if we can
                            SpendMP();
                        }
                        return;
                    }

                    var choice = isFollower ? 0 : -1;
                    while (-1 == choice) {
                        choice = Popup.ShowOptionList(Options: optionNames.ToArray(),
                                                      Spacing: 1,
                                                      Intro: "Choose a mutation for " + ParentObject.the + ParentObject.ShortDisplayName + ".",
                                                      MaxWidth: 78,
                                                      RespectOptionNewlines: true);
                    }

                    var result = choices[choice];
                    if (result.GetVariants() != null) {
                        // let companions choose their variant 😄
                        result.SetVariant(Random.Next(result.GetVariants().Count));
                    }
                    var mutationIndex = mutations.AddMutation(result, 1);
                    DidX("gain", mutations.MutationList[mutationIndex].DisplayName, "!", UsePopup: true, ColorAsGoodFor: ParentObject);
                    if (choice == newPartIndex) {
                        _ = mutations.AddChimericBodyPart();
                    }

                    if (ParentObject.UseMP(4)) {
                        NewMutationSavings -= 4;
                    }
                }
            } else {
                ParentObject.GetPart<Mutations>().LevelMutation(targetMutation, targetMutation.BaseLevel + 1);
                _ = ParentObject.UseMP(1);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseMutation), "RapidLevel")]
        public static void RapidLevelInstead(int __0, ref BaseMutation __instance) {
            // TODO: rename __0 back to Amount once the moon stair is stable
            var Amount = __0;

            // check if we're managing this creature
            var manageMutations = __instance.ParentObject.GetPart<CleverGirl_AIManageMutations>();

            if (manageMutations == null) {
                // do nothing otherwise
                return;
            }

            var whichKey = "RapidLevel_" + __instance.GetMutationClass();

            // pre-emptively reduce by the levels this mutation will gain
            _ = __instance.ParentObject.ModIntProperty(whichKey, -Amount);

            // pick an appropriate mutation instead
            var mutations = __instance.ParentObject.GetPart<Mutations>();
            var allPhysicalMutations = mutations.MutationList.Where(m => m.IsPhysical() && m.CanLevel())
                                                             .ToList()
                                                             .Shuffle(Utility.Random(manageMutations));
            var instead = allPhysicalMutations.Find(m => manageMutations.FocusingMutations.Contains(m.Name)) ??
                          allPhysicalMutations[0];
            var insteadKey = "RapidLevel_" + instead.GetMutationClass();
            manageMutations.DidX("rapidly advance",
                                 instead.DisplayName + " by " + XRL.Language.Grammar.Cardinal(Amount) + " ranks to rank " + (instead.Level + Amount),
                                 "!", ColorAsGoodFor: __instance.ParentObject);
            _ = __instance.ParentObject.ModIntProperty(insteadKey, Amount);

            Utility.MaybeLog("Moved a RapidLevel from " + whichKey + " to " + instead);
        }

        public bool ManageMutationsMenu() {
            var mutations = new List<string>();
            var menuOptions = new List<MenuOption>();
            var menuOptionTargetPropertyMap = new Dictionary<int, string>();

            // Create and format mutation options
            if (ParentObject.GetPart<Mutations>() is Mutations ownedMutations) {
                foreach (var mutation in ownedMutations.MutationList) {
                    mutations.Add(mutation.Name);
                    {
                        bool locked = false;
                        // physical mutations can RapidLevel, so can always be selected
                        bool canLevel = mutation.CanLevel();
                        bool maxLevel = mutation.BaseLevel >= mutation.GetMaxLevel() && !mutation.IsPhysical();
                        string lockReason = "";
                        if (!canLevel || maxLevel) {
                            locked = true;
                            lockReason = !canLevel ? "(fixed)" : (maxLevel ? "(maxed)" : "(???)");

                            // Make sure property isn't focused, mostly to ensure menu doesn't show a filled, yet locked, checkbox.
                            _ = Utility.EditStringPropertyCollection(ParentObject, FOCUSINGMUTATIONS_PROPERTY, mutation.Name, false);  // Dont set 'changed' for this as it shouldn't punish the player 
                        }

                        var levelAdjust = mutation.Level - mutation.BaseLevel;
                        var levelAdjustString = levelAdjust == 0 ? "" :
                                                                   levelAdjust < 0 ? "{{R|-" + (-levelAdjust) + "}}" :
                                                                                     "{{G|+" + levelAdjust + "}}";
                        var optionText = mutation.DisplayName + " (" + mutation.BaseLevel + levelAdjustString + ") " + lockReason;
                        menuOptions.Add(new MenuOption(Name: optionText,
                                                       Hotkey: Utility.GetCharInAlphabet(menuOptions.Count),
                                                       Locked: locked,
                                                       Selected: FocusingMutations.Contains(mutation.Name)));
                    }
                }
            }

            // Companion 'want new mutations'
            {
                bool locked = false;
                if (ParentObject.GetPart<Mutations>().GetMutatePool().Count == 0) {
                    locked = true;
                    WantNewMutations = false;
                }
                var option = new MenuOption(Name: "Acquire new mutations",
                                            Hotkey: Utility.GetCharInAlphabet(menuOptions.Count),
                                            Locked: locked,
                                            Selected: WantNewMutations);
                menuOptionTargetPropertyMap.Add(menuOptions.Count, WANTNEWMUTATIONS_PROPERTY);
                menuOptions.Add(option);
            }

            // Follower 'want new mutations'
            var followers = Utility.CollectFollowersOf(ParentObject);
            if (followers.Any()) {
                // This algorithm basically boils down to the following:
                // "If any followers do not have a mutation that their leader does have, then stop searching since there's atleast 1 mutation unlearned."
                {
                    bool locked = true;
                    foreach (var follower in followers) {
                        if (ParentObject.GetPart<Mutations>().MutationList.Any(m => !follower.GetPart<Mutations>().MutationList.Contains(m))) {
                            locked = false;
                            break;
                        }
                    }
                    var option = new MenuOption(Name: "Acquire new follower mutations",
                                                Hotkey: Utility.GetCharInAlphabet(menuOptions.Count),
                                                Locked: locked,
                                                Selected: FollowersWantNewMutations);
                    menuOptionTargetPropertyMap.Add(menuOptions.Count, FOLLOWERSWANTNEWMUTATIONS_PROPERTY);
                    menuOptions.Add(option);
                }
            }

            // Start the menu
            var enumerableMenu = CleverGirl_Popup.YieldSeveral(
                Title: ParentObject.the + ParentObject.ShortDisplayName,
                Intro: Options.ShowSillyText ? "Which mutations should I invest in?" : "Select mutation focus.",
                Options: menuOptions.Select(o => o.Name).ToArray(),
                Hotkeys: menuOptions.Select(o => o.Hotkey).ToArray(),
                CenterIntro: true,
                IntroIcon: ParentObject.RenderForUI(),
                AllowEscape: true,
                InitialSelections: menuOptions.FindAll(o => o.Selected).Select(o => menuOptions.IndexOf(o)).ToArray(),
                LockedOptions: menuOptions.FindAll(o => o.Locked).Select(o => menuOptions.IndexOf(o)).ToArray()
            );

            // Process selections as they happen until menu is closed
            var changed = false;
            foreach (CleverGirl_Popup.YieldResult result in enumerableMenu) {
                if (menuOptionTargetPropertyMap.TryGetValue(result.Index, out string targetPropertyName)) {
                    changed |= Utility.EditIntProperty(ParentObject, targetPropertyName, result.Value);
                } else {
                    if (result.Index >= mutations.Count) {
                        continue;
                    }
                    changed |= Utility.EditStringPropertyCollection(ParentObject, FOCUSINGMUTATIONS_PROPERTY, mutations[result.Index], result.Value);
                }
            }
            OnMenuExit();  // Spend MP and do some house keeping

            return changed;
        }

        public void OnMenuExit() {
            if (FocusingMutations.Count > 0 || WantNewMutations || FollowersWantNewMutations) {
                // spend any MP we have if relevant
                SpendMP();
                foreach (var follower in Utility.CollectFollowersOf(ParentObject)) {
                    var part = follower.RequirePart<CleverGirl_AIManageMutations>();
                    part.WantNewMutations = FollowersWantNewMutations;
                    part.FocusingMutations = FocusingMutations;
                    part.SpendMP();
                }
            } else {
                // don't bother listening if there's nothing to hear
                ParentObject.RemovePart<CleverGirl_AIManageMutations>();
                foreach (var follower in Utility.CollectFollowersOf(ParentObject)) {
                    follower.RemovePart<CleverGirl_AIManageMutations>();
                }
            }
        }
    }
}
