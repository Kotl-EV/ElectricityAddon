using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ElectricityAddon.Content.Block.ECable;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using static ElectricityAddon.Content.Block.ECable.BlockECable;
using Newtonsoft.Json;




namespace ElectricityAddon.Content.Block;

public class BEBehaviorElectricityAddon : BlockEntityBehavior
{
    private IElectricAccumulator? accumulator;

    private Facing connection;
    private IElectricConsumer? consumer;
    private bool dirty = true;
    private bool paramsSet = false;
    private Facing interruption;
    private IElectricProducer? producer;
    public float[] eparams;
    public int eparamsFace;
    private float[][] allEparams;

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
                this.paramsSet = false;
                this.Update();
            }
        }
    }

    public float[][] AllEparams
    {
        get => this.allEparams;
        set
        {
            if (this.allEparams != value)
            {
                this.allEparams = value;
                this.dirty = true;
                //this.paramsSet = false;
                this.Update();
            }
        }
    }


    public (float[],int) Eparams
    {
        get => (this.eparams,this.eparamsFace);
        set
        {
            if (this.eparams != value.Item1 || this.eparamsFace != value.Item2)
            {
                this.eparams = value.Item1;
                this.eparamsFace = value.Item2;
                this.paramsSet = true;
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


                //если обновляется connection или interrupt, то нафиг присваивать параметры
                (float[], int) Epar;
                if (!this.paramsSet)
                    Epar = (null!, 0);
                else
                    Epar = Eparams;


                if (system.Update(this.Blockentity.Pos, this.connection & ~this.interruption, Epar,ref this.allEparams))
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

        Facing selectedFacing=Facing.None; //храним направления проводов в этом блоке


        stringBuilder.AppendLine(Lang.Get("Electricity"));

        //если это кабель, то мы можем вывести только информацию о сети на одной грани
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECable entity)
        {
            if (forPlayer is { CurrentBlockSelection: { } blockSelection })
            {
                var key = CacheDataKey.FromEntity(entity);
                var hitPosition = blockSelection.HitPosition;

                var sf = new SelectionFacingCable();
                selectedFacing = sf.SelectionFacing(key, hitPosition, (BlockECable)this.Api.World.BlockAccessor.GetBlock(this.Blockentity.Pos));  //выделяем напрвление для слома под курсором

                if (selectedFacing != Facing.None)
                    selectedFacing = FacingHelper.FromFace(FacingHelper.Faces(selectedFacing).First());  //выбираем одну грань, если даже их там вдруг окажется больше
                else
                    return;

            }
        }
        else    //для не кабелей берем все что есть
        {
            selectedFacing = this.Connection;
        }


        var networkInformation = this.System?.GetNetworks(this.Blockentity.Pos, selectedFacing);      //получаем информацию о сети


        if (this.System!.AltPressed)
        {
            stringBuilder.AppendLine("├ Потребителей: " + networkInformation?.NumberOfConsumers);
            stringBuilder.AppendLine("├ Генераторов: " + networkInformation?.NumberOfProducers);
            stringBuilder.AppendLine("├ Аккумуляторов: " + networkInformation?.NumberOfAccumulators);
            stringBuilder.AppendLine("├ Блоков: " + networkInformation?.NumberOfBlocks);
        }

        stringBuilder.AppendLine("├ Генерация: " + networkInformation?.Production + " Вт");
        stringBuilder.AppendLine("├ Потребление: " + networkInformation?.Consumption + " Вт");
        stringBuilder.AppendLine("└ Дефицит: " + networkInformation?.Lack + " Вт");

        if (!this.System!.AltPressed) stringBuilder.AppendLine("Нажми Alt для подробностей");

        if (this.System!.AltPressed)
        {
            stringBuilder.AppendLine("Блок");
            stringBuilder.AppendLine("├ " + "Макс. ток: " + networkInformation?.eParamsInNetwork[0]* networkInformation?.eParamsInNetwork[3] + " А");
            stringBuilder.AppendLine("├ " + "Ток: " + networkInformation?.current + " А");

            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECable) //если кабель!
            {
                stringBuilder.AppendLine("├ " + "Уд. сопр: " + networkInformation?.eParamsInNetwork[2] + " Ом/линию");
                stringBuilder.AppendLine("├ " + "Сопротивление: " + networkInformation?.eParamsInNetwork[2]/ ( networkInformation?.eParamsInNetwork[3]* networkInformation?.eParamsInNetwork[4]) + " Ом");
                stringBuilder.AppendLine("├ " + "Линий: " + networkInformation?.eParamsInNetwork[3] + " шт.");
                stringBuilder.AppendLine("├ " + "Пл. сечения: " + networkInformation?.eParamsInNetwork[4]* networkInformation?.eParamsInNetwork[3] + " у.ед.");
            }
            stringBuilder.AppendLine("└ " + "Макс напряжение: " + networkInformation?.eParamsInNetwork[6] + " В");
        }



    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);


        tree.SetBytes("electricityaddon:connection", SerializerUtil.Serialize(this.connection));
        tree.SetBytes("electricityaddon:interruption", SerializerUtil.Serialize(this.interruption));

        //var networkInformation = this.System?.GetNetworks(this.Blockentity.Pos, this.Connection);      //получаем информацию о сети
        //tree.SetBytes("electricity:eparams", SerializerUtil.Serialize(networkInformation?.eParamsInNetwork));

        //массив массивов приходится сохранять через newtonsoftjson
        tree.SetBytes("electricityaddon:allEparams", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this.allEparams)));
        
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        var connection = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricityaddon:connection"));
        var interruption = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricityaddon:interruption"));

        //массив массивов приходится считывать через newtonsoftjson
        var AllEparamss = JsonConvert.DeserializeObject<float[][]>(Encoding.UTF8.GetString(tree.GetBytes("electricityaddon:allEparams")));

        if (connection != this.connection || interruption != this.interruption)
        {
            this.interruption = interruption;
            this.connection = connection;
            this.allEparams = AllEparamss!;
            this.dirty = true;
            this.Update();
        }
    }
}