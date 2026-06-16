using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Godot;
using MegaCrit.Sts2.Core.Runs;

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


    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {

        foreach (var path in base.GetAssetPaths(runState))
        yield return path;
        // yield 是递进的return, 允许返回一半后停下来处理别的事, 回来后接着返回.
        yield return $"wyu/images/events/{base.Id.Entry.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}.png";

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
