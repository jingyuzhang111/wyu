将原作 "sovereign_blade" 的贴图资源复制到本目录（res://src/zc/）。

把你想用的单张 325 图片放到本目录，命名为 `325.png`。

示例：res://src/zc/325.png

使用说明：
我已经在代码中新增了 `N325Vfx`（wyuCode/Nodes/Vfx/N325Vfx.cs），并在 `Zc325` 中使用它。
将 `325.png` 放到此目录后，在 Godot 编辑器中将图片导入为 `Texture2D`（默认导入即可），VFX 会自动加载并显示该图。
- 如果需要，我可以进一步把 `sovereign_blade.tscn` 场景文件也创建为完整场景并绑定到脚本上，但目前 `N325Vfx` 会在运行时创建 `TextureRect` 并读取 `res://src/zc/XXX.png`（当你调用 `SetBladeTexture` 时）。
