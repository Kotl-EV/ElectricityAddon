using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EFreezer;

public class BEBehaviorEFreezer : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    public int maxConsumption;
    public BEBehaviorEFreezer(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }

    public ConsumptionRange ConsumptionRange => new(0, maxConsumption);

    public void Consume(int amount)
    {
        if (powerSetting != amount)
        {
            powerSetting = amount;
        }
    }

    public void Consume_receive(float amount)
    {
        throw new System.NotImplementedException();
    }

    public float Consume_request()
    {
        throw new System.NotImplementedException();
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
        stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + powerSetting + "/" + 100 + " Eu");
        stringBuilder.AppendLine();
    }
}