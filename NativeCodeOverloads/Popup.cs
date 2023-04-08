// Custom 
namespace XRL.World.CleverGirl.NativeCodeOverloads {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.UI;
    using XRL.Language;
    using ConsoleLib.Console;
    using Qud.UI;

    public class CleverGirl_Popup : Popup {
        /// <summary>
        /// Copied the exact source of XRL.UI.Popup.PickSeveral(), with small changes to include initial starting state.
        /// </summary>
        public static List<int> PickSeveral(
            string Title = "", 
            string[] Options = null, 
            char[] Hotkeys = null, 
            int Amount = -1, 
            int Spacing = 0, 
            string Intro = null, 
            int MaxWidth = 60, 
            bool RespectOptionNewlines = false, 
            bool AllowEscape = false, 
            int DefaultSelected = 0, 
            string SpacingText = "", 
            Action<int> OnResult = null, 
            XRL.World.GameObject Context = null, 
            IRenderable[] Icons = null, 
            IRenderable IntroIcon = null, 
            bool CenterIntro = false, 
            bool CenterIntroIcon = true, 
            int IconPosition = -1, 
            bool ForceNewPopup = false, 
            List<int> InitialState = null)  // <-- MODIFICATION: New optional parameter to provide starting selection state
        {
            List<int> list = (InitialState == null) ? new List<int>() : new List<int>(InitialState);  // <-- MODIFICATION: Setup initializer to instead use new optional parameter if it exists
            string[] array = new string[Options.Length];
            QudMenuItem[] array2 = new QudMenuItem[2]
            {
                new QudMenuItem
                {
                    text = "{{W|[Backspace]}} {{y|Accept}}",
                    command = "option:-2",
                    hotkey = "Backspace"
                },
                new QudMenuItem
                {
                    command = "option:-3",
                    hotkey = "Tab"
                }
            };
            while (true)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = (list.Contains(i) ? "{{W|[þ]}} " : "{{y|[ ]}} ");
                    array[i] += Options[i];
                }
                array2[1].text = ((list.Count == array.Length) ? "{{W|[Tab]}} {{y|Deselect All}}" : "{{W|[Tab]}} {{y|Select All}}");
                int num = ShowOptionList(Title, array, Hotkeys, Spacing, Intro, MaxWidth, RespectOptionNewlines, AllowEscape, DefaultSelected, SpacingText, OnResult, Context, Icons, IntroIcon, array2, CenterIntro, CenterIntro, IconPosition, ForceNewPopup);
                switch (num)
                {
                case -1:
                    return null;
                case -2:
                    if (Amount >= 0 && list.Count > Amount)
                    {
                        Show("You cannot select more than " + Grammar.Cardinal(Amount) + " options!");
                        continue;
                    }
                    return list;
                case -3:
                    if (list.Count != array.Length)
                    {
                        list.Clear();
                        list.AddRange(Enumerable.Range(0, array.Length));
                    }
                    else
                    {
                        list.Clear();
                    }
                    continue;
                }
                int num2 = list.IndexOf(num);
                if (num2 >= 0)
                {
                    list.RemoveAt(num2);
                }
                else
                {
                    list.Add(num);
                }
                DefaultSelected = num;
            }
        }
    }
};