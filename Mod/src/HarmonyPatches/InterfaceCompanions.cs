namespace CleverGirl.HarmonyPatches {
    using HarmonyLib;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using XRL;
    using XRL.UI;
    using XRL.World;
    using XRL.World.Parts;
    using CleverGirl;

    // adjust smart use if we might be interfacing companions
    [HarmonyPatch(typeof(CyberneticsTerminal2), "HandleEvent", new Type[] { typeof(CommandSmartUseEvent) })]
    public static class CyberneticsTerminal2_HandleEvent_CommandSmartUseEvent_Patch {
        public static bool Prefix(CommandSmartUseEvent E, CyberneticsTerminal2 __instance, ref bool __result) {
            if (Utility.CollectNearbyCompanions(E.Actor).Any(c => c.IsTrueKin())) {
                // give normal twiddle options instead of directly interfacing player
                __result = __instance.ParentObject.Twiddle();
                return false;
            }
            return true;
        }
    }

    // add "interface a companion" option to terminal
    [HarmonyPatch(typeof(CyberneticsTerminal2), "HandleEvent", new Type[] { typeof(GetInventoryActionsEvent) })]
    public static class CyberneticsTerminal2_HandleEvent_GetInventoryActionsEvent_Patch {
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Interface Companion",
            Display = "interface a companion",
            Command = "CleverGirl_Interface",
            Key = 'c',
            Valid = E => Utility.CollectNearbyCompanions(E.Actor).Any(c => c.IsTrueKin()),
        };
        public static void Postfix(GetInventoryActionsEvent E) {
            if (ACTION.Valid(E)) {
                _ = E.AddAction(ACTION.Name, ACTION.Display, ACTION.Command, Key: ACTION.Key, FireOnActor: true);
            }
        }
    }

    // include player inventory in collection of possible implants and credits
    [HarmonyPatch(typeof(CyberneticsTerminal), "CurrentScreen", MethodType.Setter)]
    public static class CyberneticsTerminal_CurrentScreen_Setter_Patch {
        public static void Postfix(CyberneticsTerminal __instance) {
            if (__instance.Subject == The.Player) {
                return;
            }
            The.Player.ForeachInventoryAndEquipment(obj => {
                if ((obj.GetPart<CyberneticsCreditWedge>() is CyberneticsCreditWedge part) && part.Credits > 0) {
                    __instance.Credits += part.Credits * obj.Count;
                    __instance.Wedges.Add(part);
                }
            });
            The.Player.Inventory?.ForeachObject(obj => {
                if (obj.IsImplant && obj.Understood()) {
                    __instance.Implants.Add(obj);
                }
            });
        }
    }

    // put unimplanted cybernetics in the player's inventory
    [HarmonyPatch(typeof(CyberneticsScreenRemove), "Activate")]
    public static class CyberneticsScreenRemove_Activate_Patch {
        public static void Postfix(CyberneticsScreenRemove __instance) {
            if (__instance.Terminal.Subject == The.Player || The.Player.Inventory == null) {
                return;
            }
            var cybernetic = AccessTools.Field(typeof(CyberneticsScreenRemove), "Cybernetics").GetValue(__instance) as List<GameObject>;
            if (__instance.Terminal.Selected < cybernetic.Count) {
                var implant = cybernetic[__instance.Terminal.Selected];
                if (!implant.HasTag("CyberneticsNoRemove") && !implant.HasTag("CyberneticsDestroyOnRemoval")) {
                    __instance.Terminal.Subject.Inventory?.RemoveObject(implant);
                    _ = The.Player.Inventory.AddObject(implant, Silent: true);
                }
            }
        }
    }
}
