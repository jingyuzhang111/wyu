using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using wyu.wyuCode.Extensions;

namespace wyu.wyuCode.Enchantments;

public abstract class wyuEnchantment : CustomEnchantmentModel
{
    protected override string CustomIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".EnchantmentImagePath();

}
