using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace wyu.wyuCode.SeedFinder;

/// <summary>
/// 种子扫描器控制台命令。
/// 用法：
///   scan_seeds random 5000         — 随机扫描 5000 个种子
///   scan_seeds random 5000 8       — 随机扫描 5000 个 8 位种子
///   scan_seeds range ABCD0000 100  — 从 ABCD0000 开始顺序扫描 100 个
///   scan_seeds stop                — 停止当前扫描
/// </summary>
public class SeedScanCommand : AbstractConsoleCmd
{
    public override string CmdName => "scan_seeds";
    public override string Args => "<random|range|stop> [count|startSeed] [seedLength]";
    public override string Description => "批量扫描种子，查找符合条件的房间配置。";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0)
        {
            return new CmdResult(success: false,
                "用法: scan_seeds random <count> [len] | scan_seeds range <start> <count> | scan_seeds stop");
        }

        string subCmd = args[0].ToLowerInvariant();

        switch (subCmd)
        {
            case "stop":
                SeedFinder.StopRequested = true;
                return new CmdResult(success: true, "已发送停止请求。");

            case "random":
                return StartRandomScan(args);

            case "range":
                return StartRangeScan(args);

            default:
                return new CmdResult(success: false, $"未知子命令 '{subCmd}'，可用: random, range, stop");
        }
    }

    private CmdResult StartRandomScan(string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int count) || count <= 0)
        {
            return new CmdResult(success: false, "用法: scan_seeds random <count> [seedLength=10]");
        }

        int seedLength = 10;
        if (args.Length >= 3 && int.TryParse(args[2], out int len) && len > 0 && len <= 20)
        {
            seedLength = len;
        }

        var criteria = CreateDefaultCriteria();

        Task task = Task.Run(() =>
        {
            SeedFinder.ScanRandom(count, criteria, seedLength);
        });

        return new CmdResult(task, success: true,
            $"开始随机扫描 {count} 个种子（长度={seedLength}）… 结果将输出到 Godot 控制台。");
    }

    private CmdResult StartRangeScan(string[] args)
    {
        if (args.Length < 3 || !int.TryParse(args[2], out int count) || count <= 0)
        {
            return new CmdResult(success: false, "用法: scan_seeds range <startSeed> <count>");
        }

        string startSeed = args[1].ToUpperInvariant();
        var criteria = CreateDefaultCriteria();

        Task task = Task.Run(() =>
        {
            SeedFinder.ScanRange(startSeed, count, criteria);
        });

        return new CmdResult(task, success: true,
            $"开始顺序扫描 {count} 个种子（起始={startSeed}）… 结果将输出到 Godot 控制台。");
    }

    /// <summary>创建默认筛选条件 — 在这里修改你要找的条件！</summary>
    private static SeedCriteria CreateDefaultCriteria()
    {
        return new SeedCriteria
        {
            // 第 1 幕 Ancient = OROBAS（欧罗巴斯）
            // 获得后可将 BurningBlood 升级为 BlackBlood（黑暗之血）
            Act1AncientId = "OROBAS",

            // 角色必须是 Ironclad（铁甲战士），因为 BlackBlood 是从
            // Ironclad 的起始遗物 BurningBlood 升级而来
            CharacterId = "IRONCLAD",
        };
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            return CompleteArgument(
                new[] { "random", "range", "stop" },
                Array.Empty<string>(),
                args.FirstOrDefault() ?? "");
        }
        return base.GetArgumentCompletions(player, args);
    }
}
