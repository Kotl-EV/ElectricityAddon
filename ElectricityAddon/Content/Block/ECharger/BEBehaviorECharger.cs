using System.Linq;
using System.Text;
using ElectricityAddon.Content.Block.EHorn;
using ElectricityAddon.Content.Block.ELamp;
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


    public bool isBurned => this.Block.Variant["state"] == "burned";

    public void Consume_receive(float amount)
    {
        BlockEntityECharger? entity = null;
        if (Blockentity is BlockEntityECharger temp)
        {
            entity = temp;
            if (entity.inventory[0]?.Itemstack?.StackSize > 0)
            {
                if (entity.inventory[0]?.Itemstack?.Item is IEnergyStorageItem)
                {
                    var storageEnergyItem = entity.inventory[0].Itemstack.Attributes.GetInt("electricityaddon:energy");
                    var maxStorageItem = MyMiniLib.GetAttributeInt(entity.inventory[0].Itemstack.Item, "maxcapacity");
                    if (storageEnergyItem < maxStorageItem)
                    {
                        working = true;
                    }
                    else working = false;
                }
                else if (entity.inventory[0]?.Itemstack?.Block is IEnergyStorageItem)
                {
                    var storageEnergyBlock = entity.inventory[0].Itemstack.Attributes.GetInt("electricityaddon:energy");
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
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECharger entity)
        {            
            if (isBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
                stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + powerSetting + "/" + maxConsumption + " " + Lang.Get("W"));
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
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECharger entity && entity.AllEparams != null)
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
}