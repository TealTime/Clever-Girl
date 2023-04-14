namespace CleverGirl.Patches {
    using HarmonyLib;
    using System.Linq;
    using XRL.World.Parts;
    using XRL.World.Parts.Mutation;
    using CleverGirl.Parts;

    /// <summary> 
    /// Before applying a rapid level mutation on a potential companion, check to see if the creature being checked
    /// is managed by player, and prioritize rapid leveling mutations the player has selected instead of random ones.
    /// </summary>
    [HarmonyPatch(typeof(BaseMutation), "RapidLevel")]
    public static class BaseMutation_RapidLevel_Patch {
        [HarmonyPrefix]
        public static void RapidLevelInstead(int Amount, ref BaseMutation __instance) {
            // Ensure this is a player led creature and that it is being directed to focus specific mutations
            var manageMutations = __instance.ParentObject.GetPart<CleverGirl_AIManageMutations>();
            if (manageMutations == null) {
                return;
            }

            var whichKey = "RapidLevel_" + __instance.GetMutationClass();

            // Pre-emptively reduce by the levels this mutation will gain
            _ = __instance.ParentObject.ModIntProperty(whichKey, -Amount);

            // Pick a mutation from focused
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
    }
}