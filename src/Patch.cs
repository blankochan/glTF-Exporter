using MelonLoader;
using HarmonyLib;
namespace glTF_Exporter;

[HarmonyPatch(typeof(Il2CppRUMBLE.CharacterCreation.CharacterCreationLookupTable), nameof(Il2CppRUMBLE.CharacterCreation.CharacterCreationLookupTable.BakeApplyAndCachePlayerVisuals))]
public static class VisualGen_Patch
{
    static void Postfix(Il2CppRUMBLE.CharacterCreation.CharacterCreationLookupTable __instance, string __0, Il2CppRUMBLE.Players.PlayerVisualData __1, bool __2)
    {
        Melon<Exporter>.Instance.StartExport(__0, __1);
    }
}
