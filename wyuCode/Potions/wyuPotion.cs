using BaseLib.Abstracts;
using BaseLib.Utils;
using wyu.wyuCode.Character;

namespace wyu.wyuCode.Potions;

[Pool(typeof(wyuPotionPool))]
public abstract class wyuPotion : CustomPotionModel;