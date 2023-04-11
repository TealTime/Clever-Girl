namespace XRL.World.Parts {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.AI.GoalHandlers;
    using XRL.World.CleverGirl;
    using XRL.World.CleverGirl.Overloads;
    using XRL.World.CleverGirl.BackwardsCompatibility;
    using XRL.World.Anatomy;
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

        public bool SetAutoPickupGear(GameObject companion, bool value) {
            bool wasEnabled = companion.HasPart(typeof(CleverGirl_AIPickupGear));
            if (value) {
                _ = companion.RequirePart<CleverGirl_AIPickupGear>();
                _ = companion.RequirePart<CleverGirl_AIUnburden>(); // Anyone picking up gear should know how to unburden themself.
                return !wasEnabled;  // If it was disabled prior: it must have changed, so expend a turn.
            } else {
                companion.RemovePart<CleverGirl_AIPickupGear>();
                companion.RemovePart<CleverGirl_AIUnburden>();
                return wasEnabled;  // If it was enabled prior: it must have changed, so expend a turn.
            }
        }

        public bool SetFollowerAutoPickupGear(GameObject companion, bool value) {
            bool changed = false;
            foreach (var follower in Utility.CollectFollowersOf(companion)) {
                changed |= SetAutoPickupGear(follower, value);
            }
            return changed;
        }

        public bool AutoEquipExceptionsMenu(GameObject companion) {
            Utility.MaybeLog("Managing auto-equip for " + companion.DisplayNameOnlyStripped);

            // Double check whether or not the companion still has auto pickup enabled to avoid a potential NullReferenceException below
            if (!companion.HasPart(typeof(CleverGirl_AIPickupGear))) {
                Utility.MaybeLog("Companion doesn't have auto pickup enabled, yet the user is trying to change auto pickup behavior. This is likely a bug and shouldn't happen. Aborting!");
                return false;
            }

            var allBodyParts = companion.Body.GetParts();
            var optionNames = new List<string>(allBodyParts.Count);
            var optionHotkeys = new List<char>(allBodyParts.Count);
            var initiallySelectedOptions = new List<int>(allBodyParts.Count);
            var lockedOptionIndices = new List<int>();

            foreach (var part in allBodyParts) {
                int optionIndex = optionNames.Count;  // index that this option will have in the final menu

                // Before creating option, make sure it's valid.
                // Could be un-equipable in case of fungal infections, horns, TrueKin zoomy tank feet, etc.
                if (!(part.Equipped?.FireEvent("CanBeUnequipped") ?? true)) {
                    lockedOptionIndices.Add(optionIndex);

                    // Check if a previously tracked part is now unequippable. If so, stop tracking it.
                    if (companion.GetPart<CleverGirl_AIPickupGear>().IgnoredBodyPartIDs.Contains(part.ID)) {
                        _ = ModifyProperty(part.ID, false);  // Dont set 'changed' for this as it shouldn't punish the player
                    }
                }

                // Format and create the option
                string primary = CleverGirl_BackwardsCompatibility.IsPreferredPrimary(part) ? "{{g|[*]}}" : "";
                string equipped = part.Equipped?.ShortDisplayName ?? "{{k|[empty]}}";
                string optionText = part.Name + " : " + primary + " " + equipped;
                // TODO: probably push this locking behavior into YieldSeveral()
                if (lockedOptionIndices.Contains(part.ID)) {
                    optionText = "{{y|" + optionText + "}}";
                }
                optionNames.Add(optionText);
                optionHotkeys.Add(optionHotkeys.Count >= 26 ? ' ' : (char)('a' + optionHotkeys.Count));

                if (companion.GetPart<CleverGirl_AIPickupGear>().IgnoredBodyPartIDs.Contains(part.ID)) {
                    initiallySelectedOptions.Add(optionIndex);
                }
            }

            // Menu selection post-hook function. Returning false will stop the current selection.
            bool CheckIfChoiceIsValid(int index) {
                return !lockedOptionIndices.Contains(index);
            };

            // Pop up a menu for the player to checklist body parts
            var enumerableMenu = CleverGirl_Popup.YieldSeveral(
                Title: companion.the + companion.ShortDisplayName,
                Intro: "What slots should I skip when auto-equipping?",
                Options: optionNames.ToArray(),
                Hotkeys: optionHotkeys.ToArray(),
                OnPost: CheckIfChoiceIsValid,
                CenterIntro: true,
                IntroIcon: companion.RenderForUI(),
                AllowEscape: true,
                InitialSelections: initiallySelectedOptions
            );

            bool changed = false;
            foreach (CleverGirl_Popup.YieldResult result in enumerableMenu) {
                int partID = allBodyParts[result.Index].ID;
                changed |= ModifyProperty(partID, result.Value);
            }

            return changed;
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
        /// Add or remove an element from a list property
        /// Probably be done in a type generic fashion but properties are being kinda nasty to me right now.
        /// </summary
        private bool ModifyProperty(int element, bool add) {
            // TODO: Make this generic as it's duplicated across 4 classes
            List<int> property = IgnoredBodyPartIDs;
            bool existedPrior = property.Contains(element);

            if (add && !existedPrior) {
                property.Add(element);
                IgnoredBodyPartIDs = property;
                return true;
            } else if (!add && existedPrior) {
                _ = property.Remove(element);
                IgnoredBodyPartIDs = property;
                return true;
            }

            return false;
        }
    }
}
