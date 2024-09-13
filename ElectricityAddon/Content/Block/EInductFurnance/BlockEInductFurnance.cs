using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.EInductFurnance;

public class BlockEInductFurnance : Vintagestory.API.Common.Block
{
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
            { "horizontalorientation", "south" }
        });

        Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(blockCode);

        return new ItemStack(block);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        float dropQuantityMultiplier = 1)
    {
        return new[] { OnPickBlock(world, pos) };
    }
}