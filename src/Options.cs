// Global namespace holding all player selectable options pertaining to Clever-Girl.
// I shamelessly stole the format from QudUX-v2 Mod.
namespace CleverGirl.Globals {
    using static XRL.UI.Options;

    // All of the OR IsNullOrEmpty bits below are added to temporarily address this bug:
    // https://bitbucket.org/bbucklew/cavesofqud-public-issue-tracker/issues/4118
    // They should be removed after that is fixed.
    public static class Options {
        public static bool DirectingCompanionCostsEnergy => GetOption("CleverGirl_DirectingCompanionCostsEnergy").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("CleverGirl_DirectingCompanionCostsEnergy"));
        public static bool ShowSillyText => GetOption("CleverGirl_ShowSillyText").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("CleverGirl_ShowSillyText"));
        public static bool TalkFromAfar => GetOption("CleverGirl_TalkFromAfar").EqualsNoCase("Yes") || string.IsNullOrEmpty(GetOption("CleverGirl_TalkFromAfar"));
    }
}
