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
    private IElectricConsumer? consumer;
    private IElectricProducer? producer;
    private IElectricTransformator? transformator;

    private Facing connection;

    private bool dirty = true;
    private bool paramsSet = false;
    private Facing interruption;
    
    public EParams eparams;
    public int eparamsFace;
    private EParams[] allEparams;

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

    public EParams[] AllEparams
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


    public (EParams, int) Eparams
    {
        get => (this.eparams, this.eparamsFace);
        set
        {
            if (!this.eparams.Equals(value.Item1) || this.eparamsFace != value.Item2)
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
                this.transformator = null;

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

                        case IElectricTransformator { } transformator:
                            this.transformator = transformator;
                            break;
                    }
                }


                system.SetConsumer(this.Blockentity.Pos, this.consumer);
                system.SetProducer(this.Blockentity.Pos, this.producer);
                system.SetAccumulator(this.Blockentity.Pos, this.accumulator);
                system.SetTransformator(this.Blockentity.Pos, this.transformator);

                //если обновляется connection или interrupt, то нафиг присваивать параметры
                (EParams, int) Epar;
                if (!this.paramsSet)
                    Epar = (default(EParams), 0);
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




        //если это кабель, то мы можем вывести только информацию о сети на одной грани
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECable entity && entity.AllEparams!=null)
        {
            if (forPlayer is { CurrentBlockSelection: { } blockSelection })
            {
                var key = CacheDataKey.FromEntity(entity);
                var hitPosition = blockSelection.HitPosition;

                var sf = new SelectionFacingCable();
                selectedFacing = sf.SelectionFacing(key, hitPosition, this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos));  //выделяем напрвление для слома под курсором

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
            stringBuilder.AppendLine(Lang.Get("Electricity"));
            stringBuilder.AppendLine("├ "+ Lang.Get("Consumers") + ": " + networkInformation?.NumberOfConsumers);
            stringBuilder.AppendLine("├ "+ Lang.Get("Generators") + ": " + networkInformation?.NumberOfProducers);
            stringBuilder.AppendLine("├ "+ Lang.Get("Batteries") + ": " + networkInformation?.NumberOfAccumulators);
            stringBuilder.AppendLine("├ "+ Lang.Get("Transformers") + ": " + networkInformation?.NumberOfTransformators);
            stringBuilder.AppendLine("├ "+ Lang.Get("Blocks") + ": " + networkInformation?.NumberOfBlocks);
            stringBuilder.AppendLine("├ " + Lang.Get("Generation") + ": " + networkInformation?.Production + " " + Lang.Get("W"));
            stringBuilder.AppendLine("├ "+ Lang.Get("Consumption") + ": " + networkInformation?.Consumption + " " + Lang.Get("W"));
            stringBuilder.AppendLine("└ "+ Lang.Get("Shortage") + ": " + networkInformation?.Lack + " " + Lang.Get("W"));
        }



        if (!this.System!.AltPressed) stringBuilder.AppendLine(Lang.Get("Press Alt for details"));

        if (this.System!.AltPressed)
        {
            stringBuilder.AppendLine(Lang.Get("Block"));
            stringBuilder.AppendLine("├ " + Lang.Get("Max. current") + ": " + networkInformation?.eParamsInNetwork.maxCurrent* networkInformation?.eParamsInNetwork.lines + " " + Lang.Get("A"));
            stringBuilder.AppendLine("├ " + Lang.Get("Current") + ": " + networkInformation?.current + " " + Lang.Get("A"));

            if (this.Api.World.BlockAccessor.GetBlockEntity(this.Blockentity.Pos) is BlockEntityECable) //если кабель!
            {
                stringBuilder.AppendLine("├ " + Lang.Get("Resistivity") + ": " + networkInformation?.eParamsInNetwork.resisitivity/(networkInformation!.eParamsInNetwork.isolated?2.0F:1.0F) + " " + Lang.Get("Om/line"));
                stringBuilder.AppendLine("├ " + Lang.Get("Resistance") + ": " + networkInformation?.eParamsInNetwork.resisitivity/ ( networkInformation?.eParamsInNetwork.lines* networkInformation?.eParamsInNetwork.crossArea) / (networkInformation.eParamsInNetwork.isolated ? 2.0F : 1.0F) + " " + Lang.Get("Om"));
                stringBuilder.AppendLine("├ " + Lang.Get("Lines") + ": " + networkInformation?.eParamsInNetwork.lines + " " + Lang.Get("pcs."));
                stringBuilder.AppendLine("├ " + Lang.Get("Section size") + ": " + networkInformation?.eParamsInNetwork.crossArea* networkInformation?.eParamsInNetwork.lines + " " + Lang.Get("units"));
            }
            stringBuilder.AppendLine("└ " + Lang.Get("Max voltage") + ": " + networkInformation?.eParamsInNetwork.voltage + " " + Lang.Get("V"));
        }



    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetBytes("electricityaddon:connection", SerializerUtil.Serialize(this.connection));
        tree.SetBytes("electricityaddon:interruption", SerializerUtil.Serialize(this.interruption));

        //массив массивов приходится сохранять через newtonsoftjson
        tree.SetBytes("electricityaddon:allEparams", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this.allEparams)));
        
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        var connection = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricityaddon:connection"));
        var interruption = SerializerUtil.Deserialize<Facing>(tree.GetBytes("electricityaddon:interruption"));

        //массив массивов приходится считывать через newtonsoftjson
        var AllEparamss = JsonConvert.DeserializeObject<EParams[]>(Encoding.UTF8.GetString(tree.GetBytes("electricityaddon:allEparams")));

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