using System;
using Electricity.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block.EMotor;

public class BlockEntityEMotor : BlockEntity
{
    private Facing facing = Facing.None;
    public float kpds { get; set; }
    private Electricity.Content.Block.Entity.Behavior.Electricity Electricity => this.GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();

    public Facing Facing
    {
        get => this.facing;
        set
        {
            if (value != this.facing)
            {
                this.Electricity.Connection =
                    FacingHelper.FullFace(this.facing = value);
            }
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetBytes("electricity:facing", SerializerUtil.Serialize(this.facing));
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        try
        {
            this.facing = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricity:facing"));
        }
        catch (Exception exception)
        {
            this.Api?.Logger.Error(exception.ToString());
        }
    }
}
