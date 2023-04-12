/// File used to create backwards compatible implementations for instances where:
///     A.) beta/alpha have changed something that, if fixed, will break on stable.
///     B.) A Clever Girl change could break pre-existing saves.

namespace CleverGirl.BackwardsCompatibility {
    using System.Reflection;
    using XRL.World.Anatomy;

    public class CleverGirl_BackwardsCompatibility {

        /// <summary>
        /// BodyPart had a typo in one of its field names. The devs fixed this typo in [2.0.204.65].
        /// This functions serves as a backwards compatible accessor until beta becomes stable.
        /// <returns>
        /// false if the processed part is NOT the preferred primary weapon, true otherwise
        /// </returns>
        /// </summary>
        public static bool IsPreferredPrimary(BodyPart part) {
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
