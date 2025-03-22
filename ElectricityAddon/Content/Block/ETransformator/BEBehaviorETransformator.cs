using System;
using System.Linq;
using System.Text;
using ElectricityAddon.Content.Block.ECable;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;


namespace ElectricityAddon.Content.Block.ETransformator;

public class BEBehaviorETransformator : BlockEntityBehavior, IElectricTransformator
{
    float maxCurrent; //максимальный ток
    float power;      //мощность
    public BEBehaviorETransformator(BlockEntity blockEntity) : base(blockEntity)
    {
        maxCurrent = MyMiniLib.GetAttributeFloat(this.Block, "maxCurrent", 5.0F);
    }

    public bool isBurned => this.Block.Variant["status"] == "burned";
    public new BlockPos Pos => this.Blockentity.Pos;

    public int highVoltage => MyMiniLib.GetAttributeInt(this.Block, "voltage", 32);

    public int lowVoltage => MyMiniLib.GetAttributeInt(this.Block, "lowVoltage", 32);



    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityETransformator entity)
        {
            if (isBurned)
            {
                stringBuilder.AppendLine(Lang.Get("Burned"));
            }
            else
            {
                stringBuilder.AppendLine(StringHelper.Progressbar(getPower() / (lowVoltage * maxCurrent) * 100));
                stringBuilder.AppendLine("└ " + Lang.Get("Power") + ": " + getPower() + " / " + lowVoltage * maxCurrent + " " + Lang.Get("W"));
            }

        }

        stringBuilder.AppendLine();
    }


    public void Update()
    {
        //смотрим надо ли обновить модельку когда сгорает трансформатор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityETransformator entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout && entity.Block.Variant["status"]!="burned")
            {
                this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("status", "burned")).BlockId, Pos);
            }
        }

        this.Blockentity.MarkDirty(true);
    }


    public float getPower()
    {
        return this.power;
    }

    public void setPower(float power)
    {
        this.power = power;
    }
}
