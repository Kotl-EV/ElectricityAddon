using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EInductFurnance;

public class BEBehaviorEInductFurnance : BlockEntityBehavior, IElectricConsumer {
    public int powerSetting;
    public bool working;
    private int stoveTemperature;
    public BEBehaviorEInductFurnance(BlockEntity blockEntity) : base(blockEntity) {
    }
    public ConsumptionRange ConsumptionRange => working ? new ConsumptionRange(2000, 3000) : new ConsumptionRange(0, 0);
    public void Consume(int amount) {
        BlockEntityEInductFurnance? entity = null;
        if (Blockentity is BlockEntityEInductFurnance temp)
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
        stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + powerSetting + "/" + 3000 + "Eu");
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + stoveTemperature + "°");
        stringBuilder.AppendLine();
    }
}