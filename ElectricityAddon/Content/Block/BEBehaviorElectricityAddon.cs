 using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ElectricityAddon.Content.Block;

public class BEBehaviorElectricityAddon : BlockEntityBehavior
{
    private IElectricAccumulator? accumulator;

    private Facing connection;
    private IElectricConsumer? consumer;
    private bool dirty = true;
    private Facing interruption;
    private IElectricProducer? producer;
    public float[] eparams;               



    public BEBehaviorElectricityAddon(BlockEntity blockEntity)
        : base(blockEntity)
    {

    }

    public global::ElectricityAddon.ElectricityAddon? System =>
        this.Api?.ModLoader.GetModSystem<global::ElectricityAddon.ElectricityAddon>();

    public Facing Connection
    {
        get => this.connection;
        set
        {
            if (this.connection != value)
            {
                this.connection = value;
                this.dirty = true;
                this.Update();
            }
        }
    }

    public float[] Eparams
    {
        get => this.eparams;
        set
        {
            if (this.eparams != value)
            {
                this.eparams = value;
                this.dirty = true;
                this.Update();
            }
        }
    }

    public Facing Interruption
    {
        get => this.interruption;
        set
        {
            if (this.interruption != value)
            {
                this.interruption = value;
                this.dirty = true;
                this.Update();
            }
        }
    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        this.Update();
    }

    /// <summary>
    /// Что-тов цепи поменялось
    /// </summary>
    /// <param name="force"></param>
    public void Update(bool force = false)
    {
        if (this.dirty || force)
        {
            var system = this.System;

            if (system is not null)
            {
                this.dirty = false;


                this.consumer = null;
                this.producer = null;
                this.accumulator = null;

                foreach (var entityBehavior in this.Blockentity.Behaviors)
                {
                    switch (entityBehavior)
                    {
                        case IElectricConsumer { } consumer:
                            this.consumer = consumer;

                            break;
                        case IElectricProducer { } producer:
                            this.producer = producer;

                            break;
                        case IElectricAccumulator { } accumulator:
                            this.accumulator = accumulator;

                            break;
                    }
                }
                

                system.SetConsumer(this.Blockentity.Pos, this.consumer); 
                system.SetProducer(this.Blockentity.Pos, this.producer);
                system.SetAccumulator(this.Blockentity.Pos, this.accumulator);

                if (system.Update(this.Blockentity.Pos, this.connection & ~this.interruption, Eparams))
                {
                    this.Blockentity.MarkDirty(true);
                }

            }
            else
            {
                this.dirty = true;
            }
        }
    }
    

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        this.System?.Remove(this.Blockentity.Pos);
    }


    /// <summary>
    /// Подсказка при наведении на блок
    /// </summary>
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        var networkInformation = this.System?.GetNetworks(this.Blockentity.Pos, this.Connection);      //получаем информацию о сети

        stringBuilder.AppendLine(Lang.Get("Electricity"));

        if (this.System!.AltPressed)
        {
            stringBuilder.AppendLine("├ Потребителей: " + networkInformation?.NumberOfConsumers);
            stringBuilder.AppendLine("├ Генераторов: " + networkInformation?.NumberOfProducers);
            stringBuilder.AppendLine("├ Аккумуляторов: " + networkInformation?.NumberOfAccumulators);
            stringBuilder.AppendLine("├ Блоков: " + networkInformation?.NumberOfBlocks);
            stringBuilder.AppendLine("├ " + "Макс. ток: " + networkInformation?.eParamsInNetwork[0] + " Eu/t");
            stringBuilder.AppendLine("├ " + "Ток: " + networkInformation?.eParamsInNetwork[1] + " Eu/t");
            stringBuilder.AppendLine("├ " + "Потери: " + networkInformation?.eParamsInNetwork[2] + " Eu/t/блок");
            stringBuilder.AppendLine("├ " + "Линий: " + networkInformation?.eParamsInNetwork[3] + " ед");
            stringBuilder.AppendLine("├ " + "Напряжение: " + networkInformation?.eParamsInNetwork[6] + " V");
        }

        stringBuilder.AppendLine("├ Генерация: " + networkInformation?.Production + " Eu");
        stringBuilder.AppendLine("├ Потребление: " + networkInformation?.Consumption + " Eu");
        stringBuilder.AppendLine("└ Дефицит: " + networkInformation?.Lack + " Eu");

        if (!this.System!.AltPressed)  stringBuilder.AppendLine("Press Alt for details");
        
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        
        tree.SetBytes("electricity:connection", SerializerUtil.Serialize(this.connection));
        tree.SetBytes("electricity:interruption", SerializerUtil.Serialize(this.interruption));

        //var networkInformation = this.System?.GetNetworks(this.Blockentity.Pos, this.Connection);      //получаем информацию о сети
        //tree.SetBytes("electricity:eparams", SerializerUtil.Serialize(networkInformation?.eParamsInNetwork));
        
        tree.SetBytes("electricity:eparams", SerializerUtil.Serialize(this.eparams));
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        var connection = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricity:connection"));
        var interruption = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricity:interruption"));

        var eparams = SerializerUtil.Deserialize<float[]>(tree.GetBytes("electricity:eparams"));

        if (connection != this.connection || interruption != this.interruption)
        {
            this.interruption = interruption;
            this.connection = connection;
            this.eparams = eparams;
            this.dirty = true;
            this.Update();
        }
    }
}