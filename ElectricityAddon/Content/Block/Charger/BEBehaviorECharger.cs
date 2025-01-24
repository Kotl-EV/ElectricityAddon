using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.ECharger;

public class BEBehaviorECharger : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    public bool working;
    public int maxConsumption;
    public BEBehaviorECharger(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 200);
    }
    public ConsumptionRange ConsumptionRange => working ? new ConsumptionRange(1, maxConsumption) : new ConsumptionRange(0, 0);
    public void Consume(int amount)
    {
        BlockEntityECharger? entity = null;
        if (Blockentity is BlockEntityECharger temp)
        {
            entity = temp;
            if (entity.inventory[0]?.Itemstack?.StackSize > 0)
            {
                if (entity.inventory[0]?.Itemstack?.Item is IEnergyStorageItem)
                {
                    var storageEnergyItem = entity.inventory[0].Itemstack.Attributes.GetInt("electricity:energy");
                    var maxStorageItem = MyMiniLib.GetAttributeInt(entity.inventory[0].Itemstack.Item, "maxcapacity");
                    if (storageEnergyItem < maxStorageItem)
                    {
                        working = true;
                    }
                    else working = false;
                }
                else if (entity.inventory[0]?.Itemstack?.Block is IEnergyStorageItem)
                {
                    var storageEnergyBlock = entity.inventory[0].Itemstack.Attributes.GetInt("electricity:energy");
                    var maxStorageBlock = MyMiniLib.GetAttributeInt(entity.inventory[0].Itemstack.Block, "maxcapacity");
                    if (storageEnergyBlock < maxStorageBlock)
                    {
                        working = true;
                    }
                    else working = false;
                }
            }
            else working = false;


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

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
        stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + powerSetting + "/" + maxConsumption + " Eu");
        stringBuilder.AppendLine();
    }
}