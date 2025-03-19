using System.Linq;
using System.Text;
using ElectricityAddon.Content.Block.ECharger;
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



    public void Consume_receive(float amount)
    {
        if (powerSetting != amount)
        {
            powerSetting = (int)amount;
        }
    }

    public float Consume_request()
    {
        return maxConsumption;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEFreezer entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout)
            {
                stringBuilder.AppendLine("!!!Сгорел!!!");
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
                stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + powerSetting + "/" + maxConsumption + " Вт");
            }

        }

        stringBuilder.AppendLine();
    }

    public float getPowerReceive()
    {
        return this.powerSetting;
    }

    public float getPowerRequest()
    {
        return maxConsumption;
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEFreezer entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout && (entity.Block.Variant["status"] == "frozen" || entity.Block.Variant["status"] == "melted"))
            {
                string type = "status";
                string variant = "burned";  

                this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant(type, variant)).BlockId, Pos);
            }
        }

    }
}