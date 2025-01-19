using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Utils;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.ELamp
{
    public class BEBehaviorELamp :BlockEntityBehavior, IElectricConsumer {
        public BEBehaviorELamp(BlockEntity blockEntity) : base(blockEntity)
        {
            HSV = GetHSV();
            maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 0);
        }


        private int[] null_HSV = { 0, 0, 0 };   //заглушка
        public int[] HSV = { 0, 0, 0 };         //сюда берем цвет
        public int maxConsumption=0;            //максимальное потребление

        //извлекаем цвет
        public int[] GetHSV()
        {
            int[] bufHSV= MyMiniLib.GetAttributeArrayInt(this.Block, "HSV", null_HSV);
            //теперь нужно поделить H и S на 6, чтобы в игре правильно считало цвет
            bufHSV[0] = (int)Math.Round((bufHSV[0] / 6.0), MidpointRounding.AwayFromZero);
            bufHSV[1] = (int)Math.Round((bufHSV[1] / 6.0), MidpointRounding.AwayFromZero);
            return bufHSV;
        }

        public int LightLevel { get; private set; }

        public ConsumptionRange ConsumptionRange => new(1, maxConsumption);

        
        
        public void Consume(int lightLevel)
        {
            if (this.Api is { } api)
            {
                if (lightLevel != this.LightLevel)
                {
                    switch (this.LightLevel)         //меняем ассеты горящей и не горящей лампы
                    {
                        case 0 when lightLevel > 0:
                            {
                            var assetLocation = this.Blockentity.Block.CodeWithVariant("state", "enabled");
                            var block = api.World.BlockAccessor.GetBlock(assetLocation);
                            api.World.BlockAccessor.ExchangeBlock(block.Id, this.Blockentity.Pos);
                            break;
                            }
                        case > 0 when lightLevel == 0:
                            {
                            var assetLocation = this.Blockentity.Block.CodeWithVariant("state", "disabled");
                            var block = api.World.BlockAccessor.GetBlock(assetLocation);
                            api.World.BlockAccessor.ExchangeBlock(block.Id, this.Blockentity.Pos);
                            break;
                            }
                    }

                    //применяем цвет
                    this.Blockentity.Block.LightHsv = new[] {
                            (byte)this.HSV[0],
                            (byte)this.HSV[1],
                            (byte)FloatHelper.Remap(lightLevel, 0, maxConsumption, 0, this.HSV[2])
                        };

                    this.Blockentity.MarkDirty(true);
                    this.LightLevel = lightLevel;
                }
            }
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            stringBuilder.AppendLine(StringHelper.Progressbar(this.LightLevel * 100.0f / maxConsumption));
            stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + this.LightLevel + "/" + maxConsumption + " Eu");
            stringBuilder.AppendLine();
        }
    
    }
}
