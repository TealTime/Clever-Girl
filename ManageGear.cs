// equipment screen logic copied and modified from Caves of Qud's EquipmentScreen.cs

namespace XRL.World.CleverGirl {
    using ConsoleLib.Console;
    using Qud.API;
    using System.Collections.Generic;
    using System.Reflection;
    using XRL.Core;
    using XRL.Language;
    using XRL.UI;
    using XRL.World.Parts;
    using XRL.World.Anatomy;

    public static class ManageGear {
        public static readonly Utility.InventoryAction ACTION = new Utility.InventoryAction {
            Name = "Clever Girl - Manage Gear",
            Display = "manage g{{inventoryhotkey|e}}ar",
            Command = "CleverGirl_ManageGear",
            Key = 'e',
            Valid = Utility.InventoryAction.Adjacent,
        };

        public static bool Manage(GameObject Leader, GameObject Companion) {
            GameManager.Instance.PushGameView("Equipment");
            var screenBuffer = ScreenBuffer.GetScrapBuffer1();
            var selectedIndex = 0;
            var windowStart = 0;
            var screenTab = ScreenTab.Equipment;
            var Done = false;
            var Changed = false;
            var body = Companion.Body;
            var relevantBodyParts = new List<BodyPart>();
            var allCybernetics = new List<GameObject>();
            var allEquippedOrDefault = new List<GameObject>();
            var allEquipped = new List<GameObject>();
            var keymap = new Dictionary<char, int>();
            var wornElsewhere = new HashSet<GameObject>();
            while (!Done) {
                var HasCybernetics = false;
                relevantBodyParts.Clear();
                allCybernetics.Clear();
                allEquippedOrDefault.Clear();
                allEquipped.Clear();
                foreach (var loopPart in body.LoopParts()) {
                    if (screenTab == ScreenTab.Equipment) {
                        if (loopPart.Equipped != null) {
                            // equipped item
                            relevantBodyParts.Add(loopPart);
                            allEquippedOrDefault.Add(loopPart.Equipped);
                            allEquipped.Add(loopPart.Equipped);
                            allCybernetics.Add(null);
                        } else if (loopPart.DefaultBehavior != null) {
                            // natural weapon
                            relevantBodyParts.Add(loopPart);
                            allEquippedOrDefault.Add(loopPart.DefaultBehavior);
                            allEquipped.Add(null);
                            allCybernetics.Add(null);
                        } else {
                            // empty slot
                            relevantBodyParts.Add(loopPart);
                            allEquippedOrDefault.Add(null);
                            allEquipped.Add(null);
                            allCybernetics.Add(null);
                        }
                    }
                    if (loopPart.Cybernetics != null) {
                        if (screenTab == ScreenTab.Cybernetics) {
                            relevantBodyParts.Add(loopPart);
                            allEquippedOrDefault.Add(loopPart.Cybernetics);
                            allEquipped.Add(loopPart.Cybernetics);
                            allCybernetics.Add(loopPart.Cybernetics);
                        }
                        HasCybernetics = true;
                    }
                }
                var CanChangePrimaryLimb = !Companion.AreHostilesNearby();
                var CacheValid = true;
                while (!Done && CacheValid) {
                    Event.ResetPool(false);
                    if (!XRLCore.Core.Game.Running) {
                        GameManager.Instance.PopGameView();
                        return false;
                    }
                    wornElsewhere.Clear();
                    screenBuffer.Clear();
                    screenBuffer.SingleBox(0, 0, 79, 24, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
                    _ = screenBuffer.Goto(35, 0)
                        .Write(screenTab == ScreenTab.Equipment ?
                               "[ {{W|Equipment}} ]" :
                               "[ {{W|Cybernetics}} ]");
                    _ = screenBuffer.Goto(60, 0)
                        .Write(" {{W|ESC}} or {{W|5}} to exit ");
                    _ = screenBuffer.Goto(25, 24)
                        .Write(CanChangePrimaryLimb && !relevantBodyParts[selectedIndex].Abstract ?
                            "[{{W|Tab}} - Set primary limb]" :
                            "[{{K|Tab - Set primary limb}}]");
                    var rowCount = 22;
                    var firstRow = 2;
                    if (HasCybernetics) {
                        _ = screenBuffer.Goto(3, firstRow)
                            .Write(screenTab == ScreenTab.Cybernetics ?
                                "{{K|Equipment}} {{Y|Cybernetics}}" :
                                "{{Y|Equipment}} {{K|Cybernetics}}");
                        rowCount -= 2;
                        firstRow += 2;
                    }
                    if (relevantBodyParts != null) {
                        keymap.Clear();
                        for (var partIndex = windowStart; partIndex < relevantBodyParts.Count && partIndex - windowStart < rowCount; ++partIndex) {
                            var currentRow = firstRow + partIndex - windowStart;
                            if (selectedIndex == partIndex) {
                                _ = screenBuffer.Goto(27, currentRow)
                                    .Write("{{K|>}}");
                            }
                            _ = screenBuffer.Goto(1, currentRow);
                            var cursorString = selectedIndex == partIndex ? "{{Y|>}}" : " ";
                            var partDesc = "";
                            if (allCybernetics[partIndex] == null) {
                                if (Options.IndentBodyParts) {
                                    partDesc += new string(' ', body.GetPartDepth(relevantBodyParts[partIndex]));
                                }
                                partDesc += relevantBodyParts[partIndex].GetCardinalDescription();
                            } else {
                                partDesc += Grammar.MakeTitleCase(relevantBodyParts[partIndex].GetCardinalName());
                            }
                            if (relevantBodyParts[partIndex].Primary) {
                                partDesc += " {{G|*}}";
                            }
                            var key = (char)('a' + partIndex);
                            if (key > 'z') {
                                key = ' ';
                            } else {
                                keymap.Add(key, partIndex);
                            }

                            var keyString = (selectedIndex == partIndex ? "{{W|" : "{{w|") + key + "}}) ";
                            _ = screenBuffer.Write(cursorString + keyString + partDesc);

                            _ = screenBuffer.Goto(28, currentRow);
                            RenderEvent icon = null;
                            var name = "";
                            var fade = false;
                            if (allEquippedOrDefault[partIndex] == null) {
                                // nothing in this slot
                                _ = screenBuffer.Write(selectedIndex == partIndex ? "{{Y|-}}" : "{{K|-}}");
                            } else if (screenTab == ScreenTab.Cybernetics) {
                                // cybernetics
                                icon = allCybernetics[partIndex].RenderForUI();
                                name = allCybernetics[partIndex].DisplayName;
                            } else if (allEquipped[partIndex] == null) {
                                // natural weapons
                                icon = allEquippedOrDefault[partIndex].RenderForUI();
                                name = allEquippedOrDefault[partIndex].DisplayName;
                                fade = true;
                            } else {
                                // all other equipment
                                icon = allEquipped[partIndex].RenderForUI();
                                name = allEquipped[partIndex].DisplayName;

                                fade = !wornElsewhere.Add(allEquipped[partIndex]) ||
                                    (allEquipped[partIndex].HasTag("RenderImplantGreyInEquipment") && allEquipped[partIndex].GetPart<CyberneticsBaseItem>()?.ImplantedOn != null);
                            }
                            if (icon != null) {
                                if (fade) {
                                    screenBuffer.Write(icon, ColorString: "&K", TileColor: "&K", DetailColor: new char?('K'));
                                } else {
                                    screenBuffer.Write(icon);
                                }
                                _ = screenBuffer.Goto(30, currentRow)
                                    .Write(fade ? "{{K|" + ColorUtility.StripFormatting(name) + "}}" : name);
                            }
                        }
                        if (windowStart + rowCount < relevantBodyParts.Count) {
                            _ = screenBuffer.Goto(2, 24)
                                .Write("<more...>");
                        }
                        if (windowStart > 0) {
                            _ = screenBuffer.Goto(2, 0)
                                .Write("<more...>");
                        }
                        Popup._TextConsole.DrawBuffer(screenBuffer);
                        var keys = Keyboard.getvk(Options.MapDirectionsToKeypad);
                        ScreenBuffer.ClearImposterSuppression();
                        var key1 = (char.ToLower((char)Keyboard.Char).ToString() + " ").ToLower()[0];
                        if (keys == Keys.MouseEvent && Keyboard.CurrentMouseEvent.Event == "RightClick") {
                            Done = true;
                        } else if (keys == Keys.Escape || keys == Keys.NumPad5) {
                            Done = true;
                        } else if (keys == Keys.Prior) {
                            selectedIndex = 0;
                            windowStart = 0;
                        } else if ((keys == Keys.NumPad4 || keys == Keys.NumPad6) && HasCybernetics) {
                            screenTab = screenTab != ScreenTab.Cybernetics ? ScreenTab.Cybernetics : ScreenTab.Equipment;
                            selectedIndex = 0;
                            windowStart = 0;
                            CacheValid = false;
                        } else if (keys == Keys.Next) {
                            selectedIndex = relevantBodyParts.Count - 1;
                            if (relevantBodyParts.Count > rowCount) {
                                windowStart = relevantBodyParts.Count - rowCount;
                            }
                        } else if (keys == Keys.NumPad8) {
                            if (selectedIndex - windowStart <= 0) {
                                if (windowStart > 0) {
                                    --windowStart;
                                }
                            } else if (selectedIndex > 0) {
                                --selectedIndex;
                            }
                        } else if (keys == Keys.NumPad2 && selectedIndex < relevantBodyParts.Count - 1) {
                            if (selectedIndex - windowStart == rowCount - 1) {
                                ++windowStart;
                            }
                            ++selectedIndex;
                        } else if (screenTab != ScreenTab.Cybernetics && (Keyboard.vkCode == Keys.Left || keys == Keys.NumPad4) && allEquipped[selectedIndex] != null) {
                            var bodyPart = relevantBodyParts[selectedIndex];
                            var oldEquipped = allEquipped[selectedIndex];
                            _ = Companion.FireEvent(Event.New("CommandUnequipObject", "BodyPart", bodyPart));
                            if (bodyPart.Equipped != oldEquipped) {
                                // for convenience, put it in the leader's inventory
                                Yoink(oldEquipped, Companion, Leader);
                                Changed = true;
                                CacheValid = false;
                            }
                        } else if (screenTab != ScreenTab.Cybernetics && keys == Keys.Tab) {
                            if (!CanChangePrimaryLimb) {
                                Popup.Show(Companion.The + Companion.ShortDisplayName + " can't switch primary limbs in combat.");
                            } else if (relevantBodyParts[selectedIndex].Abstract) {
                                Popup.Show("This body part cannot be set as " + Companion.its + " primary.");
                            } else if (!CheckPreferredPrimary(relevantBodyParts[selectedIndex])) {
                                relevantBodyParts[selectedIndex].SetAsPreferredDefault();
                                Changed = true;
                            }
                        } else {
                            if (keys == Keys.Enter) {
                                keys = Keys.Space;
                            }
                            var useSelected = keys == Keys.Space || keys == Keys.Enter;
                            if (useSelected || (keys >= Keys.A && keys <= Keys.Z && keymap.ContainsKey(key1))) {
                                var pressedIndex = useSelected ? selectedIndex : keymap[key1];
                                if (allEquipped[pressedIndex] != null) {
                                    var oldEquipped = allEquipped[pressedIndex];
                                    EquipmentAPI.TwiddleObject(Companion, oldEquipped, ref Done);
                                    var bodyPart = relevantBodyParts[pressedIndex];
                                    var curEquipped = screenTab == ScreenTab.Cybernetics ? bodyPart.Cybernetics : bodyPart.Equipped;
                                    if (curEquipped != oldEquipped) {
                                        // for convenience, put it in the leader's inventory
                                        Yoink(oldEquipped, Companion, Leader);
                                        Changed = true;
                                        CacheValid = false;
                                    }
                                } else if (screenTab == ScreenTab.Equipment && ShowBodypartEquipUI(Leader, Companion, relevantBodyParts[pressedIndex])) {
                                    Changed = true;
                                    CacheValid = false;
                                }
                            }
                        }
                    }
                }
            }
            GameManager.Instance.PopGameView();
            return Changed;
        }
        public static bool ShowBodypartEquipUI(GameObject Leader, GameObject Companion, BodyPart SelectedBodyPart) {
            var leaderInventory = Leader.Inventory;
            var inventory = Companion.Inventory;
            if (inventory != null || leaderInventory != null) {
                var EquipmentList = new List<GameObject>(16);
                inventory?.GetEquipmentListForSlot(EquipmentList, SelectedBodyPart.Type);
                leaderInventory?.GetEquipmentListForSlot(EquipmentList, SelectedBodyPart.Type);

                if (EquipmentList.Count > 0) {
                    string CategoryPriority = null;
                    if (SelectedBodyPart.Type == "Hand") {
                        CategoryPriority = "Melee Weapon,Shield,Light Source";
                    } else if (SelectedBodyPart.Type == "Thrown Weapon") {
                        CategoryPriority = "Grenades";
                    }
                    var toEquip = PickItem.ShowPicker(EquipmentList, CategoryPriority, PreserveOrder: true);
                    if (toEquip == null) {
                        return false;
                    }
                    if ((toEquip.GetPart<Stacker>()?.StackCount ?? 1) > 1) {
                        // pick one off the stack for the companion
                        _ = toEquip.SplitStack(1);
                    }
                    _ = Companion.FireEvent(Event.New("CommandEquipObject", "Object", toEquip, "BodyPart", SelectedBodyPart));
                    return true;
                }
                Popup.Show("Neither of you have anything to use in that slot.");
            } else {
                Popup.Show("You both have no inventory!");
            }
            return false;
        }
        public static void Yoink(GameObject Item, GameObject Yoinkee, GameObject Yoinker) {
            if (Item.InInventory != Yoinkee) {
                // probably stacked with something
                var equippedCount = Item.GetPart<Stacker>()?.StackCount ?? 1;
                foreach (var otherItem in Yoinkee.Inventory.Objects) {
                    if (Item.SameAs(otherItem)) {
                        _ = otherItem.SplitStack(equippedCount, Yoinkee);
                        Item = otherItem;
                        break;
                    }
                }
            }
            if (Yoinkee.FireEvent(Event.New("CommandRemoveObject", "Object", Item).SetSilent(true))) {
                _ = Yoinker.TakeObject(Item);
            }
        }
        private enum ScreenTab {
            Equipment = 0,
            Cybernetics = 1,
        }

        /// <summary>
        /// BodyPart had a typo in one of its field names. The devs fixed this typo in [2.0.204.65].
        /// This functions serves as a backwards compatible accessor until beta becomes stable.
        /// <returns>
        /// false if the processed part is NOT the preferred primary weapon, true otherwise
        /// </returns>
        /// </summary>
        public static bool CheckPreferredPrimary(BodyPart part) {
            // TODO: Remove this once [2.0.204.65] is long considered stable.
            FieldInfo prop = part.GetType().GetField("PreferredPrimary") ??
                             part.GetType().GetField("PreferedPrimary");
            if (prop == null) {
                Utility.MaybeLog("Could not find PreferredPrimary field in BodyPart. This could be critical?");

                // This return will expend the player's action turn when it might not need to, but the potential NullReference error
                // codepath below is debatively worse.
                return false;
            }
            return (bool)prop.GetValue(part);
        }
    }
}
