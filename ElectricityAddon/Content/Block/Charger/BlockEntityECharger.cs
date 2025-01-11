using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Electricity.Utils;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Facing = Electricity.Utils.Facing;

namespace ElectricityAddon.Content.Block.ECharger;

public class BlockEntityECharger : BlockEntity, ITexPositionSource
{
    public InventoryGeneric inventory;
    MeshData[] toolMeshes = new MeshData[1];

    public Size2i AtlasSize => ((ICoreClientAPI)Api).BlockTextureAtlas.Size;

    CollectibleObject tmpItem;
    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            ToolTextures tt = null;

            if (BlockECharger.ToolTextureSubIds(Api).TryGetValue((Vintagestory.API.Common.Item)tmpItem, out tt))
            {
                int textureSubId = 0;
                if (tt.TextureSubIdsByCode.TryGetValue(textureCode, out textureSubId))
                {
                    return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[textureSubId];
                }

                return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[tt.TextureSubIdsByCode.First().Value];
            }

            return null;
        }
    }
    
    private Electricity.Content.Block.Entity.Behavior.Electricity? Electricity => GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();

    public BlockEntityECharger()
    {
        inventory = new InventoryGeneric(1, "charger", null, null, null);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        inventory.LateInitialize("charger-" + Pos, api);
        inventory.ResolveBlocksOrItems();

        if (api is ICoreClientAPI)
        {
            loadToolMeshes();
        }
        else
        {
            RegisterGameTickListener(OnTick, 500);
        }
    }

    private void OnTick(float dt)
    {
        if (inventory[0]?.Itemstack?.Item is IEnergyStorageItem)
        {
            var storageEnergyItem = inventory[0].Itemstack.Attributes.GetInt("electricity:energy");
            var maxStorageItem = MyMiniLib.GetAttributeInt(inventory[0].Itemstack.Item,"maxcapacity");
            if (storageEnergyItem < maxStorageItem && GetBehavior<BEBehaviorECharger>().powerSetting > 0)
            {
                ((IEnergyStorageItem)inventory[0].Itemstack.Item).receiveEnergy(inventory[0].Itemstack,GetBehavior<BEBehaviorECharger>().powerSetting );
            }
        }
        else if (inventory[0]?.Itemstack?.Block is IEnergyStorageItem)
        {
            Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
            var storageEnergyBlock = inventory[0].Itemstack.Attributes.GetInt("electricity:energy");
            var maxStorageBlock = MyMiniLib.GetAttributeInt(inventory[0].Itemstack.Block,"maxcapacity");
            if (storageEnergyBlock < maxStorageBlock && GetBehavior<BEBehaviorECharger>().powerSetting > 0)
            {
                ((IEnergyStorageItem)inventory[0].Itemstack.Block).receiveEnergy(inventory[0].Itemstack,GetBehavior<BEBehaviorECharger>().powerSetting );
            } 
        }
        MarkDirty();
    }

    void loadToolMeshes()
    {
        Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);

        ICoreClientAPI clientApi = (ICoreClientAPI)Api;

        toolMeshes[0] = null;
        IItemStack stack = inventory[0].Itemstack;
        if (stack == null) return;

        tmpItem = stack.Collectible;

        float scaleX = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "scaleX", 0.5F);
        float scaleY = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "scaleY", 0.5F);
        float scaleZ = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "scaleZ", 0.5F);
        float translateX = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "translateX", 0F);
        float translateY = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "translateY", 0.4F);
        float translateZ = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "translateZ", 0F);
        float rotateX = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "rotateX", 0F);
        float rotateY = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "rotateY", 0F);
        float rotateZ = MyMiniLib.GetAttributeFloat(inventory[0].Itemstack.Item, "rotateZ", 0F);

        if (stack.Class == EnumItemClass.Item)
        {
            clientApi.Tesselator.TesselateItem(stack.Item, out toolMeshes[0], this);
        }
        else
        {
            clientApi.Tesselator.TesselateBlock(stack.Block, out toolMeshes[0]);
        }

        

        if (stack.Class == EnumItemClass.Item)
        {
            origin.Y = 1f/30f;
            toolMeshes[0].Scale(origin, scaleX, scaleY, scaleZ);
            toolMeshes[0].Translate(translateX, translateY, translateZ);
            toolMeshes[0].Rotate(origin, rotateX, rotateY, rotateZ);
        }
        else
        {

            toolMeshes[0].Scale(origin, 0.3f, 0.3f, 0.3f);
        }
    }
    
    internal bool OnPlayerInteract(IPlayer byPlayer, Vec3d hit)
    {
        if (inventory[0].Itemstack != null)
        {
            return TakeFromSlot(byPlayer, 0);
        }

        return PutInSlot(byPlayer, 0);
    }

    bool PutInSlot(IPlayer player, int slot)
    {
        IItemStack stack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (stack == null || !(stack.Class == EnumItemClass.Block ? stack.Block is IEnergyStorageItem : stack.Item is IEnergyStorageItem)) return false;
        Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
        player.InventoryManager.ActiveHotbarSlot.TryPutInto(Api.World, inventory[slot]);

        didInteract(player);
        return true;
    }
    
    bool TakeFromSlot(IPlayer player, int slot)
    {
        ItemStack stack = inventory[slot].TakeOutWhole();

        if (!player.InventoryManager.TryGiveItemstack(stack))
        {
            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }
        Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
        didInteract(player);
        return true;
    }
    
    void didInteract(IPlayer player)
    {
        Api.World.PlaySoundAt(new AssetLocation("sounds/player/buildhigh"), Pos.X, Pos.Y, Pos.Z, player, false);
        if (Api is ICoreClientAPI) loadToolMeshes();
        MarkDirty(true);
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null) {
        base.OnBlockPlaced(byItemStack);
        var electricity = Electricity;
        if (electricity != null) {
            electricity.Connection = Facing.DownAll;
        }
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        var electricity = Electricity;
        electricity.Connection = Facing.DownAll;
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);
        ItemStack stack = inventory[0].Itemstack;
        if (stack != null) Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        ICoreClientAPI clientApi = (ICoreClientAPI)Api;
        Vintagestory.API.Common.Block block = Api.World.BlockAccessor.GetBlock(Pos);
        MeshData mesh = clientApi.TesselatorManager.GetDefaultBlockMesh(block);
        if (mesh == null) return true;

        mesher.AddMeshData(mesh);

        if (toolMeshes[0] != null) mesher.AddMeshData(toolMeshes[0]);


        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        if (Api != null)
        {
            inventory.Api = Api;
            inventory.ResolveBlocksOrItems();
        }

        if (Api is ICoreClientAPI)
        {
            loadToolMeshes();
            Api.World.BlockAccessor.MarkBlockDirty(Pos);
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;
    }

    public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (var slot in inventory)
        {
            if (slot.Itemstack == null) continue;

            if (slot.Itemstack.Class == EnumItemClass.Item)
            {
                itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
            }
            else
            {
                blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
            }
        }
    }

    public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
    {
        foreach (var slot in inventory)
        {
            if (slot.Itemstack == null) continue;
            if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
            {
                slot.Itemstack = null;
            }
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        if (inventory[0]?.Itemstack?.Item is IEnergyStorageItem)
        {
            var storageEnergyItem = inventory[0].Itemstack.Attributes.GetInt("electricity:energy");
            var maxStorageItem = MyMiniLib.GetAttributeInt(inventory[0].Itemstack.Item,"maxcapacity");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(inventory[0].Itemstack.GetName());
            stringBuilder.AppendLine(StringHelper.Progressbar(storageEnergyItem * 100.0f / maxStorageItem));
            stringBuilder.AppendLine("└ " + Lang.Get("Storage") + storageEnergyItem + "/" + maxStorageItem + "Eu");
        }
        else if (inventory[0]?.Itemstack?.Block is IEnergyStorageItem)
        {
            var storageEnergyBlock = inventory[0].Itemstack.Attributes.GetInt("electricity:energy");
            var maxStorageBlock = MyMiniLib.GetAttributeInt(inventory[0].Itemstack.Block,"maxcapacity");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(inventory[0].Itemstack.GetName());
            stringBuilder.AppendLine(StringHelper.Progressbar(storageEnergyBlock * 100.0f / maxStorageBlock));
            stringBuilder.AppendLine("└ " + Lang.Get("Storage") + storageEnergyBlock + "/" + maxStorageBlock + "Eu");
        }
    }
    
}
    
    
    
    
public class ToolTextures
{
    public Dictionary<string, int> TextureSubIdsByCode = new Dictionary<string, int>();
}