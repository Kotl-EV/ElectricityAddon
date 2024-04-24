using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.EFreezer;

class GuiEFreezer : GuiDialogBlockEntity
{
    public GuiEFreezer(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(
        dialogTitle, inventory, blockEntityPos, capi)
    {
        if (IsDuplicate) return;

        capi.World.Player.InventoryManager.OpenInventory(Inventory);
        Inventory.SlotModified += OnInventorySlotModified;

        SetupDialog();
    }

    public void OnInventorySlotModified(int slotid)
    {
        //SetupDialog();
        capi.Event.EnqueueMainThreadTask(SetupDialog, "setupfreezerslotdlg");
    }

    void SetupDialog()
    {
        ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
        if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
        {
            capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
        }
        else
        {
            hoveredSlot = null;
        }

        ElementBounds mainBounds = ElementBounds.Fixed(0, 0, 200, 100);
        ElementBounds slotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 10, 30, 2, 3);

        // 2. Around all that is 10 pixel padding
        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(mainBounds);

        // 3. Finally Dialog
        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);
        
        ClearComposers();
        SingleComposer = capi.Gui
                .CreateCompo("beeightslots" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                .AddItemSlotGrid(Inventory, SendInvPacket, 2, new[] { 0, 1, 2, 3, 4, 5 }, slotsBounds)
                .EndChildElements()
                .Compose()
            ;

        if (hoveredSlot != null)
        {
            SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
        }
    }
    
    private void SendInvPacket(object p)
    {
        capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }

    public override bool OnEscapePressed()
    {
        base.OnEscapePressed();
        OnTitleBarClose();
        return TryClose();
    }
}