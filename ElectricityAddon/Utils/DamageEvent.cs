using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using HarmonyLib;

#nullable disable
namespace ElectricityAddon.Utils;

public class DamageEvent : ModSystem
{
  private ICoreAPI api;
  private ICoreClientAPI capi;
  
  public override bool ShouldLoad(EnumAppSide forSide) => true;

  public override void Start(ICoreAPI api) => this.api = api;

  public override void StartClientSide(ICoreClientAPI api)
  {
    base.StartClientSide(api);
    api.Event.LevelFinalize += new Action(this.Event_LevelFinalize);
    this.capi = api;
  }

  private void Event_LevelFinalize()
  {
    EntityBehaviorHealth behavior = this.capi.World.Player.Entity.GetBehavior<EntityBehaviorHealth>();
    if (behavior != null)
      this.capi.Logger.VerboseDebug("Done wearable stats");
  }

  public override void StartServerSide(ICoreServerAPI api)
  {
    base.StartServerSide(api);
    api.Event.PlayerJoin += new PlayerDelegate(this.Event_PlayerJoin);
  }

  private void Event_PlayerJoin(IServerPlayer byPlayer)
  {
    EntityBehaviorHealth behavior = byPlayer.Entity.GetBehavior<EntityBehaviorHealth>();
    if (behavior != null)
      behavior.onDamaged +=
        (OnDamagedDelegate)((dmg, dmgSource) => this.handleDamaged((IPlayer)byPlayer, dmg, dmgSource));
  }
  
  private float handleDamaged(IPlayer player, float damage, DamageSource dmgSource)
  {
    damage = this.applyShieldProtection(player, damage, dmgSource);
    return damage;
  }

  private float applyShieldProtection(IPlayer player, float damage, DamageSource dmgSource)
  {
    double num1 = 1.0471975803375244;
    ItemSlot[] itemSlotArray = new ItemSlot[2]
    {
      player.Entity.LeftHandItemSlot,
      player.Entity.RightHandItemSlot
    };
    for (int index = 0; index < itemSlotArray.Length; ++index)
    {
      ItemSlot itemslot = itemSlotArray[index];
      JsonObject itemAttribute = itemslot.Itemstack?.ItemAttributes?["eshield"];
      if (itemAttribute != null && itemAttribute.Exists)
      {
        string key = player.Entity.Controls.Sneak ? "active" : "passive";
        float val1 = itemAttribute["damageAbsorption"][key].AsFloat();
        float num2 = itemAttribute["protectionChance"][key].AsFloat();
        if (player is IServerPlayer serverPlayer)
        {
          int damageLogChatGroup = GlobalConstants.DamageLogChatGroup;
          string message = Lang.Get("{0:0.#} of {1:0.#} Нихуясебе вьебало", (object)Math.Min(val1, damage),
            (object)damage);
          serverPlayer.SendMessage(damageLogChatGroup, message, EnumChatType.Notification);
        }

        double y1;
        double y2;
        double x;
        if (dmgSource.HitPosition != (Vec3d)null)
        {
          y1 = dmgSource.HitPosition.X;
          y2 = dmgSource.HitPosition.Y;
          x = dmgSource.HitPosition.Z;
        }
        else if (dmgSource.SourceEntity != null)
        {
          y1 = dmgSource.SourceEntity.Pos.X - player.Entity.Pos.X;
          y2 = dmgSource.SourceEntity.Pos.Y - player.Entity.Pos.Y;
          x = dmgSource.SourceEntity.Pos.Z - player.Entity.Pos.Z;
        }
        else if (dmgSource.SourcePos != (Vec3d)null)
        {
          y1 = dmgSource.SourcePos.X - player.Entity.Pos.X;
          y2 = dmgSource.SourcePos.Y - player.Entity.Pos.Y;
          x = dmgSource.SourcePos.Z - player.Entity.Pos.Z;
        }
        else
          break;

        double start = (double)player.Entity.Pos.Yaw + 1.5707963705062866;
        double pitch = (double)player.Entity.Pos.Pitch;
        double end1 = Math.Atan2(y1, x);
        float end2 = (float)Math.Atan2(y2, Math.Sqrt(y1 * y1 + x * x));
        if (((double)Math.Abs(end2) <= 1.1344640254974365
              ? (double)Math.Abs(GameMath.AngleRadDistance((float)start, (float)end1)) < num1
              : (double)Math.Abs(GameMath.AngleRadDistance((float)pitch, end2)) < 0.5235987901687622) &&
            this.api.World.Rand.NextDouble() < (double)num2)
        {
          if(itemslot.Itemstack.Attributes.GetInt("durability") > 1) damage = Math.Max(0.0f, damage - val1);
          this.api.World.PlaySoundAt(AssetLocation.Create(itemslot.Itemstack.ItemAttributes["blockSound"].AsString("shieldblock"), itemslot.Itemstack.Collectible.Code.Domain).WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg"), player);
          (this.api as ICoreServerAPI).Network.BroadcastEntityPacket(player.Entity.EntityId, 200,
            SerializerUtil.Serialize<string>("shieldBlock" + (index == 0 ? "L" : "R")));
          if (this.api.Side == EnumAppSide.Server)
          {
            itemslot.Itemstack.Collectible.DamageItem(this.api.World, dmgSource.SourceEntity, itemslot);
            itemslot.MarkDirty();
          }
        }
      }
    }
    return damage;
  }
}