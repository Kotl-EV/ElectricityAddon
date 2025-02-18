﻿using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ElectricityAddon.Content.Block.EFreezer;

public class BEBehaviorEFreezer : BlockEntityBehavior, IElectricConsumer
{
    public int powerSetting;
    public int maxConsumption;
    public BEBehaviorEFreezer(BlockEntity blockEntity) : base(blockEntity)
    {
        maxConsumption = MyMiniLib.GetAttributeInt(this.Block, "maxConsumption", 100);
    }

    public ConsumptionRange ConsumptionRange => new(10, maxConsumption);

    public void Consume(int amount)
    {
        if (powerSetting != amount)
        {
            powerSetting = amount;
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting * 100.0f / maxConsumption));
        stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + powerSetting + "/" + 100 + " Eu");
        stringBuilder.AppendLine();
    }
}