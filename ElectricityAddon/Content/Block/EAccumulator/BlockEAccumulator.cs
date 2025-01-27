using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.EAccumulator;

public class BlockEAccumulator : Vintagestory.API.Common.Block, IEnergyStorageItem
{
    public int maxcapacity;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        maxcapacity = MyMiniLib.GetAttributeInt(this, "maxcapacity", 16000);
        Durability = 100;
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int energy = itemstack.Attributes.GetInt("electricity:energy", 0);
        int received = Math.Min(maxcapacity - energy, maxReceive);
        itemstack.Attributes.SetInt("electricity:energy", energy + received);
        int durab = (energy + received) / (maxcapacity / GetMaxDurability(itemstack));
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack,
        BlockSelection blockSel, ref string failureCode)
    {
        return world.BlockAccessor
                   .GetBlock(blockSel.Position.AddCopy(BlockFacing.DOWN))
                   .SideSolid[BlockFacing.indexUP] &&
               base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
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

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("Storage") + inSlot.Itemstack.Attributes.GetInt("electricity:energy", 0) + "/" + maxcapacity + " Eu");
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        BlockEntityEAccumulator? be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityEAccumulator;
        ItemStack item = new ItemStack(world.BlockAccessor.GetBlock(pos));
        if (be != null) item.Attributes.SetInt("electricity:energy", be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity());
        if (be != null) item.Attributes.SetInt("durability", 100 * be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity() / maxcapacity);
        return new ItemStack[] { item };
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        BlockEntityEAccumulator? be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityEAccumulator;
        ItemStack item = new ItemStack(world.BlockAccessor.GetBlock(pos));
        if (be != null) item.Attributes.SetInt("electricity:energy", be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity());
        if (be != null) item.Attributes.SetInt("durability", 100 * be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity() / maxcapacity);
        return item;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(world, blockPos, byItemStack);
        if (byItemStack != null)
        {
            BlockEntityEAccumulator? be = world.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityEAccumulator;
            be.GetBehavior<BEBehaviorEAccumulator>().Store(byItemStack.Attributes.GetInt("electricity:energy", 0));
        }
    }
}