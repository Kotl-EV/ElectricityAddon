using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.EOven;
  public class InventoryEOven : InventoryBase, ISlotProvider
  {
    private ItemSlot[] slots;
    private readonly int cookingSize;
    public BlockPos pos;

    public InventoryEOven(string inventoryID, int bakeableSlots)
      : base(inventoryID, (ICoreAPI) null)
    {
      this.slots = this.GenEmptySlots(bakeableSlots + 1);
      this.cookingSize = bakeableSlots;
      this.CookingSlots = new ItemSlot[bakeableSlots];
      for (int index = 0; index < bakeableSlots; ++index)
        this.CookingSlots[index] = this.slots[index];
    }

    public ItemSlot[] CookingSlots { get; }

    public ItemSlot[] Slots => this.slots;

    public override int Count => this.slots.Length;

    public override ItemSlot this[int slotId]
    {
      get => slotId < 0 || slotId >= this.Count ? (ItemSlot) null : this.slots[slotId];
      set
      {
        if (slotId < 0 || slotId >= this.Count)
          throw new ArgumentOutOfRangeException(nameof (slotId));
        ItemSlot[] slots = this.slots;
        int index = slotId;
        slots[index] = value ?? throw new ArgumentNullException(nameof (value));
      }
    }

    public override void FromTreeAttributes(ITreeAttribute tree)
    {
      List<ItemSlot> modifiedSlots = new List<ItemSlot>();
      this.slots = this.SlotsFromTreeAttributes(tree, this.slots, modifiedSlots);
      for (int index = 0; index < modifiedSlots.Count; ++index)
        this.MarkSlotDirty(this.GetSlotId(modifiedSlots[index]));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
      this.SlotsToTreeAttributes(this.slots, tree);
    }

    protected override ItemSlot NewSlot(int i)
    {
      return (ItemSlot) new ItemSlotSurvival((InventoryBase) this);
    }

    public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
    {
      CombustibleProperties combustibleProps = sourceSlot.Itemstack.Collectible.CombustibleProps;
      return targetSlot == this.slots[this.cookingSize] && (combustibleProps == null || combustibleProps.BurnTemperature <= 0) ? 0.0f : base.GetSuitability(sourceSlot, targetSlot, isMerge);
    }

    public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
      return (ItemSlot) null;
    }
  }
