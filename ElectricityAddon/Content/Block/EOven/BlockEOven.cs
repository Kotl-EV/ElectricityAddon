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

    

}