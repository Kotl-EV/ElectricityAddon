using System.Text;
using Electricity.Content.Block.Entity;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EHorn;

public class BEBehaviorEHorn : BlockEntityBehavior, IElectricConsumer {
    private int maxTemp;
    private int powerSetting;
    private bool hasItems;
    public BEBehaviorEHorn(BlockEntity blockEntity) : base(blockEntity) {
    }
    public ConsumptionRange ConsumptionRange => hasItems ? new ConsumptionRange(10, 100) : new ConsumptionRange(0, 0);
    public void Consume(int amount) {
        ElectricForge? entity = null;
        if (Blockentity is ElectricForge temp)
        {
            entity = temp;
            hasItems = entity?.Contents?.StackSize > 0;
        }
        if (!hasItems) {
            amount = 0;
        }
        if (powerSetting != amount) {
            powerSetting = amount;
            maxTemp = amount * 1100 / 100;
            if (entity != null) {
                entity.MaxTemp = maxTemp;
                entity.IsBurning = amount > 0;
            }
        }
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting));
        stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + powerSetting + "/" + 100 + "Eu");
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + maxTemp + "° (max.)");
        stringBuilder.AppendLine();
    }
}