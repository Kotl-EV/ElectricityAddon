using ElectricityAddon.Content.Block;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.EAccumulator;

public class BlockEntityEAccumulator : BlockEntity
{
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();


    //передает значения из Block в BEBehaviorElectricityAddon
    public float[] Eparams
    {
        get => this.ElectricityAddon.Eparams;
        set => this.ElectricityAddon.Eparams = value;
    }


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        this.ElectricityAddon.Connection = Facing.DownAll;
    }
}
