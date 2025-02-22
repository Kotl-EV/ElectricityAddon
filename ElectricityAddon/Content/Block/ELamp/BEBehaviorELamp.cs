using ElectricityAddon.Utils;
using System;
using System.Text;
using ElectricityAddon.Interface;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ElectricityAddon.Content.Block.ELamp
{
    public class BEBehaviorELamp : BlockEntityBehavior, IElectricConsumer
    {
        public BEBehaviorELamp(BlockEntity blockEntity) : base(blockEntity)
        {
            HSV = GetHSV();
            maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 4);
        }


        private int[] null_HSV = { 0, 0, 0 };   //заглушка
        public int[] HSV = { 0, 0, 0 };         //сюда берем цвет
        public int maxConsumption;              //максимальное потребление

        //извлекаем цвет
        public int[] GetHSV()
        {
            int[] bufHSV = MyMiniLib.GetAttributeArrayInt(this.Block, "HSV", null_HSV);
            //теперь нужно поделить H и S на 6, чтобы в игре правильно считало цвет
            bufHSV[0] = (int)Math.Round((bufHSV[0] / 6.0), MidpointRounding.AwayFromZero);
            bufHSV[1] = (int)Math.Round((bufHSV[1] / 6.0), MidpointRounding.AwayFromZero);
            return bufHSV;
        }

        public float LightLevel { get; private set; }

        public ConsumptionRange ConsumptionRange => new(0, maxConsumption);  //удалить



        public void Consume(int lightLevel)  // можно удалить
        {

        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("electricityaddon:LightLevel", LightLevel);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            LightLevel = tree.GetFloat("electricityaddon:LightLevel");
        }




        public float Consume_request()
        {
            return maxConsumption;
        }




        public void Consume_receive(float amount)
        {
            if (this.Api is { } api)
            {
                if (amount != this.LightLevel)
                {

                    if (amount >= 1 && this.Block.Code.ToString().Contains("disabled"))                               //включаем если питание больше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "enabled")).BlockId, Pos);
                    }
                    else if (amount < 1 && this.Block.Code.ToString().Contains("enabled"))                            //гасим если питание меньше 1
                    {
                        api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "disabled")).BlockId, Pos);
                    }



                    //применяем цвет и яркость
                    this.Blockentity.Block.LightHsv = new[] {
                            (byte)this.HSV[0],
                            (byte)this.HSV[1],
                            (byte)FloatHelper.Remap(amount, 0, maxConsumption, 0, this.HSV[2])
                        };

                    this.Blockentity.MarkDirty(true);
                    this.LightLevel = amount;

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
            //this.Blockentity.MarkDirty(true);
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            stringBuilder.AppendLine(StringHelper.Progressbar(this.LightLevel * 100.0f / maxConsumption));
            stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + this.LightLevel + "/" + maxConsumption + " Вт");
            stringBuilder.AppendLine();
            
        }


    }
}
