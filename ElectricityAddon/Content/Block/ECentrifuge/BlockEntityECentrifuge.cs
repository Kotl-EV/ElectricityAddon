using System;
using System.Collections.Generic;
using Electricity.Utils;
using ElectricityAddon.CustomRecipe;
using ElectricityAddon.CustomRecipe.Recipe;
using ElectricityUnofficial.Content.Block.ECentrifuge;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.ECentrifuge;

public class BlockEntityECentrifuge : BlockEntityOpenableContainer
{
  
  internal InventoryCentrifuge inventory;
  private GuiDialogCentrifuge clientDialog;
  public override string InventoryClassName => "ecentrifuge";
  public CentrifugeRecipe CurrentRecipe;

  public string CurrentRecipeName;
  public float RecipeProgress;


  public virtual string DialogTitle => Lang.Get("ecentrifuge");

  public override InventoryBase Inventory => (InventoryBase)this.inventory;
  private Electricity.Content.Block.Entity.Behavior.Electricity? Electricity => GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();

  public BlockEntityECentrifuge()
  {
    this.inventory = new InventoryCentrifuge((string)null, (ICoreAPI)null);
    this.inventory.SlotModified += new Action<int>(this.OnSlotModifid);
  }

  public override void Initialize(ICoreAPI api)
  {
    base.Initialize(api);
    this.inventory.LateInitialize(
      "ecentrifuge-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api);
    this.RegisterGameTickListener(new Action<float>(this.Every500ms), 500);
  }
  
  private void OnSlotModifid(int slotid)
  {
    if (this.Api is ICoreClientAPI)
      this.clientDialog.Update(RecipeProgress);
    if (slotid != 0)
      return;
    if (this.InputSlot.Empty)
      this.RecipeProgress = 0;
    this.MarkDirty();
    if (this.clientDialog == null || !this.clientDialog.IsOpened())
      return;
    this.clientDialog.SingleComposer.ReCompose();
    if (Api?.Side == EnumAppSide.Server)
    {
      FindMatchingRecipe();
      MarkDirty(true);
    }
  }
  
  private bool FindMatchingRecipe()
  {
    ItemSlot[] inputSlots = new ItemSlot[] { inventory[0] };
    CurrentRecipe = null;
    CurrentRecipeName = string.Empty;

    foreach (CentrifugeRecipe recipe in CustomRecipeManager.CentrifugeRecipes)
    {
      int outsize;

      if (recipe.Matches(inputSlots, out outsize))
      {
        CurrentRecipe = recipe;
        CurrentRecipeName = recipe.Output.ResolvedItemstack.GetName();
        MarkDirty(true);
        return true;
      }
    }
    return false;
  }

  
  private void Every500ms(float dt)
  {
    if(GetBehavior<BEBehaviorECentrifuge>().PowerSetting<1)
      return;
    if (!FindMatchingRecipe())
      return;
    RecipeProgress = (float)(RecipeProgress + GetBehavior<BEBehaviorECentrifuge>().PowerSetting/CurrentRecipe.EnergyOperation);
    UpdateState(RecipeProgress);
    if (RecipeProgress >= 1)
    {
      ItemStack outputItem = CurrentRecipe.Output.ResolvedItemstack.Clone();
      if (OutputSlot.Empty) OutputSlot.Itemstack = outputItem;
      else
      {
        int freeSpace = OutputSlot.Itemstack.Collectible.MaxStackSize - OutputSlot.Itemstack.StackSize;
        if (freeSpace <= 0) Api.World.SpawnItemEntity(outputItem, Pos.UpCopy(1).ToVec3d()); // should never fire
        else if (freeSpace >= outputItem.StackSize) OutputSlot.Itemstack.StackSize += outputItem.StackSize;
        else
        {
          OutputSlot.Itemstack.StackSize += freeSpace;
          outputItem.StackSize -= freeSpace;
          Api.World.SpawnItemEntity(outputItem, Pos.UpCopy(1).ToVec3d());
        }
      }
      InputSlot.TakeOut(CurrentRecipe.Ingredients[0].Quantity);
      RecipeProgress = 0;
    }
  }
  
  protected virtual void UpdateState(float RecipeProgress)
  {                    
    if (Api != null && Api.Side == EnumAppSide.Client && clientDialog != null && clientDialog.IsOpened())
    {
      clientDialog.Update(RecipeProgress);
    }
    MarkDirty(true);
  }
  

  
  public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
  {
    if (this.Api.Side == EnumAppSide.Client)
      this.toggleInventoryDialogClient(byPlayer, (CreateDialogDelegate)(() =>
      {
        this.clientDialog =
          new GuiDialogCentrifuge(this.DialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI);
        this.clientDialog.Update(RecipeProgress);
        return (GuiDialogBlockEntity)this.clientDialog;
      }));
    return true;
  }

  public override void OnReceivedServerPacket(int packetid, byte[] data)
  {
    base.OnReceivedServerPacket(packetid, data);
    if (packetid != 1001)
      return;
    (this.Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory((IInventory)this.Inventory);
    this.invDialog?.TryClose();
    this.invDialog?.Dispose();
    this.invDialog = (GuiDialogBlockEntity)null;
  }

  public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
  {
    base.FromTreeAttributes(tree, worldForResolving);
    this.Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
    this.RecipeProgress = tree.GetFloat("PowerCurrent");
    if (this.Api != null)
      this.Inventory.AfterBlocksLoaded(this.Api.World);
    ICoreAPI api = this.Api;
    if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) == 0 || this.clientDialog == null)
      return;
    this.clientDialog.Update(RecipeProgress);
  }

  public override void ToTreeAttributes(ITreeAttribute tree)
  {
    base.ToTreeAttributes(tree);
    ITreeAttribute tree1 = (ITreeAttribute)new TreeAttribute();
    this.Inventory.ToTreeAttributes(tree1);
    tree["inventory"] = (IAttribute)tree1;
    tree.SetFloat("PowerCurrent", this.RecipeProgress);
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
    this.clientDialog?.TryClose();
    var electricity = Electricity;
    if (electricity != null) {
      electricity.Connection = Facing.None;
    }
  }
  
  public ItemSlot InputSlot => this.inventory[0];

  public ItemSlot OutputSlot => this.inventory[1];

  public ItemStack InputStack
  {
    get => this.inventory[0].Itemstack;
    set
    {
      this.inventory[0].Itemstack = value;
      this.inventory[0].MarkDirty();
    }
  }

  public ItemStack OutputStack
  {
    get => this.inventory[1].Itemstack;
    set
    {
      this.inventory[1].Itemstack = value;
      this.inventory[1].MarkDirty();
    }
  }
  
  
  public override void OnBlockUnloaded()
  {
    base.OnBlockUnloaded();
    this.clientDialog?.TryClose();
  }
}