using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cairo.Freetype;
using ElectricityAddon.Content.Block.ECable;
using ElectricityAddon.Content.Block.ESwitch;
using ElectricityAddon.Utils;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace ElectricityAddon.Content.Block.ECable
{
    public class BlockECable : Vintagestory.API.Common.Block
    {
        private readonly static ConcurrentDictionary<CacheDataKey, Dictionary<Facing, Cuboidf[]>> CollisionBoxesCache = new();

        public readonly static ConcurrentDictionary<CacheDataKey, Dictionary<Facing, Cuboidf[]>> SelectionBoxesCache = new();

        public readonly static Dictionary<CacheDataKey, MeshData> MeshDataCache = new();

        public static BlockVariant? enabledSwitchVariant;
        public static BlockVariant? disabledSwitchVariant;

        public float res;                       //удельное сопротивление из ассета
        public float maxCurrent;                //максимальный ток из ассета
        public float crosssectional;            //площадь сечения из ассета

        private ICoreAPI api;

        public static readonly Dictionary<int, string> voltages = new Dictionary<int, string>
        {
            { 32, "32v" },
            { 128, "128v" }
        };

        public static readonly Dictionary<string, int> voltagesInvert = new Dictionary<string, int>
        {
            { "32v", 32 },
            { "128v", 128 }
        };

        public static readonly Dictionary<int, string> materials = new Dictionary<int, string>
        {
            { 0, "copper" },
            { 1, "silver" },
            { 2, "lead" }
        };

        public static readonly Dictionary<string, int> materialsInvert = new Dictionary<string, int>
        {
            { "copper", 0 },
            { "silver", 1 },
            { "lead", 2  }
        };

        public static Dictionary<int, string> quantitys = new Dictionary<int, string>
        {
            { 1, "single" },
            { 2, "double" },
            { 3, "triple" },
            { 4, "quadruple"}
        };

        public static Dictionary<int, string> types = new Dictionary<int, string>
        {
            { 0, "dot" },
            { 1, "part" },
            { 2, "block" },
            { 3, "burned" },
            { 4, "fix" },
            { 5, "block_isolated" },
            { 6, "isolated" },
            { 7, "dot_isolated" }
        };


        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            BlockECable.CollisionBoxesCache.Clear();
            BlockECable.SelectionBoxesCache.Clear();
            BlockECable.MeshDataCache.Clear();

        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            this.api = api;


            // предзагрузка ассетов выключателя
            {
                var assetLocation = new AssetLocation("electricityaddon:switch-enabled");
                var block = api.World.BlockAccessor.GetBlock(assetLocation);

                enabledSwitchVariant = new BlockVariant(api, block, "enabled");
                disabledSwitchVariant = new BlockVariant(api, block, "disabled");
            }

        }


        public override bool IsReplacableBy(Vintagestory.API.Common.Block block)
        {
            return base.IsReplacableBy(block) || block is BlockECable || block is BlockESwitch;
        }


        /// <summary>
        /// Ставим кабель
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byPlayer"></param>
        /// <param name="blockSelection"></param>
        /// <param name="byItemStack"></param>
        /// <returns></returns>
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSelection, ItemStack byItemStack)
        {
            var selection = new Selection(blockSelection);
            var facing = FacingHelper.From(selection.Face, selection.Direction);

            var entity = (BlockEntityECable)world.BlockAccessor.GetBlockEntity(blockSelection.Position);

            // обновляем текущий блок с кабелем 
            //кавычка тут специально
            if (entity is BlockEntityECable && entity.AllEparams != null) //это кабель?
            {
                var lines = entity.AllEparams[FacingHelper.Faces(facing).First().Index].lines; //сколько линий на грани уже?


                if ((entity.Connection & facing) != 0)  //мы навелись уже на существующий кабель?
                {

                    var faceCoonections = entity.Connection & FacingHelper.FromFace(FacingHelper.Faces(facing).First()); //какие соединения уже есть на грани?

                    //какой блок сейчас здесь находится
                    var indexV = entity.AllEparams[FacingHelper.Faces(facing).First().Index].voltage;          //индекс напряжения этой грани
                    var indexM = entity.AllEparams[FacingHelper.Faces(facing).First().Index].indexM;          //индекс материала этой грани
                    var burn = entity.AllEparams[FacingHelper.Faces(facing).First().Index].burnout;            //сгорело?
                    var isol = entity.AllEparams[FacingHelper.Faces(facing).First().Index].isolated;            //изолировано ?

                    var block = new GetCableAsset().CableAsset(api, entity.Block, indexV, indexM, 1, isol ? 6 : 1); //берем ассет блока кабеля

                    //проверяем сколько у игрока проводов в руке и совпадают ли они с теми что есть
                    if (byItemStack != null && byItemStack.Block.Code.ToString().Contains(block.Code)
                        && !burn
                        && (byItemStack.StackSize >= FacingHelper.Count(faceCoonections) | byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative))
                    {
                        //для 32V 1-4 линии, для 128V 2 линии
                        if (lines >= 1.0F && ((entity.AllEparams[FacingHelper.Faces(facing).First().Index].voltage == 32 & lines < 4.0F) | (entity.AllEparams[FacingHelper.Faces(facing).First().Index].voltage == 128 & lines < 2.0F)))                                          //линий 1-3 имеется
                        {
                            lines++;                                                                //приращиваем линии
                            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)        //чтобы в креативе не уменьшало стак
                            {
                                byItemStack!.StackSize -= FacingHelper.Count(faceCoonections) - 1;   //отнимаем у игрока столько же, сколько установили
                            }

                            entity.AllEparams[FacingHelper.Faces(facing).First().Index].lines = lines; //применяем линии
                            entity.MarkDirty(true);
                            return true;
                        }
                        else
                        {
                            //уведомление на экране
                            if (this.api is ICoreClientAPI apii)
                            {
                                apii.TriggerIngameError((object)this, "cable", "Линий уже достаточно.");
                            }

                            return false;
                        }
                    }
                    else
                    {
                        //уведомление на экране
                        if (this.api is ICoreClientAPI apii)
                        {
                            if (!byItemStack!.Block.Code.ToString().Contains(block.Code))
                            {
                                apii.TriggerIngameError((object)this, "cable", "Кабеля должны быть того же типа.");
                            }
                            else if (byItemStack.StackSize < FacingHelper.Count(faceCoonections))
                            {
                                apii.TriggerIngameError((object)this, "cable", "Недостаточно кабелей для размещения.");
                            }
                            else if (burn == true)
                            {
                                apii.TriggerIngameError((object)this, "cable", "Уберите сгоревший кабель сначала.");
                            }
                        }

                        return false;
                    }


                }
                else
                {
                    //проверка на сплошную соседнюю грань
                    if (lines == 0)
                    {
                        var indexFacing = FacingHelper.Faces(facing).First().Index; //индекс грани под курсором
                        var pos = blockSelection.Position.Copy();
                        if (indexFacing == 0)
                        {
                            pos.Z -= 1;

                            if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                            {
                                if (!b.SideIsSolid(pos, 2))
                                    return false;
                            }
                        }
                        else if (indexFacing == 1)
                        {
                            pos.X += 1;

                            if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                            {
                                if (!b.SideIsSolid(pos, 3))
                                    return false;
                            }
                        }
                        else if (indexFacing == 2)
                        {
                            pos.Z += 1;

                            if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                            {
                                if (!b.SideIsSolid(pos, 0))
                                    return false;
                            }
                        }
                        else if (indexFacing == 3)
                        {
                            pos.X -= 1;

                            if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                            {
                                if (!b.SideIsSolid(pos, 1))
                                    return false;
                            }
                        }
                        else if (indexFacing == 4)
                        {
                            pos.Y += 1;

                            if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                            {
                                if (!b.SideIsSolid(pos, 5))
                                    return false;
                            }
                        }
                        else if (indexFacing == 5)
                        {
                            pos.Y -= 1;

                            if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                            {
                                if (!b.SideIsSolid(pos, 1))
                                    return false;
                            }
                        }
                    }



                    int indexM = materialsInvert[byItemStack.Block.Variant["material"]];  //определяем индекс материала
                    int indexV = voltagesInvert[byItemStack.Block.Variant["voltage"]];    //определяем индекс напряжения                        
                    bool iso = byItemStack.Block.Code.ToString().Contains("isolated")     //определяем изоляцию
                        ? true
                        : false;

                    //подгружаем некоторые параметры из ассета
                    res = MyMiniLib.GetAttributeFloat(byItemStack.Block, "res", 1);
                    maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack.Block, "maxCurrent", 1);
                    crosssectional = MyMiniLib.GetAttributeFloat(byItemStack.Block, "crosssectional", 1);



                    //линий 0? Значит грань была пустая    
                    if (lines == 0)
                    {
                        entity.Eparams = (
                            new EParams(indexV, maxCurrent, indexM, res, 1, crosssectional, false, iso),
                            FacingHelper.Faces(facing).First().Index);

                        entity.AllEparams[FacingHelper.Faces(facing).First().Index] = entity.Eparams.Item1;

                    }
                    else   //линий не 0, значитуже что-то там есть на грани
                    {

                        //какой блок сейчас здесь находится
                        var indexV2 = entity.AllEparams[FacingHelper.Faces(facing).First().Index].voltage;          //индекс напряжения этой грани
                        var indexM2 = entity.AllEparams[FacingHelper.Faces(facing).First().Index].indexM;          //индекс материала этой грани
                        var burn = entity.AllEparams[FacingHelper.Faces(facing).First().Index].burnout;            //сгорело?
                        var iso2 = entity.AllEparams[FacingHelper.Faces(facing).First().Index].isolated;            //изолировано ?

                        var block = new GetCableAsset().CableAsset(api, entity.Block, indexV2, indexM2, 1, iso2 ? 6 : 1); //берем ассет блока кабеля


                        //проверяем сколько у игрока проводов в руке и совпадают ли они с теми что есть
                        if (byItemStack != null && byItemStack.Block.Code.ToString().Contains(block.Code)
                            && !burn
                            && (byItemStack.StackSize >= lines | byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative))
                        {
                            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) //чтобы в креативе не уменьшало стак
                            {
                                byItemStack.StackSize -= lines - 1;          //отнимаем у игрока столько же, сколько установили
                            }

                            entity.Eparams = (
                                new EParams(indexV, maxCurrent, indexM, res, lines, crosssectional, false, iso),
                                FacingHelper.Faces(facing).First().Index);

                            entity.AllEparams[FacingHelper.Faces(facing).First().Index] = entity.Eparams.Item1;

                        }
                        else
                        {
                            //уведомление на экране
                            if (this.api is ICoreClientAPI apii)
                            {
                                if (!byItemStack!.Block.Code.ToString().Contains(block.Code))
                                {
                                    apii.TriggerIngameError(this, "cable", "Кабеля должны быть того же типа.");
                                }
                                else if (byItemStack.StackSize < lines)
                                {
                                    apii.TriggerIngameError(this, "cable", "Недостаточно кабелей для размещения.");
                                }
                                else if (burn)
                                {
                                    apii.TriggerIngameError(this, "cable", "Уберите сгоревший кабель сначала.");
                                }
                            }

                            return false;
                        }
                    }

                    entity.Connection |= facing;
                    entity.MarkDirty(true);
                }
                return true;
            }



            {

                //а грань под курсором сплошная?
                var indexFacing = FacingHelper.Faces(facing).First().Index; //индекс грани под курсором
                var pos = blockSelection.Position.Copy();
                if (indexFacing == 0)
                {
                    pos.Z -= 1;

                    if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                    {
                        if (!b.SideIsSolid(pos, 2))
                            return false;
                    }
                }
                else if (indexFacing == 1)
                {
                    pos.X += 1;

                    if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                    {
                        if (!b.SideIsSolid(pos, 3))
                            return false;
                    }
                }
                else if (indexFacing == 2)
                {
                    pos.Z += 1;

                    if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                    {
                        if (!b.SideIsSolid(pos, 0))
                            return false;
                    }
                }
                else if (indexFacing == 3)
                {
                    pos.X -= 1;

                    if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                    {
                        if (!b.SideIsSolid(pos, 1))
                            return false;
                    }
                }
                else if (indexFacing == 4)
                {
                    pos.Y += 1;

                    if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                    {
                        if (!b.SideIsSolid(pos, 5))
                            return false;
                    }
                }
                else if (indexFacing == 5)
                {
                    pos.Y -= 1;

                    if (world.BlockAccessor.GetBlock(pos) is Vintagestory.API.Common.Block b)
                    {
                        if (!b.SideIsSolid(pos, 4))
                            return false;
                    }
                }


            }


            // если установка все же успешна
            if (base.DoPlaceBlock(world, byPlayer, blockSelection, byItemStack))
            {

                entity = (BlockEntityECable)world.BlockAccessor.GetBlockEntity(blockSelection.Position);

                // обновляем текущий блок с кабелем 
                //кавычка тут специально
                if (entity is BlockEntityECable) //это кабель?
                {
                    int indexM = materialsInvert[byItemStack.Block.Variant["material"]];  //определяем индекс материала
                    int indexV = voltagesInvert[byItemStack.Block.Variant["voltage"]];    //определяем индекс напряжения
                    bool iso = byItemStack.Block.Code.ToString().Contains("isolated")     //определяем изоляцию
                        ? true
                        : false;

                    //подгружаем некоторые параметры из ассета
                    res = MyMiniLib.GetAttributeFloat(byItemStack.Block, "res", 1);
                    maxCurrent = MyMiniLib.GetAttributeFloat(byItemStack.Block, "maxCurrent", 1);
                    crosssectional = MyMiniLib.GetAttributeFloat(byItemStack.Block, "crosssectional", 1);


                    entity.Connection = facing;       //сообщаем направление
                    entity.Eparams = (
                        new EParams(indexV, maxCurrent, indexM, res, 1, crosssectional, false, iso),
                        FacingHelper.Faces(facing).First().Index);

                    entity.AllEparams[FacingHelper.Faces(facing).First().Index] = entity.Eparams.Item1;
                    //markdirty тут строго не нужен!

                }

                return true;
            }


            return false;
        }



        public override void OnBlockBroken(IWorldAccessor world, BlockPos position, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (this.api is ICoreClientAPI)
            {
                return;
            }


            if (world.BlockAccessor.GetBlockEntity(position) is BlockEntityECable entity)
            {
                if (byPlayer is { CurrentBlockSelection: { } blockSelection })
                {
                    var key = CacheDataKey.FromEntity(entity);
                    var hitPosition = blockSelection.HitPosition;

                    var sf = new SelectionFacingCable();
                    var selectedFacing = sf.SelectionFacing(key, hitPosition, entity);  //выделяем направление для слома под курсором


                    //определяем какой выключатель ломать
                    Facing faceSelect = Facing.None;
                    Facing selectedSwitches;
                    if (selectedFacing != Facing.None)
                    {
                        faceSelect = FacingHelper.FromFace(FacingHelper.Faces(selectedFacing).First());
                        selectedSwitches = entity.Switches & faceSelect;
                    }
                    else
                    {
                        selectedSwitches = Facing.None;
                    }


                    //тут ломаем переключатель
                    if (selectedSwitches != Facing.None)
                    {
                        var stackSize = FacingHelper.Faces(selectedSwitches).Count();

                        if (stackSize > 0)
                        {
                            entity.Switches &= ~faceSelect;
                            entity.MarkDirty(true);


                            var assetLocation = new AssetLocation("electricityaddon:switch-enabled");
                            var block = world.BlockAccessor.GetBlock(assetLocation);
                            var itemStack = new ItemStack(block, stackSize);
                            world.SpawnItemEntity(itemStack, position.ToVec3d());


                            return;
                        }
                    }

                    //здесь уже ломаем кабеля
                    var connection = entity.Connection & ~selectedFacing;      //отнимает выбранные соединения

                    if (connection != Facing.None)
                    {
                        var stackSize = FacingHelper.Count(selectedFacing);    //соединений выделено

                        if (stackSize > 0)
                        {
                            entity.Connection = connection;
                            entity.MarkDirty(true);


                            foreach (var face in FacingHelper.Faces(selectedFacing))         //перебираем все грани выделенных кабелей
                            {
                                var indexV = entity.AllEparams[face.Index].voltage;          //индекс напряжения этой грани
                                var indexM = entity.AllEparams[face.Index].indexM;          //индекс материала этой грани
                                var indexQ = entity.AllEparams[face.Index].lines;          //индекс линий этой грани
                                var isol = entity.AllEparams[face.Index].isolated;          //изолировано ли?
                                var burn = entity.AllEparams[face.Index].burnout;          //сгорело ли?


                                connection = selectedFacing & FacingHelper.FromFace(face);                   //берем направления только в этой грани

                                if ((entity.Connection & FacingHelper.FromFace(face)) == 0) //если грань осталась пустая
                                    entity.AllEparams[face.Index] = default;

                                stackSize = FacingHelper.Count(connection) * indexQ;          //сколько на этой грани проводов выронить

                                ItemStack itemStack = null!;
                                string material = materials[indexM]; //берем материал кабеля
                                if (burn)       //если сгорело, то бросаем кусочки металла
                                {
                                    AssetLocation assetLoc = new AssetLocation("metalbit-" + material);
                                    var item = api.World.GetItem(assetLoc);
                                    itemStack = new ItemStack(item, stackSize);
                                }
                                else
                                {
                                    var block = new GetCableAsset().CableAsset(api, entity.Block, indexV, indexM, 1, isol ? 6 : 1); //берем ассет блока кабеля
                                    itemStack = new ItemStack(block, stackSize);
                                }

                                world.SpawnItemEntity(itemStack, position.ToVec3d());


                            }

                            return;
                        }
                    }

                }
            }

            base.OnBlockBroken(world, position, byPlayer, dropQuantityMultiplier);
        }


        /// <summary>
        /// Роняем все соединения этого блока?
        /// </summary>
        /// <param name="world"></param>
        /// <param name="position"></param>
        /// <param name="byPlayer"></param>
        /// <param name="dropQuantityMultiplier"></param>
        /// <returns></returns>
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos position, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.BlockAccessor.GetBlockEntity(position) is BlockEntityECable entity)
            {
                ItemStack[] itemStacks = new ItemStack[] { };

                var connection = entity.Connection;

                foreach (var face in FacingHelper.Faces(entity.Connection))         //перебираем все грани выделенных кабелей
                {
                    var indexV = entity.AllEparams[face.Index].voltage;          //индекс напряжения этой грани
                    var indexM = entity.AllEparams[face.Index].indexM;          //индекс материала этой грани
                    var indexQ = entity.AllEparams[face.Index].lines;          //индекс линий этой грани
                    var isol = entity.AllEparams[face.Index].isolated;          //изолировано ли?
                    var burn = entity.AllEparams[face.Index].burnout;          //сгорело ли?


                    connection = entity.Connection & FacingHelper.FromFace(face);                   //берем направления только в этой грани

                    if ((entity.Connection & FacingHelper.FromFace(face)) == 0) //если грань осталась пустая
                        entity.AllEparams[face.Index] = default;

                    var stackSize = FacingHelper.Count(connection) * indexQ;          //сколько на этой грани проводов выронить

                    ItemStack itemStack = null!;
                    string material = materials[indexM]; //берем материал кабеля
                    if (burn)       //если сгорело, то бросаем кусочки металла
                    {
                        AssetLocation assetLoc = new AssetLocation("metalbit-" + material);
                        var item = api.World.GetItem(assetLoc);
                        itemStack = new ItemStack(item, stackSize);
                    }
                    else
                    {
                        var block = new GetCableAsset().CableAsset(api, entity.Block, indexV, indexM, 1, isol ? 6 : 1); //берем ассет блока кабеля
                        itemStack = new ItemStack(block, stackSize);
                    }

                    itemStacks = itemStacks.AddToArray<ItemStack>(itemStack);
                }


                return itemStacks;
            }

            return base.GetDrops(world, position, byPlayer, dropQuantityMultiplier);
        }



        /// <summary>
        /// Обновился соседний блок
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        /// <param name="neibpos"></param>
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);

            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityECable entity)
            {
                var blockFacing = BlockFacing.FromVector(neibpos.X - pos.X, neibpos.Y - pos.Y, neibpos.Z - pos.Z);
                var selectedFacing = FacingHelper.FromFace(blockFacing);

                bool delayreturn = false;
                if ((entity.Connection & ~selectedFacing) == Facing.None)
                {
                    world.BlockAccessor.BreakBlock(pos, null);

                    delayreturn = true;
                    //return;
                }


                //ломаем выключатели
                var selectedSwitches = entity.Switches & selectedFacing;

                if (selectedSwitches != Facing.None)
                {
                    var stackSize = FacingHelper.Faces(selectedSwitches).Count();

                    if (stackSize > 0)
                    {
                        var assetLocation = new AssetLocation("electricityaddon:switch-enabled");
                        var block = world.BlockAccessor.GetBlock(assetLocation);
                        var itemStack = new ItemStack(block, stackSize);
                        world.SpawnItemEntity(itemStack, pos.ToVec3d());
                    }

                    entity.Switches &= ~selectedFacing;

                }

                if (delayreturn)
                    return;

                //ломаем провода
                var selectedConnection = entity.Connection & selectedFacing;

                if (selectedConnection != Facing.None)
                {
                    var stackSize = FacingHelper.Count(selectedConnection);    //соединений выделено

                    if (stackSize > 0)
                    {
                        entity.Connection &= ~selectedConnection;

                        foreach (var face in FacingHelper.Faces(selectedConnection))         //перебираем все грани выделенных кабелей
                        {
                            var indexV = entity.AllEparams[face.Index].voltage;          //индекс напряжения этой грани
                            var indexM = entity.AllEparams[face.Index].indexM;          //индекс материала этой грани
                            var indexQ = entity.AllEparams[face.Index].lines;          //индекс линий этой грани
                            var isol = entity.AllEparams[face.Index].isolated;          //изолировано ли?
                            var burn = entity.AllEparams[face.Index].burnout;          //сгорело ли?



                            var connection = selectedConnection & FacingHelper.FromFace(face);                   //берем направления только в этой грани

                            if ((entity.Connection & FacingHelper.FromFace(face)) == 0) //если грань осталась пустая
                                entity.AllEparams[face.Index] = default;

                            stackSize = FacingHelper.Count(connection) * indexQ;          //сколько на этой грани проводов выронить

                            ItemStack itemStack = null!;
                            string material = materials[indexM]; //берем материал кабеля
                            if (burn)       //если сгорело, то бросаем кусочки металла
                            {
                                AssetLocation assetLoc = new AssetLocation("metalbit-" + material);
                                var item = api.World.GetItem(assetLoc);
                                itemStack = new ItemStack(item, stackSize);
                            }
                            else
                            {
                                var block = new GetCableAsset().CableAsset(api, entity.Block, indexV, indexM, 1, isol ? 6 : 1); //берем ассет блока кабеля
                                itemStack = new ItemStack(block, stackSize);
                            }

                            world.SpawnItemEntity(itemStack, pos.ToVec3d());


                        }



                    }

                }
            }
        }

        /// <summary>
        /// взаимодействие с кабелем/переключателем
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byPlayer"></param>
        /// <param name="blockSel"></param>
        /// <returns></returns>
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (this.api is ICoreClientAPI)
            {
                return true;
            }


            //это кабель?
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityECable entity)
            {
                var key = CacheDataKey.FromEntity(entity);
                var hitPosition = blockSel.HitPosition;

                var sf = new SelectionFacingCable();
                var selectedFacing = sf.SelectionFacing(key, hitPosition, entity);  //выделяем грань выключателя



                var selectedSwitches = selectedFacing & entity.Switches;

                if (selectedSwitches != 0)
                {
                    entity.SwitchesState ^= selectedSwitches;
                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        /// <summary>
        /// Переопределение системной функции выделений
        /// </summary>
        /// <param name="blockAccessor"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos position)
        {

            if (blockAccessor.GetBlockEntity(position) is BlockEntityECable entity && entity.AllEparams != null)
            {
                var key = CacheDataKey.FromEntity(entity);

                return CalculateBoxes(
                        key,
                        BlockECable.SelectionBoxesCache,
                        entity
                    ).Values
                    .SelectMany(x => x)
                    .Distinct()
                    .ToArray();
            }

            return base.GetSelectionBoxes(blockAccessor, position);
        }


        /// <summary>
        /// Переопределение системной функции коллизий
        /// </summary>
        /// <param name="blockAccessor"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos position)
        {

            if (blockAccessor.GetBlockEntity(position) is BlockEntityECable entity && entity.AllEparams != null)
            {
                var key = CacheDataKey.FromEntity(entity);

                return CalculateBoxes(
                        key,
                        BlockECable.CollisionBoxesCache,
                        entity
                    ).Values
                    .SelectMany(x => x)
                    .Distinct()
                    .ToArray();
            }


            return base.GetSelectionBoxes(blockAccessor, position);
        }



        /// <summary>
        /// Помогает рандомизировать шейпы
        /// </summary>
        /// <param name="rand"></param>
        /// <returns></returns>
        private float RndHelp(ref Random rand)
        {
            return (float)((rand.NextDouble() * 0.01F) - 0.005F + 1.0F);
        }



        /// <summary>
        /// Отрисовщик шейпов
        /// </summary>
        /// <param name="sourceMesh"></param>
        /// <param name="lightRgbsByCorner"></param>
        /// <param name="position"></param>
        /// <param name="chunkExtBlocks"></param>
        /// <param name="extIndex3d"></param>
        public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos position, Vintagestory.API.Common.Block[] chunkExtBlocks, int extIndex3d)
        {
            if (this.api.World.BlockAccessor.GetBlockEntity(position) is BlockEntityECable entity && entity.Connection != Facing.None && entity.AllEparams != null)
            {
                var key = CacheDataKey.FromEntity(entity);

                if (!BlockECable.MeshDataCache.TryGetValue(key, out var meshData))
                {
                    var origin = new Vec3f(0.5f, 0.5f, 0.5f);
                    var origin0 = new Vec3f(0f, 0f, 0f);

                    Random rnd = new Random(); //инициализируем рандомайзер системный


                    // рисуем на северной грани
                    if ((key.Connection & Facing.NorthAll) != 0)
                    {
                        var indexV = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].voltage; //индекс напряжения этой грани
                        var indexM = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].indexM; //индекс материала этой грани
                        var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].lines; //индекс линий этой грани
                        var indexB = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].burnout;//индекс перегорания этой грани
                        var isol = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].isolated; //изолировано ли?

                        var dotVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 7 : 0);   //получаем шейп нужной точки кабеля

                        BlockVariants partVariant;
                        if (!indexB)
                        {
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1);  //получаем шейп нужного кабеля изолированного или целого
                        }
                        else
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 3);  //получаем шейп нужного кабеля сгоревшего

                        var fixVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 4);   //получаем шейп крепления кабеля

                        //ставим точку посередине, если провода не перегорел
                        if (!indexB)
                            AddMeshData(ref meshData, fixVariant.MeshData?.Clone().Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 0.0f));


                        if ((key.Connection & Facing.NorthEast) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 90.0f * GameMath.DEG2RAD, 270.0f * GameMath.DEG2RAD, 0.0f)); //ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(0.5F, 0, 0));   //cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.NorthWest) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 90.0f * GameMath.DEG2RAD, 90.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(-0.5F, 0, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.NorthUp) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(0, 0.5F, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.NorthDown) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 90.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(0, -0.5F, 0));//cтавим крепление на ребре
                        }

                    }

                    // рисуем на восточной грани
                    if ((key.Connection & Facing.EastAll) != 0)
                    {

                        var indexV = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].voltage; //индекс напряжения этой грани
                        var indexM = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].indexM; //индекс материала этой грани
                        var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].lines; //индекс линий этой грани
                        var indexB = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].burnout; //индекс перегорания этой грани
                        var isol = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].isolated; //изолировано ли?

                        var dotVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 7 : 0);   //получаем шейп нужной точки кабеля

                        BlockVariants partVariant;
                        if (!indexB)
                        {
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1);  //получаем шейп нужного кабеля изолированного или целого
                        }
                        else
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 3);  //получаем шейп нужного кабеля сгоревшего

                        var fixVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 4);   //получаем шейп крепления кабеля

                        //ставим точку посередине, если провода не перегорел
                        if (!indexB)
                            AddMeshData(ref meshData, fixVariant.MeshData?.Clone().Rotate(origin, 0.0f, 0.0f, 90.0f * GameMath.DEG2RAD));

                        if ((key.Connection & Facing.EastNorth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 90.0f * GameMath.DEG2RAD).Translate(0, 0, -0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.EastSouth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 180.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 90.0f * GameMath.DEG2RAD).Translate(0, 0, 0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.EastUp) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 90.0f * GameMath.DEG2RAD).Translate(0, 0.5F, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.EastDown) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 90.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 90.0f * GameMath.DEG2RAD).Translate(0, -0.5F, 0));//cтавим крепление на ребре
                        }
                    }

                    // рисуем на южной грани
                    if ((key.Connection & Facing.SouthAll) != 0)
                    {
                        var indexV = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].voltage; //индекс напряжения этой грани
                        var indexM = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].indexM; //индекс материала этой грани
                        var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].lines; //индекс линий этой грани
                        var indexB = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].burnout; //индекс перегорания этой грани
                        var isol = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].isolated; //изолировано ли?

                        var dotVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 7 : 0);   //получаем шейп нужной точки кабеля

                        BlockVariants partVariant;
                        if (!indexB)
                        {
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1);  //получаем шейп нужного кабеля изолированного или целого
                        }
                        else
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 3);  //получаем шейп нужного кабеля сгоревшего

                        var fixVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 4);   //получаем шейп крепления кабеля

                        //ставим точку посередине, если провода не перегорел
                        if (!indexB)
                            AddMeshData(ref meshData, fixVariant.MeshData?.Clone().Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 0.0f));

                        if ((key.Connection & Facing.SouthEast) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 270.0f * GameMath.DEG2RAD, 270.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(0.5F, 0, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.SouthWest) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 270.0f * GameMath.DEG2RAD, 90.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(-0.5F, 0, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.SouthUp) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 270.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(0, 0.5F, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.SouthDown) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 0.0f).Translate(0, -0.5F, 0));//cтавим крепление на ребре
                        }
                    }

                    // рисуем на западной грани
                    if ((key.Connection & Facing.WestAll) != 0)
                    {
                        var indexV = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].voltage; //индекс напряжения этой грани
                        var indexM = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].indexM; //индекс материала этой грани
                        var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].lines; //индекс линий этой грани
                        var indexB = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].burnout; //индекс перегорания этой грани
                        var isol = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].isolated; //изолировано ли?

                        var dotVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 7 : 0);   //получаем шейп нужной точки кабеля

                        BlockVariants partVariant;
                        if (!indexB)
                        {
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1);  //получаем шейп нужного кабеля изолированного или целого
                        }
                        else
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 3);  //получаем шейп нужного кабеля сгоревшего

                        var fixVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 4);   //получаем шейп крепления кабеля

                        //ставим точку посередине, если провода не перегорел
                        if (!indexB)
                            AddMeshData(ref meshData, fixVariant.MeshData?.Clone().Rotate(origin, 0.0f, 0.0f, 270.0f * GameMath.DEG2RAD));

                        if ((key.Connection & Facing.WestNorth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 270.0f * GameMath.DEG2RAD).Translate(0, 0, -0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.WestSouth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 180.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 270.0f * GameMath.DEG2RAD).Translate(0, 0, 0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.WestUp) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 90.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 270.0f * GameMath.DEG2RAD).Translate(0, 0.5F, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.WestDown) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 270.0f * GameMath.DEG2RAD, 0.0f, 270.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 270.0f * GameMath.DEG2RAD).Translate(0, -0.5F, 0));//cтавим крепление на ребре
                        }
                    }

                    // рисуем на верхней грани
                    if ((key.Connection & Facing.UpAll) != 0)
                    {
                        var indexV = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].voltage; //индекс напряжения этой грани
                        var indexM = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].indexM; //индекс материала этой грани
                        var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].lines; //индекс линий этой грани
                        var indexB = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].burnout; //индекс перегорания этой грани
                        var isol = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].isolated; //изолировано ли?

                        var dotVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 7 : 0);   //получаем шейп нужной точки кабеля

                        BlockVariants partVariant;
                        if (!indexB)
                        {
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1);  //получаем шейп нужного кабеля изолированного или целого
                        }
                        else
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 3);  //получаем шейп нужного кабеля сгоревшего

                        var fixVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 4);   //получаем шейп крепления кабеля

                        //ставим точку посередине, если провода не перегорел
                        if (!indexB)
                            AddMeshData(ref meshData, fixVariant.MeshData?.Clone().Rotate(origin, 0.0f, 0.0f, 180.0f * GameMath.DEG2RAD));

                        if ((key.Connection & Facing.UpNorth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 0.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 180.0f * GameMath.DEG2RAD).Translate(0, 0, -0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.UpSouth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 180.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 180.0f * GameMath.DEG2RAD).Translate(0, 0, 0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.UpEast) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 270.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 180.0f * GameMath.DEG2RAD).Translate(0.5F, 0, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.UpWest) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 90.0f * GameMath.DEG2RAD, 180.0f * GameMath.DEG2RAD));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 180.0f * GameMath.DEG2RAD).Translate(-0.5F, 0, 0));//cтавим крепление на ребре
                        }
                    }

                    // рисуем на нижней грани
                    if ((key.Connection & Facing.DownAll) != 0)
                    {
                        var indexV = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].voltage; //индекс напряжения этой грани
                        var indexM = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].indexM; //индекс материала этой грани
                        var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].lines; //индекс линий этой грани
                        var indexB = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].burnout; //индекс перегорания этой грани
                        var isol = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].isolated; //изолировано ли?

                        var dotVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 7 : 0);   //получаем шейп нужной точки кабеля

                        BlockVariants partVariant;
                        if (!indexB)
                        {
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1);  //получаем шейп нужного кабеля изолированного или целого
                        }
                        else
                            partVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 3);  //получаем шейп нужного кабеля сгоревшего

                        var fixVariant = new BlockVariants(api, entity.Block, indexV, indexM, indexQ, 4);   //получаем шейп крепления кабеля

                        //ставим точку посередине, если провода не перегорел
                        if (!indexB)
                            AddMeshData(ref meshData, fixVariant.MeshData?.Clone().Rotate(origin, 0.0f, 0.0f, 0.0f));

                        if ((key.Connection & Facing.DownNorth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 0.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 0.0f).Translate(0, 0, -0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.DownSouth) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 180.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 0.0f).Translate(0, 0, 0.5F));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.DownEast) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 270.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 0.0f).Translate(0.5F, 0, 0));//cтавим крепление на ребре
                        }

                        if ((key.Connection & Facing.DownWest) != 0)
                        {
                            AddMeshData(ref meshData, partVariant.MeshData?.Clone().Rotate(origin, 0.0f, 90.0f * GameMath.DEG2RAD, 0.0f));//ставим кусок
                            AddMeshData(ref meshData, dotVariant.MeshData?.Clone().Scale(origin0, RndHelp(ref rnd), RndHelp(ref rnd), RndHelp(ref rnd)).Rotate(origin, 0.0f, 0.0f, 0.0f).Translate(-0.5F, 0, 0));//cтавим крепление на ребре
                        }
                    }

                    // Переключатели
                    if ((key.Switches & Facing.NorthEast) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                90.0f * GameMath.DEG2RAD,
                                0.0f
                            )
                        );
                    }

                    if ((key.Switches & Facing.NorthWest) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                270.0f * GameMath.DEG2RAD,
                                0.0f
                            )
                        );
                    }

                    if ((key.Switches & Facing.NorthUp) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD,
                                0.0f
                            )
                        );
                    }

                    if ((key.Switches & Facing.NorthDown) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                0.0f * GameMath.DEG2RAD,
                                0.0f
                            )
                        );
                    }

                    if ((key.Switches & Facing.EastNorth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                180.0f * GameMath.DEG2RAD,
                                0.0f,
                                90.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.EastSouth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                0.0f * GameMath.DEG2RAD,
                                0.0f,
                                90.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.EastUp) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                270.0f * GameMath.DEG2RAD,
                                0.0f,
                                90.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.EastDown) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                0.0f,
                                90.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.SouthEast) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                90.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.SouthWest) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                270.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.SouthUp) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.SouthDown) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                0.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.WestNorth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                180.0f * GameMath.DEG2RAD,
                                0.0f,
                                270.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.WestSouth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                0.0f * GameMath.DEG2RAD,
                                0.0f,
                                270.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.WestUp) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                270.0f * GameMath.DEG2RAD,
                                0.0f,
                                270.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.WestDown) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                90.0f * GameMath.DEG2RAD,
                                0.0f,
                                270.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.UpNorth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                0.0f,
                                180.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.UpEast) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                0.0f,
                                90.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.UpSouth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                0.0f,
                                0.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.UpWest) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone().Rotate(
                                origin,
                                0.0f,
                                270.0f * GameMath.DEG2RAD,
                                180.0f * GameMath.DEG2RAD
                            )
                        );
                    }

                    if ((key.Switches & Facing.DownNorth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone()
                            .Rotate(origin, 0.0f, 180.0f * GameMath.DEG2RAD, 0.0f)
                        );
                    }

                    if ((key.Switches & Facing.DownEast) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone()
                            .Rotate(origin, 0.0f, 90.0f * GameMath.DEG2RAD, 0.0f)
                        );
                    }

                    if ((key.Switches & Facing.DownSouth) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone()
                            .Rotate(origin, 0.0f, 0.0f * GameMath.DEG2RAD, 0.0f)
                        );
                    }

                    if ((key.Switches & Facing.DownWest) != 0)
                    {
                        AddMeshData(
                            ref meshData,
                            ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                                ? enabledSwitchVariant
                                : disabledSwitchVariant)?.MeshData?.Clone()
                            .Rotate(origin, 0.0f, 270.0f * GameMath.DEG2RAD, 0.0f)
                        );
                    }

                    BlockECable.MeshDataCache[key] = meshData!;
                }

                sourceMesh = meshData ?? sourceMesh;
            }

            base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, position, chunkExtBlocks, extIndex3d);
        }

        /// <summary>
        /// Просчет коллайдеров (колллизии проводов должны совпадать с коллизиями выделения)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="boxesCache"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Dictionary<Facing, Cuboidf[]> CalculateBoxes(CacheDataKey key, IDictionary<CacheDataKey, Dictionary<Facing, Cuboidf[]>> boxesCache, BlockEntityECable entity)
        {
            if (!boxesCache.TryGetValue(key, out var boxes))
            {
                var origin = new Vec3d(0.5, 0.5, 0.5);

                boxesCache[key] = boxes = new Dictionary<Facing, Cuboidf[]>();

                // Connections
                if ((key.Connection & Facing.NorthAll) != 0)
                {
                    var indexV = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].voltage; //индекс напряжения этой грани
                    var indexM = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].indexM; //индекс материала этой грани
                    var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].lines; //индекс линий этой грани
                    var indexB = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].burnout;//индекс перегорания этой грани
                    var isol = entity.AllEparams[FacingHelper.Faces(Facing.NorthAll).First().Index].isolated; //изолировано ли?


                    Cuboidf[] partBoxes;
                    if (!indexB)
                    {
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1).CollisionBoxes;  //получаем шейп нужного кабеля изолированного или целого
                    }
                    else
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 3).CollisionBoxes;  //получаем шейп нужного кабеля сгоревшего

                    var fixBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 4).CollisionBoxes;   //получаем шейп крепления кабеля

                    //ставим точку посередине, если провода не перегорел
                    if (!indexB)
                        boxes.Add(Facing.NorthAll, fixBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 0.0f, origin)).ToArray());


                    if ((key.Connection & Facing.NorthEast) != 0)
                    {
                        boxes.Add(Facing.NorthEast, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 270.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.NorthWest) != 0)
                    {
                        boxes.Add(Facing.NorthWest, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 90.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.NorthUp) != 0)
                    {
                        boxes.Add(Facing.NorthUp, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.NorthDown) != 0)
                    {
                        boxes.Add(Facing.NorthDown, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 180.0f, 0.0f, origin)).ToArray());
                    }

                }


                if ((key.Connection & Facing.EastAll) != 0)
                {
                    var indexV = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].voltage; //индекс напряжения этой грани
                    var indexM = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].indexM; //индекс материала этой грани
                    var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].lines; //индекс линий этой грани
                    var indexB = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].burnout;//индекс перегорания этой грани
                    var isol = entity.AllEparams[FacingHelper.Faces(Facing.EastAll).First().Index].isolated; //изолировано ли?


                    Cuboidf[] partBoxes;
                    if (!indexB)
                    {
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1).CollisionBoxes;  //получаем шейп нужного кабеля изолированного или целого
                    }
                    else
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 3).CollisionBoxes;  //получаем шейп нужного кабеля сгоревшего

                    var fixBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 4).CollisionBoxes;   //получаем шейп крепления кабеля

                    //ставим точку посередине, если провода не перегорел
                    if (!indexB)
                        boxes.Add(Facing.EastAll, fixBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 90.0f, origin)).ToArray());

                    if ((key.Connection & Facing.EastNorth) != 0)
                    {
                        boxes.Add(Facing.EastNorth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 90.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.EastSouth) != 0)
                    {
                        boxes.Add(Facing.EastSouth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(180.0f, 0.0f, 90.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.EastUp) != 0)
                    {
                        boxes.Add(Facing.EastUp, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 90.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.EastDown) != 0)
                    {
                        boxes.Add(Facing.EastDown, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 90.0f, origin)).ToArray());
                    }
                }



                if ((key.Connection & Facing.SouthAll) != 0)
                {
                    var indexV = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].voltage; //индекс напряжения этой грани
                    var indexM = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].indexM; //индекс материала этой грани
                    var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].lines; //индекс линий этой грани
                    var indexB = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].burnout;//индекс перегорания этой грани
                    var isol = entity.AllEparams[FacingHelper.Faces(Facing.SouthAll).First().Index].isolated; //изолировано ли?


                    Cuboidf[] partBoxes;
                    if (!indexB)
                    {
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1).CollisionBoxes;  //получаем шейп нужного кабеля изолированного или целого
                    }
                    else
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 3).CollisionBoxes;  //получаем шейп нужного кабеля сгоревшего

                    var fixBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 4).CollisionBoxes;   //получаем шейп крепления кабеля

                    //ставим точку посередине, если провода не перегорел
                    if (!indexB)
                        boxes.Add(Facing.SouthAll, fixBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 0.0f, origin)).ToArray());

                    if ((key.Connection & Facing.SouthEast) != 0)
                    {
                        boxes.Add(Facing.SouthEast, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 270.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.SouthWest) != 0)
                    {
                        boxes.Add(Facing.SouthWest, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 90.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.SouthUp) != 0)
                    {
                        boxes.Add(Facing.SouthUp, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 180.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.SouthDown) != 0)
                    {
                        boxes.Add(Facing.SouthDown, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 0.0f, origin)).ToArray());
                    }
                }



                if ((key.Connection & Facing.WestAll) != 0)
                {
                    var indexV = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].voltage; //индекс напряжения этой грани
                    var indexM = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].indexM; //индекс материала этой грани
                    var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].lines; //индекс линий этой грани
                    var indexB = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].burnout;//индекс перегорания этой грани
                    var isol = entity.AllEparams[FacingHelper.Faces(Facing.WestAll).First().Index].isolated; //изолировано ли?


                    Cuboidf[] partBoxes;
                    if (!indexB)
                    {
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1).CollisionBoxes;  //получаем шейп нужного кабеля изолированного или целого
                    }
                    else
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 3).CollisionBoxes;  //получаем шейп нужного кабеля сгоревшего

                    var fixBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 4).CollisionBoxes;   //получаем шейп крепления кабеля

                    //ставим точку посередине, если провода не перегорел
                    if (!indexB)
                        boxes.Add(Facing.WestAll, fixBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 270.0f, origin)).ToArray());

                    if ((key.Connection & Facing.WestNorth) != 0)
                    {
                        boxes.Add(Facing.WestNorth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 270.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.WestSouth) != 0)
                    {
                        boxes.Add(Facing.WestSouth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(180.0f, 0.0f, 270.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.WestUp) != 0)
                    {
                        boxes.Add(Facing.WestUp, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 270.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.WestDown) != 0)
                    {
                        boxes.Add(Facing.WestDown, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 270.0f, origin)).ToArray());
                    }
                }



                if ((key.Connection & Facing.UpAll) != 0)
                {
                    var indexV = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].voltage; //индекс напряжения этой грани
                    var indexM = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].indexM; //индекс материала этой грани
                    var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].lines; //индекс линий этой грани
                    var indexB = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].burnout;//индекс перегорания этой грани
                    var isol = entity.AllEparams[FacingHelper.Faces(Facing.UpAll).First().Index].isolated; //изолировано ли?



                    Cuboidf[] partBoxes;
                    if (!indexB)
                    {
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1).CollisionBoxes;  //получаем шейп нужного кабеля изолированного или целого
                    }
                    else
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 3).CollisionBoxes;  //получаем шейп нужного кабеля сгоревшего

                    var fixBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 4).CollisionBoxes;   //получаем шейп крепления кабеля

                    //ставим точку посередине, если провода не перегорел
                    if (!indexB)
                        boxes.Add(Facing.UpAll, fixBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 180.0f, origin)).ToArray());

                    if ((key.Connection & Facing.UpNorth) != 0)
                    {
                        boxes.Add(Facing.UpNorth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 180.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.UpEast) != 0)
                    {
                        boxes.Add(Facing.UpEast, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 270.0f, 180.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.UpSouth) != 0)
                    {
                        boxes.Add(Facing.UpSouth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 180.0f, 180.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.UpWest) != 0)
                    {
                        boxes.Add(Facing.UpWest, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 90.0f, 180.0f, origin)).ToArray());
                    }
                }



                if ((key.Connection & Facing.DownAll) != 0)
                {
                    var indexV = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].voltage; //индекс напряжения этой грани
                    var indexM = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].indexM; //индекс материала этой грани
                    var indexQ = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].lines; //индекс линий этой грани
                    var indexB = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].burnout;//индекс перегорания этой грани
                    var isol = entity.AllEparams[FacingHelper.Faces(Facing.DownAll).First().Index].isolated; //изолировано ли?



                    Cuboidf[] partBoxes;
                    if (!indexB)
                    {
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, isol ? 6 : 1).CollisionBoxes;  //получаем шейп нужного кабеля изолированного или целого
                    }
                    else
                        partBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 3).CollisionBoxes;  //получаем шейп нужного кабеля сгоревшего

                    var fixBoxes = new BlockVariants(entity.Api, entity.Block, indexV, indexM, indexQ, 4).CollisionBoxes;   //получаем шейп крепления кабеля

                    //ставим точку посередине, если провода не перегорел
                    if (!indexB)
                        boxes.Add(Facing.DownAll, fixBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 0.0f, origin)).ToArray());

                    if ((key.Connection & Facing.DownNorth) != 0)
                    {
                        boxes.Add(Facing.DownNorth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.DownEast) != 0)
                    {
                        boxes.Add(Facing.DownEast, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 270.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.DownSouth) != 0)
                    {
                        boxes.Add(Facing.DownSouth, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 180.0f, 0.0f, origin)).ToArray());
                    }

                    if ((key.Connection & Facing.DownWest) != 0)
                    {
                        boxes.Add(Facing.DownWest, partBoxes.Select(selectionBox => selectionBox.RotatedCopy(0.0f, 90.0f, 0.0f, origin)).ToArray());
                    }
                }

                Cuboidf[] enabledSwitchBoxes = enabledSwitchVariant?.CollisionBoxes ?? Array.Empty<Cuboidf>();
                Cuboidf[] disabledSwitchBoxes = disabledSwitchVariant?.CollisionBoxes ?? Array.Empty<Cuboidf>();

                // переключатели
                if ((key.Switches & Facing.NorthEast) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.NorthAll,
                        ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 90.0f, 0.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.NorthWest) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.NorthAll,
                        ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 270.0f, 0.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.NorthUp) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.NorthAll,
                        ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 180.0f, 0.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.NorthDown) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.NorthAll,
                        ((key.Switches & key.SwitchesState & Facing.NorthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 0.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.EastNorth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.EastAll,
                        ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(180.0f, 0.0f, 90.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.EastSouth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.EastAll,
                        ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 90.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.EastUp) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.EastAll,
                        ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 90.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.EastDown) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.EastAll,
                        ((key.Switches & key.SwitchesState & Facing.EastAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 90.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.SouthEast) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.SouthAll,
                        ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 90.0f, 180.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.SouthWest) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.SouthAll,
                        ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes).Select(
                            selectionBox =>
                                selectionBox.RotatedCopy(90.0f, 270.0f, 180.0f, origin)
                        ).ToArray()
                    );
                }

                if ((key.Switches & Facing.SouthUp) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.SouthAll,
                        ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes).Select(
                            selectionBox =>
                                selectionBox.RotatedCopy(90.0f, 180.0f, 180.0f, origin)
                        ).ToArray()
                    );
                }

                if ((key.Switches & Facing.SouthDown) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.SouthAll,
                        ((key.Switches & key.SwitchesState & Facing.SouthAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 180.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.WestNorth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.WestAll,
                        ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(180.0f, 0.0f, 270.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.WestSouth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.WestAll,
                        ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 270.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.WestUp) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.WestAll,
                        ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(270.0f, 0.0f, 270.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.WestDown) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.WestAll,
                        ((key.Switches & key.SwitchesState & Facing.WestAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(90.0f, 0.0f, 270.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.UpNorth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.UpAll,
                        ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 180.0f, 180.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.UpEast) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.UpAll,
                        ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 90.0f, 180.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.UpSouth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.UpAll,
                        ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 180.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.UpWest) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.UpAll,
                        ((key.Switches & key.SwitchesState & Facing.UpAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 270.0f, 180.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.DownNorth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.DownAll,
                        ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 180.0f, 0.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.DownEast) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.DownAll,
                        ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 90.0f, 0.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.DownSouth) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.DownAll,
                        ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 0.0f, 0.0f, origin)).ToArray()
                    );
                }

                if ((key.Switches & Facing.DownWest) != 0)
                {
                    AddBoxes(
                        ref boxes,
                        Facing.DownAll,
                        ((key.Switches & key.SwitchesState & Facing.DownAll) != 0
                            ? enabledSwitchBoxes
                            : disabledSwitchBoxes)
                        .Select(selectionBox => selectionBox.RotatedCopy(0.0f, 270.0f, 0.0f, origin)).ToArray()
                    );
                }
            }

            return boxes;
        }

        private static void AddBoxes(ref Dictionary<Facing, Cuboidf[]> cache, Facing key, Cuboidf[] boxes)
        {
            if (cache.ContainsKey(key))
            {
                cache[key] = cache[key].Concat(boxes).ToArray();
            }
            else
            {
                cache[key] = boxes;
            }
        }

        private static void AddMeshData(ref MeshData? sourceMesh, MeshData? meshData)
        {
            if (meshData != null)
            {
                if (sourceMesh != null)
                {
                    sourceMesh.AddMeshData(meshData);
                }
                else
                {
                    sourceMesh = meshData;
                }
            }
        }




        /// <summary>
        /// Структура для хранения ключей для словарей
        /// </summary>
        public struct CacheDataKey : IEquatable<CacheDataKey>
        {
            public readonly Facing Connection;
            public readonly Facing Switches;
            public readonly Facing SwitchesState;
            public readonly EParams[] AllEparams;

            public CacheDataKey(Facing connection, Facing switches, Facing switchesState, EParams[] allEparams)
            {
                Connection = connection;
                Switches = switches;
                SwitchesState = switchesState;
                AllEparams = allEparams;
            }

            public static CacheDataKey FromEntity(BlockEntityECable entityE)
            {
                EParams[] bufAllEparams = entityE.AllEparams.ToArray();
                return new CacheDataKey(
                    entityE.Connection,
                    entityE.Switches,
                    entityE.SwitchesState,
                    bufAllEparams
                );
            }

            public bool Equals(CacheDataKey other)
            {
                if (Connection != other.Connection ||
                    Switches != other.Switches ||
                    SwitchesState != other.SwitchesState ||
                    AllEparams.Length != other.AllEparams.Length)
                    return false;

                for (int i = 0; i < AllEparams.Length; i++)
                {
                    if (!AllEparams[i].Equals(other.AllEparams[i]))
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is CacheDataKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + Connection.GetHashCode();
                    hash = hash * 31 + Switches.GetHashCode();
                    hash = hash * 31 + SwitchesState.GetHashCode();
                    foreach (var param in AllEparams)
                    {
                        hash = hash * 31 + param.GetHashCode();
                    }
                    return hash;
                }
            }
        }
    }
}
