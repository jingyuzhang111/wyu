using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;
using wyu.wyuCode.Monsters;


namespace wyu.wyuCode.Cards;

public class YinYin3():
    wyuCard(cost: 1, 
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<YinYinDamageTransferPower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        if (cardPlay.Target.Side == CombatSide.Enemy)
        {
            // 设计约束：心烛不能再拥有心烛（目标本身是心烛则不能作为生成源）
            if (cardPlay.Target.GetPower<YinYinDamageTransferPower>() != null)
            {
                Godot.GD.Print("[wyu][心烛] 目标是心烛，不能再生成心烛。");
            }
            // 设计约束：每个怪物只能有一个心烛（目标已存在心烛则不能重复生成）
            else if (base.CombatState!.HittableEnemies.Any(e =>
                e.GetPower<YinYinDamageTransferPower>() is { } p && p.Leader == cardPlay.Target))
            {
                Godot.GD.Print("[wyu][心烛] 该怪物已有心烛，不重复生成。");
            }
            else
            {
                await SummonShadow(choiceContext, cardPlay);
            }
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState!)    // 目标设为全体敌人
            .Execute(choiceContext);                    // 执行动作
    }

    /// <summary>
    /// 生成心烛：复制领袖同名怪物 → 呆立 → 变黑 → 伤害传递。
    /// </summary>
    private async Task SummonShadow(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature leader = cardPlay.Target!;

        // 复制领袖(被选中的怪物)同名怪物作为心烛：视觉/骨架/皮肤/动画天然一致
        MonsterModel? copiedModel = CloneMonster(leader);
        if (copiedModel == null)
        {
            return;
        }

        Creature summonedMinion = base.CombatState!.CreateCreature(copiedModel, CombatSide.Enemy, null);
        await CreatureCmd.Add(summonedMinion);

        // 心烛固定生命值为 40（满血进场）：不管领袖原本多少血，心烛始终 40/40
        await CreatureCmd.SetMaxAndCurrentHp(summonedMinion, 40m);

        // 清除心烛自带的能力 buff：复制出的怪物会继承 Leader 的"生成后自动挂 buff"钩子
        //（如某些怪物的特殊能力），这些不该出现在干净的心烛上。
        // 此时我们自己的 MinionPower / YinYinDamageTransferPower 还没挂，可安全全清。
        RemoveInheritedBuffs(summonedMinion);

        // 位置调整：心烛固定在领袖正右上方，避免出生在场景中心
        PositionMinionNearLeader(leader, summonedMinion);

        // 变黑：整只心烛变成纯黑剪影（黑色材质，避免 additive 发光零件丢失）
        ApplyShadowToMinion(summonedMinion);

        // 给新召唤单位挂“爪牙”效果，并将效果来源设为当前卡牌指定的敌方目标。
        await PowerCmd.Apply<MinionPower>(choiceContext, summonedMinion, 1m, leader, this);

        // 伤害传递：心烛受到的伤害等量传给领袖(被选中的怪物)
        var transfer = await PowerCmd.Apply<YinYinDamageTransferPower>(
            choiceContext, summonedMinion, 1m, leader, this);
        if (transfer is YinYinDamageTransferPower t)
        {
            t.Leader = leader;
        }

        // 心烛随领袖而死：监听领袖的 Died 事件，领袖死亡时一并杀死心烛
        new ShadowDeathLink(leader, summonedMinion);
    }

    /// <summary>
    /// 把“心烛随领袖死亡”绑定到一起：监听领袖的 Died 事件，
    /// 领袖死亡时杀死心烛。
    /// </summary>
    private sealed class ShadowDeathLink
    {
        private readonly Creature _leader;
        private readonly Creature _shadow;

        public ShadowDeathLink(Creature leader, Creature shadow)
        {
            _leader = leader;
            _shadow = shadow;
            _leader.Died += OnLeaderDied;
        }

        private void OnLeaderDied(Creature _)
        {
            _leader.Died -= OnLeaderDied;
            TaskHelper.RunSafely(KillShadowSafely());
        }

        private async Task KillShadowSafely()
        {
            // 等一帧，确保领袖死亡结算完成后再杀心烛
            await Task.Delay(50);

            if (_shadow != null && _shadow.IsAlive)
            {
                await CreatureCmd.Kill(_shadow, force: true);
                Godot.GD.Print("[wyu][心烛] 领袖死亡，心烛随之消散。");
            }
        }
    }

    /// <summary>
    /// 把心烛放在领袖(被选中的怪物)的正右上方：Leader 位置 + (140, -240)。
    /// 需要等视觉节点创建完成后才能设置 GlobalPosition，所以延迟几帧。
    /// </summary>
    private static void PositionMinionNearLeader(Creature leader, Creature minion)
    {
        try
        {
            NCreature? leaderNode = NCombatRoom.Instance?.GetCreatureNode(leader);
            NCreature? selfNode = NCombatRoom.Instance?.GetCreatureNode(minion);
            if (leaderNode == null || selfNode == null)
            {
                Godot.GD.PushWarning("[wyu][心烛] 位置调整失败：找不到领袖或心烛节点。");
                return;
            }

            // 视觉节点可能尚未就绪，延迟几帧再定位（用 async 避免 lambda 捕获可空变量的 C#13 限制）
            _ = MoveMinionIntoPlace(leaderNode, selfNode);
        }
        catch (System.Exception ex)
        {
            Godot.GD.PushWarning($"[wyu][心烛] 位置调整异常: {ex.Message}");
        }
    }

    private static async Task MoveMinionIntoPlace(NCreature leaderNode, NCreature selfNode)
    {
        // 等一小会儿（约 2 帧），确保视觉节点已进入场景树
        await Task.Delay(100);

        if (!GodotObject.IsInstanceValid(leaderNode) || !GodotObject.IsInstanceValid(selfNode))
        {
            return;
        }

        selfNode.GlobalPosition = leaderNode.GlobalPosition + Vector2.Right * 140f + Vector2.Up * 100f;
        Godot.GD.Print($"[wyu][心烛] 已定位到领袖右侧: {selfNode.GlobalPosition}");
    }

    /// <summary>
    /// 把心烛整只变成黑色剪影。
    /// 用黑色 ShaderMaterial 替换 normal 与 additive 材质（additive 通过 Godot 属性设置），
    /// 不能用 Modulate —— 那会让 additive(发光)零件被“吞噬”而丢失。
    /// </summary>
    private static void ApplyShadowToMinion(Creature minion)
    {
        try
        {
            NCreature? selfNode = NCombatRoom.Instance?.GetCreatureNode(minion);
            if (selfNode?.Visuals?.SpineBody == null)
            {
                Godot.GD.PushWarning("[wyu][心烛] 变黑失败：找不到心烛 SpineBody。");
                return;
            }

            // 图层：把心烛的视觉放到最下层（负数 ZIndex 排在背景附近）
            selfNode.Visuals.ZIndex = -10;
            selfNode.Visuals.ShowBehindParent = true;

            MegaSprite spine = selfNode.Visuals.SpineBody;
            selfNode.RunWhenSpineReady(spine, _ =>
            {
                ApplyBlackMaterial(spine);
                Godot.GD.Print("[wyu][心烛] 已应用黑色剪影材质。");
            });
        }
        catch (System.Exception ex)
        {
            Godot.GD.PushWarning($"[wyu][心烛] 变黑异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 把 spine 的 normal 与 additive 材质都替换成“纯黑剪影”材质。
    /// normal 用公开 API；additive 通过 Godot 属性系统设置（绕过 Call 白名单）。
    /// </summary>
    private static void ApplyBlackMaterial(MegaSprite spine)
    {
        ShaderMaterial black = CreateBlackShaderMaterial();

        // normal 零件（普通身体）
        spine.SetNormalMaterial(black);

        // additive 零件（发光/特效）：Godot 属性系统设置
        GodotObject obj = spine.BoundObject;
        if (obj != null && obj.HasMethod("set_additive_material"))
        {
            obj.Call("set_additive_material", black);
        }
    }

    /// <summary>
    /// 创建“纯黑剪影”材质：所有颜色输出为黑色，但保留 spine 的 alpha 形状。
    /// </summary>
    private static ShaderMaterial CreateBlackShaderMaterial()
    {
        Shader shader = new()
        {
            Code = """
                shader_type canvas_item;
                void fragment() {
                    vec4 tex = texture(TEXTURE, UV);
                    COLOR = vec4(0.0, 0.0, 0.0, tex.a * COLOR.a);
                }
                """,
        };
        return new ShaderMaterial { Shader = shader };
    }

    /// <summary>
    /// 复制 leader 的同名怪物模型（视觉/骨架/皮肤/动画天然一致）。
    /// 注意：这里只负责克隆，不碰状态机（状态机要在 CreatureCmd.Add 完成之后再替换）。
    /// </summary>
    private static MonsterModel? CloneMonster(Creature leader)
    {
        if (leader.Monster == null)
        {
            Godot.GD.PushWarning("[wyu][心烛] 领袖没有 MonsterModel，无法复制。");
            return null;
        }

        // 拿领袖的 canonical 模型（ToMutable 要求 canonical），再克隆成可变副本
        MonsterModel canonical = ModelDb.GetById<MonsterModel>(leader.Monster.Id);
        MonsterModel copy = canonical.ToMutable();

        // 注册为心烛：让 RollMove 的 Prefix patch 识别它（在 Add 内部就会执行 RollMove，
        // 所以必须在 Add 之前就标记好，不能用 Power 标记——Power 是 Add 之后才挂的）
        Patch.YinYinIdleIntentPatch.ShadowModels.Add(copy);

        Godot.GD.Print($"[wyu][心烛] 已复制领袖 {copy.Id.Entry} 作为心烛。");
        return copy;
    }

    /// <summary>
    /// 移除心烛从怪物"生成后自动挂 buff"钩子继承来的能力。
    /// 用 RemoveAllPowersInternalExcept(null) 清空所有 powers；
    /// 此时我们自己的 MinionPower/YinYinDamageTransferPower 还没挂，所以全清是安全的。
    /// </summary>
    private static void RemoveInheritedBuffs(Creature minion)
    {
        try
        {
            var removed = minion.RemoveAllPowersInternalExcept(null);
            var names = string.Join(", ", removed.Select(p => p.Id.Entry));
            Godot.GD.Print($"[wyu][心烛] 已清除心烛继承的 buff: [{names}]");
        }
        catch (System.Exception ex)
        {
            Godot.GD.PushWarning($"[wyu][心烛] 清除继承 buff 异常: {ex.Message}");
        }
    }

    // 呆立动作：什么都不做
    private static Task NothingMove(IReadOnlyList<Creature> _)
    {
        return Task.CompletedTask;
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }


}