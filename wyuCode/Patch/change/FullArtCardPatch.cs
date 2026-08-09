using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 让实现 <see cref="IFullArtCard"/> 的卡以竖版全卡面显示(即使稀有度不是 Ancient)。
/// 原理:游戏内部按 Model.Rarity == Ancient 决定用 _ancientPortrait(全卡面节点)还是 _portrait(普通卡面节点)。
/// 这里用 Harmony postfix 把 IFullArtCard 卡强制切到全卡面节点,并去掉远古装饰,只留竖版大图覆盖整个卡面。
/// 用法:在想用竖版全卡面的卡类上实现 IFullArtCard 接口即可(如 public class All() : wyuCard(...), IFullArtCard)。
/// </summary>
public interface IFullArtCard { }

[HarmonyPatch]
public static class FullArtCardPatch
{
    private static readonly FieldInfo _portrait = AccessTools.Field(typeof(NCard), "_portrait");
    private static readonly FieldInfo _ancientPortrait = AccessTools.Field(typeof(NCard), "_ancientPortrait");
    private static readonly FieldInfo _frame = AccessTools.Field(typeof(NCard), "_frame");
    private static readonly FieldInfo _portraitBorder = AccessTools.Field(typeof(NCard), "_portraitBorder");
    private static readonly FieldInfo _banner = AccessTools.Field(typeof(NCard), "_banner");
    private static readonly FieldInfo _ancientBorder = AccessTools.Field(typeof(NCard), "_ancientBorder");
    private static readonly FieldInfo _ancientTextBg = AccessTools.Field(typeof(NCard), "_ancientTextBg");
    private static readonly FieldInfo _ancientBanner = AccessTools.Field(typeof(NCard), "_ancientBanner");
    private static readonly FieldInfo _ancientBorderGlassOverlay = AccessTools.Field(typeof(NCard), "_ancientBorderGlassOverlay");

    private static bool IsFullArt(CardModel model) => model is IFullArtCard;

    // Reload 控制各卡面节点的显隐(游戏内按 Rarity == Ancient 判定),postfix 强制覆盖
    [HarmonyPatch(typeof(NCard), "Reload")]
    [HarmonyPostfix]
    private static void ForceReload(NCard __instance)
    {
        var model = __instance.Model;
        if (model == null || !IsFullArt(model)) return;

        SetVisible(__instance, _ancientPortrait, true);
        SetVisible(__instance, _portrait, false);
        SetVisible(__instance, _frame, false);
        SetVisible(__instance, _portraitBorder, false);
        SetVisible(__instance, _banner, false);
        // 全卡面配远古边框(尺寸匹配);TextBg/Banner/Glass 保持隐藏:
        // - AncientTextBg getter 对非 Ancient 卡会抛 InvalidOperationException,不能给它贴图
        // - AncientBanner 是顶部远古横幅(带火动画),不需要
        SetVisible(__instance, _ancientBorder, true);
        SetVisible(__instance, _ancientTextBg, false);
        SetVisible(__instance, _ancientBanner, false);
        SetVisible(__instance, _ancientBorderGlassOverlay, false);
    }

    // UpdatePortrait 设置卡面贴图(游戏内按 Rarity == Ancient 决定贴到哪个节点),postfix 强制贴到全卡面节点
    [HarmonyPatch(typeof(NCard), "UpdatePortrait")]
    [HarmonyPostfix]
    private static void ForcePortrait(NCard __instance)
    {
        var model = __instance.Model;
        if (model == null || !IsFullArt(model)) return;

        if (_ancientPortrait.GetValue(__instance) is TextureRect tr)
            tr.Texture = model.Portrait;
        // AncientBorder 是共享贴图(AncientBorderPath 是 static),非 Ancient 卡也安全
        if (_ancientBorder.GetValue(__instance) is TextureRect br)
            br.Texture = model.AncientBorder;
    }

    private static void SetVisible(NCard card, FieldInfo field, bool visible)
    {
        if (field.GetValue(card) is Control c)
            c.Visible = visible;
    }
}
