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

        this.ElectricityAddon!.Connection = Facing.DownAll;
        this.ElectricityAddon.Eparams = (
            new EParams(128, 20, -1, 0, 1, 1, false, false),
            FacingHelper.Faces(Facing.DownAll).First().Index);

    }
}
