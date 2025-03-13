using System;
using System.Text;
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


    public new BlockPos Pos => this.Blockentity.Pos;

    public int highVoltage => MyMiniLib.GetAttributeInt(this.Block, "voltage", 32);

    public int lowVoltage => MyMiniLib.GetAttributeInt(this.Block, "lowVoltage", 32);



    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(getPower() / (lowVoltage*maxCurrent) * 100));
        stringBuilder.AppendLine("└ " + "Мощность " + getPower() + " / " + lowVoltage * maxCurrent + " Вт");
        stringBuilder.AppendLine();
    }


    public void Update()
    {
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