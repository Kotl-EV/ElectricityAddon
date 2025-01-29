using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Armor;

class EArmor : ItemWearable,IEnergyStorageItem
{
    public int consume;
    public int maxcapacity;
    public int consumefly;
    public float flySpeed;
    //EntityPlayer eplr;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        consume = MyMiniLib.GetAttributeInt(this, "consume", 20);
        maxcapacity = MyMiniLib.GetAttributeInt(this, "maxcapacity", 20000);
        consumefly = MyMiniLib.GetAttributeInt(this, "consumeFly", 40);
        flySpeed = MyMiniLib.GetAttributeFloat(this, "speedFly", 2.0F);
        Durability = maxcapacity / consume;
    }
    
    
    public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount)
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
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("electricityaddon:energy") + "/" + maxcapacity + " " + Lang.Get("W"));
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