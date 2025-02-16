using System;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;


namespace ElectricityAddon.Content.Block.EAccumulator;

public class BEBehaviorEAccumulator : BlockEntityBehavior, IElectricAccumulator
{

    public BEBehaviorEAccumulator(BlockEntity blockEntity) : base(blockEntity)
    {
    }


    public float lastCapacity=0;  //предыдущее значение емкости

    public float capacity;  //текущая емкость (сохраняется)

    public new BlockPos Pos => this.Blockentity.Pos;

    public float maxCurrent => 200.0F;   //ограничение по энергии в тик(току)!!!!!!!

    public float GetMaxCapacity()
    {
        return MyMiniLib.GetAttributeInt(this.Block, "maxcapacity", 16000);
    }

    public float GetCapacity()
    {
        return capacity;
    }

    /// <summary>
    /// Задает сразу емкость аккумулятору (вызывать только при установке аккумулятора)
    /// </summary>
    /// <returns></returns>
    public void SetCapacity(float value)
    {
        capacity = (value > GetMaxCapacity())
            ? GetMaxCapacity()
            : value;
    }


    public void Store(float amount)
    {
        var buf = Math.Min(Math.Min(amount, maxCurrent), GetMaxCapacity() - capacity);

        capacity += buf;  //не позволяем одним пакетом сохранить больше максимального тока. В теории такого превышения и не должно случиться

    }

    public float Release(float amount)
    {        
        var buf = Math.Min(capacity, Math.Min(amount, maxCurrent));
        capacity -= buf;

        return buf;                                                 //выдаем пакет c учетом тока и запасов
    }


    public float canStore()
    {
        return Math.Min(maxCurrent, GetMaxCapacity() - capacity);
    }

    public float canRelease()
    {
        return Math.Min(capacity, maxCurrent);
    }

    public float GetLastCapacity()
    {
        return this.lastCapacity;
    }

    public void Update()
    {
        lastCapacity = capacity;
        this.Blockentity.MarkDirty(true);
    }


    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetFloat("electricityaddon:capacity", capacity);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        capacity = tree.GetFloat("electricityaddon:capacity");
    }


    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(GetCapacity() * 100.0f / GetMaxCapacity()));
        stringBuilder.AppendLine("└ " + Lang.Get("Storage") + GetCapacity() + "/" + GetMaxCapacity() + " Eu");
        stringBuilder.AppendLine();
    }


}