using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EOven;

public class BlockEOven : Vintagestory.API.Common.Block
{
    private WorldInteraction[] interactions;


    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        if (api.Side != EnumAppSide.Client)
            return;
        ICoreClientAPI capi = api as ICoreClientAPI;
        interactions = ObjectCacheUtil.GetOrCreate(api, "EOvenBlockInteractions", () =>
        {
            List<ItemStack> rackableStacklist = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj.Attributes?["bakingProperties"]?.AsObject<BakingProperties>() == null) continue;
                List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                if (stacks != null) rackableStacklist.AddRange(stacks);
            }

            return new[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-oven-bakeable",
                    HotKeyCode = null,
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = rackableStacklist.ToArray(),
                },
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-toolrack-take",
                    HotKeyCode = null,
                    MouseButton = EnumMouseButton.Right,
                }
            };
        });
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection bs)
    {
        return world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityEOven blockEntity
            ? blockEntity.OnInteract(byPlayer, bs)
            : base.OnBlockInteractStart(world, byPlayer, bs);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(
        IWorldAccessor world,
        BlockSelection selection,
        IPlayer forPlayer)
    {
        return this.interactions.Append<WorldInteraction>(
            base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
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

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        AssetLocation blockCode = CodeWithVariants(new Dictionary<string, string>
        {
            { "state", (this.Variant["state"]=="enabled")? "enabled":(this.Variant["state"]=="disabled")? "disabled":"burned" },
            { "side", "north" }
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