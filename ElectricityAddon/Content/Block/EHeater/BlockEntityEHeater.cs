using System;
using ElectricityAddon.Content.Block;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Electricity.Content.Block.Entity {
    public class BlockEntityEHeater : BlockEntity, IHeatSource {
        private Facing facing = Facing.None;

        private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();

        private Behavior.BEBehaviorEHeater Behavior => this.GetBehavior<Behavior.BEBehaviorEHeater>();

        public Facing Facing {
            get => this.facing;
            set {
                if (value != this.facing) {
                    this.ElectricityAddon.Connection =
                        FacingHelper.FullFace(this.facing = value);
                }
            }
        }

        public bool IsEnabled => this.Behavior.HeatLevel > 0;

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos) {
            return this.Behavior.HeatLevel * 20f / 8.0f;
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);

            tree.SetBytes("electricity:facing", SerializerUtil.Serialize(this.facing));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            try {
                this.facing = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricity:facing"));
            }
            catch (Exception exception) {
                this.Api?.Logger.Error(exception.ToString());
            }
        }
    }
}
