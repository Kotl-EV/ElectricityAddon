using System;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block.EGenerator;

public class BlockEntityEGenerator : BlockEntity
{
    private Facing facing = Facing.None;

    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();

    public Facing Facing
    {
        get => this.facing;
        set
        {
            if (value != this.facing)
            {
                this.ElectricityAddon.Connection =
                    FacingHelper.FullFace(this.facing = value);
            }
        }
    }

    //передает значения из Block в BEBehaviorElectricityAddon
    public (EParams, int) Eparams
    {
        //get => this.ElectricityAddon.Eparams;
        set => this.ElectricityAddon!.Eparams = value;
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
