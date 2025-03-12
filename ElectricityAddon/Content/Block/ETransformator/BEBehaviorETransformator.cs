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

    public BEBehaviorETransformator(BlockEntity blockEntity) : base(blockEntity)
    {
    }


    public new BlockPos Pos => this.Blockentity.Pos;

    public int highVoltage => 128;

    public int lowVoltage => 32;

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

    }


    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        stringBuilder.AppendLine();
    }


    public void Update()
    {
        throw new NotImplementedException();
    }


    public float getPower()
    {
        throw new NotImplementedException();
    }

    public void setPower(float power)
    {
        throw new NotImplementedException();
    }
}