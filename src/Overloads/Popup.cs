/// Custom menus that are copied and modified directly from disassembled Qud code.
///
/// The function(s)/method(s) in this file are intended to be temporary, with the idea in mind that they may or may not be
/// eclipsed by a new developer implementation at some point.

namespace XRL.World.CleverGirl.NativeCodeOverloads {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.UI;
    using ConsoleLib.Console;
    using Qud.UI;

    public class CleverGirl_Popup {
        public class YieldResult {
            public YieldResult(int _Index, bool _Value) {
                Index = _Index;
                Value = _Value;
            }
            public int Index { get; }
            public bool Value { get; }
        }

        /// <summary>
        /// Copied the exact source of 'XRL.UI.Popup.PickSeveral', and edited it to
        ///     1.) Allow for prepopulation of selected options (InitialSelections optional parameter)
        ///     2.) Yield selections as they occur, instead of having to use Backspace to accept all options at once.
        ///         I found myself pressing 'Esc' instead of 'Backspace' and being confused far too often why my selections weren't being saved.
        ///     3.) Have post-hook functionality for special options. IE: greyed/disabled ones
        /// </summary>
        public static IEnumerable<YieldResult> YieldSeveral(
            string Title = "",
            string[] Options = null,
            char[] Hotkeys = null,
            // int Amount = -1,  // Removed 'Amount' all together as I can't find an suitable way to reconcile the edge-case of
            //                   // initial selection state starting with too many options selected.
            int Spacing = 0,
            string Intro = null,
            int MaxWidth = 60,
            bool RespectOptionNewlines = false,
            bool AllowEscape = false,
            int DefaultSelected = 0,
            string SpacingText = "",
            Action<int> OnResult = null,
            Predicate<int> OnPost = null,  // Another shameless addition added because Clever Girl has some options that are nice
                                           // to see visually, but can't be selected. OnResult doesn't seem to fit this use case
                                           // as it is void return type, so I made this so that it can truly gate invalid options.
                                           // I understand this might be a design mistake, (giving callers control over internals)
                                           // but I'll leave that up to the Pros to decide.
            GameObject Context = null,
            IRenderable[] Icons = null,
            IRenderable IntroIcon = null,
            bool CenterIntro = false,
            bool CenterIntroIcon = true,
            int IconPosition = -1,
            bool ForceNewPopup = false,
            List<int> InitialSelections = null)  // New optional parameter to provide starting selection state
        {
            List<int> list = (InitialSelections == null) ? new List<int>() : new List<int>(InitialSelections);  // Setup initializer to instead use new optional parameter if it exists
            string[] array = new string[Options.Length];
            QudMenuItem[] array2 = new QudMenuItem[1]
            {
                new QudMenuItem
                {
                    command = "option:-3",
                    hotkey = "Tab"
                }
            };
            while (true) {
                for (int i = 0; i < array.Length; i++) {
                    array[i] = list.Contains(i) ? "{{W|[þ]}} " : "{{y|[ ]}} ";
                    array[i] += Options[i];
                }
                array2[0].text = (list.Count == array.Length) ? "{{W|[Tab]}} {{y|Deselect All}}" : "{{W|[Tab]}} {{y|Select All}}";
                int num = Popup.ShowOptionList(Title, array, Hotkeys, Spacing, Intro, MaxWidth, RespectOptionNewlines, AllowEscape, DefaultSelected, SpacingText, OnResult, Context, Icons, IntroIcon, array2, CenterIntro, CenterIntro, IconPosition, ForceNewPopup);
                if (!OnPost(num)) {  // Check num to see if it's a valid option, otherwise skip
                    continue;
                }
                switch (num) {
                    case -1:  // Esc / Cancelled
                        yield break;
                    case -3:  // Tab
                        var tempList = new List<int>(list);  // Temporary copy for reference in yielding only changed options
                        if (list.Count != array.Length) {
                            list.Clear();
                            list.AddRange(Enumerable.Range(0, array.Length));
                            foreach (var n in list.Except(tempList)) {  // Only yield options that were added, not those that were unchanged
                                yield return new YieldResult(n, true);
                            }
                        } else {
                            list.Clear();
                            foreach (var n in tempList) {
                                yield return new YieldResult(n, false);
                            }
                        }
                        continue;
                    default:
                        break;
                }
                int num2 = list.IndexOf(num);
                if (num2 >= 0) {
                    list.RemoveAt(num2);
                    yield return new YieldResult(num, false);
                } else {
                    list.Add(num);
                    yield return new YieldResult(num, true);
                }
                DefaultSelected = num;
            }
        }

    }
};
