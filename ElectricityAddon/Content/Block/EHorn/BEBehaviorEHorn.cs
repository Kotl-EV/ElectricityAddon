using System.Text;
using Electricity.Content.Block.Entity;
using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EHorn;

public class BEBehaviorEHorn : BlockEntityBehavior, IElectricConsumer {
    private int maxTemp;
    public int powerSetting;
    private bool hasItems;
    public int maxConsumption;
    public BEBehaviorEHorn(BlockEntity blockEntity) : base(blockEntity) {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }
    public ConsumptionRange ConsumptionRange => hasItems ? new ConsumptionRange(10, maxConsumption) : new ConsumptionRange(0, 0);
    public void Consume(int amount) {
        BlockEntityEHorn? entity = null;
        if (Blockentity is BlockEntityEHorn temp)
        {
            entity = temp;
            hasItems = entity?.Contents?.StackSize > 0;
        }
        if (!hasItems) {
            amount = 0;
        }
        if (powerSetting != amount) {
            powerSetting = amount;
            maxTemp = amount * 1100 / maxConsumption;   
            if (entity != null) {
                entity.IsBurning = amount > 0;
            }
        }
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting));
        stringBuilder.AppendLine("├ " + Lang.Get("Consumption") + powerSetting + "/" + maxConsumption + " Eu");   
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + maxTemp + "° (max.)");
        stringBuilder.AppendLine();
    }
}