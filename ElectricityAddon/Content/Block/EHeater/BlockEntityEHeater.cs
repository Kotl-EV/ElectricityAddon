using System;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EHeater {
    public class BlockEntityEHeater : BlockEntity, IHeatSource {
        private Facing facing = Facing.None;

        private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();

        private BEBehaviorEHeater Behavior => this.GetBehavior<BEBehaviorEHeater>();


        public Facing Facing {
            get => this.facing;
            set {
                if (value != this.facing) {
                    this.ElectricityAddon.Connection =
                        FacingHelper.FullFace(this.facing = value);
                }
            }
        }

        //передает значения из Block в BEBehaviorElectricityAddon
        public (EParams, int) Eparams
        {
            get => this.ElectricityAddon!.Eparams;
            set => this.ElectricityAddon!.Eparams = value;
        }

        //передает значения из Block в BEBehaviorElectricityAddon
        public EParams[] AllEparams
        {
            get => this.ElectricityAddon?.AllEparams ?? null;
            set
            {
                if (this.ElectricityAddon != null)
                {
                    this.ElectricityAddon.AllEparams = value;
                }
            }
        }

        public bool IsEnabled => this.Behavior?.HeatLevel >= 1;


        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos) {
            if (this.Behavior == null)
                return 0.0f;
            else
                return this.Behavior.HeatLevel / this.Behavior.getPowerRequest() * 8.0f;
        }
        

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);

            tree.SetBytes("electricityaddon:facing", SerializerUtil.Serialize(this.facing));
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            try {
                this.facing = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricityaddon:facing"));
            }
            catch (Exception exception) {
                this.Api?.Logger.Error(exception.ToString());
            }
        }
    }
}
