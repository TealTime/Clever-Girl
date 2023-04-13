namespace CleverGirl.Parts {
    using System;
    using System.Collections.Generic;
    using XRL;
    using XRL.UI;
    using XRL.World;
    using XRL.World.Capabilities;
    using XRL.World.Parts;
    using CleverGirl;
    using CleverGirl.Menus;
    using CleverGirl.Patches;
    using Options = Globals.Options;

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
                return CleverGirl_MainMenu.Start(E.Actor, E.Item);
            }
            if (E.Command == CleverGirl_Feed.COOKING_ACTION.Command) {
                if (Utility.CollectNearbyCompanions(E.Actor).Count == 0) {
                    Popup.Show("None of your companions are nearby!");
                    return false;
                } else {
                    int EnergyCost = 100;
                    if (CleverGirl_Feed.DoFeed(E.Actor, ref EnergyCost)) {
                        CompanionDirectionEnergyCost(E.Item, EnergyCost, "Feed Companions");
                    }
                }
            }
            if (E.Command == CyberneticsTerminal2_HandleEvent_GetInventoryActionsEvent_Patch.ACTION.Command) {
                GameObject companion = null;
                if (InterfaceCompanions.DoInterface(E, ref companion)) {
                    CompanionDirectionEnergyCost(companion, 100, "Interface");
                }
            }

            return base.HandleEvent(E);
        }

        public bool HandleEvent(CleverGirl_MenuSelectEvent E) {
            /** MainMenu Options **/
            if (E.Command == CleverGirl_Feed.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_Feed.DoFeed(E.Actor, E.Item)) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Feed");
                }
            }
            if (E.Command == CleverGirl_ManageGear.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_ManageGear.Manage(E.Actor, E.Item)) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Manage Gear");
                }
            }
            if (E.Command == CleverGirl_AIManageSkills.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageSkills>().ManageSkillsMenu()) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Manage Skills");
                }
            }
            if (E.Command == CleverGirl_AIManageAttributes.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageAttributes>().ManageAttributesMenu()) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Manage Attributes");
                }
            }
            if (E.Command == CleverGirl_AIManageMutations.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIManageMutations>().ManageMutationsMenu()) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Manage Mutations");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.ACTION.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                return CleverGirl_BehaviorsMenu.Start(E.Actor, E.Item);
            }

            /** AutoPickupEquipMenu Options **/
            if (E.Command == CleverGirl_BehaviorsMenu.ENABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_AIPickupGear.SetAutoPickupGear(E.Item, true)) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Enable Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.DISABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (CleverGirl_AIPickupGear.SetAutoPickupGear(E.Item, false)) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Disable Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.FOLLOWER_ENABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().SetFollowerAutoPickupGear(true)) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Enable Follower Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.FOLLOWER_DISABLE_PICKUP.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().SetFollowerAutoPickupGear(false)) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Disable Follower Auto Gear Pickup");
                }
            }
            if (E.Command == CleverGirl_BehaviorsMenu.AUTO_EQUIP_EXCEPTIONS.Command && ParentObject.CheckCompanionDirection(E.Item)) {
                if (E.Item.RequirePart<CleverGirl_AIPickupGear>().AutoEquipExceptionsMenu()) {
                    CompanionDirectionEnergyCost(E.Item, 100, "Set Auto Equip Exceptions");
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

        /// <summary>
        /// Wrapper function around GameObject.CompanionDirectionEnergyCost to allow players to toggle this behavior.
        /// Each time you direct your companion, Clever-Girl usually subtracts 100 energy (becomes 10 for Telepathy).
        /// </summary>
        private void CompanionDirectionEnergyCost(GameObject GO, int EnergyCost, string Action) {
            if (Options.DirectingCompanionCostsEnergy) {
                ParentObject.CompanionDirectionEnergyCost(GO, EnergyCost, Action);
            }
        }
    }
}
