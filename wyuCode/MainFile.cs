using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Monsters;
using wyu.wyuCode.Patch;

namespace wyu.wyuCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "wyu"; //Used for resource filepath

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Godot.GD.Print("[wyu][动画] Initialize 开始");
        Harmony harmony = new(ModId);

        Harmony.DEBUG = true;
        harmony.PatchAll();
        Godot.GD.Print("[wyu][动画] PatchAll 完成");

        // 注册怪物的自定义动画控制器（统一动态打补丁，版本更新更稳）：
        // 运行时按具体怪物类型解析真正会被调用的 GenerateAnimator（基类或重写），
        // 避免 beta 版某些怪物(如 Fabricator/Ovicopter)重写了该方法后静态补丁打不中的问题。
        MonsterAnimatorPatch.Register<Fabricator>("FABRICATOR", FabricatorAnimatorBuilder.Build);
        Godot.GD.Print("[wyu][动画] Register<Fabricator> 完成");
        MonsterAnimatorPatch.Register<Guardbot>("GUARDBOT", GuardbotAnimatorBuilder.Build);
        MonsterAnimatorPatch.Register<Noisebot>("NOISEBOT", NoisebotAnimatorBuilder.Build);
        MonsterAnimatorPatch.Register<Stabbot>("STABBOT", StabbotAnimatorBuilder.Build);
        MonsterAnimatorPatch.Register<Ovicopter>("OVICOPTER", OvicopterAnimatorBuilder.Build);

        // 三阶段 Boss:阶段由 TestSubject.Respawns 决定,构建器需要读取怪物实例
        // 注意怪物 ID 是带下划线的 TEST_SUBJECT(不是 TESTSUBJECT)
        MonsterAnimatorPatch.RegisterWithModel<TestSubject>("TEST_SUBJECT",
            (model, controller) => TestSubjectAnimatorBuilder.Build((TestSubject)model, controller));
        Godot.GD.Print("[wyu][动画] RegisterWithModel<TestSubject> 完成");
    }
}
