using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Electricity.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EStove;

public class BlockEntityEStove : BlockEntityContainer, IHeatSource, ITexPositionSource
{
    protected Shape nowTesselatingShape;
    protected CollectibleObject nowTesselatingObj;
    protected MeshData[] meshes;
    ICoreClientAPI capi;
    ICoreServerAPI sapi;
    internal InventorySmelting inventory;
    private Electricity.Content.Block.Entity.Behavior.Electricity? Electricity => GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();

    // Temperature before the half second tick
    public float prevStoveTemperature = 20;

    // Current temperature of the furnace
    public float stoveTemperature = 20;

    public float inputStackCookingTime;

    GuiDialogBlockEntityEStove clientDialog;
    bool clientSidePrevBurning;




    #region Config

    public virtual float HeatModifier
    {
        get { return 1f; }
    }

    // Resting temperature
    public virtual int enviromentTemperature()
    {
        return 20;
    }

    // seconds it requires to melt the ore once beyond melting point
    public virtual float maxCookingTime()
    {
        return inputSlot.Itemstack == null ? 30f : inputSlot.Itemstack.Collectible.GetMeltingDuration(Api.World, inventory, inputSlot);
    }
    
    public override InventoryBase Inventory => inventory;

    public override string InventoryClassName => "blockestove";

    public virtual string DialogTitle
    {
        get { return Lang.Get("BlockEStove"); }
    }

    #endregion



    public BlockEntityEStove()
    {
        inventory = new InventorySmelting(null, null);
        inventory.SlotModified += OnSlotModifid;
        meshes = new MeshData[6];
    }



    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        inventory.pos = Pos;
        inventory.LateInitialize("smelting-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
        if (api.Side == EnumAppSide.Server)
        {
            sapi = api as ICoreServerAPI;
        }
        else
        {
            capi = api as ICoreClientAPI;
        }
        UpdateMeshes();
        MarkDirty(true);
        RegisterGameTickListener(OnBurnTick, 250);
        RegisterGameTickListener(On500msTick, 500);
    }
    
        public void UpdateMesh(int slotid)
    {
        if (Api == null || Api.Side == EnumAppSide.Server)
        {
            return;
        }
        if (slotid == 0) return;
        if (inventory[slotid].Empty)
        {
            meshes[slotid] = null;
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
                case 1:
                {
                    y = 1.04f;
                    break;
                }
                case 2:
                {
                    y = 1.04f;
                    break;
                }
            }


            if (!Inventory[slotId].Empty)
            {
                if (Inventory[slotId].Itemstack.Class == EnumItemClass.Block)
                {
                    meshData.Scale(new Vec3f(0.5f, 0, 0.5f), 0.93f, 0.93f, 0.93f);
                    meshData.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 8 * GameMath.DEG2RAD, 0);
                }
                else
                {
                    meshData.Scale(new Vec3f(0.5f, 0, 0.5f), 1.0f, 1.0f, 1.0f);
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
            
        public Size2i AtlasSize
        {
            get
            {
                return capi.BlockTextureAtlas.Size;
            }
        }

        public TextureAtlasPosition this [string textureCode]
        {
            get
            {
                Vintagestory.API.Common.Item item = nowTesselatingObj as Vintagestory.API.Common.Item;
                Dictionary<string, CompositeTexture> dictionary = (Dictionary<string, CompositeTexture>)((item != null) ? item.Textures : (nowTesselatingObj as Vintagestory.API.Common.Block).Textures);
                AssetLocation assetLocation = null;
                CompositeTexture compositeTexture;
                if (dictionary.TryGetValue(textureCode, out compositeTexture))
                {
                    assetLocation = compositeTexture.Baked.BakedName;
                }
                if (assetLocation == null && dictionary.TryGetValue("all", out compositeTexture))
                {
                    assetLocation = compositeTexture.Baked.BakedName;
                }
                if (assetLocation == null)
                {
                    Shape shape = nowTesselatingShape;
                    if (shape != null)
                    {
                        shape.Textures.TryGetValue(textureCode, out assetLocation);
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
                IAsset asset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (asset != null)
                {
                    int num;
                    capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out num, out textureAtlasPosition, null, 0.005f);
                }
                else
                {
                    ILogger logger = capi.World.Logger;
                    string str = "For render in block ";
                    AssetLocation code = Block.Code;
                    logger.Warning(str + ((code != null) ? code.ToString() : null) + ", item {0} defined texture {1}, not no such texture found.", nowTesselatingObj.Code, texturePath);
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
                    meshData = coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                }
                else
                {
                    nowTesselatingObj = stack.Collectible;
                    nowTesselatingShape = null;
                    if (stack.Item.Shape != null)
                    {
                        nowTesselatingShape = coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                        
                    }
                    coreClientAPI.Tesselator.TesselateItem(stack.Item, out meshData, this);
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
        for (int i = 0; i < inventory.Count-1; i++)
        {
            UpdateMesh(i);
        }
        MarkDirty(true);
    }
    
    
    
    private void OnSlotModifid(int slotid)
    {
        Block = Api.World.BlockAccessor.GetBlock(Pos);

        MarkDirty(Api.Side == EnumAppSide.Server); // Save useless triple-remesh by only letting the server decide when to redraw

        if (Api is ICoreClientAPI && clientDialog != null)
        {
            SetDialogValues(clientDialog.Attributes);
        }

        Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
    }



    public bool IsBurning;


    public int getInventoryStackLimit()
    {
        return 64;
    }


    private void OnBurnTick(float dt)
    {
        // Only tick on the server and merely sync to client
        if (Api is ICoreClientAPI)
        {
            //renderer.contentStackRenderer.OnUpdate(InputStackTemp);
            return;
        }

        // Furnace is burning: Heat furnace
        if (IsBurning)
        {
            stoveTemperature = changeTemperature(stoveTemperature, GetBehavior<BEBehaviorEStove>().powerSetting * 13.25F, dt);
        }

        // Ore follows furnace temperature
        if (canHeatInput())
        {
            heatInput(dt);
        }
        else
        {
            inputStackCookingTime = 0;
        }

        if (canHeatOutput())
        {
            heatOutput(dt);
        }


        // Finished smelting? Turn to smelted item
        if (canSmeltInput() && inputStackCookingTime > maxCookingTime())
        {
            smeltItems();
        }
        
        //if (GetBehavior<BEBehaviorEStove>().powerSetting > 0)
        if (GetBehavior<BEBehaviorEStove>()?.powerSetting > 0)
        {

            if (!IsBurning)
            {
                IsBurning = true;

                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                MarkDirty(true);
            }
        }
        else
        {
            if (IsBurning)                     //готовка закончилась
            {
                IsBurning = false;

                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
                MarkDirty(true);
                Api.World.PlaySoundAt(new AssetLocation("electricityaddon:sounds/din_din_din"), Pos.X, Pos.Y, Pos.Z, null, false, 8.0F, 0.4F);
            }
        }


        // Furnace is not burning: Cool down furnace and ore also turn of fire
        if (!IsBurning)
        {
            stoveTemperature = changeTemperature(stoveTemperature, enviromentTemperature(), dt);
        }

    }


    // Sync to client every 500ms
    private void On500msTick(float dt)
    {
        if (Api is ICoreServerAPI && (IsBurning || prevStoveTemperature != stoveTemperature))
        {
            MarkDirty();
        }

        prevStoveTemperature = stoveTemperature;
    }


    public float changeTemperature(float fromTemp, float toTemp, float dt)
    {
        float diff = Math.Abs(fromTemp - toTemp);

        dt = dt + dt * (diff / 28);


        if (diff < dt)
        {
            return toTemp;
        }

        if (fromTemp > toTemp)
        {
            dt = -dt;
        }

        if (Math.Abs(fromTemp - toTemp) < 1)
        {
            return toTemp;
        }

        return fromTemp + dt;
    }

    public void heatInput(float dt)
    {
        float oldTemp = InputStackTemp;
        float nowTemp = oldTemp;
        float meltingPoint = inputSlot.Itemstack.Collectible.GetMeltingPoint(Api.World, inventory, inputSlot);

        // Only Heat ore. Cooling happens already in the itemstack
        if (oldTemp < stoveTemperature)
        {
            float f = (1 + GameMath.Clamp((stoveTemperature - oldTemp) / 30, 0, 1.6f)) * dt;
            if (nowTemp >= meltingPoint) f /= 11;

            float newTemp = changeTemperature(oldTemp, stoveTemperature, f);
            int maxTemp = 0;
            if (inputStack.ItemAttributes != null)
            {
                maxTemp = Math.Max(inputStack.Collectible.CombustibleProps == null ? 0 : inputStack.Collectible.CombustibleProps.MaxTemperature, inputStack.ItemAttributes["maxTemperature"] == null ? 0 : inputStack.ItemAttributes["maxTemperature"].AsInt());
            }
            else
            {
                maxTemp = inputStack.Collectible.CombustibleProps == null ? 0 : inputStack.Collectible.CombustibleProps.MaxTemperature;
            }
            if (maxTemp > 0)
            {
                newTemp = Math.Min(maxTemp, newTemp);
            }

            if (oldTemp != newTemp)
            {
                InputStackTemp = newTemp;
                nowTemp = newTemp;
            }
        }

        // Begin smelting when hot enough
        if (nowTemp >= meltingPoint)
        {
            float diff = nowTemp / meltingPoint;
            inputStackCookingTime += GameMath.Clamp((int)(diff), 1, 30) * dt;
        }
        else
        {
            if (inputStackCookingTime > 0) inputStackCookingTime--;
        }
    }

    public void heatOutput(float dt)
    {
        //dt *= 20;

        float oldTemp = OutputStackTemp;

        // Only Heat ore. Cooling happens already in the itemstack
        if (oldTemp < stoveTemperature)
        {
            float newTemp = changeTemperature(oldTemp, stoveTemperature, 2 * dt);
            int maxTemp = Math.Max(outputStack.Collectible.CombustibleProps == null ? 0 : outputStack.Collectible.CombustibleProps.MaxTemperature, outputStack.ItemAttributes["maxTemperature"] == null ? 0 : outputStack.ItemAttributes["maxTemperature"].AsInt());
            if (maxTemp > 0)
            {
                newTemp = Math.Min(maxTemp, newTemp);
            }

            if (oldTemp != newTemp)
            {
                OutputStackTemp = newTemp;
            }
        }
    }


    public float InputStackTemp
    {
        get
        {
            return GetTemp(inputStack);
        }
        set
        {
            SetTemp(inputStack, value);
        }
    }

    public float OutputStackTemp
    {
        get
        {
            return GetTemp(outputStack);
        }
        set
        {
            SetTemp(outputStack, value);
        }
    }


    float GetTemp(ItemStack stack)
    {
        if (stack == null) return enviromentTemperature();

        if (inventory.CookingSlots.Length > 0)
        {
            bool haveStack = false;
            float lowestTemp = 0;
            for (int i = 0; i < inventory.CookingSlots.Length; i++)
            {
                ItemStack cookingStack = inventory.CookingSlots[i].Itemstack;
                if (cookingStack != null)
                {
                    float stackTemp = cookingStack.Collectible.GetTemperature(Api.World, cookingStack);
                    lowestTemp = haveStack ? Math.Min(lowestTemp, stackTemp) : stackTemp;
                    haveStack = true;
                }

            }

            return lowestTemp;

        }

        return stack.Collectible.GetTemperature(Api.World, stack);
    }

    void SetTemp(ItemStack stack, float value)
    {
        if (stack == null) return;
        if (inventory.CookingSlots.Length > 0)
        {
            for (int i = 0; i < inventory.CookingSlots.Length; i++)
            {
                if (inventory.CookingSlots[i].Itemstack != null) inventory.CookingSlots[i].Itemstack.Collectible.SetTemperature(Api.World, inventory.CookingSlots[i].Itemstack, value);
            }
        }
        else
        {
            stack.Collectible.SetTemperature(Api.World, stack, value);
        }
    }

    public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
    {
        return IsBurning ? 5 : 0;
    }

    public bool canHeatInput()
    {
        return
            canSmeltInput() || inputStack != null && inputStack.ItemAttributes != null && (inputStack.ItemAttributes["allowHeating"] != null && inputStack.ItemAttributes["allowHeating"].AsBool())
        ;
    }

    public bool canHeatOutput()
    {
        return outputStack?.ItemAttributes?["allowHeating"] != null && outputStack.ItemAttributes["allowHeating"].AsBool();
    }

    public bool canSmeltInput()
    {
        return
            inputStack != null
            && inputStack.Collectible.CanSmelt(Api.World, inventory, inputSlot.Itemstack, outputSlot.Itemstack)
            && (inputStack.Collectible.CombustibleProps == null || !inputStack.Collectible.CombustibleProps.RequiresContainer)
        ;
    }


    public void smeltItems()
    {
        inputStack.Collectible.DoSmelt(Api.World, inventory, inputSlot, outputSlot);
        InputStackTemp = enviromentTemperature();
        inputStackCookingTime = 0;
        MarkDirty(true);
        inputSlot.MarkDirty();
    }


    #region Events
    
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
                writer.Write("BlockEntityStove");
                writer.Write(DialogTitle);
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
        Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

        if (Api != null)
        {
            Inventory.AfterBlocksLoaded(Api.World);
        }


        stoveTemperature = tree.GetFloat("stoveTemperature");
        inputStackCookingTime = tree.GetFloat("oreCookingTime");

        if (Api != null)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                if (clientDialog != null) SetDialogValues(clientDialog.Attributes);
            }


            if (Api.Side == EnumAppSide.Client && clientSidePrevBurning != IsBurning)
            {
                clientSidePrevBurning = IsBurning;
                MarkDirty(true);
            }
            inventory.AfterBlocksLoaded(Api.World);
            if (Api.Side == EnumAppSide.Client)
            {
                UpdateMeshes();
            }
        }
        
    }

    void SetDialogValues(ITreeAttribute dialogTree)
    {
        dialogTree.SetFloat("stoveTemperature", stoveTemperature);
        dialogTree.SetFloat("oreCookingTime", inputStackCookingTime);

        if (inputSlot.Itemstack != null)
        {
            float meltingDuration = inputSlot.Itemstack.Collectible.GetMeltingDuration(Api.World, inventory, inputSlot);

            dialogTree.SetFloat("oreTemperature", InputStackTemp);
            dialogTree.SetFloat("maxOreCookingTime", meltingDuration);
        }
        else
        {
            dialogTree.RemoveAttribute("oreTemperature");
        }

        dialogTree.SetString("outputText", inventory.GetOutputText());
        dialogTree.SetInt("haveCookingContainer", inventory.HaveCookingContainer ? 1 : 0);
        dialogTree.SetInt("quantityCookingSlots", inventory.CookingSlots.Length);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        Inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;

        tree.SetFloat("stoveTemperature", stoveTemperature);
        tree.SetFloat("oreCookingTime", inputStackCookingTime);
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

        if (clientDialog != null)
        {
            clientDialog.TryClose();
            if (clientDialog != null)
            {
                clientDialog.Dispose();
                clientDialog = null;
            }
        }
    }
    
    public override void OnBlockBroken(IPlayer? byPlayer = null) {
        base.OnBlockBroken(byPlayer);
        if (inputStack != null) {
            Api.World.SpawnItemEntity(inputStack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }
    }
    
    public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
    {
        if (packetid < 1000)
        {
            Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);

            // Tell server to save this chunk to disk again
            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();

            return;
        }

        if (packetid == (int)EnumBlockStovePacket.CloseGUI)
        {
            if (player.InventoryManager != null)
            {
                player.InventoryManager.CloseInventory(Inventory);
            }
        }
    }

    public override void OnReceivedServerPacket(int packetid, byte[] data)
    {
        if (packetid == (int)EnumBlockStovePacket.OpenGUI)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);

                string dialogClassName = reader.ReadString();
                string dialogTitle = reader.ReadString();

                TreeAttribute tree = new TreeAttribute();
                tree.FromBytes(reader);
                Inventory.FromTreeAttributes(tree);
                Inventory.ResolveBlocksOrItems();

                IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                SyncedTreeAttribute dtree = new SyncedTreeAttribute();
                SetDialogValues(dtree);

                if (clientDialog != null)
                {
                    clientDialog.TryClose();
                    clientDialog = null;
                }
                else
                {
                    clientDialog = new GuiDialogBlockEntityEStove(dialogTitle, Inventory, Pos, dtree, Api as ICoreClientAPI);
                    clientDialog.OnClosed += () => { clientDialog.Dispose(); clientDialog = null; };
                    clientDialog.TryOpen();

                }
            }
        }

        if (packetid == (int)EnumBlockEntityPacketId.Close)
        {
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
            clientWorld.Player.InventoryManager.CloseInventory(Inventory);
        }
    }

    #endregion

    #region Helper getters


    public ItemSlot inputSlot
    {
        get { return inventory[1]; }
    }

    public ItemSlot outputSlot
    {
        get { return inventory[2]; }
    }

    public ItemSlot[] otherCookingSlots
    {
        get { return inventory.CookingSlots; }
    }

    public ItemStack inputStack
    {
        get { return inventory[1].Itemstack; }
        set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
    }

    public ItemStack outputStack
    {
        get { return inventory[2].Itemstack; }
        set { inventory[2].Itemstack = value; inventory[2].MarkDirty(); }
    }


    public CombustibleProperties fuelCombustibleOpts
    {
        get { return getCombustibleOpts(0); }
    }

    public CombustibleProperties getCombustibleOpts(int slotid)
    {
        ItemSlot slot = inventory[slotid];
        if (slot.Itemstack == null) return null;
        return slot.Itemstack.Collectible.CombustibleProps;
    }

    #endregion


    public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (var slot in Inventory)
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

            slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
        }

        foreach (ItemSlot slot in inventory.CookingSlots)
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

            slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
        }
    }

    public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
    {
        foreach (var slot in Inventory)
        {
            if (slot.Itemstack == null) continue;
            if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
            {
                slot.Itemstack = null;
            }
            else
            {
                slot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, slot, oldBlockIdMapping, oldItemIdMapping);
            }
        }

        foreach (ItemSlot slot in inventory.CookingSlots)
        {
            if (slot.Itemstack == null) continue;
            if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, Api.World))
            {
                slot.Itemstack = null;
            }
            else
            {
                slot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, slot, oldBlockIdMapping, oldItemIdMapping);
            }
        }
    }
    
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        if (inputStack != null) {
            var temp = (int)inputStack.Collectible.GetTemperature(Api.World, inputStack);  //беда с отобраением температуры, иногда забывает
            stringBuilder.AppendLine();
                if (temp <= 25)
                {
                    stringBuilder.AppendLine(Lang.Get("Contents") + inputStack.StackSize + "×" +
                                             inputStack.GetName() +
                                             "\n└ " + Lang.Get("Temperature") + Lang.Get("Cold"));
                }
                else
                    stringBuilder.AppendLine(Lang.Get("Contents") + inputStack.StackSize + "×" +
                                             inputStack.GetName() +
                                             "\n└ " + Lang.Get("Temperature") + temp + " °C");  
            
        }
    }
}