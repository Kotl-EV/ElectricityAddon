using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EHornNew;

public class BlockEHornNew : Vintagestory.API.Common.Block
{
    WorldInteraction[] interactions;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        if (api.Side != EnumAppSide.Client) return;
        ICoreClientAPI capi = api as ICoreClientAPI;

        interactions = ObjectCacheUtil.GetOrCreate(api, "forgeBlockInteractions", () =>
        {
            List<ItemStack> heatableStacklist = new List<ItemStack>();
            List<ItemStack> fuelStacklist = new List<ItemStack>();
            List<ItemStack> canIgniteStacks = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                string firstCodePart = obj.FirstCodePart();

                if (firstCodePart == "ingot" || firstCodePart == "metalplate" || firstCodePart == "workitem")
                {
                    List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                    if (stacks != null) heatableStacklist.AddRange(stacks);
                }
                else
                {
                    if (obj.CombustibleProps != null)
                    {
                        if (obj.CombustibleProps.BurnTemperature > 1000)
                        {
                            List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                            if (stacks != null) fuelStacklist.AddRange(stacks);
                        }
                    }
                }

                if (obj is Vintagestory.API.Common.Block && (obj as Vintagestory.API.Common.Block).HasBehavior<BlockBehaviorCanIgnite>())
                {
                    List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                    if (stacks != null) canIgniteStacks.AddRange(stacks);
                }
            }

            return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-forge-addworkitem",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            BlockEntityForge bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityForge;
                            if (bef!= null && bef.Contents != null)
                            {
                                return wi.Itemstacks.Where(stack => stack.Equals(api.World, bef.Contents, GlobalConstants.IgnoredStackAttributes)).ToArray();
                            }
                            return wi.Itemstacks;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-forge-takeworkitem",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            BlockEntityForge bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityForge;
                            if (bef!= null && bef.Contents != null)
                            {
                                return new ItemStack[] { bef.Contents };
                            }
                            return null;
                        }
                    }
                };
        });
    }


    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        BlockEntityEHornNew bea = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityEHornNew;
        if (bea != null)
        {
            return bea.OnPlayerInteract(world, byPlayer, blockSel);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Vintagestory.API.Common.Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return blockFace != BlockFacing.UP;
    }
}