using System.Text;
using Electricity.Content.Block.Entity;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EOven;

public class BEBehaviorEOven : BlockEntityBehavior, IElectricConsumer {
    public int powerSetting;
    public bool working;
    private float OvenTemperature;
    public BEBehaviorEOven(BlockEntity blockEntity) : base(blockEntity) {
    }
    public ConsumptionRange ConsumptionRange => !working ? new ConsumptionRange(10, 100) : new ConsumptionRange(0, 0);
    public void Consume(int amount) {
        BlockEntityEOven? entity = null;
        if (Blockentity is BlockEntityEOven temp)
        {
            entity = temp;
            working = entity.ovenInv.Empty;
            OvenTemperature = (int)entity.ovenTemperature;
        }
        if (working) {
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
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + OvenTemperature + "°");
        stringBuilder.AppendLine();
    }
}