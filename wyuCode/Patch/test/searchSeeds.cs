using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 被动种子扫描器 — 每次开始新 run 时自动检查房间配置。
/// 使用方法: scan_seeds random 5000
/// </summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
public static class SeedScanner
{
    private static readonly HashSet<ulong> SeenSeeds = [];

    /// <summary>在这里修改筛选条件</summary>
    private static bool IsMatch(ActModel act0, ActModel act1)
    {
        // Ancient（先古之民）在 _rooms.Ancient，不在 events 列表里！
        var rooms1 = Traverse.Create(act1).Field<RoomSet>("_rooms").Value;
        return rooms1?.Ancient?.Id.Entry == "OROBAS";
    }

    static void Postfix(RunManager __instance)
    {
        var state = __instance.DebugOnlyGetState();
        if (state == null) return;

        ulong seed = state.Rng.Seed;
        if (!SeenSeeds.Add(seed)) return;

        var acts = state.Acts;
        if (acts.Count < 2) return;

        if (IsMatch(acts[0], acts[1]))
        {
            var act0 = acts[0];
            var act1 = acts[1];
            var rooms0 = Traverse.Create(act0).Field<RoomSet>("_rooms").Value;
            var rooms1 = Traverse.Create(act1).Field<RoomSet>("_rooms").Value;

            Godot.GD.Print($"[SeedScanner] ★ 命中! seed={state.Rng.StringSeed}");
            Godot.GD.Print($"  Act0 Boss={act0.BossEncounter?.Id.Entry}");
            Godot.GD.Print($"  Act1 Boss={act1.BossEncounter?.Id.Entry}");
            Godot.GD.Print($"  Act1 Ancient={rooms1?.Ancient?.Id.Entry}");
            Godot.GD.Print($"  Act0 Events=[{string.Join(",", rooms0?.events.Select(e => e.Id.Entry) ?? [])}]");
            Godot.GD.Print($"  Act1 Events=[{string.Join(",", rooms1?.events.Select(e => e.Id.Entry) ?? [])}]");
        }
    }
}
