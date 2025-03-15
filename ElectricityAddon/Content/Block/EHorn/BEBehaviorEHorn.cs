using System.Linq;
using System.Text;
using ElectricityAddon.Content.Block.EAccumulator;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ElectricityAddon.Content.Block.EHorn;

public class BEBehaviorEHorn : BlockEntityBehavior, IElectricConsumer
{
    private float maxTemp;
    private float maxTargetTemp;

    private float powerRequest = maxConsumption;         // Нужно энергии (сохраняется)
    private float powerReceive = 0;             // Дали энергии  (сохраняется)



    private bool hasItems;
    public static int maxConsumption;

    public BEBehaviorEHorn(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
        maxTargetTemp = MyMiniLib.GetAttributeFloat(this.Block, "maxTargetTemp", 1100.0F);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEHorn entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout)
            {
                stringBuilder.AppendLine("!!!Сгорел!!!");
                entity.IsBurning = false;
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerReceive / maxConsumption * 100));
                stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + powerReceive + "/" + maxConsumption + " Вт");
                stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + maxTemp + "° (max.)");
            }

        }

        stringBuilder.AppendLine();
    }


    public float Consume_request()
    {
        return this.powerRequest;
    }


    public void Consume_receive(float amount)
    {
        BlockEntityEHorn? entity = null;
        if (Blockentity is BlockEntityEHorn temp)
        {
            entity = temp;
            hasItems = entity?.Contents?.StackSize > 0;
        }
        if (!hasItems)
        {
            amount = 0;
        }

        if (this.powerReceive != amount)
        {
            this.powerReceive = amount;
            maxTemp = amount * maxTargetTemp / maxConsumption;

        }
    }

    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEHorn entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout && entity.Block.Variant["status"] == "normal")
            {
                string state = "disabled";
                string side=entity.Block.Variant["side"];

                string[] types = new string[3] { "state", "status", "side" };   //типы горна
                string[] variants = new string[3] { state, "burned", side };  //нужный вариант гона

                this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
            }
        }
                
    }

    public float getPowerReceive()
    {
        return this.powerReceive;
    }

    public float getPowerRequest()
    {
        return this.powerRequest;
    }


    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetFloat("electricityaddon:powerRequest", powerRequest);
        tree.SetFloat("electricityaddon:powerRecieve", powerReceive);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        powerRequest = tree.GetFloat("electricityaddon:powerRequest");
        powerReceive = tree.GetFloat("electricityaddon:powerReceive");
    }

}