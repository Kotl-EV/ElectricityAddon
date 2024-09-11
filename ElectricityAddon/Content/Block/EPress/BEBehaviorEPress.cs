using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EPress;

public class BEBehaviorEPress : BlockEntityBehavior, IElectricConsumer
{
    public bool working;
    public int PowerSetting;
    public BEBehaviorEPress(BlockEntity blockEntity) : base(blockEntity)
    {
    }

    public ConsumptionRange ConsumptionRange => working ? new ConsumptionRange(600, 1000) : new ConsumptionRange(0, 0);
    public void Consume(int amount) {
        BlockEntityEPress? entity = null;
        if (Blockentity is BlockEntityEPress temp)
        {
            entity = temp;
            working = entity.FindMatchingRecipe();
        }
        if (!working) {
            amount = 0;
        }
        if (PowerSetting != amount) {
            PowerSetting = amount;
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(PowerSetting));
        stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + PowerSetting + "/" + 1000 + "Eu");
        stringBuilder.AppendLine();
    }
}