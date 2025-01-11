using Electricity.Content.Block.Entity;
using Electricity.Utils;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.EConnector;

public class BlockEntityConnector : Cable {
    private Electricity.Content.Block.Entity.Behavior.Electricity Electricity
    {
        get => this.GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();
    }
    
    public override void OnBlockPlaced(ItemStack? byItemStack = null) {
        base.OnBlockPlaced(byItemStack);

        var electricity = this.Electricity;

        if (electricity != null) {
            electricity.Connection = Facing.AllAll;
        }
    }
}