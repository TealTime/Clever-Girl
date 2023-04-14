namespace CleverGirl.Patches {
    using HarmonyLib;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using XRL.World;
    using CleverGirl.Parts;

    // hide any INoSaveParts so GameObject doesn't try to save them; restore after save
    /// <summary>
    /// Unattach and then afterwards re-attach all currently attached INoSavePart's in the GameObject being saved.
    ///
    /// This is done so that the game will ONLY store our saved properties, but not the INoSavePart objects themselves.
    /// </summary>
    [HarmonyPatch(typeof(GameObject), "Save", new Type[] { typeof(SerializationWriter) })]
    public static class GameObject_Save_Patch {
        private static List<CleverGirl_INoSavePart> cachedParts;
        public static void Prefix(GameObject __instance) {
            cachedParts = __instance.GetPartsDescendedFrom<CleverGirl_INoSavePart>();
            if (cachedParts.Count > 0) {
                _ = __instance.PartsList.RemoveAll(p => p is CleverGirl_INoSavePart);
            }
        }
        public static void Postfix(GameObject __instance) {
            if (cachedParts != null) {
                __instance.PartsList.AddRange(cachedParts);
                cachedParts = null;
            }
        }
    }

    /// <summary>
    /// Re-attach all previously unattached INoSavePart's to the GameObject being loaded. 
    ///
    /// Determine which INoSavePart's to re-attach by querying the unique identifying property (UID_PROPERTY) which is 
    /// contractually registered to the GameObject by all attached INoSavePart's.
    /// </summary>
    [HarmonyPatch(typeof(GameObject), "Load", new Type[] { typeof(SerializationReader) })]
    public static class GameObject_Load_Patch {
        private static List<Type> INoSavePartDescendantClassTypes;  // Cache to speed up future loaded GameObjects
        public static void Postfix(GameObject __instance) {
            if (INoSavePartDescendantClassTypes == null) {
                // Get the subset of defined Type's from within all of Caves of Qud's assemblies that inherit from INoSavePart
                INoSavePartDescendantClassTypes = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(x => x.GetTypes())
                            .Where(x => typeof(CleverGirl_INoSavePart).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                            .ToList();
            }
            foreach (var classType in INoSavePartDescendantClassTypes) {
                string property = classType.GetProperty("PROPERTY")?.GetValue(null) as string ?? "";
                if (property != "" && __instance.HasProperty(property)) {
                    _ = __instance.AddPart(Activator.CreateInstance(classType) as IPart);
                }
            }
        }
    }
}
