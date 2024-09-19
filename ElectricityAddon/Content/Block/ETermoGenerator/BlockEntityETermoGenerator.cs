using System;
using Electricity.Utils;
using ElectricityUnofficial.Content.Block.ETermoGenerator;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.ETermoGenerator;

public class BlockEntityETermoGenerator : BlockEntityGenericTypedContainer
{
    ICoreClientAPI capi;
    ICoreServerAPI sapi;
    private InventoryTermoGenerator inventory;
    private GuiBlockEntityETermoGenerator clientDialog;
    private BlockEntityAnimationUtil animUtil => this.GetBehavior<BEBehaviorAnimatable>()?.animUtil;
    private Electricity.Content.Block.Entity.Behavior.Electricity Electricity => this.GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();
    
    private float prevGenTemp = 20f;
    public float genTemp = 20f;

    private int maxTemp;
    private float fuelBurnTime;
    private float maxBurnTime;
    public float GenTemp => genTemp;
    private ItemSlot FuelSlot => this.inventory[0];
    private ItemStack FuelStack
    {
        get { return this.inventory[0].Itemstack; }
        set
        {
            this.inventory[0].Itemstack = value;
            this.inventory[0].MarkDirty();
        }
    }

    public override InventoryBase Inventory => inventory;

    public string DialogTitle => Lang.Get("termogen");

    public override string InventoryClassName => "termogen";

    public BlockEntityETermoGenerator()
    {
        this.inventory = new InventoryTermoGenerator(null, null);
        this.inventory.SlotModified += OnSlotModified;
    }

    public override void OnBlockBroken(IPlayer byPlayer = null)
    {
        base.OnBlockBroken(null);
    }
    
    public void OnSlotModified(int slotId)
    {
        if (slotId == 0)
        {
            if (Inventory[0].Itemstack != null && !Inventory[0].Empty &&
                Inventory[0].Itemstack.Collectible.CombustibleProps != null)
            {
                if (fuelBurnTime == 0) CanDoBurn();
            }
        }

        base.Block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
        this.MarkDirty(this.Api.Side == EnumAppSide.Server, null);
        if (this.Api is ICoreClientAPI && this.clientDialog != null)
        {
            clientDialog.Update(genTemp, fuelBurnTime);
        }

        IWorldChunk chunkatPos = this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos);
        if (chunkatPos == null) return;
        chunkatPos.MarkModified();
    }
    public void OnBurnTick(float deltatime)
    {
        if (this.Api is ICoreServerAPI)
        {
            if (fuelBurnTime > 0f)
            {
                genTemp = ChangeTemperature(genTemp, maxTemp, deltatime);
                fuelBurnTime -= deltatime; // burn!
                if (fuelBurnTime <= 0f)
                {
                    fuelBurnTime = 0f;
                    maxBurnTime = 0f;
                    maxTemp = 20; // important
                    if (!Inventory[0].Empty) CanDoBurn();
                }
            }
            else
            {
                if (genTemp != 20f) genTemp = ChangeTemperature(genTemp, 20f, deltatime);
                CanDoBurn();
            }
            MarkDirty(true, null);
        }

        if (Api != null && Api.Side == EnumAppSide.Client)
        {
            if (this.clientDialog != null) clientDialog.Update(genTemp, fuelBurnTime);
            if (GenTemp > 20)
            {

                BlockEntityAnimationUtil animUtil = this.animUtil;
                if (animUtil != null)
                {
                    animUtil.StartAnimation(new AnimationMetaData()
                    {
                        Animation = "work-on",
                        Code = "work-on",
                        AnimationSpeed = 1F,
                        EaseOutSpeed = 4f,
                        EaseInSpeed = 1f
                    });
                }
            }
            else
            {
                if (animUtil != null && animUtil.activeAnimationsByAnimCode.Count > 0)
                {
                    animUtil.StopAnimation("work-on");
                }
            }
                
        }
    }
    public void CanDoBurn()
    {
        CombustibleProperties fuelProps = FuelSlot.Itemstack?.Collectible.CombustibleProps;
        if (fuelProps == null) return;
        if (fuelBurnTime > 0) return;
        if (fuelProps.BurnTemperature > 0f && fuelProps.BurnDuration > 0f)
        {
            maxBurnTime = fuelBurnTime = fuelProps.BurnDuration;
            maxTemp = fuelProps.BurnTemperature;
            FuelStack.StackSize--;
            if (FuelStack.StackSize <= 0)
            {
                FuelStack = null;
            }

            FuelSlot.MarkDirty();
            MarkDirty(true);
        }
    }
    public float ChangeTemperature(float fromTemp, float toTemp, float deltaTime)
    {
        float diff = Math.Abs(fromTemp - toTemp);
        deltaTime += deltaTime * (diff / 28f);
        if (diff < deltaTime)
        {
            return toTemp;
        }

        if (fromTemp > toTemp)
        {
            deltaTime = -deltaTime;
        }

        if (Math.Abs(fromTemp - toTemp) < 1f)
        {
            return toTemp;
        }
        return fromTemp + deltaTime;
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api.Side == EnumAppSide.Server)
        {
            sapi = api as ICoreServerAPI;
        }
        else
        {
            capi = api as ICoreClientAPI;
        }

        this.inventory.Pos = this.Pos;
        this.inventory.LateInitialize($"{InventoryClassName}-{this.Pos.X}/{this.Pos.Y}/{this.Pos.Z}", api);
        this.RegisterGameTickListener(new Action<float>(OnBurnTick),1000);
        CanDoBurn();
        if (api.Side == EnumAppSide.Client)
        {
            if (animUtil != null)
            {
                animUtil.InitializeAnimator("etrmogen", null, null, new Vec3f(0, GetRotation(), 0f));
            }
        }
    }
    public int GetRotation()
    {
        string side = Block.Variant["side"];
        int adjustedIndex = ((BlockFacing.FromCode(side)?.HorizontalAngleIndex ?? 1) + 3) & 3;
        return adjustedIndex * 90;
    }

    public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
    {
        if (this.Api.Side == EnumAppSide.Client)
        {
            base.toggleInventoryDialogClient(byPlayer, delegate
            {
                this.clientDialog =
                    new GuiBlockEntityETermoGenerator(DialogTitle, Inventory, this.Pos, this.Api as ICoreClientAPI, this);
                clientDialog.Update(genTemp, fuelBurnTime);
                return this.clientDialog;
            });
        }
        return true;
    }
    
    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        var electricity = Electricity;
        if (electricity != null)
        {
            electricity.Connection = Facing.DownAll;
        }
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        this.clientDialog?.TryClose();
        var electricity = Electricity;
        if (electricity != null) {
            electricity.Connection = Facing.None;
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        this.inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;
        tree.SetFloat("genTemp", genTemp);
        tree.SetInt("maxTemp", maxTemp);
        tree.SetFloat("fuelBurnTime", fuelBurnTime);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        this.inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        if (Api != null) Inventory.AfterBlocksLoaded(this.Api.World);
        genTemp = tree.GetFloat("genTemp", 0);
        maxTemp = tree.GetInt("maxTemp", 0);
        fuelBurnTime = tree.GetFloat("fuelBurnTime", 0);
        if (Api != null && Api.Side == EnumAppSide.Client)
        {
            if (this.clientDialog != null) clientDialog.Update(genTemp, fuelBurnTime);
            MarkDirty(true, null);
        }
    }
    
}