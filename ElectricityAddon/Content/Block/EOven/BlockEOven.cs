using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EOven;

public class BlockEOven : Vintagestory.API.Common.Block
{
  private WorldInteraction[] interactions;
  private AdvancedParticleProperties[] particles;
  private Vec3f[] basePos;

  public override void OnLoaded(ICoreAPI api)
  {
    base.OnLoaded(api);
    if (api.Side != EnumAppSide.Client)
      return;
    ICoreClientAPI capi = api as ICoreClientAPI;
    if (capi != null)
      this.interactions = ObjectCacheUtil.GetOrCreate<WorldInteraction[]>(api, "ovenInteractions",
        (CreateCachableObjectDelegate<WorldInteraction[]>)(() =>
        {
          List<ItemStack> itemStackList1 = new List<ItemStack>();
          List<ItemStack> fuelStacklist = new List<ItemStack>();
          List<ItemStack> itemStackList2 = BlockBehaviorCanIgnite.CanIgniteStacks(api, true);
          foreach (CollectibleObject collectible in api.World.Collectibles)
          {
            JsonObject attributes = collectible.Attributes;
            if ((attributes != null ? (attributes.IsTrue("isClayOvenFuel") ? 1 : 0) : 0) != 0)
            {
              List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
              if (handBookStacks != null)
                fuelStacklist.AddRange((IEnumerable<ItemStack>)handBookStacks);
            }
            else
            {
              if (collectible.Attributes?["bakingProperties"]?.AsObject<BakingProperties>() == null)
              {
                CombustibleProperties combustibleProps = collectible.CombustibleProps;
                if ((combustibleProps != null ? (combustibleProps.SmeltingType == EnumSmeltType.Bake ? 1 : 0) : 0) ==
                    0 || collectible.CombustibleProps.SmeltedStack == null ||
                    collectible.CombustibleProps.MeltingPoint >= 260)
                  continue;
              }

              List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
              if (handBookStacks != null)
                itemStackList1.AddRange((IEnumerable<ItemStack>)handBookStacks);
            }
          }

          return new WorldInteraction[1]
          {
            new WorldInteraction()
            {
              ActionLangCode = "blockhelp-oven-bakeable",
              HotKeyCode = (string)null,
              MouseButton = EnumMouseButton.Right,
              Itemstacks = itemStackList1.ToArray(),
              GetMatchingStacks = (InteractionStacksDelegate)((wi, bs, es) =>
              {
                if (wi.Itemstacks.Length == 0)
                  return (ItemStack[])null;
                return !(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityEOven blockEntity2)
                  ? (ItemStack[])null
                  : blockEntity2.CanAdd(wi.Itemstacks);
              })
            }
          };
        }));
    this.InitializeParticles();
  }

  public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

  public override bool OnBlockInteractStart(
    IWorldAccessor world,
    IPlayer byPlayer,
    BlockSelection bs)
  {
    return world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityEOven blockEntity
      ? blockEntity.OnInteract(byPlayer, bs)
      : base.OnBlockInteractStart(world, byPlayer, bs);
  }
  
  public override WorldInteraction[] GetPlacedBlockInteractionHelp(
    IWorldAccessor world,
    BlockSelection selection,
    IPlayer forPlayer)
  {
    return this.interactions.Append<WorldInteraction>(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
  }

  public override void OnAsyncClientParticleTick(
    IAsyncParticleManager manager,
    BlockPos pos,
    float windAffectednessAtPos,
    float secondsTicking)
  {
    if (manager.BlockAccess.GetBlockEntity(pos) is BlockEntityEOven blockEntity && blockEntity.IsBurning)
      blockEntity.RenderParticleTick(manager, pos, windAffectednessAtPos, secondsTicking, this.particles);
    base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
  }

  private void InitializeParticles()
  {
    this.particles = new AdvancedParticleProperties[16];
    this.basePos = new Vec3f[this.particles.Length];
    Cuboidf[] cuboidfArray = new Cuboidf[4]
    {
      new Cuboidf(0.125f, 0.0f, 0.125f, 5f / 16f, 0.5f, 0.875f),
      new Cuboidf(0.7125f, 0.0f, 0.125f, 0.875f, 0.5f, 0.875f),
      new Cuboidf(0.125f, 0.0f, 0.125f, 0.875f, 0.5f, 5f / 16f),
      new Cuboidf(0.125f, 0.0f, 0.7125f, 0.875f, 0.5f, 0.875f)
    };
    for (int index = 0; index < 4; ++index)
    {
      AdvancedParticleProperties particleProperties = this.ParticleProperties[0].Clone();
      Cuboidf cuboidf = cuboidfArray[index];
      this.basePos[index] = new Vec3f(0.0f, 0.0f, 0.0f);
      particleProperties.PosOffset[0].avg = cuboidf.MidX;
      particleProperties.PosOffset[0].var = cuboidf.Width / 2f;
      particleProperties.PosOffset[1].avg = 0.3f;
      particleProperties.PosOffset[1].var = 0.05f;
      particleProperties.PosOffset[2].avg = cuboidf.MidZ;
      particleProperties.PosOffset[2].var = cuboidf.Length / 2f;
      particleProperties.Quantity.avg = 0.5f;
      particleProperties.Quantity.var = 0.2f;
      particleProperties.LifeLength.avg = 0.8f;
      this.particles[index] = particleProperties;
    }

    for (int index = 4; index < 8; ++index)
    {
      AdvancedParticleProperties particleProperties = this.ParticleProperties[1].Clone();
      particleProperties.PosOffset[1].avg = 0.06f;
      particleProperties.PosOffset[1].var = 0.02f;
      particleProperties.Quantity.avg = 0.5f;
      particleProperties.Quantity.var = 0.2f;
      particleProperties.LifeLength.avg = 0.3f;
      particleProperties.VertexFlags = 128;
      this.particles[index] = particleProperties;
    }

    for (int index = 8; index < 12; ++index)
    {
      AdvancedParticleProperties particleProperties = this.ParticleProperties[2].Clone();
      particleProperties.PosOffset[1].avg = 0.09f;
      particleProperties.PosOffset[1].var = 0.02f;
      particleProperties.Quantity.avg = 0.5f;
      particleProperties.Quantity.var = 0.2f;
      particleProperties.LifeLength.avg = 0.18f;
      particleProperties.VertexFlags = 192;
      this.particles[index] = particleProperties;
    }

    for (int index = 12; index < 16; ++index)
    {
      AdvancedParticleProperties particleProperties = this.ParticleProperties[3].Clone();
      particleProperties.PosOffset[1].avg = 0.12f;
      particleProperties.PosOffset[1].var = 0.03f;
      particleProperties.Quantity.avg = 0.2f;
      particleProperties.Quantity.var = 0.1f;
      particleProperties.LifeLength.avg = 0.12f;
      particleProperties.VertexFlags = (int)byte.MaxValue;
      this.particles[index] = particleProperties;
    }
  }
}