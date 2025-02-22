using ElectricityAddon.Content.Block.ECable;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.EConnector;

public class BlockEntityEConnector : BlockEntityECable {
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();
    
    public override void OnBlockPlaced(ItemStack? byItemStack = null) {
        base.OnBlockPlaced(byItemStack);

        var electricity = this.ElectricityAddon;

        if (electricity != null) {
            electricity.Connection = Facing.AllAll;
        }
    }
}