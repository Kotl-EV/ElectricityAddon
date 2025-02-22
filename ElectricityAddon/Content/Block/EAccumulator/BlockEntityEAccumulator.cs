using ElectricityAddon.Content.Block;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.EAccumulator;

public class BlockEntityEAccumulator : BlockEntity
{
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();


    


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        this.ElectricityAddon.Connection = Facing.DownAll;
        this.ElectricityAddon.Eparams= new float[7]
                        {
                            10,                                 //максимальный ток
                            0,                                  //----
                            0,                                  //потери энергии в элементе цепи
                            1,                                  //количество линий элемента цепи/провода
                            0,                                  //напряжение (возможно будет про запас)
                            0,                                  //сгорел или нет
                            32                                  //напряжение
                        };

    }
}
