﻿using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Item;

public class EWeapon : Vintagestory.API.Common.Item,IEnergyStorageItem
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
    
    public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
    {
        return "interactstatic";
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, ref EnumHandHandling handling)
    {
        byEntity.Attributes.SetInt("didattack", 0);

        byEntity.World.RegisterCallback((dt) =>
        {
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            if (byPlayer == null) return;

            if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemAttack)
            {
                var pitch = (byEntity as EntityPlayer).talkUtil.pitchModifier;
                byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/strike"), byPlayer.Entity, byPlayer,
                    pitch * 0.9f + (float)api.World.Rand.NextDouble() * 0.2f, 16, 0.35f);
            }
        }, (int)(400 / 1.25f));

        handling = EnumHandHandling.PreventDefault;
    }

    public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        return false;
    }

    public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSelection, EntitySelection entitySel)
    {
        int energy = slot.Itemstack.Attributes.GetInt("electricityaddon:energy");
        secondsPassed *= 1.25f;

        float backwards = -Math.Min(0.35f, 2 * secondsPassed);
        float stab = Math.Min(1.2f, 20 * Math.Max(0, secondsPassed - 0.35f));

        if (byEntity.World.Side == EnumAppSide.Client)
        {
            IClientWorldAccessor world = byEntity.World as IClientWorldAccessor;
            ModelTransform tf = new ModelTransform();
            tf.EnsureDefaultValues();

            float sum = stab + backwards;
            float easeout = Math.Max(0, 2 * (secondsPassed - 1));

            if (secondsPassed > 0.4f) sum = Math.Max(0, sum - easeout);

            tf.Translation.Set(-1.4f * sum, 0, -sum * 0.8f * 2.6f);
            tf.Rotation.Set(-sum * 90, 0, sum * 10);

            byEntity.Controls.UsingHeldItemTransformAfter = tf;


            if (stab > 1.15f && byEntity.Attributes.GetInt("didattack") == 0 && energy >= consume)
            {
                world.TryAttackEntity(entitySel);
                byEntity.Attributes.SetInt("didattack", 1);
                world.AddCameraShake(0.25f);
            }
        }



        return secondsPassed < 1.2f;
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
            itemslot.Itemstack.Attributes.SetInt("durability", 1);
        }
    }

    public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSelection, EntitySelection entitySel)
    {

    }
    
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("electricityaddon:energy") + "/" + maxcapacity + " " + Lang.Get("W"));
    }
    
    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int received = Math.Min(maxcapacity - itemstack.Attributes.GetInt("electricityaddon:energy"), maxReceive);
        itemstack.Attributes.SetInt("electricityaddon:energy", itemstack.Attributes.GetInt("electricityaddon:energy") + received);
        int durab = Math.Max(1, itemstack.Attributes.GetInt("electricityaddon:energy") / consume);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }
}