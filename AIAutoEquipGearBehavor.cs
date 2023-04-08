namespace XRL.World.Parts {
    using ConsoleLib.Console;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using XRL.World.AI.GoalHandlers;
    using Qud.API;
    using XRL.World.CleverGirl;
    using XRL.World.Anatomy;
    using XRL.UI;

    [Serializable]
    public class CleverGirl_AIAutoEquipGearBehavior : CleverGirl_INoSavePart {
        public static string PROPERTY => "CleverGirl_AIAutoEquipGearBehavior";
        public static string IGNOREDBODYPARTS_PROPERTY => PROPERTY + "_IgnoredBodyParts";
        public override void Register(GameObject Object) {
            _ = Object.SetIntProperty(PROPERTY, 1);
            if (!Object.HasStringProperty(IGNOREDBODYPARTS_PROPERTY)) {
                Object.SetStringProperty(IGNOREDBODYPARTS_PROPERTY, "");
            }
        }
        public override void Remove() {
            ParentObject.RemoveIntProperty(PROPERTY);
            ParentObject.RemoveStringProperty(IGNOREDBODYPARTS_PROPERTY);
        }

        public List<string> IgnoredBodyParts {
            get => ParentObject.GetStringProperty(IGNOREDBODYPARTS_PROPERTY).Split(',').Where(s => !s.IsNullOrEmpty()).ToList();
            set => ParentObject.SetStringProperty(IGNOREDBODYPARTS_PROPERTY, string.Join(",", value));
        }
    }
}