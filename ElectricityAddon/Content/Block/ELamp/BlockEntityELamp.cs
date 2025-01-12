using Electricity.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block.ELamp
{
    internal class BlockEntityELamp : BlockEntity
    {
        private Facing facing = Facing.None;

        private Electricity.Content.Block.Entity.Behavior.Electricity Electricity => this.GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();

        private BEBehaviorELamp Behavior => this.GetBehavior<BEBehaviorELamp>();

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

        public bool IsEnabled => this.Behavior.LightLevel > 0;

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
                this.facing = Facing.UpNorth;
                this.Api?.Logger.Error(exception.ToString());
            }
        }
    }
}

