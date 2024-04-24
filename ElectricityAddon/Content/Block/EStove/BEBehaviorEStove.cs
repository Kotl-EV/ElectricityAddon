using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EStove;

public class BEBehaviorEStove : BlockEntityBehavior, IElectricConsumer {
    public int powerSetting;
    public bool working;
    private int stoveTemperature;
    public BEBehaviorEStove(BlockEntity blockEntity) : base(blockEntity) {
    }
    public ConsumptionRange ConsumptionRange => working ? new ConsumptionRange(10, 100) : new ConsumptionRange(0, 0);
    public void Consume(int amount) {
        BlockEntityEStove? entity = null;
        if (Blockentity is BlockEntityEStove temp)
        {
            entity = temp;
            working = entity.canHeatInput();
            stoveTemperature = (int)entity.stoveTemperature;
        }
        if (!working) {
            amount = 0;
        }
        if (powerSetting != amount) {
            powerSetting = amount;
        }
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting));
        stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + powerSetting + "/" + 100 + "Eu");
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + stoveTemperature + "°");
        stringBuilder.AppendLine();
    }
}