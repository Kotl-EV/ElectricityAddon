using System;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block.ECable
{
    public class BlockEntityECable : BlockEntity {
        private Facing switches = Facing.None;

        private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();

        public Facing Connection {
            get => this.ElectricityAddon!.Connection;
            set => this.ElectricityAddon!.Connection = value;
        }

        //передает значения из Block в BEBehaviorElectricityAddon
        public (float[],int) Eparams
        {
            //get => this.ElectricityAddon!.Eparams;
            set => this.ElectricityAddon!.Eparams = value;
        }

        //передает значения из Block в BEBehaviorElectricityAddon
        public float[][] AllEparams
        {
            get => this.ElectricityAddon!.AllEparams;
            set => this.ElectricityAddon!.AllEparams = value;
        }

        public Facing Switches {
            get => this.switches;
            set => this.ElectricityAddon!.Interruption &= this.switches = value;
        }

        public Facing SwitchesState {
            get => ~this.ElectricityAddon!.Interruption;
            set => this.ElectricityAddon!.Interruption = this.switches & ~value;
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);

            tree.SetBytes("electricityaddon:switches", SerializerUtil.Serialize(this.switches));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            try {
                this.switches = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricityaddon:switches"));
            }
            catch (Exception exception) {
                this.Api?.Logger.Error(exception.ToString());
            }
        }
    }
}
