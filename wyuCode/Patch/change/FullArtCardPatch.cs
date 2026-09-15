using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 让实现 <see cref="IFullArtCard"/> 的卡以竖版全卡面(完整先古样式)显示(即使稀有度不是 Ancient)。
///
/// 原理:游戏 <c>NCard.Reload</c> / <c>NCard.UpdatePortrait</c> / <c>NCard.ReloadOverlay</c>
/// 全部按 <c>Model.Rarity == CardRarity.Ancient</c> 决定用哪套节点、贴图和材质。
/// 这里不再用反射去搬节点(旧做法),而是【在刷新这张卡的这段时间里把稀有度伪造成 Ancient】,
/// 让游戏自己的远古分支跑完整流程,从而白拿到:
///   - _ancientPortrait / _ancientBorder
///   - _ancientTextBg   (旧做法拿不到: CardModel.AncientTextBg 对非 Ancient 卡会直接抛异常)
///   - _ancientBorderGlassOverlay / _ancientBanner
///   - _portraitCanvasGroup 的远古遮罩材质 card_canvas_group_mask_material.tres
/// 好处:不反射任何私有字段(游戏改字段名不会静默失效),外观与真正的先古卡完全一致。
///
/// 作用域用 [ThreadStatic] 深度计数,只在 NCard 刷 UI 的调用栈内生效;
/// 卡池 / 掉落 / 奖励等游戏逻辑读到的仍然是真实稀有度,不受影响。
///
/// 用法:在想用竖版全卡面的卡类上实现 IFullArtCard 接口即可(如 public class All() : wyuCard(...), IFullArtCard)。
/// </summary>
public interface IFullArtCard { }

[HarmonyPatch]
public static class FullArtCardPatch
{
    /// <summary>NCard 卡面刷新的嵌套深度(Reload 内部还会调用 UpdatePortrait / ReloadOverlay)。</summary>
    [ThreadStatic] private static int _uiRefreshDepth;

    private static bool InUiRefresh => _uiRefreshDepth > 0;

    private static void Enter() => _uiRefreshDepth++;

    private static void Exit()
    {
        if (_uiRefreshDepth > 0) _uiRefreshDepth--;
    }

    // ── 1) 在 UI 刷新期间,把 IFullArtCard 的稀有度伪造成 Ancient ──
    // CardModel.Rarity 是 virtual 只读属性, BaseLib 的 CustomCardModel 没有重写它,
    // 所以打基类 getter 就能覆盖本 mod 的全部卡牌。
    [HarmonyPatch(typeof(CardModel), "get_Rarity")]
    [HarmonyPostfix]
    private static void SpoofRarity(CardModel __instance, ref CardRarity __result)
    {
        if (__result != CardRarity.Ancient && InUiRefresh && __instance is IFullArtCard)
            __result = CardRarity.Ancient;
    }

    // ── 2) 作用域开关:包住 NCard 里三个「按稀有度分发外观」的方法 ──
    // Prefix 进入, Finalizer 退出(即使原方法抛异常也会复位, 不会让整条线程一直处于伪造状态)。

    [HarmonyPatch(typeof(NCard), "Reload")]
    [HarmonyPrefix]
    private static void ReloadEnter() => Enter();

    [HarmonyPatch(typeof(NCard), "Reload")]
    [HarmonyFinalizer]
    private static Exception? ReloadExit(Exception? __exception)
    {
        Exit();
        return __exception;
    }

    [HarmonyPatch(typeof(NCard), "UpdatePortrait")]
    [HarmonyPrefix]
    private static void UpdatePortraitEnter() => Enter();

    [HarmonyPatch(typeof(NCard), "UpdatePortrait")]
    [HarmonyFinalizer]
    private static Exception? UpdatePortraitExit(Exception? __exception)
    {
        Exit();
        return __exception;
    }

    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    [HarmonyPrefix]
    private static void ReloadOverlayEnter() => Enter();

    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    [HarmonyFinalizer]
    private static Exception? ReloadOverlayExit(Exception? __exception)
    {
        Exit();
        return __exception;
    }
}
