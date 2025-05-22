using BulwarkStudios.Stanford.Core.UI;
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
            if (__args[0] != null && __args[0].ToString().Contains("captnced/"))
                __result = __args[0].ToString().Replace("captnced/", "");
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
}