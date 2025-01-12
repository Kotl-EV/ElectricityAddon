using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.ELamp
{
    public class BEBehaviorELamp :BlockEntityBehavior, IElectricConsumer {
        public BEBehaviorELamp(BlockEntity blockEntity) : base(blockEntity)
        {
            HSV = GetHSV();
        }


        private int[] null_HSV = { 0, 0, 0 };
        public int[] HSV = { 0, 0, 0 };
        public int[] GetHSV()
        {
            int[] bufHSV= MyMiniLib.GetAttributeArray(this.Block, "HSV", null_HSV);
            //теперь нужно поделить H и S на 6, чтобы в игре правильно считало цвет
            bufHSV[0] = (int)Math.Round((bufHSV[0] / 6.0), MidpointRounding.AwayFromZero);
            bufHSV[1] = (int)Math.Round((bufHSV[1] / 6.0), MidpointRounding.AwayFromZero);
            return bufHSV;
        }

        public int LightLevel { get; private set; }

        public ConsumptionRange ConsumptionRange => new(1, 8);

        
        
        public void Consume(int lightLevel)
        {
            if (this.Api is { } api)
            {
                if (lightLevel != this.LightLevel)
                {
                    switch (this.LightLevel)
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

                    this.Blockentity.Block.LightHsv = new[] {
                            (byte)this.HSV[0],
                            (byte)this.HSV[1],
                            (byte)FloatHelper.Remap(lightLevel, 0, 8, 0, this.HSV[2])
                        };

                    this.Blockentity.MarkDirty(true);
                    this.LightLevel = lightLevel;
                }
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
        {
            base.GetBlockInfo(forPlayer, stringBuilder);

            stringBuilder.AppendLine(StringHelper.Progressbar(this.LightLevel * 100.0f / 8.0f));
            stringBuilder.AppendLine("└ " + Lang.Get("Consumption") + this.LightLevel + "/" + 8 + "Eu");
            stringBuilder.AppendLine();
        }
    
    }
}
