using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block.EFreezer;

class BlockEFreezer : Vintagestory.API.Common.Block
{
    private BlockEntityEFreezer be;

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        be = null;
        if (blockSel.Position != null)
        {
            be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityEFreezer;
        }

        bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);

        if (!handled && !byPlayer.WorldData.EntityControls.Sneak && blockSel.Position != null)
        {
            if (be != null)
            {
                if (Variant["state"] == "open")
                    be.OnBlockInteract(byPlayer, false, blockSel);
                else
                    return false;
            }

            return true;
        }

        AssetLocation newCode;

        // -18C

        if (Variant["state"] == "closed")
        {
            be.isOpened = true;
            newCode = CodeWithVariant("state", "open");
            world.PlaySoundAt(new AssetLocation("electricityaddon:sounds/freezer_open.ogg"), byPlayer,
                byPlayer, false);
        }
        else
        {
            be.isOpened = false;
            newCode = CodeWithVariant("state", "closed");
            world.PlaySoundAt(new AssetLocation("electricityaddon:sounds/freezer_close.ogg"), byPlayer,
                byPlayer, false);
        }

        Vintagestory.API.Common.Block newBlock = world.BlockAccessor.GetBlock(newCode);

        world.BlockAccessor.ExchangeBlock(newBlock.BlockId, blockSel.Position);
        return true;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        AssetLocation blockCode = CodeWithVariants(new Dictionary<string, string>
        {
            { "state", "closed" },
            { "status", "melted" },
            { "horizontalorientation", "north" }
        });

        Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(blockCode);

        return new ItemStack(block);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        float dropQuantityMultiplier = 1)
    {
        return new[] { OnPickBlock(world, pos) };
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        return new[]
        {
            new WorldInteraction
            {
                ActionLangCode = "freezer-over-sneak-help",
                HotKeyCode = "sneak",
                MouseButton = EnumMouseButton.Right,
            },
            new WorldInteraction
            {
                ActionLangCode = "freezer-over-help",
                MouseButton = EnumMouseButton.Right,
            }
        }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }
}