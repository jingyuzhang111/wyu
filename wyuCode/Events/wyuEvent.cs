using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Godot;
using wyu.wyuCode.Extensions;

namespace wyu.wyuCode.Events;

public abstract class wyuEvent : EventModel
{
    internal static readonly List<wyuEvent> All = [];

    protected wyuEvent()
    {
        All.Add(this);
    }

    public override void OnRoomEnter()
    {
        GD.Print($"[wyu] 进入事件: {base.Id.Entry}");
    }

    protected string Opt(string page, string option) =>
        $"{StringHelper.Slugify(GetType().Name)}.pages.{page}.options.{option}";

    protected LocString Desc(string page, string? option = null)
    {
        var key = $"{StringHelper.Slugify(GetType().Name)}.pages.{page}";
        if (option != null) key += $".options.{option}";
        key += ".description";
        return L10NLookup(key);
    }

    // ---------- 图片路径：重定向到 wyu/images/events/ ----------
    [HarmonyPatch(typeof(EventModel), nameof(CreateInitialPortrait))]
    [HarmonyPrefix]
    private static bool RedirectPortrait(EventModel __instance, ref Texture2D __result)
    {
        if (__instance is wyuEvent)
        {
            var path = $"res://{__instance.Id.Entry.ToLowerInvariant()}.png".EventImagePath();
            __result = PreloadManager.Cache.GetTexture2D(path);
            return false;
        }
        return true;
    }

    // ---------- 注册到全部 Act ----------
    private static IEnumerable<EventModel> Inject(IEnumerable<EventModel> __result)
    {
        foreach (var e in __result) yield return e;
        foreach (var e in All)
        {
            GD.Print($"[wyu] 注册事件: {e.Id.Entry}");
            yield return e;
        }
    }

    [HarmonyPatch(typeof(Overgrowth), "get_AllEvents")] private class R1 { [HarmonyPostfix] static IEnumerable<EventModel> P(IEnumerable<EventModel> r) => Inject(r); }
    [HarmonyPatch(typeof(Hive),       "get_AllEvents")] private class R2 { [HarmonyPostfix] static IEnumerable<EventModel> P(IEnumerable<EventModel> r) => Inject(r); }
    [HarmonyPatch(typeof(Glory),      "get_AllEvents")] private class R3 { [HarmonyPostfix] static IEnumerable<EventModel> P(IEnumerable<EventModel> r) => Inject(r); }
    [HarmonyPatch(typeof(Underdocks), "get_AllEvents")] private class R4 { [HarmonyPostfix] static IEnumerable<EventModel> P(IEnumerable<EventModel> r) => Inject(r); }
}
