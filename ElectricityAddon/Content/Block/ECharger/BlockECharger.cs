using System.Collections.Generic;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block.ECharger;

public class BlockECharger : Vintagestory.API.Common.Block
{
    public static Dictionary<Vintagestory.API.Common.Item, ToolTextures> ToolTextureSubIds(ICoreAPI api)
    {
        Dictionary<Vintagestory.API.Common.Item, ToolTextures> toolTextureSubIds;
        object obj;

        if (api.ObjectCache.TryGetValue("toolTextureSubIdsTest", out obj))
        {

            toolTextureSubIds = obj as Dictionary<Vintagestory.API.Common.Item, ToolTextures>;
        }
        else
        {
            api.ObjectCache["toolTextureSubIdsTest"] = toolTextureSubIds = new Dictionary<Vintagestory.API.Common.Item, ToolTextures>();
        }

        return toolTextureSubIds;
    }


    WorldInteraction[] interactions;
    int output = 1000;

    public override void OnLoaded(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Client) return;
        ICoreClientAPI capi = api as ICoreClientAPI;

        output = MyMiniLib.GetAttributeInt(this, "output", output);

        interactions = ObjectCacheUtil.GetOrCreate(api, "chargerBlockInteractions", () =>
        {
            List<ItemStack> rackableStacklist = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj.Attributes?["rechargeable"].AsBool() != true) continue;

                List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                if (stacks != null) rackableStacklist.AddRange(stacks);
            }

            return new[] {
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-toolrack-place",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = rackableStacklist.ToArray()
                    },
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-toolrack-take",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                    }
                };
        });
    }



    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (be is BlockEntityECharger)
        {
            BlockEntityECharger rack = (BlockEntityECharger)be;
            return rack.OnPlayerInteract(byPlayer, blockSel.HitPosition);
        }

        return false;
    }



    // We need the tool item textures also in the block atlas
    public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
    {
        base.OnCollectTextures(api, textureDict);

        for (int i = 0; i < api.World.Items.Count; i++)
        {
            Vintagestory.API.Common.Item item = api.World.Items[i];
            if (item.Attributes?["rechargeable"].AsBool() != true) continue;

            ToolTextures tt = new ToolTextures();


            if (item.Shape != null)
            {
                IAsset asset = api.Assets.TryGet(item.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
                if (asset != null)
                {
                    Shape shape = asset.ToObject<Shape>();
                    foreach (var val in shape.Textures)
                    {
                        CompositeTexture ctex = new CompositeTexture(val.Value.Clone());
                        ctex.Bake(api.Assets);

                        textureDict.AddTextureLocation(new AssetLocationAndSource(ctex.Baked.BakedName, "Shape code " + item.Shape.Base));
                        tt.TextureSubIdsByCode[val.Key] = textureDict[new AssetLocationAndSource(ctex.Baked.BakedName)];
                    }
                }
            }

            foreach (var val in item.Textures)
            {
                val.Value.Bake(api.Assets);
                textureDict.AddTextureLocation(new AssetLocationAndSource(val.Value.Baked.BakedName, "Item code " + item.Code));
                tt.TextureSubIdsByCode[val.Key] = textureDict[new AssetLocationAndSource(val.Value.Baked.BakedName)];
            }



            ToolTextureSubIds(api)[item] = tt;
        }
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Vintagestory.API.Common.Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return blockFace == BlockFacing.DOWN;
    }
    
    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
        base.OnNeighbourBlockChange(world, pos, neibpos);

        if (
            !world.BlockAccessor
                .GetBlock(pos.AddCopy(BlockFacing.DOWN))
                .SideSolid[BlockFacing.indexUP]
        ) {
            world.BlockAccessor.BreakBlock(pos, null);
        }
    }

    /// <summary>
    /// Проверка на возможность установки блока
    /// </summary>
    /// <param name="world"></param>
    /// <param name="byPlayer"></param>
    /// <param name="blockSelection"></param>
    /// <param name="byItemStack"></param>
    /// <returns></returns>
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSelection, ItemStack byItemStack)
    {
        if (byItemStack.Block.Variant["state"] == "burned")
        {
            return false;
        }
        return base.DoPlaceBlock(world, byPlayer, blockSelection, byItemStack);
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        AssetLocation blockCode = CodeWithVariants(new Dictionary<string, string>
        {
            { "state", (this.Variant["state"]=="enabled")? "enabled":(this.Variant["state"]=="disabled")? "disabled":"burned" },
            { "side", "south" }
        });

        Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(blockCode);

        return new ItemStack(block);
    }
    
    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        float dropQuantityMultiplier = 1)
    {
        return new[] { OnPickBlock(world, pos) };
    }
    
}
 
 
 
 
 
 



 
 

