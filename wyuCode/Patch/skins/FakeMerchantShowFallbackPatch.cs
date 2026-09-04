using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers; // RunWhenSpineReady
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 假商人事件（NFakeMerchant）玩家立绘的 relaxed_loop 兜底。
/// 该事件用玩家的战斗视觉(CreateVisuals)做展示并写死播 "relaxed_loop"，
/// 而玩家自定义骨骼没有该动画名会报错/卡住。
/// 这里：骨骼没有 relaxed_loop 时，接管并改播它自己存在的站姿动画（idle_loop/Idle 等），
/// 不换骨骼、不改皮肤——只是让展示能正常播。有 relaxed_loop 的原版骨骼走原逻辑。
/// </summary>
[HarmonyPatch(typeof(NFakeMerchant), "StartCharacterAnimation")]
public static class FakeMerchantShowFallbackPatch
{
    private static readonly string[] ShowAnimCandidates =
        { "relaxed_loop", "Idle", "idle_loop", "Skill_Idle", "Default" };

    [HarmonyPrefix]
    static bool Prefix(NCreatureVisuals visuals)
    {
        try
        {
            if (visuals?.SpineBody?.BoundObject is not Node2D body)
            {
                return true;
            }

            var mega = new MegaSprite(body);

            // 骨骼有 relaxed_loop → 走游戏原逻辑
            if (mega.HasAnimation("relaxed_loop"))
            {
                return true;
            }

            // 无 relaxed_loop：等 ready 后改播骨骼自有的展示动画（站姿/idle）
            visuals.RunWhenSpineReady(mega, _ =>
            {
                foreach (string candidate in ShowAnimCandidates)
                {
                    if (mega.HasAnimation(candidate))
                    {
                        visuals.SpineBody?.GetAnimationState().SetAnimation(candidate, true);
                        break;
                    }
                }
            });
            return false; // 接管，跳过原 SetAnimation("relaxed_loop")
        }
        catch (Exception ex)
        {
            GD.PrintErr("[wyu][立绘] StartCharacterAnimation relaxed 兜底异常: " + ex);
            return true;
        }
    }
}
