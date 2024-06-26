namespace XRL.World.Parts {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.AI.GoalHandlers;
    using Qud.API;
    using XRL.World.Anatomy;
    using CleverGirl;

    [Serializable]
    public class CleverGirl_AIPickupGear : IPart {
        public static string PROPERTY => "CleverGirl_AIPickupGear";
        public override void Register(GameObject Object, IEventRegistrar Registrar) {
            _ = Object.SetIntProperty(PROPERTY, 1);
        }
        public override void Remove() {
            ParentObject.RemoveIntProperty(PROPERTY);
        }
        public static readonly Utility.InventoryAction ENABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Enable Gear Pickup",
            Display = "enable gear {{inventoryhotkey|p}}ickup",
            Command = "CleverGirl_EnableGearPickup",
            Key = 'p',
            Valid = e => e.Object.PartyLeader == The.Player && !e.Object.HasPart(typeof(CleverGirl_AIPickupGear)),
        };
        public static readonly Utility.InventoryAction DISABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Disable Gear Pickup",
            Display = "disable gear {{inventoryhotkey|p}}ickup",
            Command = "CleverGirl_DisableGearPickup",
            Key = 'p',
            Valid = e => e.Object.PartyLeader == The.Player && e.Object.HasPart(typeof(CleverGirl_AIPickupGear)),
        };
        public static readonly Utility.InventoryAction FOLLOWER_ENABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Enable Follower Gear Pickup",
            Display = "enable follower gear {{inventoryhotkey|p}}ickup",
            Command = "CleverGirl_EnableFollowerGearPickup",
            Key = 'P',
            Valid = e => e.Object.PartyLeader == The.Player && Utility.CollectFollowersOf(e.Object).Any(obj => !obj.HasPart(nameof(CleverGirl_AIPickupGear))),
        };
        public static readonly Utility.InventoryAction FOLLOWER_DISABLE = new Utility.InventoryAction {
            Name = "Clever Girl - Disable Follower Gear Pickup",
            Display = "disable follower gear {{inventoryhotkey|p}}ickup",
            Command = "CleverGirl_DisableFollowerGearPickup",
            Key = 'P',
            Valid = e => e.Object.PartyLeader == The.Player && Utility.CollectFollowersOf(e.Object).Any(obj => obj.HasPart(nameof(CleverGirl_AIPickupGear))),
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
                    if (thing.Physics.InInventory != ParentObject && thing.WeightEach - (bodyPart.Equipped?.Weight ?? 0) > capacity) {
                        Utility.MaybeLog("No way to equip " + thing.DisplayNameOnlyStripped + " on " + bodyPart.Name + " without being overburdened");
                        continue;
                    }
                    if (thing.HasPart(typeof(Cursed))) {
                        // just say no to the amaranthine prism
                        continue;
                    }
                    if (thingComparer.Compare(thing, bodyPart.Equipped) < 0) {
                        if (thing.Physics.InInventory == ParentObject) {
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
            ParentObject.Brain.Think("I want that " + item.DisplayNameOnlyStripped);
            _ = ParentObject.Brain.PushGoal(new CleverGirl_GoPickupGear(item));
            _ = ParentObject.Brain.PushGoal(new MoveTo(item.CurrentCell));
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
    }
}
