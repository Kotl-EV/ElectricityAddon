using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EHorn;

public class BlockEntityEHorn : BlockEntity, IHeatSource
{
    private readonly Vec3d tmpPos = new();
    private ILoadedSound? ambientSound;
    private bool burning;
    private bool clientSidePrevBurning;
    private double lastTickTotalHours;
    private double lastPlaySoundDin=0;
    private float maxTargetTemp => MyMiniLib.GetAttributeFloat(this.Block, "maxTargetTemp", 1100.0F);
    private int maxConsumption => MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);

    private ForgeContentsRenderer? renderer;
    private WeatherSystemBase? weatherSystem;


    public ItemStack? Contents { get; private set; }

    public bool IsBurning
    {
        get => this.burning;
        set
        {
            if (this.burning != value)
            {
                if (value && !this.burning)
                {
                    this.renderer?.SetContents(this.Contents, 0, this.burning, false);
                    this.lastTickTotalHours = this.Api.World.Calendar.TotalHours;
                    this.MarkDirty();
                }

                this.burning = value;
            }
        }
    }

    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();


    //передает значения из Block в BEBehaviorElectricityAddon
    public (EParams, int) Eparams
    {
        get => this.ElectricityAddon!.Eparams;
        set => this.ElectricityAddon!.Eparams = value;
    }

    //передает значения из Block в BEBehaviorElectricityAddon
    public EParams[] AllEparams
    {
        get => this.ElectricityAddon?.AllEparams ?? null;
        set
        {
            if (this.ElectricityAddon != null)
            {
                this.ElectricityAddon.AllEparams = value;
            }
        }
    }


    public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
    {
        return this.burning
            ? 7
            : 0;
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        this.Contents?.ResolveBlockOrItem(api.World);

        if (api is ICoreClientAPI clientApi)
        {
            clientApi.Event.RegisterRenderer(this.renderer = new ForgeContentsRenderer(this.Pos, clientApi),
                EnumRenderStage.Opaque, "forge");
            this.renderer.SetContents(this.Contents, 0, this.burning, true);

            this.RegisterGameTickListener(this.OnClientTick, 50);
        }

        this.weatherSystem = api.ModLoader.GetModSystem<WeatherSystemBase>();

        this.RegisterGameTickListener(this.OnCommonTick, 200);
    }

    private void OnClientTick(float dt)
    {
        ICoreAPI api = this.Api;
        if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0 && this.clientSidePrevBurning != this.burning)
        {
            this.ToggleAmbientSounds(this.IsBurning);
            this.clientSidePrevBurning = this.IsBurning;
        }
        if (this.burning && this.Api.World.Rand.NextDouble() < 0.13)
            BlockEntityCoalPile.SpawnBurningCoalParticles(this.Api, this.Pos.ToVec3d().Add(0.25, 0.875, 0.25), 0.5f, 0.5f);
        if (this.renderer == null)
            return;
        this.renderer.SetContents(this.Contents, 0, this.burning, false);
    }

    private void OnCommonTick(float dt)
    {
        if (this.burning)
        {
            double num1 = this.Api.World.Calendar.TotalHours - this.lastTickTotalHours;
            if (this.Contents != null)
            {
                float temperature = this.Contents.Collectible.GetTemperature(this.Api.World, this.Contents);
                float power = GetBehavior<BEBehaviorEHorn>().getPowerReceive();
                if ((double) temperature < power * maxTargetTemp / maxConsumption)
                {
                    float num2 = (float) (num1 * 1500.0);
                    
                    this.Contents.Collectible.SetTemperature(this.Api.World, this.Contents, Math.Min(power * 11F, temperature + num2));
                }
                else
                {

                    if (this.Api.Side != EnumAppSide.Client &&  this.Api.World.Calendar.TotalHours - this.lastPlaySoundDin > 1)  //если нагрелось до максимума, то какждый раз в час звоним, чтобы игрок не забыл за горн
                    {
                        Api.World.PlaySoundAt(new AssetLocation("electricityaddon:sounds/din_din_din"), Pos.X, Pos.Y, Pos.Z, null, false, 8.0F, 0.4F);
                        this.lastPlaySoundDin = this.Api.World.Calendar.TotalHours;
                    }
                }

            }
            else
            {
                this.IsBurning = false;
            }
        }
        this.tmpPos.Set(this.Pos.X + 0.5, this.Pos.Y + 0.5, this.Pos.Z + 0.5);

        double rainLevel = 0;

        var rainCheck = this.Api.Side == EnumAppSide.Server
                        && this.Api.World.Rand.NextDouble() < 0.15
                        && this.Api.World.BlockAccessor.GetRainMapHeightAt(this.Pos.X, this.Pos.Z) <= this.Pos.Y
                        && (rainLevel = this.weatherSystem!.GetPrecipitation(this.tmpPos)) > 0.1;

        if (rainCheck && this.Api.World.Rand.NextDouble() < rainLevel * 5) {
            var playSound = false;

            if (this.burning) {
                playSound = true;

                this.MarkDirty();
            }

            var temp = this.Contents == null
                ? 0
                : this.Contents.Collectible.GetTemperature(this.Api.World, this.Contents);

            if (temp > 20) {
                playSound = temp > 100;
                this.Contents?.Collectible.SetTemperature(this.Api.World, this.Contents, Math.Min(GetBehavior<BEBehaviorEHorn>().getPowerReceive() * 11F, temp - 8), false);
                this.MarkDirty();
            }

            if (playSound) {
                this.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), this.Pos.X + 0.5, this.Pos.Y + 0.75, this.Pos.Z + 0.5, null, false, 16);
            }
        }
        this.lastTickTotalHours = this.Api.World.Calendar.TotalHours;
    }

    public void ToggleAmbientSounds(bool on)
    {
        if (this.Api.Side != EnumAppSide.Client)
        {
            return;
        }

        if (on)
        {
            if (!(this.ambientSound is { IsPlaying: true }))
            {
                this.ambientSound = ((IClientWorldAccessor)this.Api.World).LoadSound(
                    new SoundParams
                    {
                        Location = new AssetLocation("sounds/effect/embers.ogg"),
                        ShouldLoop = true,
                        Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 1
                    }
                );

                this.ambientSound.Start();
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                
            }
        }
        else
        {
            this.ambientSound?.Stop();
            this.ambientSound?.Dispose();
            this.ambientSound = null;
            Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);

        }
    }

    internal bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        //проверяем не сгорел ли прибор
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Pos) is BlockEntityEHorn entity && entity.AllEparams != null)
        {
            bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
            if (hasBurnout)
            {
                return false;
            }
        }


        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (!byPlayer.Entity.Controls.ShiftKey)
        {
            if (this.Contents == null)
            {
                return false;
            }

            var split = this.Contents.Clone();
            split.StackSize = 1;
            this.Contents.StackSize--;

            if (this.Contents.StackSize == 0)
            {
                this.Contents = null;
            }

            if (!byPlayer.InventoryManager.TryGiveItemstack(split))
            {
                world.SpawnItemEntity(split, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            this.renderer?.SetContents(this.Contents, 0, this.burning, true);
            this.MarkDirty();
            this.Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), this.Pos.X, this.Pos.Y, this.Pos.Z,
                byPlayer, false);

            return true;
        }

        if (slot.Itemstack == null)
        {
            return false;
        }

        var firstCodePart = slot.Itemstack.Collectible.FirstCodePart();
        var forgableGeneric = slot.Itemstack.Collectible.Attributes?.IsTrue("forgable") == true;

        // Add heatable item
        if (this.Contents == null && (firstCodePart == "ingot" || firstCodePart == "metalplate" ||
                                      firstCodePart == "workitem" || forgableGeneric))
        {
            this.Contents = slot.Itemstack.Clone();
            this.Contents.StackSize = 1;

            slot.TakeOut(1);
            slot.MarkDirty();

            this.renderer?.SetContents(this.Contents, 0, this.burning, true);
            this.MarkDirty();
            this.Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), this.Pos.X, this.Pos.Y, this.Pos.Z,
                byPlayer, false);

            this.IsBurning = true;

            return true;
        }

        // Merge heatable item
        if (!forgableGeneric && this.Contents != null &&
            this.Contents.Equals(this.Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes) &&
            this.Contents.StackSize < 4 &&
            this.Contents.StackSize < this.Contents.Collectible.MaxStackSize)
        {
            var myTemp = this.Contents.Collectible.GetTemperature(this.Api.World, this.Contents);
            var histemp = slot.Itemstack.Collectible.GetTemperature(this.Api.World, slot.Itemstack);

            this.Contents.Collectible.SetTemperature(world, this.Contents,
                (myTemp * this.Contents.StackSize + histemp * 1) / (this.Contents.StackSize + 1));
            this.Contents.StackSize++;

            slot.TakeOut(1);
            slot.MarkDirty();

            this.renderer?.SetContents(this.Contents, 0, this.burning, true);
            this.Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), this.Pos.X, this.Pos.Y, this.Pos.Z,
                byPlayer, false);

            this.MarkDirty();

            return true;
        }

        return false;
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        var electricity = this.ElectricityAddon;

        if (electricity != null)
        {
            electricity.Connection = Facing.DownAll;

            //задаем параметры блока/проводника
            var voltage = MyMiniLib.GetAttributeInt(byItemStack!.Block, "voltage", 32);
            var maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack!.Block, "maxCurrent", 5.0F);
            var isolated = MyMiniLib.GetAttributeBool(byItemStack!.Block, "isolated", false);

            this.ElectricityAddon!.Connection = Facing.DownAll;
            this.ElectricityAddon.Eparams = (
                new EParams(voltage, maxCurrent, "", 0, 1, 1, false, isolated),
                FacingHelper.Faces(Facing.DownAll).First().Index);
        }
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        if (this.renderer != null)
        {
            this.renderer.Dispose();
            this.renderer = null;
        }

        this.ambientSound?.Dispose();
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);

        if (this.Contents != null)
        {
            this.Api.World.SpawnItemEntity(this.Contents, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        this.ambientSound?.Dispose();
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        this.Contents = tree.GetItemstack("contents");
        this.burning = tree.GetInt("burning") > 0;
        this.lastTickTotalHours = tree.GetDouble("lastTickTotalHours");

        if (this.Api != null)
        {
            this.Contents?.ResolveBlockOrItem(this.Api.World);
        }

        this.renderer?.SetContents(this.Contents, 0, this.burning, true);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetItemstack("contents", this.Contents);

        tree.SetInt("burning",
            this.burning
                ? 1
                : 0);

        tree.SetDouble("lastTickTotalHours", this.lastTickTotalHours);
    }
    
    
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (this.Contents == null)
            return;
        int temperature = (int) this.Contents.Collectible.GetTemperature(this.Api.World, this.Contents);
        if (temperature <= 25)
            dsc.AppendLine(Lang.Get("forge-contentsandtemp-cold", (object) this.Contents.StackSize, (object) this.Contents.GetName()));
        else
            dsc.AppendLine(Lang.Get("forge-contentsandtemp", (object) this.Contents.StackSize, (object) this.Contents.GetName(), (object) temperature));
    }

    public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve,
        Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping,
        int schematicSeed)
    {
        base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed);

        if (this.Contents?.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve) == false)
        {
            this.Contents = null;
        }
    }

    public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping,
        Dictionary<int, AssetLocation> itemIdMapping)
    {
        base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);

        if (this.Contents != null)
        {
            if (this.Contents.Class == EnumItemClass.Item)
            {
                blockIdMapping[this.Contents.Id] = this.Contents.Item.Code;
            }
            else
            {
                itemIdMapping[this.Contents.Id] = this.Contents.Block.Code;
            }
        }
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();

        this.renderer?.Dispose();
    }
}