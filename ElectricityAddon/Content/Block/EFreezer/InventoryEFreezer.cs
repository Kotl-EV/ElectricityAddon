using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ElectricityAddon.Content.Block.EFreezer;

class InventoryEFreezer : InventoryBase, ISlotProvider
{
    ItemSlot[] slots;

    public ItemSlot[] Slots => slots;

    public override int Count => slots.Length;

    public InventoryEFreezer(string inventoryID, ICoreAPI api) : base(inventoryID, api)
    {
        slots = GenEmptySlots(6);
        baseWeight = 4;
    }

    protected override ItemSlot NewSlot(int slotId)
    {
        return new ItemSlot(this);
    }

    public override ItemSlot this[int slotId]
    {
        get
        {
            if (slotId < 0 || slotId >= Count) return null;
            return slots[slotId];
        }
        set
        {
            if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
            if (value == null) throw new ArgumentNullException(nameof(value));
            slots[slotId] = value;
        }
    }


    public override void FromTreeAttributes(ITreeAttribute tree)
    {
        slots = SlotsFromTreeAttributes(tree);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        SlotsToTreeAttributes(slots, tree);
    }
}