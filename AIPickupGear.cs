namespace XRL.World.Parts {
    using ConsoleLib.Console;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.AI.GoalHandlers;
    using Qud.API;
    using XRL.World.CleverGirl;
    using XRL.World.Anatomy;
    using XRL.UI;

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

        public List<string> IgnoredBodyPartIDs {
            get => ParentObject.GetStringProperty(IGNOREDBODYPARTS_PROPERTY).Split(',').Where(s => !s.IsNullOrEmpty()).ToList();
            set => ParentObject.SetStringProperty(IGNOREDBODYPARTS_PROPERTY, string.Join(",", value));
        }

        // InventoryAction options
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Manage Gear Pickup",
            Display = "manage g{{inventoryhotkey|E}}ar auto-pickup",
            Command = "CleverGirl_ManageGearPickup",
            Key = 'E',
            Valid = e => e.Object.PartyLeader == The.Player,
        };

        // OptionAction options
        public static readonly Utility.OptionAction ENABLE = new Utility.OptionAction {
            Name = "Clever Girl - Enable Gear Pickup",
            Display = "enable gear auto pickup/equip",
            ActionCall = (leader, companion) => SetAutoPickupGear(companion, true),
            Key = 'e',
            Valid = (leader, companion) => leader == The.Player && !companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            Behavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction DISABLE = new Utility.OptionAction {
            Name = "Clever Girl - Disable Gear Pickup",
            Display = "disable gear auto pickup/equip",
            ActionCall = (leader, companion) => SetAutoPickupGear(companion, false),
            Key = 'e',
            Valid = (leader, companion) => leader == The.Player && companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            Behavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_ENABLE = new Utility.OptionAction {
            Name = "Clever Girl - Enable Follower Gear Pickup",
            Display = "enable {{inventoryhotkey|f}}ollower gear auto pickup/equip",
            ActionCall = (leader, companion) => SetFollowerAutoPickupGear(companion, true),
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => !obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            Behavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction FOLLOWER_DISABLE = new Utility.OptionAction {
            Name = "Clever Girl - Disable Follower Gear Pickup",
            Display = "disable {{inventoryhotkey|f}}ollower auto pickup/equip",
            ActionCall = (leader, companion) => SetFollowerAutoPickupGear(companion, false),
            Key = 'f',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            Behavior = Utility.InvalidOptionBehavior.HIDE,
        };
        public static readonly Utility.OptionAction AUTO_EQUIP_BEHAVIOR = new Utility.OptionAction {
            Name = "Clever Girl - Control Auto Equip Behavior",
            Display = "{{inventoryhotkey|c}}ontrol auto equip behavior",
            ActionCall = (leader, companion) => AutoEquipBehaviorMenu(companion),
            Key = 'c',
            Valid = (leader, companion) => leader == The.Player && companion.HasPart(typeof(CleverGirl_AIPickupGear)),
            Behavior = Utility.InvalidOptionBehavior.DARKEN,
        };
        public static readonly Utility.OptionAction FOLLOWER_AUTO_EQUIP_BEHAVIOR = new Utility.OptionAction {
            Name = "Clever Girl - Apply Auto Equip Slots To All Followers",
            Display = "{{inventoryhotkey|C}}opy current auto equip behavior to all autonomous followers",
            ActionCall = (leader, companion) => CopyAutoEquipBehaviorToFollowers(companion),
            Key = 'C',
            Valid = (leader, companion) => leader == The.Player && Utility.CollectFollowersOf(companion).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
            Behavior = Utility.InvalidOptionBehavior.DARKEN,
        };

        public static readonly List<Utility.OptionAction> MANAGE_MENU_OPTIONS = new List<Utility.OptionAction> {
            ENABLE,
            DISABLE,
            FOLLOWER_ENABLE,
            FOLLOWER_DISABLE,
            AUTO_EQUIP_BEHAVIOR,
            FOLLOWER_AUTO_EQUIP_BEHAVIOR,
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
                    if (bodyPart)
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

            // Filter out any options that are not valid at this time, if and only if it is set to HIDE behavior
            List<Utility.OptionAction> filteredOptions = MANAGE_MENU_OPTIONS.Where(o => 
                o.Valid(leader, companion) ||
                o.Behavior != Utility.InvalidOptionBehavior.HIDE).ToList();

            var optionNames = new List<string>(filteredOptions.Count);
            var optionHotkeys = new List<char>(filteredOptions.Count);

            foreach (var option in filteredOptions) {
                // Handle options with special behaviors
                string finalName = option.Display;
                if (!option.Valid(leader, companion) && option.Behavior == Utility.InvalidOptionBehavior.DARKEN) {
                    finalName = "&K" + ColorUtility.StripFormatting(finalName);
                }

                optionNames.Add(finalName);                
                optionHotkeys.Add(option.Key);                
            }

            while (true) {
                var index = Popup.ShowOptionList(Title: "Gear Management Behavior"
                                                 Options: optionNames.ToArray(),
                                                 Hotkeys: optionHotkeys.ToArray(),
                                                 Intro: "How should " + companion.the + companion.DisplayNameOnlyStripped + " manage " + companion.theirs + " gear?",
                                                 AllowEscape: true);
                
                // User cancelled, abort!
                if (index < 0) {
                    return false;
                } else {
                    return filteredOptions[index].ActionCall(leader, companion);
                }
            }
        }

        private static bool SetAutoPickupGear(GameObject target, bool value) {
            bool wasEnabled = target.HasPart(typeof(CleverGirl_AIPickupGear));
            if (value) {
                _ = target.RequirePart<CleverGirl_AIPickupGear>();
                _ = target.RequirePart<CleverGirl_AIUnburden>(); // Anyone picking up gear should know how to unburden themself
                return wasEnabled == false;  // If it was disabled: it changed, so expend a turn
            } else {
                target.RemovePart<CleverGirl_AIPickupGear>();
                target.RemovePart<CleverGirl_AIUnburden>();
                return wasEnabled == true;  // If it was enabled: it changed, so expend a turn
            }
        }

        private static bool SetFollowerAutoPickupGear(GameObject target, bool value) {
            bool changed = false;
            foreach (var follower in Utility.CollectFollowersOf(target)) {
                changed |= SetAutoPickupGear(follower, value);
            }
            return changed;
        }

        public static bool AutoEquipBehaviorMenu(GameObject companion) {
            Utility.MaybeLog("Managing auto-equip for " + companion.DisplayNameOnlyStripped);

            bool changed = false;
            var allBodyParts = companion.Body.GetParts();
            var optionNames = new List<string>(allBodyParts.Count);

            // Create pretty menu options that show equipped items on the right
            foreach (var part in allBodyParts) {
                optionNames.Add(part.Name + " : " + part.Equipped?.ShortDisplayName ?? "[empty]");
            }

            // Pop up a menu for the player to checklist body parts
            var chosenBodyPartIndices = Popup.PickSeveral(Options: optionNames.ToArray(),
                                                          Intro: "What body parts is " + companion.the + companion.ShortDisplayName + " allowed to auto-equip?",
                                                          AllowEscape: true);
            // User cancelled, abort!
            if (chosenBodyPartIndices == null) {
                Utility.MaybeLog("User aborted!");
                return false;
            }

            Utility.MaybeLog("Auto-equip menu finished");
            return changed;

        }
        
        public static bool EditAutoEquipBehavior(GameObject target, IEnumerable<string> parts) {
            bool changed = false; 
            // TODO:
            //// Evaluate whether any change was actually made in menu selection
            //if (chosenBodyPartIndices.Count == IgnoredBodyParts.Count) {
            //    foreach (var index in chosenBodyPartIndices) {
            //        if (IgnoredBodyParts.Contains(allBodyParts[chosenBodyPartIndices[index]]) == false) {
            //            return true;
            //        }
            //    }
            //}
            return changed;
        }

        public static bool CopyAutoEquipBehaviorToFollowers(GameObject target) {
            if (!target.HasPart
            bool changed = false; 
            int count = 0;
            foreach (var follower in Utility.CollectFollowersOf(target, target.Ignored)) {
                count++;
                changed |= EditAutoEquipBehavior(follower);
            }
            return changed;
        }
    }
}
