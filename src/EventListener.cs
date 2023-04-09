namespace XRL.World.Parts {
    using System;
    using System.Collections.Generic;
    using XRL.UI;
    using XRL.World.Capabilities;
    using XRL.World.CleverGirl;
    using XRL.World.Parts.CleverGirl;

    [Serializable]
    public class CleverGirl_EventListener : CleverGirl_INoSavePart {
        public bool RestingUntilPartyHealed;
        public override bool WantEvent(int ID, int cascade) =>
            base.WantEvent(ID, cascade) ||
            ID == OwnerGetInventoryActionsEvent.ID ||
            ID == InventoryActionEvent.ID ||
            ID == CommandEvent.ID ||
            ID == GetCookingActionsEvent.ID;
        public override bool HandleEvent(OwnerGetInventoryActionsEvent E) {
            if (E.Actor == ParentObject && E.Object?.IsPlayerLed() == true && !E.Object.IsPlayer()) {
                if (E.Object.HasPart(typeof(CannotBeInfluenced))) {
                    // don't manage someone who can't be managed
                    return true;
                }
                var actions = new List<Utility.InventoryAction>{
                        CleverGirl_AIPickupGear.ACTION,
                        CleverGirl_AIManageSkills.ACTION,
                        CleverGirl_AIManageMutations.ACTION,
                        CleverGirl_AIManageAttributes.ACTION,
                        ManageGear.ACTION,
                        Feed.ACTION,
                    };
                foreach (var action in actions) {
                    if (action.Valid(E)) {
                        _ = E.AddAction(action.Name, action.Display, action.Command, Key: action.Key, FireOnActor: true, WorksAtDistance: true);
                    }
                }
            }
            return true;
        }

        public override bool HandleEvent(InventoryActionEvent E) {
            if (E.Command == CleverGirl_AIPickupGear.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_AIPickupGear.Manage(E.Actor, E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_AIManageSkills.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageSkills>().Manage()) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Skills");
                }
            }
            if (E.Command == CleverGirl_AIManageMutations.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageMutations>().Manage()) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Mutations");
                }
            }
            if (E.Command == CleverGirl_AIManageAttributes.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageAttributes>().Manage()) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Attributes");
                }
            }
            if (E.Command == ManageGear.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (ManageGear.Manage(E.Actor, E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Gear");
                }
            }
            if (E.Command == Feed.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (Feed.DoFeed(E.Actor, E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Feed");
                }
            }
            if (E.Command == Feed.COOKING_ACTION.Command) {
                if (Utility.CollectNearbyCompanions(E.Actor).Count == 0) {
                    Popup.Show("None of your companions are nearby!");
                    return false;
                } else {
                    int EnergyCost = 100;
                    if (Feed.DoFeed(E.Actor, ref EnergyCost)) {
                        ParentObject.CompanionDirectionEnergyCost(E.Item, EnergyCost, "Feed Companions");
                    }
                }
            }
            if (E.Command == CyberneticsTerminal2_HandleEvent_GetInventoryActionsEvent_Patch.ACTION.Command) {
                GameObject companion = null;
                if (InterfaceCompanions.DoInterface(E, ref companion)) {
                    ParentObject.CompanionDirectionEnergyCost(companion, 100, "Interface");
                }
            }

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E) {
            if (E.Command == "CleverGirl_CmdWaitUntilPartyHealed" && !AutoAct.ShouldHostilesInterrupt("r", popSpot: true)) {
                AutoAct.Setting = "r";
                The.Game.ActionManager.RestingUntilHealed = true;
                The.Game.ActionManager.RestingUntilHealedCount = 0;
                RestingUntilPartyHealed = true;
                _ = ParentObject.UseEnergy(1000, "Pass");
                Loading.SetLoadingStatus("Resting until party healed...");
            }
            if (E.Command == "CleverGirl_CmdCompanionsMenu") {
                CompanionsMenu.OpenMenu();
            }
            return true;
        }

        public override bool HandleEvent(GetCookingActionsEvent E) {
            var action = Feed.COOKING_ACTION;
            _ = E.AddAction(action.Name,
                            Campfire.EnabledDisplay(Utility.CollectNearbyCompanions(E.Actor).Count > 0, action.Display),
                            action.Command,
                            Key: action.Key,
                            FireOnActor: true);
            return true;
        }
    }
}
