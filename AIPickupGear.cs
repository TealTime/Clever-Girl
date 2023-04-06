namespace XRL.World.Parts {
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

        public List<string> IgnoredBodyParts {
            get => ParentObject.GetStringProperty(IGNOREDBODYPARTS_PROPERTY).Split(',').Where(s => !s.IsNullOrEmpty()).ToList();
            set => ParentObject.SetStringProperty(IGNOREDBODYPARTS_PROPERTY, string.Join(",", value));
        }

        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Manage Gear Pickup",
            Display = "manage g{{inventoryhotkey|E}}ar auto-pickup",
            Command = "CleverGirl_ManageGearPickup",
            Key = 'E',
            Valid = e => e.Object.PartyLeader == The.Player,
        };
        public static readonly Utility.InventoryAction ENABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Enable Gear Pickup",
            Display = "enable gear {{inventoryhotkey|a}}uto-pickup",
            Command = "CleverGirl_EnableGearPickup",
            Key = 'a',
            Valid = e => e.Object.PartyLeader == The.Player && !e.Object.HasPart(typeof(CleverGirl_AIPickupGear)),
        };
        public static readonly Utility.InventoryAction DISABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Disable Gear Pickup",
            Display = "disable gear {{inventoryhotkey|a}}uto-pickup",
            Command = "CleverGirl_DisableGearPickup",
            Key = 'a',
            Valid = e => e.Object.PartyLeader == The.Player && e.Object.HasPart(typeof(CleverGirl_AIPickupGear)),
        };
        public static readonly Utility.InventoryAction CONTROL = new Utility.InventoryAction {
            Name = "Clever Girl - Control Auto Equip Behavior",
            Display = "{{inventoryhotkey|c}}ontrol auto-equip behavior",
            Command = "CleverGirl_ControlAutoEquipBehavior",
            Key = 'c',
            Valid = e => e.Object.PartyLeader == The.Player && e.Object.HasPart(typeof(CleverGirl_AIPickupGear)),
        };
        public static readonly Utility.InventoryAction FOLLOWER_ENABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Enable Follower Gear Pickup",
            Display = "enable {{inventoryhotkey|f}}ollower gear auto-pickup",
            Command = "CleverGirl_EnableFollowerGearPickup",
            Key = 'f',
            Valid = e => e.Object.PartyLeader == The.Player && Utility.CollectFollowersOf(e.Object).Any(obj => !obj.HasPart(nameof(CleverGirl_AIPickupGear))),
        };
        public static readonly Utility.InventoryAction FOLLOWER_DISABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Disable Follower Gear Pickup",
            Display = "disable {{inventoryhotkey|f}}ollower gear auto-pickup",
            Command = "CleverGirl_DisableFollowerGearPickup",
            Key = 'f',
            Valid = e => e.Object.PartyLeader == The.Player && Utility.CollectFollowersOf(e.Object).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
        };
        public static readonly Utility.InventoryAction FOLLOWER_APPLY = new Utility.InventoryAction {
            Name = "Clever Girl - Apply Auto Equip Slots To All Followers",
            Display = "apply auto-equip behavior to all currently enabled {{inventoryhotkey|F}}ollowers",
            Command = "CleverGirl_ApplyAutoEquipSlotsToAllFollowers",
            Key = 'F',
            Valid = e => e.Object.PartyLeader == The.Player && Utility.CollectFollowersOf(e.Object).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
        };

        public static readonly List<Utility.InventoryAction> MANAGE_MENU_OPTIONS = new List<Utility.InventoryAction> {
            ENABLE,
            DISABLE,
            CONTROL,
            FOLLOWER_ENABLE,
            FOLLOWER_DISABLE,
            FOLLOWER_APPLY,
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
        public static bool Manage() {

            var optionNames = new List<string>(MANAGE_MENU_OPTIONS.Count);
            var optionHotkeys = new List<char>(MANAGE_MENU_OPTIONS.Count);

            foreach (var option in MANAGE_MENU_OPTIONS) {
                optionNames.Add(option.Display);                
                optionHotkeys.Add(option.Key);                
            }

            while (true) {
                var index = Popup.ShowOptionList(Options: optionNames.ToArray(),
                                                 Hotkeys: optionHotkeys.ToArray(),
                                                 AllowEscape: true);
                

                // User cancelled, abort!
                if (index < 0) {
                    return false;
                }
                
                //TODO:
                return true;
            }
        }
        public static bool ControlAutoEquipMenu(GameObject companion, GameObject player) {
            Utility.MaybeLog("Managing auto-equip for " + companion.DisplayNameOnlyStripped);

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

            // TODO:
            //// Evaluate whether any change was actually made in menu selection
            //if (chosenBodyPartIndices.Count == IgnoredBodyParts.Count) {
            //    foreach (var index in chosenBodyPartIndices) {
            //        if (IgnoredBodyParts.Contains(allBodyParts[chosenBodyPartIndices[index]]) == false) {
            //            return true;
            //        }
            //    }
            //}
            Utility.MaybeLog("Auto-equip menu finished");
            return true;

        }
        
        private static void SetAutoPickupGear(GameObject companion, bool value) {
            if (value) {
                _ = companion.RequirePart<CleverGirl_AIPickupGear>();
                _ = companion.RequirePart<CleverGirl_AIUnburden>(); // Anyone picking up gear should know how to unburden themself
            } else {
                companion.RemovePart<CleverGirl_AIPickupGear>();
                companion.RemovePart<CleverGirl_AIUnburden>();

            }
        }
    }
}
