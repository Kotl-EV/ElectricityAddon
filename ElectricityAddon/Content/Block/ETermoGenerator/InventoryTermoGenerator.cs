using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace ElectricityUnofficial.Content.Block.ETermoGenerator;

public class InventoryTermoGenerator : InventoryBase, ISlotProvider
{
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        private ItemSlot[] _slots;
        public IPlayer genuser;

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == _slots[0] && sourceSlot.Itemstack.Collectible.CombustibleProps != null)
            {
                return 4f;
            }
            return base.GetSuitability(sourceSlot, targetSlot, isMerge);
        }

        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
        {
            return sourceSlot.Itemstack.Collectible.CombustibleProps != null;
        }

        public override bool HasOpened(IPlayer player)
        {
            return (genuser != null && genuser.PlayerUID == player.PlayerUID);
        }

        public override bool RemoveOnClose { get { return true; } }

        public ItemSlot[] Slots
        {
            get { return this._slots; }
        }

        public override int Count
        {
            get { return _slots.Length; }
        }

        public override ItemSlot this[int slotId] 
        { 
            get 
            {
                if (slotId > 0 || slotId < 0) return null;

                return _slots[slotId]; 
            } 
            set 
            {
                if (slotId > 0 || slotId < 0) throw new ArgumentOutOfRangeException("slotId");
                if (value == null) throw new ArgumentNullException("value");
                _slots[slotId] = value; 
            } 
        }

        public InventoryTermoGenerator(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            _slots = base.GenEmptySlots(1);
        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);            
            if (api.Side == EnumAppSide.Server)
            {
                sapi = api as ICoreServerAPI;
            }
            else
            {
                capi = api as ICoreClientAPI;
            }

        }

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return _slots[0];
        }
        protected override ItemSlot NewSlot(int i)
        {
            return new ItemSlotSurvival(this);
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            this._slots = this.SlotsFromTreeAttributes(tree, this._slots, null);           
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(_slots, tree);
            this.ResolveBlocksOrItems();
        }
}

