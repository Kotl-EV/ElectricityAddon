using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.EStove;

public class BlockEStove : Vintagestory.API.Common.Block
{
    private BlockEntityEStove be;
    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Vintagestory.API.Common.Block block, BlockPos pos,
        BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return true;
    }
    
    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        AssetLocation blockCode = CodeWithVariants(new Dictionary<string, string>
        {
            { "state", "disabled" },
            { "status", this.Variant["status"] },
            { "side", "south" }
        });

        Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(blockCode);

        return new ItemStack(block);
    }
    
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        be = null;
        if (blockSel.Position != null)
        {
            be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityEStove;
        }

        bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);

        if (!handled && !byPlayer.WorldData.EntityControls.Sneak && blockSel.Position != null) //зачем тут sneak
        {
            if (be != null)
            {
                be.OnBlockInteract(byPlayer, false, blockSel);
            }

            return true;
        }

        return true;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        float dropQuantityMultiplier = 1)
    {
        return new[] { OnPickBlock(world, pos) };
    }


    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
    {
        base.OnNeighbourBlockChange(world, pos, neibpos);

        if (
            !world.BlockAccessor
                .GetBlock(pos.AddCopy(BlockFacing.DOWN))
                .SideSolid[BlockFacing.indexUP]
        )
        {
            world.BlockAccessor.BreakBlock(pos, null);
        }
    }
}