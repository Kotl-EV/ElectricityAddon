using ElectricityAddon.Utils;
using System;
using System.Text;
using ElectricityAddon.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using ElectricityAddon.Content.Block.ETransformator;
using System.Linq;
using ElectricityAddon.Content.Block.EHorn;
using ElectricityAddon.Content.Block.EHeater;

namespace ElectricityAddon.Content.Block.ELamp
{
    public class BEBehaviorELamp : BlockEntityBehavior, IElectricConsumer
    {
        public BEBehaviorELamp(BlockEntity blockEntity) : base(blockEntity)
        {
            maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 4);
        }


        private int[] null_HSV = { 0, 0, 0 };   //заглушка
        public int maxConsumption;              //максимальное потребление

        public bool isBurned => this.Block.Variant["state"] == "burned";

        public int LightLevel { get; private set; }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("electricityaddon:LightLevel", LightLevel);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            LightLevel = tree.GetInt("electricityaddon:LightLevel");

        }


        public float Consume_request()
        {
            return maxConsumption;
        }


        public void Consume_receive(float amount)
        {
            if (this.Api is { } api)
            {
                if ((int)Math.Round(amount, MidpointRounding.AwayFromZero) != this.LightLevel && this.Block.Variant["state"] != "burned")
                {

                    if ((int)Math.Round(amount, MidpointRounding.AwayFromZero) >= 1 && this.Block.Variant["state"]== "disabled")                               //включаем если питание больше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                    }
                    else if ((int)Math.Round(amount, MidpointRounding.AwayFromZero) < 1 && this.Block.Variant["state"] == "enabled")                            //гасим если питание меньше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
                    }

                    int[] bufHSV = MyMiniLib.GetAttributeArrayInt(this.Block, "HSV", null_HSV);
                    //теперь нужно поделить H и S на 6, чтобы в игре правильно считало цвет
                    bufHSV[0] = (int)Math.Round((bufHSV[0] / 6.0), MidpointRounding.AwayFromZero);
                    bufHSV[1] = (int)Math.Round((bufHSV[1] / 6.0), MidpointRounding.AwayFromZero);

                    //применяем цвет и яркость
                    this.Blockentity.Block.LightHsv = new[] {
                            (byte)bufHSV[0],
                            (byte)bufHSV[1],
                            (byte)FloatHelper.Remap((int)Math.Round(amount, MidpointRounding.AwayFromZero), 0, maxConsumption, 0, bufHSV[2])
                        };

                    this.Blockentity.MarkDirty(true);
                    this.LightLevel = (int)Math.Round(amount, MidpointRounding.AwayFromZero);

                }
            }
        }


        public float getPowerReceive()
        {
            return this.LightLevel;
        }

        public float getPowerRequest()
        {
            return maxConsumption;
        }

        public void Update()
        {
            //смотрим надо ли обновить модельку когда сгорает прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityELamp entity && entity.AllEparams != null)
            {
                bool hasBurnout = entity.AllEparams.Any(e => e.burnout);
                if (hasBurnout && entity.Block.Variant["state"] != "burned")
                {
                    string tempK = entity.Block.Variant["tempK"];

                    string[] types = new string[2] { "tempK" , "state" };   //типы лампы
                    string[] variants = new string[2] { tempK, "burned" };     //нужный вариант лампы

                    this.Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariants(types, variants)).BlockId, Pos);


                }

            }
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            //проверяем не сгорел ли прибор
            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityELamp entity)
            {
                if (isBurned)
                {
                    stringBuilder.AppendLine(Lang.Get("Burned"));
                }
                else
                {
                    stringBuilder.AppendLine(StringHelper.Progressbar(this.LightLevel * 100.0f / maxConsumption));
                    stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + ": " + this.LightLevel + "/" + maxConsumption + " " + Lang.Get("W"));
                }
            
            }
            stringBuilder.AppendLine();            
        }


    }
}
