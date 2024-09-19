using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.ETermoGenerator;

public class BEBehaviorTermoEGenerator : BlockEntityBehavior, IElectricProducer
{
    private int powerSetting;

    public BEBehaviorTermoEGenerator(BlockEntity blockEntity) : base(blockEntity)
    {
    }
    
    public int Produce()
    {
        BlockEntityETermoGenerator? entity = null;
        if (Blockentity is BlockEntityETermoGenerator temp)
        {
            entity = temp;
            if (temp.GenTemp > 20)
            {
                powerSetting = (int)temp.GenTemp/2;
            }else powerSetting = 0;
                
        }
        return powerSetting;
    }
    
    
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting));
        stringBuilder.AppendLine("└ "+ Lang.Get("Production") + this.powerSetting + "/" + MyMiniLib.GetAttributeFloat(this.Block, "maxProduction",10000) + "Eu");
        stringBuilder.AppendLine();
    }
}
