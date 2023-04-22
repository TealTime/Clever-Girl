
namespace CleverGirl.Events {
    using System.Collections.Generic;
    using Occult.Engine.CodeGeneration;
    using XRL.World.Effects;
    using XRL.World;

    [GenerateMinEventDispatchPartials]
    public class CleverGirl_MenuSelectEvent : IActOnItemEvent {
        public string Command;
        public static new readonly int ID;

        private static List<CleverGirl_MenuSelectEvent> Pool;
        private static int PoolCounter;


        // Qud Wiki Note:
        // "This is necessary for custom events that need the HandleEvent to be reflected."
        public override bool WantInvokeDispatch() {
            return true;
        }

        public override bool handlePartDispatch(IPart Part) {
            if (!base.handlePartDispatch(Part)) {
                return false;
            }
            return Part.HandleEvent(this);
        }

        public override bool handleEffectDispatch(Effect Effect) {
            if (!base.handleEffectDispatch(Effect)) {
                return false;
            }
            return Effect.HandleEvent(this);
        }

        public override bool handleProceduralCookingTriggeredActionDispatch(ProceduralCookingTriggeredAction Action) {
            if (!base.handleProceduralCookingTriggeredActionDispatch(Action)) {
                return false;
            }
            return Action.HandleEvent(this);
        }

        static CleverGirl_MenuSelectEvent() {
            ID = AllocateID();
            RegisterPoolReset(ResetPool);
            RegisterPoolCount(nameof(CleverGirl_MenuSelectEvent), () => (Pool != null) ? Pool.Count : 0);
        }

        public CleverGirl_MenuSelectEvent() {
            base.ID = ID;
        }

        public static void ResetPool() {
            while (PoolCounter > 0) {
                Pool[--PoolCounter].Reset();
            }
        }

        public static CleverGirl_MenuSelectEvent FromPool() {
            return FromPool(ref Pool, ref PoolCounter);
        }

        public static CleverGirl_MenuSelectEvent FromPool(GameObject Actor, GameObject Item, string Command) {
            CleverGirl_MenuSelectEvent menuEvent = FromPool();
            menuEvent.Actor = Actor;
            menuEvent.Item = Item;
            menuEvent.Command = Command;
            return menuEvent;
        }
    }
}