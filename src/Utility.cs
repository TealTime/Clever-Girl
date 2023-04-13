namespace CleverGirl {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using XRL;
    using XRL.Rules;
    using XRL.World;
    using CleverGirl.Parts;

    public static class Utility {
        public static bool debug = true;

        public static void MaybeLog(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            if (debug) {
                MetricsManager.LogInfo(filePath + ":" + lineNumber + ": " + message);
            }
        }

        private static readonly Dictionary<string, Random> RandomDict = new Dictionary<string, Random>();
        public static Random Random(IPart part) {
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
                key += "_" + part.ParentObject.id;
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

        // For menu hotkeys
        public static char GetCharInAlphabet(int index) {
            return index >= 26 ? ' ' : (char)('a' + index);
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

        public static string PadTwoStrings(string left, string right, int distance, char fill = '-', char color = 'K') {
            int numPadding = distance - left.Length;
            string padding = (numPadding > 0) ? new string('-', numPadding) : "";
            return left + " {{" + color + "|" + padding + "}} " + right;
        }

        public static List<string> PadTwoCollections(List<string> lefts, List<string> rights, int distance = -1, char fill = '-', char color = 'K') {
            if (lefts.Count != rights.Count) {
                return null;
            }
            var final = new List<string>(lefts.Count);
            distance = (distance == -1) ? lefts.Max(s => s.Length) + 1 : distance;
            for (int i = 0; i < lefts.Count; i++) {
                final.Add(PadTwoStrings(lefts[i], rights[i], distance, fill, color));
            }
            return final;
        }

        /// <summary>
        /// Removes all Clever-Girl parts from companion and followers.
        /// Used after dismissing a companion, but honestly I'm not sure if this is desirable behavior.
        /// </summary>
        public static void CleanCompanion(GameObject companion) {
            List<CleverGirl_INoSavePart> parts = companion.GetPartsDescendedFrom<CleverGirl_INoSavePart>();
            if (parts.Count > 0) {
                _ = companion.PartsList.RemoveAll(p => p is CleverGirl_INoSavePart);
            }
            foreach (var follower in CollectFollowersOf(companion)) {
                CleanCompanion(follower);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Property functions used in the process of storing/retrieving serialization data in INoSaveParts. ///
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Wrapper around GameObject.SetIntProperty() that instead returns a boolean indicating if value changed.
        /// </summary>
        public static bool EditIntProperty(GameObject obj, string propName, int value) {
            bool changed = obj.GetIntProperty(propName) != value;
            _ = obj.SetIntProperty(propName, value);
            return changed;
        }

        public static bool EditIntProperty(GameObject obj, string propName, bool value) {
            return EditIntProperty(obj, propName, value ? 1 : 0);
        }

        /// <summary>
        /// Wrapper around GameObject.SetStringProperty() that instead returns a boolean indicating if value changed.
        /// </summary>
        public static bool EditStringProperty(GameObject obj, string propName, string value) {
            bool changed = obj.GetStringProperty(propName) != value;
            obj.SetStringProperty(propName, value);  // Rant: Why is this void but SetIntProperty returns GameObject???
            return changed;
        }

        /// <summary>
        /// Add or remove a value in a string property collection. Return true if the collection changed.
        /// </summary>
        public static bool EditStringPropertyCollection(GameObject obj, string propName, string value, bool add) {
            var collection = obj.GetStringProperty(propName)?.Split(',').Where(s => !s.IsNullOrEmpty()).ToList();
            if (collection == null) {
                MaybeLog("Trying to edit a string property which doesn't exist? Probably a typo in '" + propName + "', or it was never defined.");
                return false;
            }

            bool existedPrior = collection.Contains(value);
            bool changed = true;
            if (add && !existedPrior) {
                collection.Add(value);
            } else if (!add && existedPrior) {
                _ = collection.Remove(value);
            } else {
                changed = false;
            }

            obj.SetStringProperty(propName, string.Join(",", collection));

            return changed;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        /// Menu classes/enums which should probably be moved to a Menus/ file at some point ///
        ////////////////////////////////////////////////////////////////////////////////////////

        // Format of options to be processed in EventListener
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

        // Format of options to be processed manually in ShowOptionList
        public class OptionAction {
            public string Name;
            public string Display;
            public char Key;
            public string Command;
            public Func<GameObject, GameObject, bool> Valid;
            public InvalidOptionBehavior InvalidBehavior = InvalidOptionBehavior.DARKEN;
            public static bool Adjacent(GameObject leader, GameObject companion) {
                return leader.CurrentCell.IsAdjacentTo(companion.CurrentCell);
            }
        }

        public enum InvalidOptionBehavior {
            DARKEN,  // Make option look darker to indicate the option is invalid
            HIDE,  // Make option disappear entirely
        }
    }
}
