using BulwarkStudios.Stanford.Core.UI;
using BulwarkStudios.Stanford.Menu.UI;
using HarmonyLib;
using I2.Loc;

namespace IMHelper;

public static class Patches
{
    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslation))]
    public static class LocalizationPatch
    {
        public static void Postfix(ref string __result, object[] __args)
        {
            if (__args[0] != null && __args[0].ToString()!.Contains("captnced/"))
                __result = __args[0].ToString()!.Replace("captnced/", "");
        }
    }

    [HarmonyPatch(typeof(UIEscapeChecker), nameof(UIEscapeChecker.Ui_OnClosedManually))]
    public static class EscCheckerPatch
    {
        public static void Prefix()
        {
            if (PopupHelper.textPopup.isOpened) PopupHelper.textPopup.close();
        }
    }

    [HarmonyPatch(typeof(UIWindowLoadGameItem), nameof(UIWindowLoadGameItem.Initialize))]
    public static class LoadMenuItemPatch
    {
        public static void Postfix(UIWindowLoadGameItem __instance)
        {
            SavesHelper.setupLoadMenuItem(__instance);
        }
    }
    
    [HarmonyPatch(typeof(UIWindowSaveGameItem), nameof(UIWindowSaveGameItem.Initialize))]
    public static class SaveMenuItemPatch
    {
        public static void Postfix(UIWindowSaveGameItem __instance)
        {
            SavesHelper.setupSaveMenuItem(__instance);
        }
    }
}