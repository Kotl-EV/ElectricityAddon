using System;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block.ELamp
{
    internal class BlockEntityELamp : BlockEntity
    {
        private Facing facing = Facing.None;

        private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();

        private BEBehaviorELamp Behavior => this.GetBehavior<BEBehaviorELamp>();

        public Facing Facing
        {
            get => this.facing;
            set
            {
                if (value != this.facing)
                {
                    if (this.Block.Code.ToString().Contains("small"))                           //смотрим какая все же лампочка вызвала
                    {
                        this.ElectricityAddon.Connection = this.facing = value;                      //если лампа маленькая
                    }
                    else
                    {
                        this.ElectricityAddon.Connection = FacingHelper.FullFace(this.facing = value);  //если лампа обычная
                    }
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
                if (!this.Block.Code.ToString().Contains("small"))
                    this.facing = Facing.UpNorth;
                this.Api?.Logger.Error(exception.ToString());
            }
        }
    }
}

