using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EHornNew;

public class BEBehaviorEHornNew : BlockEntityBehavior, IElectricConsumer
{
    private int maxTemp;
    public int powerSetting;
    private bool hasItems;
    public BEBehaviorEHornNew(BlockEntity blockEntity) : base(blockEntity)
    {
    }
    public ConsumptionRange ConsumptionRange => hasItems ? new ConsumptionRange(10, 100) : new ConsumptionRange(0, 0);
    public void Consume(int amount)
    {
        BlockEntityEHornNew? entity = null;
        if (Blockentity is BlockEntityEHornNew temp)
        {
            entity = temp;
            hasItems = entity?.Contents?.StackSize > 0;
        }
        if (!hasItems)
        {
            amount = 0;
        }
        if (powerSetting != amount)
        {
            powerSetting = amount;
            maxTemp = amount * 1100 / 100;
            if (entity != null)
            {
                entity.burning = true;
            }
        }
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting));
        stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + powerSetting + "/" + 100 + "Eu");
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + maxTemp + "° (max.)");
        stringBuilder.AppendLine();
    }

    public float Consume_request()
    {
        throw new System.NotImplementedException();
    }

    public void Consume_receive(float amount)
    {
        throw new System.NotImplementedException();
    }
}