using System;
using System.Collections.Generic;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Item;

class EAxe : ItemAxe,IEnergyStorageItem
{
    int consume;
    int maxcapacity;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        consume = MyMiniLib.GetAttributeInt(this, "consume", 20);
        maxcapacity = MyMiniLib.GetAttributeInt(this, "maxcapacity", 20000);
        Durability = maxcapacity / consume;
    }

    public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
    {
        int energy = itemslot.Itemstack.Attributes.GetInt("electricity:energy");
        if (energy >= consume * amount)
        {
            energy -= consume * amount;
            itemslot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            itemslot.Itemstack.Attributes.SetInt("electricity:energy", energy);
        }
        else
        {
            itemslot.Itemstack.Attributes.SetInt("durability", 1);
        }
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        int energy = slot.Itemstack.Attributes.GetInt("electricity:energy");
        if (energy >= consume)
        {
            energy -= consume;
            slot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            slot.Itemstack.Attributes.SetInt("electricity:energy", energy);
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }
        else
        {
            slot.Itemstack.Attributes.SetInt("durability", 1);
        }
        slot.MarkDirty();
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        int energy = slot.Itemstack.Attributes.GetInt("electricity:energy");
        if (energy >= consume)
        {
            energy -= consume;
            slot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            slot.Itemstack.Attributes.SetInt("electricity:energy", energy);
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }
        else
        {
            slot.Itemstack.Attributes.SetInt("durability", 1);
        }
        slot.MarkDirty();
    }

    public override bool OnBlockBrokenWith(
      IWorldAccessor world,
      Entity byEntity,
      ItemSlot itemslot,
      BlockSelection blockSel,
      float dropQuantityMultiplier = 1f)
    {
        int energy = itemslot.Itemstack.Attributes.GetInt("electricity:energy");
        if (energy >= consume)
        {
            IPlayer player = null;
            if (byEntity is EntityPlayer)
                player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            WeatherSystemBase modSystem = this.api.ModLoader.GetModSystem<WeatherSystemBase>();
            double num1 = modSystem != null ? modSystem.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) : 0.0;
            Stack<BlockPos> tree = this.FindTree(world, blockSel.Position, out int _, out int _);
            if (tree.Count == 0)
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            bool flag1 = this.DamagedBy != null &&
                         this.DamagedBy.Contains<EnumItemDamageSource>(EnumItemDamageSource.BlockBreaking);
            float num2 = 1f;
            float num3 = 0.8f;
            int num4 = 0;
            bool flag2 = true;
            while (tree.Count > 0)
            {
                BlockPos pos = tree.Pop();
                Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(pos);
                bool flag3 = block.BlockMaterial == EnumBlockMaterial.Wood;
                if (!flag3 || flag2)
                {
                    ++num4;
                    bool flag4 = block.Code.Path.Contains("branchy");
                    bool flag5 = block.BlockMaterial == EnumBlockMaterial.Leaves;
                    world.BlockAccessor.BreakBlock(pos, player, flag5 ? num2 : (flag4 ? num3 : 1f));
                    if (flag1 & flag3)
                    {
                        this.DamageItem(world, byEntity, itemslot);
                        if (itemslot.Itemstack == null)
                            flag2 = false;
                    }

                    if (flag5 && num2 > 0.029999999329447746)
                        num2 *= 0.85f;
                    if (flag4 && num3 > 0.014999999664723873)
                        num3 *= 0.7f;
                }
            }

            if (num4 > 35 & flag2)
            {
                Vec3d vec3d = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                this.api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), vec3d.X, vec3d.Y, vec3d.Z,
                    player, false, volume: GameMath.Clamp(num4 / 100f, 0.25f, 1f));
            }

            return true;
        }
        else
            return false;

    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("electricity:energy") + "/" + maxcapacity + " Eu");
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int received = Math.Min(maxcapacity - itemstack.Attributes.GetInt("electricity:energy"), maxReceive);
        itemstack.Attributes.SetInt("electricity:energy", itemstack.Attributes.GetInt("electricity:energy") + received);
        int durab = Math.Max(1, itemstack.Attributes.GetInt("electricity:energy") / consume);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }
}