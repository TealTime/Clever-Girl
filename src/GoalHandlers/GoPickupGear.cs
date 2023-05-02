namespace CleverGirl.GoalHandlers {
    using System;
    using System.Collections.Generic;
    using XRL.World;
    using XRL.World.AI;
    using XRL.World.Parts;
    using XRL.World.Anatomy;

    [Serializable]
    public class CleverGirl_GoPickupGear : GoalHandler {
        public readonly GameObject Gear;
        private List<BodyPart> ForbiddenBodyParts;

        public override bool Finished() => false;

        public override void TakeAction() {
            Pop();
            var currentCell = ParentBrain.pPhysics.CurrentCell;
            if (currentCell == null) {
                return;
            }
            if (currentCell != Gear.CurrentCell) {
                return;
            }
            if (Gear.IsTakeable()) {
                _ = ParentBrain.ParentObject.TakeObject(Gear);

                // This is a hack to avoid PerformReequip() from considering certain body parts:
                // "Lock down" forbidden body parts by equipping them with temporary unremovable items
                var gearStorage = new List<GameObject>(ForbiddenBodyParts.Count);
                foreach (var part in ForbiddenBodyParts) {
                    gearStorage.Add(part.Equipped);
                    part.Unequip();  // Maybe should force unequip?
                    var tempNaturalGear = GameObject.create("Item");
                    _ = tempNaturalGear.AddPart(new NaturalEquipment());
                    _ = tempNaturalGear.SetIntProperty("CleverGirl_GearLock", 1);
                    _ = part.Equip(tempNaturalGear, Silent: true);
                    Utility.MaybeLog("Replaced with natural gear to prepare for reequip.");
                }

                // Perform actual reequip
                ParentBrain.PerformReequip();

                // Remove any gear locks + add original gear
                for (var i = 0; i < ForbiddenBodyParts.Count; i++) {
                    if (ForbiddenBodyParts[i].Equipped?.HasIntProperty("CleverGirl_GearLock") == true) {
                        _ = ForbiddenBodyParts[i].ForceUnequip(Silent: true);
                        if (gearStorage[i] != null) {
                            _ = ForbiddenBodyParts[i].Equip(gearStorage[i]);
                        }
                        Utility.MaybeLog("Reequipped " + ForbiddenBodyParts[i].Equipped);
                    } else {
                        Utility.MaybeLog("CleverGirl_LockGear didn't work");
                    }
                }
            }
        }

        public CleverGirl_GoPickupGear(GameObject gear, List<BodyPart> forbiddenBodyParts) {
            Gear = gear;
            ForbiddenBodyParts = forbiddenBodyParts;
        }
    }
}
