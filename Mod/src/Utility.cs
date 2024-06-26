namespace CleverGirl {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using XRL;
    using XRL.Rules;
    using XRL.World;
    using XRL.UI;
    using ConsoleLib.Console;

    using static CleverGirl.Static;

    public static class Utility {
        public static void MaybeLog(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            if (OptionDebug) {
                MetricsManager.LogInfo(filePath + ":" + lineNumber + ": " + message);
            }
        }

        private static readonly Dictionary<string, Random> RandomDict = new Dictionary<string, Random>();
        /// <summary>
        /// Create a random number generator that is seeded by the typename and ID of a part.
        /// [Note to future authors]: I speculate this was originally implemented to combat "save scumming" (for companion skills/mutations), 
        /// but I am unsure so please investigate the original author's intentions.
        /// </summary>
        public static Random SeededRandom(IPart part) {
            var key = GetKey(part);
            if (!RandomDict.ContainsKey(key)) {
                MaybeLog("Creating Random " + key);
                RandomDict[key] = Stat.GetSeededRandomGenerator(key);
            }
            return RandomDict[key];
        }

        public static int Roll(string Dice, IPart part) {
            return Stat.Roll(Dice, GetKey(part));
        }

        private static string GetKey(IPart part) {
            var key = "Kizby_CleverGirl_" + part.GetType().Name;
            if (part.ParentObject != null) {
                key += "_" + part.ParentObject.ID;
            }
            return key;
        }

        private static readonly Dictionary<string, Regex> RegexCache = new Dictionary<string, Regex>();
        public static string RegexReplace(string String, string Regex, string Replacement) {
            if (!RegexCache.ContainsKey(Regex)) {
                RegexCache.Add(Regex, new Regex(Regex));
            }
            return RegexCache[Regex].Replace(String, Replacement);
        }

        /// <summary>
        /// a lot of messages in the game hardcode "you" pronouns and need reconjugating
        /// I have /zero/ interest in generalizing distinguishing verbs, so hardcode the ones we've seen and treat as an object otherwise
        /// </summary>
        public static string AdjustSubject(string Message, GameObject Subject) {
            if (Subject.IsPlural || Subject.IsPseudoPlural) {
                Message = RegexReplace(Message, "\\bUsted ama\\b", "Ustedes aman");
            }
            Message = RegexReplace(Message, "\\bYou feel\\b", Subject.It + Subject.GetVerb("feel"));
            Message = RegexReplace(Message, "\\bYou don't\\b", Subject.It + Subject.GetVerb("don't"));
            Message = RegexReplace(Message, "\\byou start\\b", Subject.it + Subject.GetVerb("start"));
            Message = RegexReplace(Message, "\\byour\\b", Subject.its);
            Message = RegexReplace(Message, "\\bYour\\b", Subject.Its);
            Message = RegexReplace(Message, "\\byou\\b", Subject.them);
            return Message;
        }

        public static List<GameObject> CollectNearbyCompanions(GameObject Leader) {
            var result = new List<GameObject>();

            // allow companions to be daisy-chained so long as they're adjacent to each other
            var toInspect = new List<Cell> { Leader.CurrentCell };
            for (int i = 0; i < toInspect.Count; ++i) {
                var cell = toInspect[i];
                cell.ForeachObject(obj => {
                    if (obj == Leader || obj.IsLedBy(Leader)) {
                        cell.ForeachLocalAdjacentCell(adj => {
                            if (!toInspect.Contains(adj)) {
                                toInspect.Add(adj);
                            }
                        });
                        if (obj != Leader) {
                            result.Add(obj);
                        }
                    }
                });
            }
            return result;
        }

        public static IEnumerable<GameObject> CollectFollowersOf(GameObject Leader) {
            return The.ActiveZone.FindObjects(obj => obj.IsLedBy(Leader));
        }

        public static int ShowTabularPopup(string Title, List<List<string>> Columns, List<int> ColumnWidths = null, List<IRenderable> Icons = null, IRenderable IntroIcon = null) {
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

        public class InventoryAction {
            public string Name;
            public string Display;
            public string Command;
            public char Key;
            public Predicate<IInventoryActionsEvent> Valid = _ => true;
            public static bool Adjacent(IInventoryActionsEvent e) {
                return e.Actor.CurrentCell.IsAdjacentTo(e.Object.CurrentCell);
            }
        }
    }
}
