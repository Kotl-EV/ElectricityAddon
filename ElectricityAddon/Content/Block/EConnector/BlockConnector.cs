using Electricity.Content.Block.Entity;
using Electricity.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.EConnector;

public class BlockConnector : Vintagestory.API.Common.Block {


        public override void OnBlockBroken(IWorldAccessor world, BlockPos position, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
            if (this.api is ICoreClientAPI) {
                return;
            }

            if (world.BlockAccessor.GetBlockEntity(position) is Cable  entity) {
                if (byPlayer is { CurrentBlockSelection: { } blockSelection }) {
                    var connection = entity.Connection & ~Facing.AllAll;

                    if (connection != Facing.None) {
                        var stackSize = FacingHelper.Count(Facing.AllAll);

                        if (stackSize > 0) {
                            entity.Connection = connection;
                            entity.MarkDirty(true);
                            return;
                        }
                    }
                }
            }

            base.OnBlockBroken(world, position, byPlayer, dropQuantityMultiplier);
        }
        
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
            base.OnNeighbourBlockChange(world, pos, neibpos);

            if (world.BlockAccessor.GetBlockEntity(pos) is Cable entity) {
                var blockFacing = BlockFacing.FromVector(neibpos.X - pos.X, neibpos.Y - pos.Y, neibpos.Z - pos.Z);
                var selectedFacing = FacingHelper.FromFace(blockFacing);

                if ((entity.Connection & ~ selectedFacing) == Facing.None) {
                    world.BlockAccessor.BreakBlock(pos, null);

                    return;
                }

                var selectedConnection = entity.Connection & selectedFacing;

                if (selectedConnection != Facing.None) {
                    var stackSize = FacingHelper.Count(selectedConnection);

                    if (stackSize > 0) {
                        entity.Connection &= ~selectedConnection;
                    }
                }
            }
        }
    }