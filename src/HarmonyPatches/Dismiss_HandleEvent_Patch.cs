namespace CleverGirl.Patches {
    using HarmonyLib;
    using System;
    using XRL;
    using XRL.UI;
    using XRL.World;
    using XRL.World.Effects;

    public static class Helpers {
        public static bool DoubleCheck(GameObject companion, GameObject Leader) {
            if (Leader != The.Player) {
                return true;
            }
            return Popup.ShowYesNo(string.Format("Do you really want to dismiss {0} from {1} service?",
                                   companion.GetDisplayName(),
                                   Leader.its)) == DialogResult.Yes;
        }
    }

    // Simply prompt for yes or no confirmation prior to dismissing rebuked companions
    [HarmonyPatch(typeof(Rebuked), "HandleEvent", new Type[] { typeof(InventoryActionEvent) })]
    public static class Rebuked_HandleEvent_InventoryActionEvent_Patch {
        public static bool Prefix(InventoryActionEvent E, Rebuked __instance) {
            if (E.Command == "DismissServitor" && E.Actor == __instance.Rebuker && E.Item == __instance.Object && __instance.Rebuker.CheckCompanionDirection(__instance.Object)) {
                return Helpers.DoubleCheck(__instance.Object, __instance.Rebuker);
            }
            return true;
        }
    }

    // Simply prompt for yes or no confirmation prior to dismissing proselytized companions
    [HarmonyPatch(typeof(Proselytized), "HandleEvent", new Type[] { typeof(InventoryActionEvent) })]
    public static class Proselytized_HandleEvent_InventoryActionEvent_Patch {
        public static bool Prefix(InventoryActionEvent E, Proselytized __instance) {
            if (E.Command == "DismissProselyte" && E.Actor == __instance.Proselytizer && E.Item == __instance.Object && __instance.Proselytizer.CheckCompanionDirection(__instance.Object)) {
                return Helpers.DoubleCheck(__instance.Object, __instance.Proselytizer);
            }
            return true;
        }
    }
}