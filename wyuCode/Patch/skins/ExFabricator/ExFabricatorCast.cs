using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace wyu.wyuCode.Patch;

// FABRICATOR 的纯召唤行动默认不会触发 Cast，这里手动补一个。
[HarmonyPatch(typeof(Fabricator), "FabricateMove")]
public static class FabricatorCastTriggerPatch
{
    private static void Prefix(Fabricator __instance)
    {
        try
        {
            // 不阻塞行动流程，发起 Cast 触发让动画器切到 Cast 分支。
            _ = CreatureCmd.TriggerAnim(__instance.Creature, "Cast", 0.35f);
        }
        catch (Exception)
        {
            // 失败时保持原流程，避免影响战斗逻辑。
        }
    }
}
// 这里将召唤小怪的动作映射到Cast上