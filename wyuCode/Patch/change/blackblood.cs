using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace wyu.wyuCode.Patch;

[HarmonyPatch]
public static class BlackBloodPatch
{
    // ---- 效果 ----
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
    [HarmonyPrefix]
    private static bool NoBlock(Creature target, ref decimal __result,
        ref IEnumerable<AbstractModel> modifiers)
    {
        if (!Has(target)) return true;
        __result = 0m;
        modifiers = System.Array.Empty<AbstractModel>();
        return false;
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCurrentHpChanged))]
    [HarmonyPrefix]
    private static void OnHpChanged(IRunState runState, CombatState? combatState,
        Creature creature, decimal delta)
    {
        if (delta >= 0 || creature.IsDead || !Has(creature)) return;
        Find(creature)?.Flash();
        TaskHelper.RunSafely(CreatureCmd.Heal(creature, 12m));
    }

    // ---- 文本：劫持 LocString 渲染出口 ----
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetFormattedText))]
    [HarmonyPrefix]
    private static bool FixText(LocString __instance, ref string __result)
    {
        if (__instance.LocTable != "relics" || !__instance.LocEntryKey.StartsWith("BLACK_BLOOD"))
            return true;

        __result = __instance.LocEntryKey.Contains(".title")
            ? "黑曼巴之血"
            : "每当损失生命值时，恢复[green]12[/green]点生命。\n你无法获得格挡。";
        return false;
    }

    private static bool Has(Creature c) => Find(c) != null;
    private static BlackBlood? Find(Creature c)
        => c.Player?.Relics.OfType<BlackBlood>().FirstOrDefault();
}
