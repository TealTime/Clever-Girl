namespace CleverGirl.Parts {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.AI.GoalHandlers;
    using CleverGirl;
    using CleverGirl.GoalHandlers;
    using XRL.World;
    using XRL.World.Anatomy;
    using XRL.World.Parts;
    using Qud.API;

    [Serializable]
    public class CleverGirl_AIPickupGear : CleverGirl_INoSavePart {
        public static string PROPERTY => "CleverGirl_AIPickupGear";
        public static string IGNOREDBODYPARTIDS_PROPERTY => PROPERTY + "_IgnoredBodyPartIDs";
        public static string IGNOREDEQUIPMENTTYPES_PROPERTY => PROPERTY + "_IgnoredEquipmentTypes";
        public override void Register(GameObject Object) {
            _ = Object.SetIntProperty(PROPERTY, 1);
        }
        public override void Remove() {
            ParentObject.RemoveIntProperty(PROPERTY);
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
            // TODO: Add preferences for AV/DV armor and stuff like that
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
                return false;
            }

            // consider items in our inventory too in case PerformReequip isn't equipping something
            // we think it should
            things.AddRange(ParentObject.Inventory.Objects.Where(whichThings));
            things.Sort(thingComparer);

            // Note: This NoEquip + NoAIEquip behavior was documented in the discord. Essentially, in certain situations companions 
            // should NOT equip certain sensitive items. Take for example the Bey Lah questline: a companion picking up and 
            // equipping a "bloody patch of fur" clue item would prevent the player from progressing the through the quest until 
            // the player eventually notices that it's in their companion's inventory.
            var noEquip = ParentObject.GetPropertyOrTag("NoEquip");
            var noEquipList = string.IsNullOrEmpty(noEquip) ? null : new List<string>(noEquip.CachedCommaExpansion());

            // Don't auto-pickup for a bodypart if it has a better alternative already in companion's inventory
            var hasBetterInInventory = new List<BodyPart>();

            foreach (var thing in things) {
                if (noEquipList?.Contains(thing.Blueprint) ?? false) {
                    continue;
                }
                if (thing.HasPropertyOrTag("NoAIEquip")) {
                    continue;
                }
                foreach (var bodyPart in allBodyParts) {
                    if (!whichBodyParts(bodyPart, thing) || hasBetterInInventory.Contains(bodyPart)) {
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
                            hasBetterInInventory.Add(bodyPart);
                            continue;
                        }
                        Utility.MaybeLog("I'm going to go get that " + thing.DisplayNameOnlyStripped);
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

        public bool SetFollowerAutoPickupGear(bool value) {
            bool changed = false;
            foreach (var follower in Utility.CollectFollowersOf(ParentObject)) {
                changed |= SetAutoPickupGear(follower, value);
            }
            return changed;
        }

        public static bool SetAutoPickupGear(GameObject creature, bool value) {
            bool wasEnabled = creature.HasPart(typeof(CleverGirl_AIPickupGear));
            if (value) {
                _ = creature.RequirePart<CleverGirl_AIPickupGear>();
                _ = creature.RequirePart<CleverGirl_AIUnburden>(); // Anyone picking up gear should know how to unburden themself.
                return !wasEnabled;  // If it was disabled prior: it must have changed, so expend a turn.
            } else {
                var part = creature.GetPart<CleverGirl_AIPickupGear>();
                if (part != null) {
                    // "soft" removal, which leaves the stored properties intact, so we "pickup" (HA!) where we left off when reenabling.
                    _ = creature.PartsList.Remove(part);
                }
                creature.RemovePart<CleverGirl_AIUnburden>();
                return wasEnabled;  // If it was enabled prior: it must have changed, so expend a turn.
            }
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
