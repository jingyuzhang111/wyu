using BaseLib.Abstracts;
using wyu.wyuCode.Extensions;
using Godot;

namespace wyu.wyuCode.Character;

public class wyuPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => wyu.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}