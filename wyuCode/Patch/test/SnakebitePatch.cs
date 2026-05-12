using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;


// [HarmonyPatch(typeof(Snakebite), "Get_CanonicalVars")]
// public static class SnakebitePatch
// {

//     // 加在函数执行之后
//     static void Postfix(ref IEnumerable<DynamicVar> __result)
//     {
//         __result = new List<DynamicVar>
//         {
//             new PowerVar<PoisonPower>(0),
//         };
//     }
// }

[HarmonyPatch(typeof(TheArchitect))]
public static class TheArchitectPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("WinRun")]
    static bool BeforeWinRun(TheArchitect __instance)
    {
        if (LocalContext.IsMe(__instance.Owner))
        {
            if(__instance.Owner.Character.Id.ToString() == "")
            {
                RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
                return false;// false会跳过原函数, true会继续执行原函数
            }
        }
        return true;
    }
}


