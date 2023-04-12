namespace CleverGirl.Menus {
    using HarmonyLib;
    using ConsoleLib.Console;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using XRL;
    using XRL.UI;
    using XRL.World;
    using XRL.Rules;

    public static class CleverGirl_CompanionsMenu {
        private static readonly PropertyInfo DisplayNameBaseProperty = AccessTools.Property(typeof(GameObject), "DisplayNameBase");
        private static string CompanionName(GameObject Companion) => ColorUtility.ClipToFirstExceptFormatting(DisplayNameBaseProperty.GetValue(Companion) as string, ',');

        public static void OpenMenu() {
            if (The.Player == null) {
                // too early?
                return;
            }
            var companionMap = new Dictionary<GameObject, SortedSet<GameObject>>();
            The.ActiveZone.ForeachObject(o => {
                if (o.IsPlayerLed()) {
                    var set = companionMap.GetValue(o.PartyLeader);
                    if (set == null) {
                        set = new SortedSet<GameObject>(Comparer<GameObject>.Create((a, b) => {
                            var c = ColorUtility.CompareExceptFormattingAndCase(CompanionName(a), CompanionName(b));
                            if (c != 0) {
                                return c;
                            }
                            return a.id.CompareTo(b.id);
                        }));
                        companionMap.Add(o.PartyLeader, set);
                    }
                    _ = set.Add(o);
                }
            });
            if (companionMap.GetValue(The.Player) == null) {
                // no companions
                return;
            }

            var names = new List<string>();
            var status = new List<string>();
            var effects = new List<string>();
            var icons = new List<IRenderable>();
            var companionList = new List<GameObject>();
            /// <summary>
            /// Gather and store important properties/fields from all player companions recursively, to eventually be dumped as menu options.
            /// </summary>
            void HarvestFields(IEnumerable<GameObject> Companions, string IndentString = "") {
                foreach (var companion in Companions) {
                    companionList.Add(companion);
                    names.Add(IndentString + CompanionName(companion));
                    icons.Add(companion.pRender);
                    if (!companion.IsVisible()) {
                        status.Add((companion.IsAudible(The.Player) ? "{{W|" : "{{O|") + The.Player.DescribeDirectionToward(companion.CurrentCell) + "}}");
                        effects.Add("");
                    } else {
                        status.Add(Strings.WoundLevel(companion));
                        var effectString = "";
                        foreach (var effect in companion.Effects) {
                            var description = effect.GetDescription();
                            if (!description.IsNullOrEmpty()) {
                                if (effectString.Length > 0) {
                                    effectString += ", ";
                                }
                                effectString += description;
                            }
                        }
                        effects.Add(effectString);
                    }
                    if (companionMap.TryGetValue(companion, out SortedSet<GameObject> subCompanions)) {
                        HarvestFields(subCompanions, IndentString + "\xFF");
                    }
                }
            }
            HarvestFields(companionMap[The.Player]);

            var selected = ShowTabularPopup("Companions", new List<List<string>>() { names, status, effects }, new List<int> { 30, 20, 20 }, icons, The.Player.pRender);
            if (selected != -1) {
                // Interact with companion, if possible
                _ = companionList[selected].Twiddle();
            }
        }

        /// <summary>
        /// Custom option menu that automatically performs vertical category alignment.
        /// </summary>
        private static int ShowTabularPopup(string Title, List<List<string>> Columns, List<int> ColumnWidths = null, List<IRenderable> Icons = null, IRenderable IntroIcon = null) {
            if (ColumnWidths == null) {
                ColumnWidths = new List<int>();
                foreach (var column in Columns) {
                    ColumnWidths.Add(column.Max(row => ColorUtility.LengthExceptFormatting(row)));
                }
            } else {
                for (var i = 0; i < Columns.Count; ++i) {
                    var maxWidth = Columns[i].Max(row => ColorUtility.LengthExceptFormatting(row));
                    if (maxWidth < ColumnWidths[i]) {
                        // shrink columns to actual content when possible
                        ColumnWidths[i] = maxWidth;
                    }
                }
            }
            var lines = new string[Columns.Max(c => c.Count)];
            for (int row = 0; row < lines.Length; ++row) {
                lines[row] = "{{y|";
                for (int column = 0; column < Columns.Count; ++column) {
                    if (Columns[column].Count <= row) {
                        continue;
                    }
                    var entry = Columns[column][row];
                    if (entry.Length == 0) {
                        continue;
                    }
                    var padding = ColumnWidths[column] - ColorUtility.LengthExceptFormatting(entry);
                    if (padding < 0) {
                        padding = 0;
                    }
                    if (column > 0) {
                        lines[row] += " | ";
                    }
                    lines[row] += entry + new string('\xFF', padding);
                }
                lines[row] += "}}";
            }
            var hotkeys = new char[lines.Length];
            for (var i = 0; i < hotkeys.Length; ++i) {
                hotkeys[i] = i < 26 ? (char)('a' + i) : ' ';
            }
            return Popup.ShowOptionList(Title: Title, Options: lines, Hotkeys: hotkeys, IntroIcon: IntroIcon, Icons: Icons.ToArray(), AllowEscape: true);
        }

    }
}
