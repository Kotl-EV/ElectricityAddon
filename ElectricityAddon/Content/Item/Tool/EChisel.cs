using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Item;

class EChisel : ItemChisel,IEnergyStorageItem
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