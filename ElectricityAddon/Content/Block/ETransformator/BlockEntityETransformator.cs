using ElectricityAddon.Content.Block;
using ElectricityAddon.Utils;
using System;
using System.Linq;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.ETransformator;

public class BlockEntityETransformator : BlockEntity
{
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();


    //передает значения из Block в BEBehaviorElectricityAddon
    public (EParams, int) Eparams
    {
        //get => this.ElectricityAddon.Eparams;
        set => this.ElectricityAddon!.Eparams = value;
    }


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        //задаем параметры блока/проводника
        var voltage = MyMiniLib.GetAttributeInt(byItemStack.Block, "voltage", 32);
        var lowVoltage = MyMiniLib.GetAttributeInt(byItemStack.Block, "lowVoltage", 32);
        var maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack.Block, "maxCurrent", 5.0F);

        this.ElectricityAddon!.Connection = Facing.DownAll;
        this.ElectricityAddon.Eparams = (
            new EParams(voltage, maxCurrent, "", 0, 1, 1, false, false),
            FacingHelper.Faces(Facing.DownAll).First().Index);

    }
}
