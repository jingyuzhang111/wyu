using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;

using MegaCrit.Sts2.Core.Commands;

// 提供数值
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;

using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using Godot;

// 读取电脑运行程序需要的库
using System.Diagnostics;

using wyu.wyuCode.Powers;

namespace wyu.wyuCode.Cards;

public class MuchCard():
    wyuCard(cost: 1, 
    type: CardType.Power,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;


    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MuchCardPower>(),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [

    ];

    private static (bool arknightsRunning, List<string> processNames) ReadRunningProcessNames()
    {
        Log.Info("[MuchCard] 开始读取后台运行程序...");
        var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = process.ProcessName;
                if (!string.IsNullOrWhiteSpace(name))
                    processNames.Add(name);
            }
            catch
            {
                // Some system processes may reject access; ignore and continue.
            }
        }

        var orderedNames = processNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

        var arknightsRunning = orderedNames.Any(n =>
            n.Contains("Arknights", StringComparison.OrdinalIgnoreCase)
            || n.Contains("明日方舟", StringComparison.OrdinalIgnoreCase));



        return (arknightsRunning, orderedNames);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var threadnum = 0;
        
        Log.Info("[MuchCard] 读取后台运行程序");
        try
        {
            // 得到运行程序列表
            var (arknightsRunning, processNames) = ReadRunningProcessNames();
            Log.Info($"[MuchCard] 当前后台进程总数(去重后): {processNames.Count}");
            Log.Info($"[MuchCard] 后台进程列表: {string.Join(", ", processNames)}");
            Log.Info($"[MuchCard] 检测结果 -> 明日方舟: {arknightsRunning}");

            if (arknightsRunning)
            {
                threadnum += 2;
            }
        }
        catch (Exception e)
        {
            Log.Info($"[MuchCard] 读取后台进程失败: {e.Message}");
        }

        AbstractRoom currentRoom = base.CombatState.RunState.CurrentRoom;
		if (currentRoom is CombatRoom combatRoom && threadnum > 0)
		{
            await PowerCmd.Apply<MuchCardPower>(base.Owner.Creature, threadnum, base.Owner.Creature, null);
		}
    }


    

    // 升级
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }

}
