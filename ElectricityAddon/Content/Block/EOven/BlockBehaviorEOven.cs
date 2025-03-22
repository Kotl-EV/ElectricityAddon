using System.Linq;
using System.Text;
using ElectricityAddon.Content.Block.ECharger;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EOven;

public class BEBehaviorEOven : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    public bool working;
    private float OvenTemperature;
    public int maxConsumption;
    public BEBehaviorEOven(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }


    public bool isBurned => this.Block.Variant["state"] == "burned";


    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEOven entity)
        {
            if (isBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
                stringBuilder.AppendLine("├ " + Lang.Get("Consumption")+": " + powerSetting + "/" + maxConsumption + " " + Lang.Get("W"));
                stringBuilder.AppendLine("└ " + Lang.Get("Temperature")+": " + OvenTemperature + "°");
            }
        }

        stringBuilder.AppendLine();
    }

    public float Consume_request()
    {
        return maxConsumption;
    }

    public void Consume_receive(float amount)
    {
        BlockEntityEOven? entity = null;
        if (Blockentity is BlockEntityEOven temp)
        {
            entity = temp;
            OvenTemperature = (int)entity.ovenTemperature;

            //проверяем количество занятых слотов и готовой еды
            int stack_count = 0;
            int stack_count_perfect = 0;
            for (int index = 0; index < entity.bakeableCapacity; ++index)
            {
                ItemStack itemstack = entity.ovenInv[index].Itemstack;
                if (itemstack != null)
                {
                    if (itemstack.Class == EnumItemClass.Block)
                    {
                        if (itemstack.Block.Code.ToString().Contains("perfect") || itemstack.Block.Code.ToString().Contains("charred"))
                            stack_count_perfect++;
                    }
                    else
                    {
                        if (itemstack.Item.Code.ToString().Contains("perfect") || itemstack.Item.Code.ToString().Contains("rot") || itemstack.Item.Code.ToString().Contains("charred"))
                            stack_count_perfect++;
                    }

                    stack_count++;
                }
            }

            if (stack_count > 0)   //если еда есть - греем печку
            {
                working = true;
                if (stack_count_perfect == stack_count) //если еда вся готова - не греем
                {
                    working = false;
                }
            }
            else                      //если еды нет - не греем
                working = false;
        }
        if (!working)
        {
            amount = 0;
        }
        if (powerSetting != amount)
        {
            powerSetting = (int)amount;
        }
    }



    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityEOven entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout && entity.Block.Variant["state"] != "burned")
            {
                string side = entity.Block.Variant["side"];

                string[] types = new string[2] { "state", "side" };   //типы горна
                string[] variants = new string[2] { "burned", side };  //нужный вариант 

                this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);
            }
        }
    }

    public float getPowerReceive()
    {
        return this.powerSetting;
    }

    public float getPowerRequest()
    {
        return maxConsumption;
    }
}