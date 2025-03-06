using ElectricityAddon.Content.Block;
using ElectricityAddon.Utils;
using System;
using System.Linq;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.EAccumulator;

public class BlockEntityEAccumulator : BlockEntity
{
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        this.ElectricityAddon!.Connection = Facing.DownAll;
        this.ElectricityAddon.Eparams = (
            new EParams(32, 10, -1, 0, 1, 1, false, false),
            FacingHelper.Faces(Facing.DownAll).First().Index);

    }
}
