using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace wyu.wyuCode.SeedFinder;

/// <summary>种子筛选条件</summary>
public class SeedCriteria
{
    public string? Act0BossId { get; set; }
    public string? Act1AncientId { get; set; }
    public HashSet<string>? Act1EventIds { get; set; }
    public HashSet<string>? Act0EventIds { get; set; }
    public HashSet<string>? RelicIds { get; set; }
    public string CharacterId { get; set; } = "IRONCLAD";

    public bool Matches(IReadOnlyList<ActModel> acts)
    {
        if (acts.Count < 2) return false;

        if (!string.IsNullOrEmpty(Act0BossId))
        {
            if (acts[0].BossEncounter?.Id.Entry != Act0BossId)
                return false;
        }

        // 检查 Act1 Ancient（先古之民在 _rooms.Ancient，不在 events 列表里！）
        if (!string.IsNullOrEmpty(Act1AncientId))
        {
            var rooms1 = Traverse.Create(acts[1]).Field<RoomSet>("_rooms").Value;
            if (rooms1?.Ancient?.Id.Entry != Act1AncientId)
                return false;
        }

        // 检查 Act1 普通事件列表
        if (Act1EventIds is { Count: > 0 })
        {
            var rooms1 = Traverse.Create(acts[1]).Field<RoomSet>("_rooms").Value;
            if (rooms1?.events == null) return false;
            var eventIds = rooms1.events.Select(e => e.Id.Entry).ToHashSet();
            foreach (var id in Act1EventIds)
                if (!eventIds.Contains(id))
                    return false;
        }

        if (Act0EventIds is { Count: > 0 })
        {
            var rooms0 = Traverse.Create(acts[0]).Field<RoomSet>("_rooms").Value;
            if (rooms0?.events == null) return false;
            var eventIds = rooms0.events.Select(e => e.Id.Entry).ToHashSet();
            foreach (var id in Act0EventIds)
                if (!eventIds.Contains(id))
                    return false;
        }

        return true;
    }
}

/// <summary>种子扫描结果</summary>
public class ScanResult
{
    public string Seed { get; init; } = "";
    public string Act0Boss { get; init; } = "";
    public string Act1Boss { get; init; } = "";
    public string Act1Ancient { get; init; } = "";
    public List<string> Act0Events { get; init; } = [];
    public List<string> Act1Events { get; init; } = [];
    public List<string> MatchedRelics { get; init; } = [];
}

public static class SeedFinder
{
    public static event Action<List<ScanResult>>? OnResultsFound;
    public static bool IsScanning { get; private set; }
    public static bool StopRequested { get; set; }

    private static readonly HashSet<RelicRarity> GrabBagRarities = new()
    {
        RelicRarity.Common, RelicRarity.Uncommon, RelicRarity.Rare, RelicRarity.Shop
    };

    // ─── 公开 API ─────────────────────────────────────────────

    public static List<ScanResult> ScanRandom(int count, SeedCriteria criteria,
        int seedLength = 10, int batchSize = 1000)
    {
        var results = new List<ScanResult>();
        IsScanning = true;
        StopRequested = false;

        for (int i = 0; i < count; i++)
        {
            if (StopRequested) break;
            string seed = SeedHelper.GetRandomSeed(seedLength);
            var result = CheckSeed(seed, criteria);
            if (result != null)
            {
                results.Add(result);
                GD.Print($"[SeedFinder] ★ 命中 #{results.Count}: seed={seed}");
                OnResultsFound?.Invoke(new List<ScanResult> { result });
            }
            if ((i + 1) % batchSize == 0)
                GD.Print($"[SeedFinder] 已扫描 {i + 1}/{count}，命中 {results.Count}");
        }

        GD.Print($"[SeedFinder] 扫描完成: {count} 个种子，命中 {results.Count}");
        IsScanning = false;
        return results;
    }

    public static List<ScanResult> ScanRange(string startSeed, int count,
        SeedCriteria criteria, int batchSize = 1000)
    {
        var results = new List<ScanResult>();
        IsScanning = true;
        StopRequested = false;

        if (string.IsNullOrEmpty(startSeed))
        {
            IsScanning = false;
            return results;
        }

        long startIndex = SeedToIndex(startSeed);
        long endIndex = Math.Min(startIndex + count,
            (long)Math.Pow(SeedChars.Length, startSeed.Length));

        for (long idx = startIndex; idx < endIndex; idx++)
        {
            if (StopRequested) break;
            string seed = IndexToSeed(idx, startSeed.Length);
            var result = CheckSeed(seed, criteria);
            if (result != null)
            {
                results.Add(result);
                GD.Print($"[SeedFinder] ★ 命中 #{results.Count}: seed={seed}");
                OnResultsFound?.Invoke(new List<ScanResult> { result });
            }
            if ((idx - startIndex + 1) % batchSize == 0)
                GD.Print($"[SeedFinder] 已扫描 {idx - startIndex + 1}/{count}，命中 {results.Count}");
        }

        GD.Print($"[SeedFinder] 扫描完成: {count} 个种子，命中 {results.Count}");
        IsScanning = false;
        return results;
    }

    public static List<ScanResult> ScanList(IEnumerable<string> seeds, SeedCriteria criteria)
    {
        var results = new List<ScanResult>();
        IsScanning = true;
        StopRequested = false;
        int total = 0;
        foreach (string seed in seeds)
        {
            if (StopRequested) break;
            total++;
            var result = CheckSeed(seed, criteria);
            if (result != null)
            {
                results.Add(result);
                GD.Print($"[SeedFinder] ★ 命中 #{results.Count}: seed={seed}");
            }
        }
        GD.Print($"[SeedFinder] 扫描完成: {total} 个种子，命中 {results.Count}");
        IsScanning = false;
        return results;
    }

    public static async Task<List<ScanResult>> ScanRandomAsync(int count, SeedCriteria criteria,
        int seedLength = 10, int batchSize = 1000)
    {
        return await Task.Run(() => ScanRandom(count, criteria, seedLength, batchSize));
    }

    // ─── 核心逻辑 ─────────────────────────────────────────────

    private static ScanResult? CheckSeed(string seed, SeedCriteria criteria)
    {
        try
        {
            seed = SeedHelper.CanonicalizeSeed(seed);
            var unlockState = UnlockState.all;
            bool isMultiplayer = false;

            // 1. 确定 act 列表（用独立 RNG，与游戏内 GetRandomList 一致）
            var actRng = new Rng((uint)StringHelper.GetDeterministicHashCode(seed));
            var acts = ActModel.GetRandomList(actRng, unlockState, isMultiplayer)
                .Select(a => a.ToMutable())
                .ToList();

            // 2. 创建房间生成 RNG（UpFront 流）
            var rngSet = new RunRngSet(seed);
            var rng = rngSet.UpFront;

            // 3. 模拟 InitializeNewRun 的遗物抓取袋填充（必须先消耗 RNG）
            var matchedRelics = SimulateRelicPopulation(rng, unlockState, criteria);

            if (criteria.RelicIds is { Count: > 0 } && matchedRelics.Count == 0)
                return null;

            // 4. 模拟 SharedAncient 分配
            var sharedAncients = unlockState.SharedAncients.ToList();
            sharedAncients.UnstableShuffle(rng);

            foreach (var act in acts.Skip(1))
            {
                int takeCount = rng.NextInt(sharedAncients.Count + 1);
                var subset = sharedAncients.Take(takeCount).ToList();
                sharedAncients = sharedAncients.Except(subset).ToList();
                act.SetSharedAncientSubset(subset);
            }

            // 5. 为每一幕生成房间
            foreach (var act in acts)
                act.GenerateRooms(rng, unlockState, isMultiplayer);

            // 6. 检查条件
            if (!criteria.Matches(acts))
                return null;

            // 7. 提取结果
            var rooms0 = Traverse.Create(acts[0]).Field<RoomSet>("_rooms").Value;
            var rooms1 = Traverse.Create(acts[1]).Field<RoomSet>("_rooms").Value;

            return new ScanResult
            {
                Seed = seed,
                Act0Boss = acts[0].BossEncounter?.Id.Entry ?? "",
                Act1Boss = acts[1].BossEncounter?.Id.Entry ?? "",
                Act1Ancient = rooms1?.Ancient?.Id.Entry ?? "",
                Act0Events = rooms0?.events.Select(e => e.Id.Entry).ToList() ?? [],
                Act1Events = rooms1?.events.Select(e => e.Id.Entry).ToList() ?? [],
                MatchedRelics = matchedRelics,
            };
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SeedFinder] 检查种子 {seed} 时出错: {ex.Message}");
            return null;
        }
    }

    // ─── 遗物池模拟 ──────────────────────────────────────────

    private static List<string> SimulateRelicPopulation(
        Rng rng, UnlockState unlockState, SeedCriteria criteria)
    {
        var matched = new List<string>();
        var targetIds = criteria.RelicIds;
        bool checkRelics = targetIds is { Count: > 0 };

        // --- 共享遗物池 ---
        var sharedRelics = ModelDb.RelicPool<SharedRelicPool>()
            .GetUnlockedRelics(unlockState)
            .Where(r => GrabBagRarities.Contains(r.Rarity))
            .ToList();

        if (checkRelics)
        {
            foreach (var relic in sharedRelics)
                if (targetIds!.Contains(relic.Id.Entry))
                    matched.Add(relic.Id.Entry);
        }

        // 按稀有度分组并打乱（消耗 RNG）
        foreach (var list in sharedRelics.GroupBy(r => r.Rarity).Select(g => g.ToList()))
            list.UnstableShuffle(rng);

        // --- 角色遗物池 ---
        try
        {
            var charId = new ModelId(
                ModelId.SlugifyCategory<CharacterModel>(),
                criteria.CharacterId.ToUpperInvariant());
            var character = ModelDb.GetById<CharacterModel>(charId);

            var charRelics = character.RelicPool
                .GetUnlockedRelics(unlockState)
                .Where(r => GrabBagRarities.Contains(r.Rarity))
                .ToList();

            if (checkRelics)
            {
                foreach (var relic in charRelics)
                    if (targetIds!.Contains(relic.Id.Entry) && !matched.Contains(relic.Id.Entry))
                        matched.Add(relic.Id.Entry);
            }

            // 合并后按稀有度分组再打乱（消耗 RNG）
            foreach (var list in sharedRelics.Concat(charRelics)
                .GroupBy(r => r.Rarity).Select(g => g.ToList()))
                list.UnstableShuffle(rng);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SeedFinder] 角色 '{criteria.CharacterId}' 遗物池加载失败: {ex.Message}");
        }

        return matched;
    }

    // ─── 种子枚举工具 ─────────────────────────────────────────

    private const string SeedChars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private static long SeedToIndex(string seed)
    {
        long index = 0, multiplier = 1;
        for (int i = seed.Length - 1; i >= 0; i--)
        {
            int charIndex = SeedChars.IndexOf(seed[i]);
            if (charIndex < 0) charIndex = 0;
            index += charIndex * multiplier;
            multiplier *= SeedChars.Length;
        }
        return index;
    }

    private static string IndexToSeed(long index, int length)
    {
        char[] result = new char[length];
        for (int i = length - 1; i >= 0; i--)
        {
            result[i] = SeedChars[(int)(index % SeedChars.Length)];
            index /= SeedChars.Length;
        }
        return new string(result);
    }
}
