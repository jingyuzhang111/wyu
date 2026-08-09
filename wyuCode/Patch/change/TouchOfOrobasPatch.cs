using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using wyu.wyuCode.Relics;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 把「绝食(JueShi)→ 武者(JueShi2)」注册进欧罗巴斯之触(TouchOfOrobas)的精炼表 RefinementUpgrades。
/// 走官方路径,无需手动替换:
/// - 图鉴/悬停显示:GetUpgradedStarterRelic 会查到武者并正常弹出升级遗物信息
/// - 获得时:官方 AfterObtained 自动把绝食替换成武者
/// </summary>
[HarmonyPatch]
public static class TouchOfOrobasPatch
{
    [HarmonyPatch(typeof(TouchOfOrobas), "get_RefinementUpgrades")]
    [HarmonyPostfix]
    private static void AddJueShiRefinement(ref Dictionary<ModelId, RelicModel> __result)
    {
        // 注册精炼关系:绝食 → 武者
        __result[ModelDb.Relic<JueShi>().Id] = ModelDb.Relic<JueShi2>();
    }
}
