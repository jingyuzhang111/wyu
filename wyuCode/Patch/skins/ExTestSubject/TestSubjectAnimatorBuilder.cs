using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace wyu.wyuCode.Patch;

/// <summary>
/// TESTSUBJECT（三阶段 Boss，死三次）的自定义动画控制器构建器。
/// 由 MonsterAnimatorPatch.RegisterWithModel 注册，因为阶段由 TestSubject.Respawns 决定。
///
/// 阶段规则（和原版一致）：Respawns==0 → 阶段1，Respawns==1 → 阶段2，Respawns>=2 → 阶段3。
///
/// 触发器 → 动画状态的对应（和原版 TestSubject.GenerateAnimator 一致）：
///   "Hit"               → 受击 hurt
///   "BiteTrigger"       → 攻击 attack_double
///   "MultiAttackTrigger"→ 攻击 attack_big
///   "GrowthSpurtTrigger"→ 治疗/成长 heal
///   "DeadTrigger"       → 击倒 knockout（非循环，播完停在末尾帧静止）
///   "RespawnTrigger"    → 复活 regenerate
///   "Dead"              → 最终死亡 die（阶段3才有）
///   "BurnTrigger"       → 燃烧 burn（阶段3才有）
///
/// ── 你要填的只有每个 new AnimState("...") 里的动画名（对照你 spine 里实际的动画名）──
/// 提示：原版每个阶段的动画名规律是 xxx1 / xxx2 / xxx3（如 idle_loop1、idle_loop2、idle_loop3）。
/// </summary>
public static class TestSubjectAnimatorBuilder
{
    // Respawns 是 private,用反射读取(缓存 FieldInfo 避免重复查找)
    private static readonly System.Reflection.FieldInfo _respawnsField =
        typeof(TestSubject).GetField("_respawns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?? throw new InvalidOperationException("找不到 TestSubject._respawns 字段");

    /// <summary>返回当前阶段:0=阶段1, 1=阶段2, 2=阶段3</summary>
    private static int GetPhase(TestSubject subject) =>
        (int)(_respawnsField.GetValue(subject) ?? 0);

    public static CreatureAnimator Build(TestSubject subject, MegaSprite controller)
    {
        // 复活倒放标志:RespawnTrigger 触发时置 true,信号回调里把 D_Die 倒放
        bool reverseDie = false;

        // ============ 阶段1（Respawns == 0）============
        AnimState idle1 = new("A_Idle", isLooping: true);            // 待机(循环)
        AnimState hurt1 = new("A_Idle", isLooping: false);           // 受击
        AnimState atkDouble1 = new("A_Attack", isLooping: false);      // 攻击(BiteTrigger)
        AnimState atkBig1 = new("A_Attack", isLooping: false);         // 攻击(MultiAttackTrigger)
        AnimState heal1 = new("B_Skill_2", isLooping: false);           // 治疗/成长(GrowthSpurtTrigger)
        AnimState knockout1 = new("A_Skill_1_Begin", isLooping: false);           // 击倒(DeadTrigger,播完停在末尾帧静止)
        hurt1.NextState = idle1;
        atkDouble1.NextState = idle1;
        atkBig1.NextState = idle1;
        heal1.NextState = idle1;
        // knockout1 不设 NextState → 非循环动画播完自动停在最后一帧(倒地静止)
        // 阶段1 不需要自己的复活动画:RespawnTrigger 触发时 Respawns 已=1,
        // 匹配的是下方阶段2 的 regen2(阶段1→2 复活)

        // ============ 阶段2（Respawns == 1）============
        AnimState idle2 = new("A_Idle", isLooping: true);
        AnimState hurt2 = new("A_Idle", isLooping: false);
        AnimState atkDouble2 = new("A_Attack", isLooping: false);
        AnimState atkBig2 = new("A_Attack", isLooping: false);
        AnimState heal2 = new("B_Skill_2", isLooping: false);
        AnimState knockout2 = new("Die", isLooping: false);            // 击倒(DeadTrigger,正放,播完停在末尾帧静止)
        AnimState regen2 = new("A_Skill_1_End", isLooping: false);          // 复活(RespawnTrigger)
        hurt2.NextState = idle2;
        atkDouble2.NextState = idle2;
        atkBig2.NextState = idle2;
        heal2.NextState = idle2;
        // knockout2 不设 NextState → 非循环动画播完自动停在最后一帧(倒地静止)
        regen2.NextState = idle2;

        // ============ 阶段3（Respawns >= 2）============
        AnimState idle3 = new("D_Idle", isLooping: true);
        AnimState hurt3 = new("D_Idle", isLooping: false);
        AnimState atkDouble3 = new("D_Attack", isLooping: false);
        AnimState atkBig3 = new("D_Attack", isLooping: false);
        AnimState heal3 = new("D_Move", isLooping: false);
        AnimState burn3 = new("D_End", isLooping: false);           // 燃烧(BurnTrigger)
        AnimState die3 = new("D_Die", isLooping: false);            // 最终死亡(Dead)
        AnimState regen3 = new("Die", isLooping: false);     // 阶段2→3 复活:先倒放 Die(从死亡站回来)
        hurt3.NextState = idle3;
        atkDouble3.NextState = idle3;
        atkBig3.NextState = idle3;
        heal3.NextState = idle3;
        burn3.NextState = idle3;
        // regen3 不设 NextState:倒放 Die 后由 spine 队列(AddAnimation)接 D_Begin → D_Idle,
        // 因为倒放的 completed 信号不可靠,不能让 CreatureAnimator 依赖它切状态。

        CreatureAnimator animator = new(idle1, controller);

        // ---------- 触发器注册 ----------
        // 阶段1
        animator.AddAnyState("Hit", hurt1, () => GetPhase(subject) == 0);
        animator.AddAnyState("BiteTrigger", atkDouble1, () => GetPhase(subject) == 0);
        animator.AddAnyState("MultiAttackTrigger", atkBig1, () => GetPhase(subject) == 0);
        animator.AddAnyState("GrowthSpurtTrigger", heal1, () => GetPhase(subject) == 0);
        animator.AddAnyState("DeadTrigger", knockout1, () => GetPhase(subject) == 0);
        // 注意:RespawnTrigger 触发时 Respawns 已 +1(见 RespawnMove),所以阶段1→2 复活
        // 匹配的是下方 GetPhase==1 的 regen2,阶段2→3 复活匹配阶段3 的 regen3

        // 阶段2
        animator.AddAnyState("Hit", hurt2, () => GetPhase(subject) == 1);
        animator.AddAnyState("BiteTrigger", atkDouble2, () => GetPhase(subject) == 1);
        animator.AddAnyState("MultiAttackTrigger", atkBig2, () => GetPhase(subject) == 1);
        animator.AddAnyState("GrowthSpurtTrigger", heal2, () => GetPhase(subject) == 1);
        animator.AddAnyState("DeadTrigger", knockout2, () => GetPhase(subject) == 1);
        animator.AddAnyState("RespawnTrigger", regen2, () => GetPhase(subject) == 1);

        // 阶段3
        animator.AddAnyState("Hit", hurt3, () => GetPhase(subject) >= 2);
        animator.AddAnyState("BiteTrigger", atkDouble3, () => GetPhase(subject) >= 2);
        animator.AddAnyState("MultiAttackTrigger", atkBig3, () => GetPhase(subject) >= 2);
        animator.AddAnyState("GrowthSpurtTrigger", heal3, () => GetPhase(subject) >= 2);
        animator.AddAnyState("BurnTrigger", burn3, () => GetPhase(subject) >= 2);
        animator.AddAnyState("Dead", die3, () => GetPhase(subject) >= 2);
        animator.AddAnyState("RespawnTrigger", regen3, () =>
        {
            reverseDie = true; // 标记:接下来播放的 Die 要倒放(复活站回)
            return GetPhase(subject) >= 2;
        });

        // 监听动画开始:Die 被 CreatAnimator 播放(复活场景)时,跳到末尾反向播放
        Godot.GD.Print("[wyu][倒放] 连接 animation_started 信号");
        controller.ConnectAnimationStarted(Callable.From((Godot.GodotObject _, Godot.GodotObject __, Godot.GodotObject ___) =>
        {
            if (!reverseDie)
            {
                return;
            }
            var state = controller.GetAnimationState();
            if (state == null)
            {
                reverseDie = false;
                return;
            }
            var entry = state.GetCurrent(0);
            Godot.GD.Print($"[wyu][倒放] 动画开始回调: reverseDie={reverseDie}, entryNull={entry == null}, anim={(entry?.GetAnimationName() ?? "null")}");
            if (entry != null && entry.GetAnimationName() == "Die")
            {
                float dur = entry.GetAnimationEnd();
                entry.SetTrackTime(dur); // 跳到动画末尾
                entry.SetTimeScale(-1f); // 倒放
                Godot.GD.Print($"[wyu][倒放] 已设置 Die 倒放, 时长={dur}");
                reverseDie = false;
                // 倒放结束后(时长为 delay)显式接 D_Begin → D_Idle,不依赖 spine 队列/信号。
                // 提前 0.1 秒接管:避免倒放 track 被 spine 清空成 default 的那一瞬闪现。
                ScheduleReviveFinish(controller, Math.Max(0f, dur - 0.1f));
            }
        }));

        return animator;
    }

    /// <summary>
    /// 倒放 Die 结束后(delay 秒),依次播放 C_Idle(过渡)→ D_Begin(变身)→ D_Idle(循环)。
    /// 用 Godot 定时器串联,不用 spine 的 AddAnimation 队列(过渡/衔接时队列推进不可靠)。
    /// </summary>
    private static void ScheduleReviveFinish(MegaSprite controller, float delay)
    {
        if (controller.BoundObject is not Godot.Node node) return;
        var tree = node.GetTree();
        if (tree == null) return;
        var timer = tree.CreateTimer(delay);
        timer.Timeout += () =>
        {
            Godot.GD.Print("[wyu][倒放] 定时器触发:播放 D_Begin");
            var st = controller.GetAnimationState();
            if (st == null) return;
            st.GetCurrent(0)?.SetMixDuration(0.08f);
            st.SetAnimation("D_Begin", false, 0);
            // D_Begin 播完后再接 D_Idle(循环)
            float bDur = st.GetCurrent(0)?.GetAnimationEnd() ?? 0.3f;
            PlayAfter(controller, bDur, () =>
            {
                var st3 = controller.GetAnimationState();
                st3?.SetAnimation("D_Idle", true, 0);
                Godot.GD.Print("[wyu][倒放] D_Begin 播完:进入 D_Idle(循环)");
            });
        };
    }

    /// <summary>delay 秒后执行 action(用 Godot 定时器)。</summary>
    private static void PlayAfter(MegaSprite controller, float delay, System.Action action)
    {
        if (controller.BoundObject is not Godot.Node node) return;
        var tree = node.GetTree();
        if (tree == null) return;
        var timer = tree.CreateTimer(delay);
        timer.Timeout += () => action();
    }
}
