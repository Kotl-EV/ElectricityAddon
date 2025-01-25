using System.Text;
using Electricity.Content.Block.Entity;
using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EOven;

public class BEBehaviorEOven : BlockEntityBehavior, IElectricConsumer {
    public int powerSetting;
    public bool working;
    private float OvenTemperature;
    public int maxConsumption;
    public BEBehaviorEOven(BlockEntity blockEntity) : base(blockEntity) {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }
    public ConsumptionRange ConsumptionRange => working ? new ConsumptionRange(10, maxConsumption) : new ConsumptionRange(0, 0);
    public void Consume(int amount) {
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
                        if (itemstack.Block.Code.ToString().Contains("perfect"))
                            stack_count_perfect++;
                    }
                    else
                    {
                        if (itemstack.Item.Code.ToString().Contains("perfect"))
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
            powerSetting = amount;
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
        stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + powerSetting + "/" + maxConsumption + " Eu");
        stringBuilder.AppendLine("└ " + Lang.Get("Temperature") + OvenTemperature + "°");
        stringBuilder.AppendLine();
    }
}