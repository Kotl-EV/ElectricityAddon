using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EFreezer;

public class BEBehaviorEFreezer : BlockEntityBehavior, IElectricConsumer
{
    public int PowerSetting;

    public BEBehaviorEFreezer(BlockEntity blockEntity) : base(blockEntity)
    {
    }

    public ConsumptionRange ConsumptionRange => new(10, 100);

    public void Consume(int amount)
    {
        if (PowerSetting != amount)
        {
            PowerSetting = amount;
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(PowerSetting));
        stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + PowerSetting + "/" + 100 + "Eu");
        stringBuilder.AppendLine();
    }
}