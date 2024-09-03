using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ElectricityAddon.Content.Item;

class EShield : Vintagestory.API.Common.Item,IEnergyStorageItem
{
    int consume;
    int maxcapacity;
    
    protected byte[] lightHsv = new byte[3]
    {
        (byte) 4,
        (byte) 4,
        (byte) 15
    };
    
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
    
    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        int energy = slot.Itemstack.Attributes.GetInt("electricity:energy");
        string str1 = byEntity.LeftHandItemSlot == slot ? "left" : "right";
        string str2 = byEntity.LeftHandItemSlot == slot ? "right" : "left";
        if (byEntity.Controls.Sneak)
        {
            if (!byEntity.AnimManager.IsAnimationActive("raiseshield-" + str1))
                byEntity.AnimManager.StartAnimation("raiseshield-" + str1);
        }
        else if (byEntity.AnimManager.IsAnimationActive("raiseshield-" + str1))
            byEntity.AnimManager.StopAnimation("raiseshield-" + str1);
        if (byEntity.AnimManager.IsAnimationActive("raiseshield-" + str2))
            byEntity.AnimManager.StopAnimation("raiseshield-" + str2);
        if (energy > consume)
            slot.Itemstack.Item.LightHsv = lightHsv;
        else slot.Itemstack.Item.LightHsv = null;
        
        base.OnHeldIdle(slot, byEntity);
    }
    
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        JsonObject itemAttribute = inSlot.Itemstack?.ItemAttributes?["eshield"];
        if (itemAttribute == null || !itemAttribute.Exists)
            return;
        float num1 = itemAttribute["damageAbsorption"]["active"].AsFloat();
        float num2 = itemAttribute["protectionChance"]["active"].AsFloat();
        float num3 = itemAttribute["damageAbsorption"]["passive"].AsFloat();
        float num4 = itemAttribute["protectionChance"]["passive"].AsFloat();
        dsc.AppendLine(Lang.Get("shield-stats", (int) (100.0 * num2), (int) (100.0 * num4), num1, num3));
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