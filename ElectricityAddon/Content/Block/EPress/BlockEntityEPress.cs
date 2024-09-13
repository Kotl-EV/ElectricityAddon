using System;
using Electricity.Utils;
using ElectricityAddon.CustomRecipe;
using ElectricityAddon.CustomRecipe.Recipe;
using ElectricityUnofficial.Content.Block.EPress;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EPress;

public class BlockEntityEPress : BlockEntityGenericTypedContainer
{
  
  internal InventoryPress inventory;
  private GuiDialogPress clientDialog;
  public override string InventoryClassName => "epress";
  public PressRecipe CurrentRecipe;

  public string CurrentRecipeName;
  public float RecipeProgress;

  public virtual string DialogTitle => Lang.Get("epress");

  public override InventoryBase Inventory => (InventoryBase)this.inventory;
  private Electricity.Content.Block.Entity.Behavior.Electricity? Electricity => GetBehavior<Electricity.Content.Block.Entity.Behavior.Electricity>();
  private BlockEntityAnimationUtil animUtil => this.GetBehavior<BEBehaviorAnimatable>()?.animUtil;

  public BlockEntityEPress()
  {
    this.inventory = new InventoryPress((string)null, (ICoreAPI)null);
    this.inventory.SlotModified += new Action<int>(this.OnSlotModifid);
  }

  public override void Initialize(ICoreAPI api)
  {
    base.Initialize(api);
    this.inventory.LateInitialize(
      "epress-" + this.Pos.X.ToString() + "/" + this.Pos.Y.ToString() + "/" + this.Pos.Z.ToString(), api);
    this.RegisterGameTickListener(new Action<float>(this.Every500ms), 500);
    if (api.Side == EnumAppSide.Client)
    {
      if (animUtil != null)
      {
        animUtil.InitializeAnimator("epress", null, null, new Vec3f(0, GetRotation(), 0f));
      }
    }
  }
  
  public int GetRotation()
  {
    string side = Block.Variant["side"];
    // The BlockFacing horiztonal index goes counter-clockwise from east. That needs to be converted so that
    // it goes counter-clockwise from north instead.
    int adjustedIndex = ((BlockFacing.FromCode(side)?.HorizontalAngleIndex ?? 1) + 3) & 3;
    return adjustedIndex * 90;
  }
  
  private void OnSlotModifid(int slotid)
  {
    if (this.Api is ICoreClientAPI)
      this.clientDialog.Update(RecipeProgress);
    if (slotid != 0)
      return;
    if (this.InputSlot0.Empty&& this.InputSlot1.Empty&& this.InputSlot2.Empty)
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
  
  public bool FindMatchingRecipe()
  {
    ItemSlot[] inputSlots = new ItemSlot[] { inventory[0], inventory[1],inventory[2]};
    CurrentRecipe = null;
    CurrentRecipeName = string.Empty;

    foreach (PressRecipe recipe in CustomRecipeManager.PressRecipes)
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
    if(GetBehavior<BEBehaviorEPress>().PowerSetting<1)
      return;
    if (!FindMatchingRecipe())
      return;
    RecipeProgress = (float)(RecipeProgress + GetBehavior<BEBehaviorEPress>().PowerSetting/CurrentRecipe.EnergyOperation);
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
      InputSlot0.TakeOut(CurrentRecipe.Ingredients[0].Quantity);
      InputSlot1.TakeOut(CurrentRecipe.Ingredients[0].Quantity);
      InputSlot2.TakeOut(CurrentRecipe.Ingredients[0].Quantity);
      RecipeProgress = 0;
    }
  }
  
  protected virtual void UpdateState(float RecipeProgress)
  {                    
    if (Api != null && Api.Side == EnumAppSide.Client && clientDialog != null && clientDialog.IsOpened())
    {
      clientDialog.Update(RecipeProgress);
    }
    if (Api != null && Api.Side == EnumAppSide.Client)
    {
      BlockEntityAnimationUtil animUtil = this.animUtil;
      if (animUtil != null)
      {
        Console.WriteLine("Анимация");
        animUtil.StartAnimation(new AnimationMetaData()
        {
          Animation = "work-on",
          Code = "work-on",
          AnimationSpeed = (float)(GetBehavior<BEBehaviorEPress>().PowerSetting/CurrentRecipe.EnergyOperation)*40,
          EaseOutSpeed = 4f,
          EaseInSpeed = 1f
        });
      }
    }
    MarkDirty(true);
  }
  

  
  public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
  {
    if (this.Api.Side == EnumAppSide.Client)
      this.toggleInventoryDialogClient(byPlayer, (CreateDialogDelegate)(() =>
      {
        this.clientDialog =
          new GuiDialogPress(this.DialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI);
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
  
  public ItemSlot InputSlot0 => this.inventory[0];
  public ItemSlot InputSlot1 => this.inventory[1];
  public ItemSlot InputSlot2 => this.inventory[2];

  public ItemSlot OutputSlot => this.inventory[3];
  
  public override void OnBlockUnloaded()
  {
    base.OnBlockUnloaded();
    this.clientDialog?.TryClose();
  }
}