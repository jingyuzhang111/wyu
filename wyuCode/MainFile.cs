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
        Harmony harmony = new(ModId);

        Harmony.DEBUG = true;
        harmony.PatchAll();

        // 注册怪物的自定义动画控制器（统一动态打补丁，版本更新更稳）：
        // 运行时按具体怪物类型解析真正会被调用的 GenerateAnimator（基类或重写），
        // 避免 beta 版某些怪物(如 Fabricator/Ovicopter)重写了该方法后静态补丁打不中的问题。
        MonsterAnimatorPatch.Register<Fabricator>("FABRICATOR", FabricatorAnimatorBuilder.Build);
        MonsterAnimatorPatch.Register<Guardbot>("GUARDBOT", GuardbotAnimatorBuilder.Build);
        MonsterAnimatorPatch.Register<Noisebot>("NOISEBOT", NoisebotAnimatorBuilder.Build);
        MonsterAnimatorPatch.Register<Stabbot>("STABBOT", StabbotAnimatorBuilder.Build);
        MonsterAnimatorPatch.Register<Ovicopter>("OVICOPTER", OvicopterAnimatorBuilder.Build);
    }
}
