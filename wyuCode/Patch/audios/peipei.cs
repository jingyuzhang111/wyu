using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using wyu.wyuCode.Cards;



[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class BGMControlPatch
{
    static void Postfix()
    {
        PeiPei.StopBGM();
    }
}