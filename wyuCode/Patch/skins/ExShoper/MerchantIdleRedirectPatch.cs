using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 消除可露希尔商人换骨后的 "Can not find animation: idle_loop"。
///
/// 背景：游戏 NMerchantButton._Ready 通过 RunWhenSpineReady 注册一个回调，
/// spine 就绪后固定执行 animState.SetAnimation("idle_loop")。原版商人骨骼带 idle_loop，
/// 但我们把骨骼换成了 keluxier（方舟命名：Idle / Skill_2_Idle / Start / Default / Attack_*），
/// 没有 idle_loop → native 层报错，且模型停在初始姿势不做待机。
///
/// 做法：patch SpineNodeExtensions.RunWhenSpineReady，仅当 host 是 NMerchantButton 时
/// 把回调包一层。回调执行时 spine 一定已 ready，可准确用 sprite.HasAnimation 判定：
///   - 骨骼带 idle_loop（未换骨的原版商人）→ 原样放行，不误伤；
///   - 骨骼无 idle_loop（已被我们换骨）→ 把待机请求重定向到 keluxier 真实存在的动画，
///     彻底消除 error，且商人能正常播放待机动画。
///
/// 故意不调用原回调（原回调 = SetAnimation("idle_loop") + 赋值 _merchantSkeleton）：
/// 赋值 _merchantSkeleton 会让 OnFocus/OnUnfocus 走 SetSkinByName("outline"/"default")，
/// 而 keluxier 大概率没有这两个 skin，反而引发新的 skin 报错；让 _merchantSkeleton
/// 保持 null 是安全的 —— 游戏内所有使用处都是 _merchantSkeleton?.（空安全）。
/// </summary>
[HarmonyPatch(typeof(SpineNodeExtensions), "RunWhenSpineReady",
    new Type[] { typeof(Node), typeof(MegaSprite), typeof(Action<MegaAnimationState>) })]
public static class MerchantIdleRedirectPatch
{
    /// <summary>keluxier 骨骼的候选"待机"动画名，按优先级探测。</summary>
    private static readonly string[] IdleCandidates = { "Idle", "Skill_2_Idle", "Default" };

    [HarmonyPrefix]
    static void Prefix(Node host, MegaSprite sprite, ref Action<MegaAnimationState> onReady)
    {
        try
        {
            // 只关心商人按钮的 spine 初始化；其它（玩家/怪物/NPC）一律放行。
            if (host is not NMerchantButton || sprite == null || onReady == null)
            {
                return;
            }

            Action<MegaAnimationState> original = onReady;
            onReady = state =>
            {
                try
                {
                    // spine 此刻必已 ready：骨骼自带 idle_loop（原版商人）→ 原样执行。
                    if (sprite.HasAnimation("idle_loop"))
                    {
                        original(state);
                        return;
                    }

                    // 被换骨的骨骼（keluxier）没有 idle_loop → 挑一个真实存在的待机动画。
                    string? idle = IdleCandidates.FirstOrDefault(sprite.HasAnimation);
                    if (idle == null)
                    {
                        GD.PushWarning("[wyu][ShopNPC] 骨骼无候选待机动画，退回原逻辑");
                        original(state);
                        return;
                    }

                    GD.Print($"[wyu][ShopNPC] 商人待机动画重定向 idle_loop -> {idle}");
                    state.SetAnimation(idle);
                    // 不赋值 NMerchantButton._merchantSkeleton：避免 outline/default skin 缺失报错，
                    // 游戏侧均为 ?. 空安全访问。
                }
                catch (Exception ex)
                {
                    GD.PrintErr("[wyu][ShopNPC] 待机重定向回调异常: " + ex);
                    try { original(state); } catch { /* 忽略 */ }
                }
            };
        }
        catch (Exception ex)
        {
            GD.PrintErr("[wyu][ShopNPC] idle 重定向 Prefix 异常: " + ex);
        }
    }
}
