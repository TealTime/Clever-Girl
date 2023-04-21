namespace CleverGirl.Menus {
    using ConsoleLib.Console;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.UI;
    using XRL.World;
    using Qud.UI;
    using CleverGirl.Events;

    public class CleverGirl_BasicMenu {
        public static void SetupOptions(GameObject leader,
                                        GameObject companion,
                                        List<Utility.OptionAction> allOptions,  // NOTE: Once C# >= 7.2, this should be "in" reference
                                        ref List<Utility.OptionAction> filteredOptions,
                                        ref List<string> optionNames,
                                        ref List<char> optionHotkeys,
                                        ref List<bool> optionValidities) {

            // Filter options, keeping only those that are valid or NOT set to HIDE behavior
            filteredOptions = allOptions.Where(o =>
                o.Valid(leader, companion) ||
                o.InvalidBehavior != Utility.InvalidOptionBehavior.HIDE).ToList();

            optionNames.Clear();
            optionHotkeys.Clear();
            optionValidities.Clear();

            // Populate option information lists
            foreach (var option in filteredOptions) {
                string finalName = option.Display;

                // Handle options with special behaviors
                if (!option.Valid(leader, companion)) {
                    optionValidities.Add(false);
                    if (option.InvalidBehavior == Utility.InvalidOptionBehavior.DARKEN) {
                        finalName = "{{K|" + ColorUtility.StripFormatting(finalName) + "}}";
                    }
                } else {
                    optionValidities.Add(true);
                }

                optionNames.Add(finalName);
                optionHotkeys.Add(option.Key);
            }
        }
        /// <summary>
        /// Handle the interactive menu for managing companion auto-equip on a per-BodyPart basis.
        /// </summary>
        /// <returns>
        /// boolean flag to indicate that energy was spent talking with companion (true), or not (false)
        /// </returns>
        public static bool Start(GameObject leader,
                                 GameObject companion,
                                 List<Utility.OptionAction> allOptions,  // NOTE: Once C# >= 7.2, this should be "in" reference
                                 string Title = "",
                                 string[] Options = null,
                                 char[] Hotkeys = null,
                                 int Spacing = 0,
                                 string Intro = null,
                                 int MaxWidth = 60,
                                 bool RespectOptionNewlines = false,
                                 bool AllowEscape = false,
                                 int defaultSelected = 0,
                                 string SpacingText = "",
                                 Action<int> onResult = null,
                                 GameObject context = null,
                                 IRenderable[] Icons = null,
                                 IRenderable IntroIcon = null,
                                 QudMenuItem[] Buttons = null,
                                 bool centerIntro = false,
                                 bool centerIntroIcon = true,
                                 int iconPosition = -1,
                                 bool forceNewPopup = false) {

            var optionNames = new List<string>(allOptions.Count);
            var optionHotkeys = new List<char>(allOptions.Count);
            var optionValidities = new List<bool>(allOptions.Count);
            var filteredOptions = new List<Utility.OptionAction>(allOptions.Count);


            // Gear management menu loop
            int lastIndex = -1;
            while (true) {
                SetupOptions(leader, companion, allOptions, ref filteredOptions, ref optionNames, ref optionHotkeys, ref optionValidities);
                var index = Popup.ShowOptionList(Title: Title,
                                                 Options: optionNames.ToArray(),
                                                 Hotkeys: optionHotkeys.ToArray(),
                                                 Spacing: Spacing,
                                                 Intro: Intro,
                                                 MaxWidth: MaxWidth,
                                                 RespectOptionNewlines: RespectOptionNewlines,
                                                 AllowEscape: AllowEscape,
                                                 defaultSelected: (lastIndex >= 0) ? lastIndex : defaultSelected,
                                                 SpacingText: SpacingText,
                                                 onResult: onResult,
                                                 context: context,
                                                 Icons: Icons,
                                                 IntroIcon: IntroIcon,
                                                 Buttons: Buttons,
                                                 centerIntro: centerIntro,
                                                 centerIntroIcon: centerIntroIcon,
                                                 iconPosition: iconPosition,
                                                 forceNewPopup: forceNewPopup);

                // User cancelled, abort!
                if (index < 0) {
                    break;
                } else if (optionValidities[index]) {
                    _ = leader.HandleEvent(CleverGirl_MenuSelectEvent.FromPool(leader, companion, filteredOptions[index].Command));
                }
                lastIndex = index;
            }

            return false;  // Navigating menus shouldn't cost player energy
        }
    }
}
