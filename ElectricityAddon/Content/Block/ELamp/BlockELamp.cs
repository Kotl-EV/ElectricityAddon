using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Content.Block.ELamp
{
    internal class BlockELamp : Vintagestory.API.Common.Block
    {
        private readonly static Dictionary<CacheDataKey, MeshData> MeshDataCache = new();
        private readonly static Dictionary<CacheDataKey, Cuboidf[]> SelectionBoxesCache = new();
        private readonly static Dictionary<CacheDataKey, Cuboidf[]> CollisionBoxesCache = new();

        

        public override void OnLoaded(ICoreAPI coreApi)
        {
            base.OnLoaded(coreApi);
            
        }
        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            BlockELamp.MeshDataCache.Clear();
            BlockELamp.SelectionBoxesCache.Clear();
            BlockELamp.CollisionBoxesCache.Clear();
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            var selection = new Selection(blockSel);
            var facing = FacingHelper.From(selection.Face, selection.Direction);

            if (
                FacingHelper.Faces(facing).First() is { } blockFacing &&
                !world.BlockAccessor
                    .GetBlock(blockSel.Position.AddCopy(blockFacing))
                    .SideSolid[blockFacing.Opposite.Index]
            )
            {
                return false;
            }

            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {

            // если блок сгорел, то не ставим
            if (byItemStack.Block.Variant["state"] == "burned")
            {
                return false;
            }

            var selection = new Selection(blockSel);
            var facing = FacingHelper.From(selection.Face, selection.Direction);

            if (
                base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack) &&
                world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityELamp entity
            )
            {
                entity.Facing = facing;

                //задаем параметры блока/проводника
                var voltage = MyMiniLib.GetAttributeInt(byItemStack!.Block, "voltage", 32);
                var maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack!.Block, "maxCurrent", 5.0F);
                var isolated = MyMiniLib.GetAttributeBool(byItemStack!.Block, "isolated", false);

                entity.Eparams = (
                    new EParams(voltage, maxCurrent, "", 0, 1, 1, false, isolated),
                    FacingHelper.Faces(facing).First().Index);

                return true;
            }

            return false;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            AssetLocation blockCode = CodeWithVariants(new Dictionary<string, string>
        {
            { "tempK", this.Variant["tempK"] },
            { "state", (this.Variant["state"]=="enabled")? "enabled":(this.Variant["state"]=="disabled")? "disabled":"burned" }
        });

            Vintagestory.API.Common.Block block = world.BlockAccessor.GetBlock(blockCode);

            return new ItemStack(block);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
            float dropQuantityMultiplier = 1)
        {
            return new[] { OnPickBlock(world, pos) };
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);

            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityELamp entity)
            {
                var blockFacing = BlockFacing.FromVector(neibpos.X - pos.X, neibpos.Y - pos.Y, neibpos.Z - pos.Z);
                var selectedFacing = FacingHelper.FromFace(blockFacing);

                if ((entity.Facing & ~selectedFacing) == Facing.None)
                {
                    world.BlockAccessor.BreakBlock(pos, null);
                }
            }
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var origin = new Vec3d(0.5, 0.5, 0.5);

            if (
                this.api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityELamp entity &&
                entity.Facing != Facing.None
            )
            {
                var key = CacheDataKey.FromEntity(entity);

                if (!BlockELamp.CollisionBoxesCache.TryGetValue(key, out var boxes))
                {
                    if ((key.Facing & Facing.NorthEast) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 270.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.NorthWest) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 90.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.NorthUp) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 0.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.NorthDown) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 180.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastNorth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastSouth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(180.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastUp) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastDown) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(270.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthEast) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 270.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthWest) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 90.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthUp) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 0.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthDown) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 180.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestNorth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestSouth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(180.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestUp) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(90.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestDown) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(270.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpNorth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 0.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpEast) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 270.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpSouth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 180.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpWest) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 90.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownNorth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 0.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownEast) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 270.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownSouth) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 180.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownWest) != 0)
                    {
                        boxes = this.CollisionBoxes.Select(collisionBox => collisionBox.RotatedCopy(0.0f, 90.0f, 0.0f, origin)).ToArray();
                    }

                    if (boxes != null) BlockELamp.CollisionBoxesCache.Add(key, boxes);
                }

                if (boxes != null)
                {
                    return boxes;
                }
            }

            return Array.Empty<Cuboidf>();
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var origin = new Vec3d(0.5, 0.5, 0.5);

            if (
                this.api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityELamp entity &&
                entity.Facing != Facing.None
            )
            {
                var key = CacheDataKey.FromEntity(entity);

                if (!BlockELamp.SelectionBoxesCache.TryGetValue(key, out var boxes))
                {
                    if ((key.Facing & Facing.NorthEast) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 270.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.NorthWest) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 90.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.NorthUp) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.NorthDown) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 180.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastNorth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastSouth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(180.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastUp) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.EastDown) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 90.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthEast) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 270.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthWest) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 90.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthUp) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.SouthDown) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 180.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestNorth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestSouth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(180.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestUp) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.WestDown) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 270.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpNorth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpEast) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 270.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpSouth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 180.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.UpWest) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 90.0f, 180.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownNorth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownEast) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 270.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownSouth) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 180.0f, 0.0f, origin)).ToArray();
                    }
                    else if ((key.Facing & Facing.DownWest) != 0)
                    {
                        boxes = this.SelectionBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 90.0f, 0.0f, origin)).ToArray();
                    }

                    if (boxes != null) BlockELamp.SelectionBoxesCache.Add(key, boxes);
                }

                if (boxes != null)
                {
                    return boxes;
                }
            }

            return Array.Empty<Cuboidf>();
        }

        public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Vintagestory.API.Common.Block[] chunkExtBlocks, int extIndex3d)
        {
            if (
                this.api is ICoreClientAPI clientApi &&
                this.api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityELamp entity &&
                entity.Facing != Facing.None
            )
            {
                var key = CacheDataKey.FromEntity(entity);

                if (!BlockELamp.MeshDataCache.TryGetValue(key, out var meshData))
                {
                    var origin = new Vec3f(0.5f, 0.5f, 0.5f);


                    clientApi.Tesselator.TesselateBlock(this, out meshData);

                    if ((key.Facing & Facing.NorthEast) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 270.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    if ((key.Facing & Facing.NorthWest) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 90.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    if ((key.Facing & Facing.NorthUp) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    if ((key.Facing & Facing.NorthDown) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    if ((key.Facing & Facing.EastNorth) != 0)
                    {
                        meshData.Rotate(origin, 0.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.EastSouth) != 0)
                    {
                        meshData.Rotate(origin, 180.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.EastUp) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.EastDown) != 0)
                    {
                        meshData.Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.SouthEast) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 270.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.SouthWest) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 90.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.SouthUp) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.SouthDown) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.WestNorth) != 0)
                    {
                        meshData.Rotate(origin, 0.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.WestSouth) != 0)
                    {
                        meshData.Rotate(origin, 180.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.WestUp) != 0)
                    {
                        meshData.Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.WestDown) != 0)
                    {
                        meshData.Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.UpNorth) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 0.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.UpEast) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 270.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.UpSouth) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 180.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.UpWest) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 90.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD);
                    }

                    if ((key.Facing & Facing.DownNorth) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 0.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    if ((key.Facing & Facing.DownEast) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 270.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    if ((key.Facing & Facing.DownSouth) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 180.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    if ((key.Facing & Facing.DownWest) != 0)
                    {
                        meshData.Rotate(origin, 0.0f, 90.0f * GameMath.DEG2RAD, 0.0f);
                    }

                    BlockELamp.MeshDataCache.Add(key, meshData);
                }

                sourceMesh = meshData;
            }
        }


        /// <summary>
        /// Структура ключа для кеширования данных блока.
        /// </summary>
        internal struct CacheDataKey
        {
            public readonly Facing Facing;
            public readonly bool IsEnabled;
            public readonly string code;

            public CacheDataKey(Facing facing, bool isEnabled, string code)
            {
                this.Facing = facing;
                this.IsEnabled = isEnabled;
                this.code = code;
            }

            public static CacheDataKey FromEntity(BlockEntityELamp entity)
            {
                return new CacheDataKey(
                    entity.Facing,
                    entity.IsEnabled,
                    entity.Block.Code
                );
            }
        }
    }
}
