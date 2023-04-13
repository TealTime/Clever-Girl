/// Custom menus that are copied and modified directly from disassembled Qud code.
///
/// The function(s)/method(s) in this file are intended to be temporary, with the idea in mind that they may or may not be
/// eclipsed by a new developer implementation at some point.

namespace CleverGirl.Menus.Overloads {
    using ConsoleLib.Console;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.UI;
    using XRL.World;
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
        ///     3.) Allow for locking of options
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
            GameObject Context = null,
            IRenderable[] Icons = null,
            IRenderable IntroIcon = null,
            bool CenterIntro = false,
            bool CenterIntroIcon = true,
            int IconPosition = -1,
            bool ForceNewPopup = false,
            int[] InitialSelections = null,  // New optional parameter to provide starting selection state
            int[] LockedOptions = null)      // New optional parameter to lock certain options. Might be better to instantiate
                                             // objects if there's multiple special option types beyond just locking in future.
        {
            LockedOptions = LockedOptions ?? new int[0];
            var list = (InitialSelections is null) ? new List<int>() : new List<int>(InitialSelections.Except(LockedOptions));  // Setup initializer to instead use new optional parameter if it exists
            int numEnabledOptions = Options.Length - LockedOptions.Count();

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
                    if (LockedOptions.Contains(i)) {
                        array[i] = "{{K|[X] " + ColorUtility.StripFormatting(Options[i]) + "}}";
                        continue;
                    }
                    array[i] = list.Contains(i) ? "{{W|[þ]}} " : "{{y|[ ]}} ";
                    array[i] += Options[i];
                }
                array2[0].text = (list.Count < numEnabledOptions) ? "{{W|[Tab]}} {{y|Select All}}" : "{{W|[Tab]}} {{y|Deselect All}}";
                int num = Popup.ShowOptionList(Title, array, Hotkeys, Spacing, Intro, MaxWidth, RespectOptionNewlines, AllowEscape, DefaultSelected, SpacingText, OnResult, Context, Icons, IntroIcon, array2, CenterIntro, CenterIntro, IconPosition, ForceNewPopup);
                if (num >= 0 && LockedOptions.Contains(num)) {
                    continue;
                }
                switch (num) {
                    case -1:  // Esc / Cancelled
                        yield break;
                    case -3:  // Tab
                        var tempList = new List<int>(list);  // Temporary copy for reference in yielding only changed options
                        if (list.Count < numEnabledOptions) {
                            list.Clear();
                            list.AddRange(Enumerable.Range(0, array.Length).Except(LockedOptions));
                            // Yield options that changed
                            foreach (var n in list.Except(tempList).Where(n => !LockedOptions.Contains(n))) {
                                yield return new YieldResult(n, true);
                            }
                        } else {
                            list.Clear();
                            // Yield options that changed
                            foreach (var n in tempList.Where(n => !LockedOptions.Contains(n))) {
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
