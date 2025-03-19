using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EFreezer;

class BlockEntityEFreezer : ContainerEFreezer, ITexPositionSource
{
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();
    private static readonly Vec3f center = new Vec3f(0.5f, 0.25f, 0.5f);
    internal InventoryEFreezer inventory;
    GuiEFreezer freezerDialog;
    ICoreClientAPI capi;
    public bool isOpened;
    public int closedDelay;
    protected MeshData[] meshes;
    protected Shape nowTesselatingShape;
    protected CollectibleObject nowTesselatingObj;
    public int maxConsumption;


    public override InventoryBase Inventory => inventory;

    public override string InventoryClassName => "efreezer";

    //передает значения из Block в BEBehaviorElectricityAddon
    public (EParams, int) Eparams
    {
        get => this.ElectricityAddon!.Eparams;
        set => this.ElectricityAddon!.Eparams = value;
    }

    //передает значения из Block в BEBehaviorElectricityAddon
    public EParams[] AllEparams
    {
        get => this.ElectricityAddon?.AllEparams ?? null!;
        set
        {
            if (this.ElectricityAddon != null)
            {
                this.ElectricityAddon.AllEparams = value;
            }
        }
    }



    public BlockEntityEFreezer()
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
        inventory = new InventoryEFreezer(null!, null!);
        isOpened = false;
        closedDelay = 0;
        meshes = new MeshData[inventory.Count];
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (api.Side == EnumAppSide.Client)
        {
            capi = api as ICoreClientAPI;
        }

        inventory.LateInitialize("efreezer-1", api);
        UpdateMeshes();
        MarkDirty(true);
        RegisterGameTickListener(FreezerTick, 500);
    }

    public void UpdateMesh(int slotid)
    {
        if (Api == null || Api.Side == EnumAppSide.Server)
        {
            return;
        }

        if (slotid == inventory.Count) return;
        if (inventory[slotid].Empty)
        {
            meshes[slotid] = null!;
            return;
        }

        MeshData meshData = GenMesh(inventory[slotid].Itemstack);
        if (meshData != null)
        {
            TranslateMesh(meshData, slotid);
            meshes[slotid] = meshData;
        }
    }

    public void TranslateMesh(MeshData meshData, int slotId)
    {
        if (meshData == null) return;
        float x = 0;
        float y = 0;
        float stdoffset = 0.2f;
        switch (slotId)
        {
            case 0:
            {
                x = -stdoffset;
                y = 1.435f;
                break;
            }
            case 1:
            {
                x = +stdoffset;
                y = 1.435f;
                break;
            }
            case 2:
            {
                x = -stdoffset;
                y = 0.81f;
                break;
            }
            case 3:
            {
                x = +stdoffset;
                y = 0.81f;
                break;
            }
            case 4:
            {
                x = -stdoffset;
                y = 0.19f;
                break;
            }
            case 5:
            {
                x = +stdoffset;
                y = 0.19f;
                break;
            }
        }


        if (!Inventory[slotId].Empty)
        {
            if (Inventory[slotId].Itemstack.Class == EnumItemClass.Block)
            {
                meshData.Scale(new Vec3f(0.5f, 0, 0.5f), 0.53f, 0.53f, 0.53f);
                meshData.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 8 * GameMath.DEG2RAD, 0);
            }
            else
            {
                meshData.Scale(new Vec3f(0.5f, 0, 0.5f), 0.8f, 0.8f, 0.8f);
                meshData.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 15 * GameMath.DEG2RAD, 0);

            }
        }

        meshData.Translate(x, y, 0.025f);

        int orientationRotate = 0;
        if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
        if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
        if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
        meshData.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, orientationRotate * GameMath.DEG2RAD, 0);

    }

    public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            Vintagestory.API.Common.Item item = nowTesselatingObj as Vintagestory.API.Common.Item;
            Dictionary<string, CompositeTexture> dictionary = (Dictionary<string, CompositeTexture>)((item != null)
                ? item.Textures
                : (nowTesselatingObj as Vintagestory.API.Common.Block).Textures);
            AssetLocation assetLocation = null;
            CompositeTexture compositeTexture;
            if (dictionary.TryGetValue(textureCode, out compositeTexture!))
            {
                assetLocation = compositeTexture.Baked.BakedName;
            }

            if (assetLocation == null && dictionary.TryGetValue("all", out compositeTexture!))
            {
                assetLocation = compositeTexture.Baked.BakedName;
            }

            if (assetLocation == null)
            {
                Shape shape = nowTesselatingShape;
                if (shape != null)
                {
                    shape.Textures.TryGetValue(textureCode, out assetLocation!);
                }
            }

            if (assetLocation == null)
            {
                assetLocation = new AssetLocation(textureCode);
            }

            return getOrCreateTexPos(assetLocation);
        }
    }

    private TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
    {
        TextureAtlasPosition textureAtlasPosition = capi.BlockTextureAtlas[texturePath];
        if (textureAtlasPosition == null)
        {
            IAsset asset =
                capi.Assets.TryGet(
                    texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
            if (asset != null)
            {
                int num;
                capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out num, out textureAtlasPosition, null,
                    0.005f);
            }
            else
            {
                ILogger logger = capi.World.Logger;
                string str = "For render in block ";
                AssetLocation code = Block.Code;
                logger.Warning(
                    str + ((code != null) ? code.ToString() : null) +
                    ", item {0} defined texture {1}, not no such texture found.", nowTesselatingObj.Code, texturePath);
            }
        }

        return textureAtlasPosition;
    }

    public MeshData GenMesh(ItemStack stack)
    {
        IContainedMeshSource meshsource = stack.Collectible as IContainedMeshSource;
        MeshData meshData;
        if (meshsource != null)
        {
            meshData = meshsource.GenMesh(stack, capi.BlockTextureAtlas, Pos);
            meshData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, Block.Shape.rotateY * 0.0174532924f, 0f);
        }
        else
        {
            ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
            if (stack.Class == EnumItemClass.Block)
            {
                meshData = coreClientAPI!.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
            }
            else
            {
                nowTesselatingObj = stack.Collectible;
                nowTesselatingShape = null;
                if (stack.Item.Shape != null)
                {
                    nowTesselatingShape = coreClientAPI!.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);

                }

                coreClientAPI!.Tesselator.TesselateItem(stack.Item, out meshData, this);
                meshData.RenderPassesAndExtraBits.Fill((short)2);
            }
        }

        return meshData;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] != null)
            {
                mesher.AddMeshData(meshes[i]);
            }
        }

        return false;
    }

    public void UpdateMeshes()
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            UpdateMesh(i);
        }

        MarkDirty(true);
    }


    /// <summary>
    /// Тики холодильника
    /// </summary>
    /// <param name="dt"></param>
    private void FreezerTick(float dt)
    {
        if (Api.Side == EnumAppSide.Server && this.Block.Variant["status"]!="burned")
        {
            TryRefuel();
            if (GetBehavior<BEBehaviorEFreezer>().powerSetting < maxConsumption*0.1)
            {
                Vintagestory.API.Common.Block originalBlock = Api.World.BlockAccessor.GetBlock(Pos);
                AssetLocation newBlockAL = originalBlock.CodeWithVariant("status", "melted");
                Vintagestory.API.Common.Block newBlock = Api.World.GetBlock(newBlockAL);
                Api.World.BlockAccessor.ExchangeBlock(newBlock.Id, Pos);
                MarkDirty();
            }
        }
    }

    private void TryRefuel()
    {
        if (GetBehavior<BEBehaviorEFreezer>().powerSetting >= maxConsumption * 0.1)
        {
            Vintagestory.API.Common.Block originalBlock = Api.World.BlockAccessor.GetBlock(Pos);
            AssetLocation newBlockAL = originalBlock.CodeWithVariant("status", "frozen");
            Vintagestory.API.Common.Block newBlock = Api.World.GetBlock(newBlockAL);
            Api.World.BlockAccessor.ExchangeBlock(newBlock.Id, Pos);
            MarkDirty();
        }

    }

    public void OnBlockInteract(IPlayer byPlayer, bool isOwner, BlockSelection blockSel)
    {
        if (Api.Side == EnumAppSide.Client)
        {

        }
        else
        {
            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                TreeAttribute tree = new TreeAttribute();
                inventory.ToTreeAttributes(tree);
                tree.ToBytes(writer);
                data = ms.ToArray();
            }

            ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                (IServerPlayer)byPlayer,
                blockSel.Position,
                (int)EnumBlockStovePacket.OpenGUI,
                data
            );

            byPlayer.InventoryManager.OpenInventory(inventory);
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        closedDelay = tree.GetInt("closedDelay");
        isOpened = tree.GetBool("isOpened");
        if (Api != null)
        {
            inventory.AfterBlocksLoaded(Api.World);
            if (Api.Side == EnumAppSide.Client)
            {
                UpdateMeshes();
            }
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetInt("closedDelay", closedDelay);
        tree.SetBool("isOpened", isOpened);
    }


    public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
    {
        if (packetid <= 1000)
        {
            inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);
        }

        if (packetid == (int)EnumBlockEntityPacketId.Close)
        {
            if (fromPlayer.InventoryManager != null)
            {
                fromPlayer.InventoryManager.CloseInventory(Inventory);
            }
        }
    }

    public override void OnReceivedServerPacket(int packetid, byte[] data)
    {
        base.OnReceivedServerPacket(packetid, data);

        if (packetid == (int)EnumBlockStovePacket.OpenGUI)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                TreeAttribute tree = new TreeAttribute();
                tree.FromBytes(reader);
                Inventory.FromTreeAttributes(tree);
                Inventory.ResolveBlocksOrItems();

                IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                if (freezerDialog == null)
                {
                    freezerDialog = new GuiEFreezer(Lang.Get("freezer-title-gui"), Inventory, Pos,
                        Api as ICoreClientAPI);
                    freezerDialog.OnClosed += () => { freezerDialog = null!; };
                }

                freezerDialog.TryOpen();
            }

        }

        if (packetid == (int)EnumBlockEntityPacketId.Close)
        {
            (Api.World as IClientWorldAccessor)!.Player.InventoryManager.CloseInventory(Inventory);
            freezerDialog?.TryClose();
            freezerDialog?.Dispose();
            freezerDialog = null;

        }
    }

    public override float GetPerishRate()
    {
        float initial = base.GetPerishRate();
        EnumAppSide side = Api.Side;
        if (GetBehavior<BEBehaviorEFreezer>().powerSetting < 10)
            return initial;
        return 5.0f / GetBehavior<BEBehaviorEFreezer>().powerSetting;
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack!);
        var electricity = ElectricityAddon;
        if (electricity != null)
        {
            electricity.Connection = Facing.DownAll;

            //задаем параметры блока/проводника
            var voltage = MyMiniLib.GetAttributeInt(byItemStack!.Block, "voltage", 32);
            var maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack!.Block, "maxCurrent", 5.0F);
            var isolated = MyMiniLib.GetAttributeBool(byItemStack!.Block, "isolated", false);

            this.ElectricityAddon.Eparams = (
                new EParams(voltage, maxCurrent, "", 0, 1, 1, false, isolated),
                FacingHelper.Faces(Facing.DownAll).First().Index);
        }
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        var electricity = ElectricityAddon;
        electricity!.Connection = Facing.None;
        if (freezerDialog != null)
        {
            freezerDialog.TryClose();
            if (freezerDialog != null)
            {
                freezerDialog.Dispose();
                freezerDialog = null!;
            }
        }
    }
}