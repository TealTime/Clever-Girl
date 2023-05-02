namespace CleverGirl.GoalHandlers {
    using System;
    using XRL.World;
    using XRL.World.AI;

    [Serializable]
    public class CleverGirl_GoPickupGear : GoalHandler {
        public readonly GameObject Gear;

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
                ParentBrain.PerformReequip();
            }
        }

        public CleverGirl_GoPickupGear(GameObject gear) {
            Gear = gear;
        }
    }
}
