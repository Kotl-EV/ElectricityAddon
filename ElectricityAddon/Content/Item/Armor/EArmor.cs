using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Armor;

class EArmor : ItemWearable,IEnergyStorageItem
{
    int consume;
    int maxcapacity;
    //EntityPlayer eplr;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        consume = MyMiniLib.GetAttributeInt(this, "consume", 20);
        maxcapacity = MyMiniLib.GetAttributeInt(this, "maxcapacity", 20000);
        Durability = maxcapacity / consume;
    }
    
    
    public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
    {
        int energy = itemslot.Itemstack.Attributes.GetInt("electricityaddon:energy");
        if (energy >= consume * amount)
        {
            energy -= consume * amount;
            itemslot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            itemslot.Itemstack.Attributes.SetInt("electricityaddon:energy", energy);
        }
        else
        {
            itemslot.Itemstack.Attributes.SetInt("durability", 0);
        }
        itemslot.MarkDirty();
    }
    
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("electricityaddon:energy") + "/" + maxcapacity + " Eu");
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int received = Math.Min(maxcapacity - itemstack.Attributes.GetInt("electricityaddon:energy"), maxReceive);
        itemstack.Attributes.SetInt("electricity:energy", itemstack.Attributes.GetInt("electricityaddon:energy") + received);
        int durab = Math.Max(1, itemstack.Attributes.GetInt("electricityaddon:energy") / consume);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }
}