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
// ref: 修改原函数的返回值
// HarmonyPatch有3个参量可选:typeof(NMerchantDialogue), "ShowRandom", new[] { typeof(IEnumerable<LocString>) }
// 分别为 类名, 方法名, 方法参数类型列表
// Prefix(ref IEnumerable<LocString> lines) 可以把原方法的参数截胡
// [HarmonyPatch(typeof(NMerchantDialogue), "ShowRandom", new[] { typeof(IEnumerable<LocString>) })]
// public static class ShoperDialogueManager
// {
//     static bool Prefix(ref IEnumerable<LocString> lines)
//     {
//          var customLine = ShoperAudioManager.linePath;
//         if (customLine == null)
//             return true;

//         lines = new List<LocString> { customLine };
//         return true;
//     }
// }
