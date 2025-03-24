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
        int energy = itemstack.Attributes.GetInt("electricityaddon:energy", 0);
        int received = Math.Min(maxcapacity - energy, maxReceive);
        itemstack.Attributes.SetInt("electricityaddon:energy", energy + received);
        int durab = (energy + received) / (maxcapacity / GetMaxDurability(itemstack));
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack,
        BlockSelection blockSel, ref string failureCode)
    {
        return world.BlockAccessor
                   .GetBlock(blockSel.Position.AddCopy(BlockFacing.DOWN))
                   .SideSolid[BlockFacing.indexUP]
                   &&  base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
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


    /// <summary>
    /// Получение информации о предмете в инвентаре
    /// </summary>
    /// <param name="inSlot"></param>
    /// <param name="dsc"></param>
    /// <param name="world"></param>
    /// <param name="withDebugInfo"></param>
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("Storage")+": " + inSlot.Itemstack.Attributes.GetInt("electricityaddon:energy", 0) + "/" + maxcapacity + " " + Lang.Get("J"));
        dsc.AppendLine(Lang.Get("Voltage") + ": " + MyMiniLib.GetAttributeInt(inSlot.Itemstack.Block, "voltage", 0) + " " + Lang.Get("V"));
        dsc.AppendLine(Lang.Get("Power") + ": " + MyMiniLib.GetAttributeFloat(inSlot.Itemstack.Block, "power", 0) + " " + Lang.Get("W"));
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        BlockEntityEAccumulator? be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityEAccumulator;
        ItemStack item = new ItemStack(world.BlockAccessor.GetBlock(pos));
        if (be != null) item.Attributes.SetInt("electricityaddon:energy", (int)be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity());
        if (be != null) item.Attributes.SetInt("durability", (int)(100 * be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity() / maxcapacity));
        return new ItemStack[] { item };
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        BlockEntityEAccumulator? be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityEAccumulator;
        ItemStack item = new ItemStack(world.BlockAccessor.GetBlock(pos));
        if (be != null) item.Attributes.SetInt("electricityaddon:energy", (int)be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity());
        if (be != null) item.Attributes.SetInt("durability", (int)(100 * be.GetBehavior<BEBehaviorEAccumulator>().GetCapacity() / maxcapacity));
        return item;
    }

    /// <summary>
    /// Проверка на возможность установки блока
    /// </summary>
    /// <param name="world"></param>
    /// <param name="byPlayer"></param>
    /// <param name="blockSelection"></param>
    /// <param name="byItemStack"></param>
    /// <returns></returns>
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSelection, ItemStack byItemStack)
    {
        if (byItemStack.Block.Variant["status"] == "burned")
        {
            return false;
        }
        return base.DoPlaceBlock(world, byPlayer, blockSelection, byItemStack);
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(world, blockPos, byItemStack);
        if (byItemStack != null)
        {
            BlockEntityEAccumulator? be = world.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityEAccumulator;
            be!.GetBehavior<BEBehaviorEAccumulator>().SetCapacity(byItemStack.Attributes.GetInt("electricityaddon:energy", 0));
        }
    }
}