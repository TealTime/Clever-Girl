namespace XRL.World.Parts {
    using ConsoleLib.Console;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.AI.GoalHandlers;
    using XRL.World.CleverGirl;
    using XRL.World.CleverGirl.NativeCodeOverloads;
    using XRL.World.CleverGirl.BackwardsCompatibility;
    using XRL.World.Anatomy;
    using XRL.UI;
    using Qud.API;

    [Serializable]
    public class CleverGirl_AIPickupGear : CleverGirl_INoSavePart {
        public static string PROPERTY => "CleverGirl_AIPickupGear";
        public static string IGNOREDBODYPARTS_PROPERTY => PROPERTY + "_IgnoredBodyParts";
        public override void Register(GameObject Object) {
            _ = Object.SetIntProperty(PROPERTY, 1);
            if (!Object.HasStringProperty(IGNOREDBODYPARTS_PROPERTY)) {
                Object.SetStringProperty(IGNOREDBODYPARTS_PROPERTY, "");
            }
        }
        public override void Remove() {
            ParentObject.RemoveIntProperty(PROPERTY);
            ParentObject.RemoveStringProperty(IGNOREDBODYPARTS_PROPERTY);
        }

        // TODO: Determine whether this is actually really inefficient/unoptimized, or if I'm just falling into the root of all evil.
        public List<int> IgnoredBodyPartIDs {
            get => ParentObject.GetStringProperty(IGNOREDBODYPARTS_PROPERTY).Split(',').Where(s => !s.IsNullOrEmpty()).Select(int.Parse).ToList();
            set => ParentObject.SetStringProperty(IGNOREDBODYPARTS_PROPERTY, string.Join(',', value));
        }

        // InventoryAction options
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Manage Gear Pickup",
            Display = "manage g{{inventoryhotkey|E}}ar auto-pickup",
            Command = "CleverGirl_ManageGearPickup",
            Key = 'E',
            Valid = Utility.InventoryAction.Adjacent,
        };

        // OptionAction options
        public static readonly Utility.OptionAction ENABLE = new Utility.OptionAction {
            Name = "Clever Girl - Enable Gear Pickup",
            Display = "{{y|[ ]}} auto {{hotkey|p}}ickup/equip gear",
            ActionCall = (leader, companion) => SetAutoPickupGear(companion, true),
            Key = 'p',
            Valid = (leader, companion) => leader == The.Player && !companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction DISABLE = new Utility.OptionAction {
            Name = "Clever Girl - Disable Gear Pickup",
            Display = "{{W|[þ]}} auto {{hotkey|p}}ickup/equip gear",
            ActionCall = (leader, companion) => SetAutoPickupGear(companion, false),
            Key = 'p',
            Valid = (leader, companion) => leader == The.Player && companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_ENABLE = new Utility.OptionAction {
            Name = "Clever Girl - Enable Follower Gear Pickup",
            Display = "{{y|[ ]}} auto pickup/equip gear ({{hotkey|f}}ollowers)",
            ActionCall = (leader, companion) => SetFollowerAutoPickupGear(companion, true),
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => !obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_DISABLE = new Utility.OptionAction {
            Name = "Clever Girl - Disable Follower Gear Pickup",
            Display = "{{W|[þ]}} auto pickup/equip gear ({{hotkey|f}}ollowers)",
            ActionCall = (leader, companion) => SetFollowerAutoPickupGear(companion, false),
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            InvalidBehavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction AUTO_EQUIP_BEHAVIOR = new Utility.OptionAction {
            Name = "Clever Girl - Specify Forbidden Equipment Slots",
            Display = "{{hotkey|s}}pecify forbidden equipment slots",
            ActionCall = (leader, companion) => SpecifyForbiddenEquipmentSlots(companion),
            Key = 's',
            Valid = (leader, companion) => leader == The.Player && companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            InvalidBehavior = Utility.InvalidOptionBehavior.DARKEN,
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

        public static readonly List<Utility.OptionAction> MANAGE_MENU_OPTIONS = new List<Utility.OptionAction> {
            ENABLE,
            DISABLE,
            FOLLOWER_ENABLE,
            FOLLOWER_DISABLE,
            AUTO_EQUIP_BEHAVIOR,
        };

        public override bool WantTurnTick() => true;

        public override void TurnTick(long TurnNumber) {
            if (ParentObject.IsBusy()) {
                return;
            }

            if (ParentObject.IsPlayer()) {
                return;
            }

            Utility.MaybeLog("Turn " + TurnNumber);

            // Primary weapon
            if (ParentObject.IsCombatObject() &&
                FindBetterThing("MeleeWeapon",
                                go => go.HasTag("MeleeWeapon"),
                                new Brain.WeaponSorter(ParentObject),
                                (part, thing) => part.Primary && part.Type == thing.GetPart<MeleeWeapon>()?.Slot)) {
                return;
            }

            var currentShield = ParentObject.Body.GetShield();
            if (ParentObject.HasSkill("Shield")) {
                Utility.MaybeLog("Considering shields");
                // manually compare to our current best shield since the WornOn's might not match
                if (FindBetterThing("Shield",
                                    go => go.HasTag("Shield") && Brain.CompareShields(go, currentShield, ParentObject) < 0,
                                    new ShieldSorter(ParentObject),
                                    (part, thing) => !part.Primary && part.Type == thing.GetPart<Shield>()?.WornOn)) {
                    // hack because the game's reequip logic doesn't consider better shields
                    if (currentShield?.TryUnequip() == true) {
                        EquipmentAPI.DropObject(currentShield);
                    }
                    return;
                }
            }

            // Armor
            if (FindBetterThing("Armor",
                                _ => true,
                                new Brain.GearSorter(ParentObject),
                                (part, thing) => part.Type == thing.GetPart<Armor>()?.WornOn)) {
                return;
            }

            // Additional weapons
            if (ParentObject.IsCombatObject() &&
                FindBetterThing("MeleeWeapon",
                                go => go.HasTag("MeleeWeapon"),
                                new Brain.WeaponSorter(ParentObject),
                                (part, thing) => !part.Primary &&
                                                 part.Equipped != currentShield &&
                                                 part.Type == thing.GetPart<MeleeWeapon>()?.Slot)) {
            }
        }

        private bool FindBetterThing(string SearchPart,
                                     Func<GameObject, bool> whichThings,
                                     Comparer<GameObject> thingComparer,
                                     Func<BodyPart, GameObject, bool> whichBodyParts) {
            var allBodyParts = ParentObject.Body.GetParts();
            var currentCell = ParentObject.CurrentCell;

            // total carry capacity if we dropped everything in our inventory
            var capacity = ParentObject.GetMaxCarriedWeight() - ParentObject.Body.GetWeight();
            // if we're already overburdened by just our equipment, nothing to do
            if (capacity < 0) {
                return false;
            }

            var things = currentCell.ParentZone
                .FastFloodVisibility(currentCell.X, currentCell.Y, 30, SearchPart, ParentObject)
                .Where(whichThings)
                .Where(go => ParentObject.HasLOSTo(go))
                .ToList();
            if (things.Count == 0) {
                Utility.MaybeLog("No " + SearchPart + "s");
                return false;
            }

            // consider items in our inventory too in case PerformReequip isn't equipping something
            // we think it should
            things.AddRange(ParentObject.Inventory.Objects.Where(whichThings));
            things.Sort(thingComparer);

            var noEquip = ParentObject.GetPropertyOrTag("NoEquip");
            var noEquipList = string.IsNullOrEmpty(noEquip) ? null : new List<string>(noEquip.CachedCommaExpansion());
            var ignoreParts = new List<BodyPart>();

            foreach (var thing in things) {
                if (noEquipList?.Contains(thing.Blueprint) ?? false) {
                    continue;
                }
                if (thing.HasPropertyOrTag("NoAIEquip")) {
                    continue;
                }
                foreach (var bodyPart in allBodyParts) {
                    if (!whichBodyParts(bodyPart, thing) || ignoreParts.Contains(bodyPart)) {
                        continue;
                    }
                    if (!(bodyPart.Equipped?.FireEvent("CanBeUnequipped") ?? true)) {
                        Utility.MaybeLog("Can't unequip the " + bodyPart.Equipped.DisplayNameOnlyStripped);
                        continue;
                    }
                    if (thing.pPhysics.InInventory != ParentObject && thing.WeightEach - (bodyPart.Equipped?.Weight ?? 0) > capacity) {
                        Utility.MaybeLog("No way to equip " + thing.DisplayNameOnlyStripped + " on " + bodyPart.Name + " without being overburdened");
                        continue;
                    }
                    if (thing.HasPart(typeof(Cursed))) {
                        // just say no to the amaranthine prism
                        continue;
                    }
                    if (thingComparer.Compare(thing, bodyPart.Equipped) < 0) {
                        if (IgnoredBodyPartIDs.Contains(bodyPart.ID)) {
                            Utility.MaybeLog("Ignoring " + thing.DisplayNameOnlyStripped + " even though its better than my " +
                                (bodyPart.Equipped?.DisplayNameOnlyStripped ?? "nothing") + " because I'm forbidden to reequip my " + bodyPart.Name);
                            continue;
                        }
                        if (thing.pPhysics.InInventory == ParentObject) {
                            Utility.MaybeLog(thing.DisplayNameOnlyStripped + " in my inventory is already better than my " +
                                (bodyPart.Equipped?.DisplayNameOnlyStripped ?? "nothing"));
                            ignoreParts.Add(bodyPart);
                            continue;
                        }
                        GoGet(thing);
                        return true;
                    }
                }
            }
            return false;
        }

        private void GoGet(GameObject item) {
            ParentObject.pBrain.Think("I want that " + item.DisplayNameOnlyStripped);
            _ = ParentObject.pBrain.PushGoal(new CleverGirl_GoPickupGear(item));
            _ = ParentObject.pBrain.PushGoal(new MoveTo(item.CurrentCell));
        }

        /// <summary>
        /// why doesn't Brain have this? 😭
        /// </summary>
        private class ShieldSorter : Comparer<GameObject> {
            private readonly GameObject POV;
            private readonly bool Reverse;

            public ShieldSorter(GameObject POV) {
                this.POV = POV;
            }

            public ShieldSorter(GameObject POV, bool Reverse)
                : this(POV) {
                this.Reverse = Reverse;
            }

            public override int Compare(GameObject x, GameObject y) {
                return Brain.CompareShields(x, y, POV) * (Reverse ? -1 : 1);
            }
        }

        /// <summary>
        /// Handle the interactive menu for managing companion auto-equip on a per-BodyPart basis.
        /// </summary>
        /// <returns>
        /// boolean flag to indicate that energy was spent talking with companion (true), or not (false)
        /// </returns>
        public static bool Manage(GameObject leader, GameObject companion) {

            var optionNames = new List<string>(MANAGE_MENU_OPTIONS.Count);
            var optionHotkeys = new List<char>(MANAGE_MENU_OPTIONS.Count);
            var optionValidities = new List<bool>(MANAGE_MENU_OPTIONS.Count);
            var filteredOptions = new List<Utility.OptionAction>(MANAGE_MENU_OPTIONS.Count);

            void SetupOptions(GameObject _leader, 
                              GameObject _companion, 
                              ref List<Utility.OptionAction> _filteredOptions, 
                              ref List<string> _optionNames, 
                              ref List<char> _optionHotkeys, 
                              ref List<bool> _optionValidities) {

                // Filter options, keeping only those that are valid or NOT set to HIDE behavior
                filteredOptions = MANAGE_MENU_OPTIONS.Where(o => 
                    o.Valid(leader, companion) ||
                    o.InvalidBehavior != Utility.InvalidOptionBehavior.HIDE).ToList();

                _optionNames.Clear();
                _optionHotkeys.Clear();
                _optionValidities.Clear();

                // Populate option information lists
                foreach (var option in _filteredOptions) {
                    string finalName = option.Display;

                    // Handle options with special behaviors
                    if (!option.Valid(leader, companion)) {
                        _optionValidities.Add(false);
                        if (option.InvalidBehavior == Utility.InvalidOptionBehavior.DARKEN) {
                            finalName = "&K" + ColorUtility.StripFormatting(finalName);
                        }
                    } else {
                        _optionValidities.Add(true);
                    }

                    _optionNames.Add(finalName);                
                    _optionHotkeys.Add(option.Key);                
                }
            }

            bool changed = false;
            // Gear management menu loop
            while (true) {
                SetupOptions(leader, companion, ref filteredOptions, ref optionNames, ref optionHotkeys, ref optionValidities);
                var index = Popup.ShowOptionList(Title: "Gear Management",
                                                 Options: optionNames.ToArray(),
                                                 Hotkeys: optionHotkeys.ToArray(),
                                                 Intro: companion.the + companion.ShortDisplayName,
                                                 centerIntro: true,
                                                 AllowEscape: true);
                
                // User cancelled, abort!
                if (index < 0) {
                    break;
                } else if (optionValidities[index]) {
                    changed |= filteredOptions[index].ActionCall(leader, companion);
                }
            }

            return changed;
        }

        private static bool SetAutoPickupGear(GameObject target, bool value) {
            bool wasEnabled = target.HasPart(typeof(CleverGirl_AIPickupGear));
            if (value) {
                _ = target.RequirePart<CleverGirl_AIPickupGear>();
                _ = target.RequirePart<CleverGirl_AIUnburden>(); // Anyone picking up gear should know how to unburden themself.
                return wasEnabled == false;  // If it was disabled prior: it must have changed, so expend a turn.
            } else {
                target.RemovePart<CleverGirl_AIPickupGear>();
                target.RemovePart<CleverGirl_AIUnburden>();
                return wasEnabled == true;  // If it was enabled prior: it must have changed, so expend a turn.
            }
        }

        private static bool SetFollowerAutoPickupGear(GameObject target, bool value) {
            bool changed = false;
            foreach (var follower in Utility.CollectFollowersOf(target)) {
                changed |= SetAutoPickupGear(follower, value);
            }
            return changed;
        }

        public static bool SpecifyForbiddenEquipmentSlots(GameObject companion) {
            Utility.MaybeLog("Managing auto-equip for " + companion.DisplayNameOnlyStripped);

            // Double check whether or not the companion still has auto pickup enabled to avoid a potential NullReferenceException below
            if (!companion.HasPart(typeof(CleverGirl_AIPickupGear))) {
                Utility.MaybeLog("Companion doesn't have auto pickup enabled, yet the user is trying to change auto pickup behavior. This is likely a bug and shouldn't happen. Aborting!");
                return false;
            }

            var allBodyParts = companion.Body.GetParts();
            var optionNames = new List<string>(allBodyParts.Count);
            var optionHotkeys = new List<char>(allBodyParts.Count);
            var initialSelections = new List<int>(allBodyParts.Count);

            // Grab initial menu selection state from stored ID's
            int bodyPartIndex = 0;
            var tempStoredIDs = companion.GetPart<CleverGirl_AIPickupGear>().IgnoredBodyPartIDs;
            var invalidBodyPartIDs = new List<int>();
            foreach (var part in allBodyParts) {
                // Check if part is equipable in case of fungal infections, horns, TrueKin zoomy tank feet, etc
                if (!(part.Equipped?.FireEvent("CanBeUnequipped") ?? true)) {
                    Utility.MaybeLog(part.ID + " Can't be unequipped");
                    invalidBodyPartIDs.Add(part.ID);  // Add to the list so it appears as an immutable option later on

                    // If it's also stored: wipe it. Can happen if an already forbidden companion part becomes immutable.
                    if (tempStoredIDs.Contains(part.ID)) {
                        Utility.MaybeLog(part.ID + " Can't be unequipped AND it's ignored!");
                        tempStoredIDs.RemoveAll(storedID => part.ID == storedID);
                        companion.GetPart<CleverGirl_AIPickupGear>().IgnoredBodyPartIDs = tempStoredIDs;
                    }
                } else {
                    if (tempStoredIDs.Contains(part.ID)) {
                        initialSelections.Add(bodyPartIndex);
                    }
                }
                bodyPartIndex++;
            }
            
            // Show currently equipped items in options
            foreach (var part in allBodyParts) {
                bool invalid = invalidBodyPartIDs.Contains(part.ID); 
                string primary = CleverGirl_BackwardsCompatibility.IsPreferredPrimary(part) ? "{{g|[*]}}" : "";
                string equipped = part.Equipped?.ShortDisplayName ?? "{{k|[empty]}}";
                // TODO: make invalid options grey or something
                optionNames.Add(part.Name + " : " + primary + " " + equipped);
                optionHotkeys.Add(optionHotkeys.Count >= 26 ? ' ' : (char)('a' + optionHotkeys.Count));
            }

            // Predicate that will be called directly after every option selection. Returning false stops the selection.
            Predicate<int> CheckIfChoiceIsValid = delegate(int index) {
                if (index >= 0 && index < allBodyParts.Count) {
                    Utility.MaybeLog(allBodyParts[index].ID + " : " + invalidBodyPartIDs.Contains(allBodyParts[index].ID) + " [" + string.Join(", ", allBodyParts.Select(p => p.ID)) + "] -> " + string.Join(", ", invalidBodyPartIDs));
                    return !invalidBodyPartIDs.Contains(allBodyParts[index].ID);
                }
                Utility.MaybeLog("Invalid choice index " + index);
                return true;
            };

            // Pop up a menu for the player to checklist body parts
            var enumerableMenu = CleverGirl_Popup.YieldSeveral(
                Title: "Auto Equip Behavior",
                Options: optionNames.ToArray(),
                Hotkeys: optionHotkeys.ToArray(),
                OnPost: CheckIfChoiceIsValid,
                Intro: companion.the + companion.ShortDisplayName + "\nSelect forbidden auto equipment slots",
                CenterIntro: true,
                AllowEscape: true,
                InitialSelections: initialSelections
            );

            bool changed = false;
            foreach (CleverGirl_Popup.YieldResult result in enumerableMenu)
            {
                if (result.index >= allBodyParts.Count) {
                    Utility.MaybeLog("Selecting body part outside of acceptable range! Abort!");
                    return false;
                }

                int partID = allBodyParts[result.index].ID;
                if (result.value) {
                    if (!tempStoredIDs.Contains(partID)) {
                        Utility.MaybeLog("Adding ID: " + partID + " to " + string.Join(",", tempStoredIDs));
                        tempStoredIDs.Add(partID);
                        changed = true;
                    }
                } else {
                    if (tempStoredIDs.Contains(partID)) {
                        Utility.MaybeLog("Removing ID: " + partID + " in " + string.Join(",", tempStoredIDs));
                        tempStoredIDs.RemoveAll(storedID => partID == storedID);
                        changed = true;
                    }
                }
                if (changed) {
                    companion.GetPart<CleverGirl_AIPickupGear>().IgnoredBodyPartIDs = tempStoredIDs;
                }
            }

            Utility.MaybeLog("IgnoredBodyPartIDs: " + companion.GetPart<CleverGirl_AIPickupGear>().ParentObject.GetStringProperty(IGNOREDBODYPARTS_PROPERTY));

            return changed;

        }
    }
}
