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
            ID == GetCookingActionsEvent.ID ||
            ID == CleverGirl_MenuSelectEvent.ID;
        public override bool HandleEvent(OwnerGetInventoryActionsEvent E) {
            if (E.Actor == ParentObject && E.Object?.IsPlayerLed() == true && !E.Object.IsPlayer()) {
                if (E.Object.HasPart(typeof(CannotBeInfluenced))) {
                    // don't manage someone who can't be managed
                    return true;
                }
                var actions = new List<Utility.InventoryAction>{
                    CleverGirl_MainMenu.ACTION,
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
            if (E.Command == CleverGirl_MainMenu.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                // TODO: One time Popup to explain/hand-wave Clever Girl's existence in the Qud universe in a fun way
                if (CleverGirl_MainMenu.Start(E.Actor, E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Clever Girl Main Menu");
                }
            }
            if (E.Command == CleverGirl_Feed.COOKING_ACTION.Command) {
                if (Utility.CollectNearbyCompanions(E.Actor).Count == 0) {
                    Popup.Show("None of your companions are nearby!");
                    return false;
                } else {
                    int EnergyCost = 100;
                    if (CleverGirl_Feed.DoFeed(E.Actor, ref EnergyCost)) {
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

        public bool HandleEvent(CleverGirl_MenuSelectEvent E) {
            /** MainMenu Options **/
            if (E.Command == CleverGirl_Feed.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_Feed.DoFeed(E.Actor, E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Feed");
                }
            }
            if (E.Command == CleverGirl_ManageGear.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_ManageGear.Manage(E.Actor, E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Gear");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_BehaviorsMenu.Start(E.Actor, E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Gear Auto Pickup/Equip Behavior");
                }
            }
            if (E.Command == CleverGirl_AIManageSkills.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageSkills>().StartManageSkillsMenu()) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Skills");
                }
            }
            if (E.Command == CleverGirl_AIManageAttributes.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageAttributes>().Manage()) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Attributes");
                }
            }
            if (E.Command == CleverGirl_AIManageMutations.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageMutations>().Manage()) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Mutations");
                }
            }

            /** AutoPickupEquipMenu Options **/
            if (E.Command == CleverGirl_BehaviorsMenu.ENABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().SetAutoPickupGear(E.Item, true)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Enable Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.DISABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().SetAutoPickupGear(E.Item, false)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Disable Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.FOLLOWER_ENABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().SetFollowerAutoPickupGear(E.Item, true)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Enable Follower Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.FOLLOWER_DISABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().SetFollowerAutoPickupGear(E.Item, false)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Disable Follower Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.AUTO_EQUIP_EXCEPTIONS.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().StartAutoEquipBehaviorMenu(E.Item)) {
                    ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Set Auto Equip Exceptions");
                }
            }

            return true;
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
                CleverGirl_CompanionsMenu.OpenMenu();
            }
            return true;
        }

        public override bool HandleEvent(GetCookingActionsEvent E) {
            var action = CleverGirl_Feed.COOKING_ACTION;
            _ = E.AddAction(action.Name,
                            Campfire.EnabledDisplay(Utility.CollectNearbyCompanions(E.Actor).Count > 0, action.Display),
                            action.Command,
                            Key: action.Key,
                            FireOnActor: true);
            return true;
        }
    }
}
