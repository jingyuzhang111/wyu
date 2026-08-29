using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Monsters;

public sealed class YinYinPlaceholderMinion : CustomMonsterModel
{
    public override LocString Title => new("monsters", "WYU-YIN_YIN_PLACEHOLDER_MINION.name");

    // 怪物视觉必须是 .tscn 场景；若给 png 会在加载时触发 InvalidCastException。
    // public override string CustomVisualPath => "res://scenes/creature_visuals/big_dummy.tscn";
    // 视觉默认用占位场景；CreateCustomVisuals 会优先改为复用领袖(Leader)的视觉场景。
    public override string CustomVisualPath => "res://wyu/Scenes/wyuVisual.tscn";

    // 显式创建视觉节点
    public override NCreatureVisuals? CreateCustomVisuals()
    {
        // 如果召唤时已指定 Leader，直接复用 Leader 的视觉场景：
        // 骨架/皮肤/动画配置全部来自 Leader 的视觉场景，天然完整，不会缺零件。
        if (Leader?.Monster != null)
        {
            string? leaderVisualPath = GetMonsterVisualPath(Leader.Monster);
            if (!string.IsNullOrEmpty(leaderVisualPath) && ResourceLoader.Exists(leaderVisualPath))
            {
                Godot.GD.Print($"[wyu][心烛] 复用领袖视觉场景: {leaderVisualPath}");
                return NodeFactory<NCreatureVisuals>.CreateFromScene(leaderVisualPath);
            }
            Godot.GD.PushWarning($"[wyu][心烛] 领袖视觉场景不可用: {leaderVisualPath}");
        }

        return NodeFactory<NCreatureVisuals>.CreateFromScene(CustomVisualPath);
    }

    // 反射读取 MonsterModel.VisualsPath（protected virtual），拿到该怪物专属的视觉场景路径
    private static string? GetMonsterVisualPath(MonsterModel model)
    {
        try
        {
            var prop = typeof(MonsterModel).GetProperty("VisualsPath",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return prop?.GetValue(model) as string;
        }
        catch (System.Exception ex)
        {
            Godot.GD.PushWarning($"[wyu][心烛] 读取领袖 VisualsPath 失败: {ex.Message}");
            return null;
        }
    }

    // 动画器委托给领袖：播放 Leader 的动画（零件完整，不缺失）。动画仍独立（只播 idle）。
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        if (Leader?.Monster != null)
        {
            return Leader.Monster.GenerateAnimator(controller);
        }
        return base.GenerateAnimator(controller);
    }

    // 皮肤设置委托给领袖：用 Leader 的皮肤规则设置心烛的皮肤（零件完整，不缺失）。
    public override void SetupSkins(MegaSprite spine, MegaSkeleton skeleton)
    {
        if (Leader?.Monster != null)
        {
            Leader.Monster.SetupSkins(spine, skeleton);
            return;
        }
        base.SetupSkins(spine, skeleton);
    }

    public override int MinInitialHp => 30;

    public override int MaxInitialHp => 30;

    // 由召唤方在生成时写入：该爪牙受伤后会把等量伤害传给此领袖。
    public Creature? Leader { get; set; }

    // 影子效果：整只心烛变成纯黑剪影（保留全部零件与形状）
    private static readonly Color ShadowColor = new(0f, 0f, 0f, 1f);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        if (Leader == null)
        {
            return;
        }

        NCreature? leaderNode = NCombatRoom.Instance?.GetCreatureNode(Leader);
        NCreature? selfNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
        if (leaderNode == null || selfNode == null)
        {
            return;
        }

        // 敌方爪牙固定在领袖正上方，避免出生在场景中心。
        // selfNode.GlobalPosition = leaderNode.GlobalPosition + Vector2.Up * 240f;
        selfNode.GlobalPosition = leaderNode.GlobalPosition + Vector2.Right * 140f + Vector2.Up * 240f;

        // 视觉已经在 CreateCustomVisuals 里复用了领袖的场景；这里做变黑 + 独立 idle。
        ApplyShadowAndIdle(selfNode);
    }

    /// <summary>
    /// 对已复用领袖视觉的心烛：
    /// 1. 整只变黑做"影子"（黑色材质，避免 additive 发光零件丢失）
    /// 2. 播一个心烛自己的 idle 动画（动画独立，不跟随领袖）
    /// </summary>
    private static void ApplyShadowAndIdle(NCreature selfNode)
    {
        MegaSprite? selfSpine = selfNode.Visuals.SpineBody;
        if (selfSpine == null)
        {
            Godot.GD.PushWarning("[wyu][心烛] 心烛没有 SpineBody，无法变黑。");
            return;
        }

        selfNode.RunWhenSpineReady(selfSpine, _ =>
        {
            // 变黑：黑色材质（不用 Modulate，避免 additive 零件丢失）
            ApplyShadowMaterial(selfSpine);

            // 播一个 idle 动画（动画独立）
            string? idle = selfSpine.GetSkeleton()?.GetData()?.GetAnimationNames()
                .FirstOrDefault(n => n.Contains("idle", System.StringComparison.OrdinalIgnoreCase))
                ?? null;
            if (!string.IsNullOrEmpty(idle))
            {
                selfSpine.GetAnimationState().SetAnimation(idle, true);
            }
        });
    }

    /// <summary>
    /// 把 spine 的 normal 与 additive 材质都替换成“纯黑剪影”材质，
    /// 使整只怪物变成黑色影子，同时保留所有零件（包括发光/特效零件）。
    /// 注意：MegaSprite 只公开 SetNormalMaterial，additive 需要通过 Godot 属性设置。
    /// </summary>
    private static void ApplyShadowMaterial(MegaSprite spine)
    {
        try
        {
            ShaderMaterial blackMaterial = CreateBlackShaderMaterial();

            // normal 零件（普通身体）：用公开 API 设置
            spine.SetNormalMaterial(blackMaterial);

            // additive 零件（发光/特效）：用 Godot 属性系统设置，绕过 Call 白名单限制
            GodotObject obj = spine.BoundObject;
            if (obj != null && obj.HasMethod("set_additive_material"))
            {
                obj.Call("set_additive_material", blackMaterial);
            }
        }
        catch (System.Exception ex)
        {
            Godot.GD.PushWarning($"[wyu][心烛] 应用阴影材质失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建“纯黑剪影”材质：所有颜色输出为黑色，但保留 spine 的 alpha 形状。
    /// 使用 CanvasItem shader，借助 spine 传入的纹理 UV 采样来保留剪影轮廓。
    /// </summary>
    private static ShaderMaterial CreateBlackShaderMaterial()
    {
        Shader shader = new()
        {
            Code = """
                shader_type canvas_item;
                // 保留 spine 贴图的形状轮廓，但把颜色压成纯黑
                void fragment() {
                    vec4 tex = texture(TEXTURE, UV);
                    COLOR = vec4(0.0, 0.0, 0.0, tex.a * COLOR.a);
                }
                """,
        };

        ShaderMaterial material = new()
        {
            Shader = shader,
        };
        return material;
    }

    // 行为状态机
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idleMove = new("NOTHING", NothingMove, new HiddenIntent());
        idleMove.FollowUpState = idleMove;
        return new MonsterMoveStateMachine(new List<MonsterState> { idleMove }, idleMove);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 敌方回合结束后立即移除，实现”只存在一回合”。
        if (side == CombatSide.Enemy && base.Creature.IsAlive)
        {
            await CreatureCmd.Kill(base.Creature, force: true);
        }
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Creature || Leader == null || !Leader.IsAlive)
        {
            return;
        }

        // 只把实际掉血量传给领袖，避免把被格挡部分也转移过去。
        if (result.UnblockedDamage <= 0)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, Leader, result.UnblockedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered, dealer ?? base.Creature, null, null);
    }

    private static Task NothingMove(IReadOnlyList<Creature> _)
    {
        // 占位动作：不攻击、不施法，仅占位。
        return Task.CompletedTask;
    }
}
