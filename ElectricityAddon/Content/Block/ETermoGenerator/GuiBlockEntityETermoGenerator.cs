using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.ETermoGenerator;

public class GuiBlockEntityETermoGenerator : GuiDialogBlockEntity
{
    private BlockEntityETermoGenerator betestgen;
    private float _gentemp;

    public GuiBlockEntityETermoGenerator(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi,
        BlockEntityETermoGenerator bentity) : base(dialogTitle, inventory, blockEntityPos, capi)
    {
        if (base.IsDuplicate)
        {
            return;
        }

        capi.World.Player.InventoryManager.OpenInventory(inventory);
        betestgen = bentity;

        this.SetupDialog();
    }

    private void OnSlotModified(int slotid)
    {
        this.capi.Event.EnqueueMainThreadTask(new Action(this.SetupDialog), "termogen");
    }

    public void SetupDialog()
    {
        ElementBounds dialogBounds = ElementBounds.Fixed(250, 60);
        ElementBounds dialog = ElementBounds.Fill.WithFixedPadding(0);
        ElementBounds fuelGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 17, 50, 1, 1);
        ElementBounds stoveBounds = ElementBounds.Fixed(17, 50, 210, 150);
        ElementBounds textBounds = ElementBounds.Fixed(115, 60, 121, 100);
        dialog.BothSizing = ElementSizing.FitToChildren;
        dialog.WithChildren(stoveBounds);

        dialog.BothSizing = ElementSizing.FitToChildren;
        dialog.WithChildren(new ElementBounds[]
        {
            dialogBounds,
            fuelGrid,
            textBounds
        });
        ElementBounds window = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);
        if (capi.Settings.Bool["immersiveMouseMode"])
        {
            window.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-12, 0);
        }
        else
        {
            window.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(20, 0);
        }

        BlockPos blockPos = base.BlockEntityPosition;

        CairoFont outputText = CairoFont.WhiteDetailText().WithWeight(FontWeight.Normal);

        this.SingleComposer = capi.Gui.CreateCompo("termogen" + (blockPos?.ToString()), window)
            .AddShadedDialogBG(dialog, true, 5)
            .AddDialogTitleBar(Lang.Get("termogen"), new Action(OnTitleBarClose), null, null)
            .BeginChildElements(dialog)

            .AddDynamicCustomDraw(stoveBounds, OnBgDraw, "symbolDrawer")

            .AddItemSlotGrid(Inventory, new Action<object>(SendInvPacket), 1, new int[1], fuelGrid, "inputSlot")
            .AddDynamicText("", outputText, textBounds, "outputText")
            .EndChildElements()
            .Compose(true);
    }

    private void SendInvPacket(object packet)
    {
        this.capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z,
            packet);
    }

    private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
    {
        // 1. Fire
        ctx.Save();
        Matrix m = ctx.Matrix;
        m.Translate(GuiElement.scaled(5), GuiElement.scaled(53));
        m.Scale(GuiElement.scaled(0.25), GuiElement.scaled(0.25));
        ctx.Matrix = m;
        capi.Gui.Icons.DrawFlame(ctx);

        double dy = 210 - 210 * ( _gentemp/ 1300);
        ctx.Rectangle(0, dy, 200, 210 - dy);
        ctx.Clip();
        LinearGradient gradient = new LinearGradient(0, GuiElement.scaled(250), 0, 0);
        gradient.AddColorStop(0, new Color(1, 1, 0, 1));
        gradient.AddColorStop(1, new Color(1, 0, 0, 1));
        ctx.SetSource(gradient);
        capi.Gui.Icons.DrawFlame(ctx, 0, false, false);
        gradient.Dispose();
        ctx.Restore();
    }

    public void Update(float gentemp, float burntime)
    {
        if (!this.IsOpened()) return;
        _gentemp = gentemp;
        string newText = $"{gentemp:N1}°C{System.Environment.NewLine}{burntime:N1} {Lang.Get("gui-word-seconds")}{System.Environment.NewLine}{System.Environment.NewLine}";
        if (this.SingleComposer != null)
        {
            base.SingleComposer.GetDynamicText("outputText").SetNewText(newText);
            this.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
        }
    }

    private void OnTitleBarClose()
    {
        this.TryClose();
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        base.Inventory.SlotModified += this.OnSlotModified;
    }

    public override void OnGuiClosed()
    {
        base.Inventory.SlotModified -= this.OnSlotModified;
        base.SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(this.capi);
        base.OnGuiClosed();
    }
}