using ElectricityAddon.Content.Armor;
using ElectricityAddon.Content.Block.EAccumulator;
using ElectricityAddon.Content.Block.ECharger;
using ElectricityAddon.Content.Block.EConnector;
using ElectricityAddon.Content.Block.EFreezer;
using ElectricityAddon.Content.Block.EGenerator;
using ElectricityAddon.Content.Block.EHorn;
using ElectricityAddon.Content.Block.EMotor;
using ElectricityAddon.Content.Block.EStove;
using ElectricityAddon.Content.Block.ELamp;
using ElectricityAddon.Content.Block.EOven;
using ElectricityAddon.Content.Item;
using Vintagestory.API.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Electricity.Content.Block;
using Electricity.Content.Block.Entity;
using ElectricityAddon.Content.Block;
using ElectricityAddon.Content.Block.ESwitch;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.MathTools;
using HarmonyLib;

[assembly: ModDependency("game", "1.20.0")]
[assembly: ModInfo(
    "ElectricityAddon",
    "electricityaddon",
    Website = "https://github.com/Kotl-EV/ElectricityAddon",
    Description = "Brings electricity into the game!",
    Version = "0.0.15",
    Authors = new[] {
        "Kotl"
    }
)]

namespace ElectricityAddon;

public class ElectricityAddon : ModSystem
{
    private readonly List<Consumer> consumers = new();
    private readonly List<Producer> producers = new();
    private readonly HashSet<Network> networks = new();
    private readonly Dictionary<BlockPos, NetworkPart> parts = new(); //хранит все элементы всех цепей
    public static bool combatoverhaul = false;                        //установлен ли combatoverhaul
    public int speedOfElectricity = 1;                                  //скорость электричетсва в проводах при одном обновлении сети (блоков в тик)

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.RegisterBlockClass("BlockECable", typeof(BlockECable));
        api.RegisterBlockEntityClass("BlockEntityECable", typeof(BlockEntityECable));

        api.RegisterBlockClass("BlockESwitch", typeof(BlockESwitch));

        api.RegisterBlockClass("BlockEHorn", typeof(BlockEHorn));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEHorn", typeof(BEBehaviorEHorn));
        api.RegisterBlockEntityClass("BlockEntityEHorn", typeof(BlockEntityEHorn));

        api.RegisterBlockClass("BlockEAccumulator", typeof(BlockEAccumulator));
        api.RegisterBlockEntityClass("BlockEntityEAccumulator", typeof(BlockEntityEAccumulator));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEAccumulator", typeof(BEBehaviorEAccumulator));

        api.RegisterBlockClass("BlockELamp", typeof(BlockELamp));
        api.RegisterBlockClass("BlockESmallLamp", typeof(BlockESmallLamp));

        api.RegisterBlockEntityClass("BlockEntityELamp", typeof(BlockEntityELamp));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorELamp", typeof(BEBehaviorELamp));

        api.RegisterBlockClass("BlockConnector", typeof(BlockConnector));
        api.RegisterBlockEntityClass("BlockEntityConnector", typeof(BlockEntityEConnector));

        api.RegisterBlockClass("BlockECharger", typeof(BlockECharger));
        api.RegisterBlockEntityClass("BlockEntityECharger", typeof(BlockEntityECharger));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorECharger", typeof(BEBehaviorECharger));

        api.RegisterBlockClass("BlockEStove", typeof(BlockEStove));
        api.RegisterBlockEntityClass("BlockEntityEStove", typeof(BlockEntityEStove));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEStove", typeof(BEBehaviorEStove));

        api.RegisterBlockClass("BlockEFreezer", typeof(BlockEFreezer));
        api.RegisterBlockEntityClass("BlockEntityEFreezer", typeof(BlockEntityEFreezer));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEFreezer", typeof(BEBehaviorEFreezer));

        api.RegisterBlockClass("BlockEOven", typeof(BlockEOven));
        api.RegisterBlockEntityClass("BlockEntityEOven", typeof(BlockEntityEOven));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEOven", typeof(BEBehaviorEOven));

        api.RegisterBlockClass("BlockEMotorTier1", typeof(BlockEMotorTier1));
        api.RegisterBlockClass("BlockEMotorTier2", typeof(BlockEMotorTier2));
        api.RegisterBlockClass("BlockEMotorTier3", typeof(BlockEMotorTier3));
        api.RegisterBlockEntityClass("BlockEntityEMotor", typeof(BlockEntityEMotor));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEMotorTier1", typeof(BEBehaviorEMotorTier1));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEMotorTier2", typeof(BEBehaviorEMotorTier2));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEMotorTier3", typeof(BEBehaviorEMotorTier3));

        api.RegisterBlockClass("BlockEGeneratorTier1", typeof(BlockEGeneratorTier1));
        api.RegisterBlockClass("BlockEGeneratorTier2", typeof(BlockEGeneratorTier2));
        api.RegisterBlockClass("BlockEGeneratorTier3", typeof(BlockEGeneratorTier3));
        api.RegisterBlockEntityClass("BlockEntityEGenerator", typeof(BlockEntityEGenerator));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEGeneratorTier1", typeof(BEBehaviorEGeneratorTier1));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEGeneratorTier2", typeof(BEBehaviorEGeneratorTier2));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorEGeneratorTier3", typeof(BEBehaviorEGeneratorTier3));

        api.RegisterBlockEntityBehaviorClass("ElectricityAddon", typeof(BEBehaviorElectricityAddon));

        api.RegisterItemClass("EChisel", typeof(EChisel));
        api.RegisterItemClass("EAxe", typeof(EAxe));
        api.RegisterItemClass("EDrill", typeof(EDrill));
        api.RegisterItemClass("EArmor", typeof(EArmor));
        api.RegisterItemClass("EWeapon", typeof(EWeapon));
        api.RegisterItemClass("EShield", typeof(EShield));

        api.Event.RegisterGameTickListener(this.OnGameTick, 500);

        if (api.ModLoader.IsModEnabled("combatoverhaul"))
            combatoverhaul = true;
    }


    //в цепи изменения, значит обновить все соединения
    public bool Update(BlockPos position, Facing facing, float[] setEparams)
    {



        if (!this.parts.TryGetValue(position, out var part))     //смотрим, есть ли такой элемент уже в этом блоке
        {
            if (facing == Facing.None)
            {
                return false;
            }

            part = this.parts[position] = new NetworkPart(position);   //если нет, то создаем новый
        }



        var addedConnections = ~part.Connection & facing;      // вычисляет, что добавилось
        var removedConnections = part.Connection & ~facing;    // вычисляет, что убавилось


        //if (facing == part.Connection)       //если соединения совпадают и параметры соединения, то зачем вызывали?
        //{
        //    return false;
        //}


        part.Connection = facing;                              // раз уж просят, применим направления

        this.AddConnections(ref part, addedConnections, setEparams);         // добавляем новое соединение
        this.RemoveConnections(ref part, removedConnections);  // убираем соединение

        if (part.Connection == Facing.None)                    // если направлений в блоке не осталось, то
        {
            this.parts.Remove(position);                       // вообще удаляем этот элемент из системы
        }

        //тут очистка всех элементов цепи 
        Cleaner(false);


        return true;
    }

    //удаляет элемент в этом блоке
    public void Remove(BlockPos position)
    {
        if (this.parts.TryGetValue(position, out var part))
        {
            this.parts.Remove(position);
            this.RemoveConnections(ref part, part.Connection);
        }
    }


    public void Cleaner(bool all = false)
    {
        //тут очистка пакетов в parts c током и запросами 
        foreach (var network in this.networks)              //каждую сеть считаем
        {
            foreach (var pos in network.PartPositions)              //каждую позицию подчищаем
            {
                if (this.parts[pos].eparams != null)
                    this.parts[pos].eparams[6] = 0;
                else
                    this.parts[pos].eparams = new float[7];
            }

            if (all)
            {
                this.consumers.Clear();
                foreach (var consumer in network.Consumers.Select(electricConsumer => new Consumer(electricConsumer)))  //выбираем всех потребителей из этой сети
                {
                    this.consumers.Add(consumer);      //создаем список с потребителями
                }

                foreach (var consumer in this.consumers)     //работаем со всеми потребителями в этой сети
                {
                    
                    consumer.ElectricConsumer.Consume_receive(0.0F);      //обнуляем 
                    var varr = consumer.ElectricConsumer.Consume_request();      //вызываем, чтобы обновить ентити

                }

                this.producers.Clear();

                foreach (var producer in network.Producers.Select(electricProducer => new Producer(electricProducer)))  //выбираем всех производителей из этой сети
                {
                    this.producers.Add(producer);      //создаем список с производителями
                }

                foreach (var producer in this.producers)     //работаем со всеми производителями в этой сети
                {
                    producer.ElectricProducer.Produce_order(0.0F);                               //обнуляем
                    var varr = producer.ElectricProducer.Produce_give();                           //вызываем, чтобы обновить ентити
                }

            }
        }
    }


    //просчет сетей в этом тике
    private void OnGameTick(float _)
    {
        //var accumulators = new List<IElectricAccumulator>();
        speedOfElectricity = 1;                                 //временно тут
        while (speedOfElectricity >= 1)
        {
            foreach (var network in this.networks)              //каждую сеть считаем
            {
                Cleaner();                                      //обязательно чистим eparams


                this.consumers.Clear();                         //очистка списка всех потребителей, потому как для каждой network список свой


                foreach (var consumer in network.Consumers.Select(electricConsumer => new Consumer(electricConsumer)))  //выбираем всех потребителей из этой сети
                {
                    this.consumers.Add(consumer);      //создаем список с потребителями
                }


                // работаем с потребителями цепи
                BlockPos[] startPositions = null;

                foreach (var consumer in this.consumers)     //работаем со всеми потребителями в этой сети
                {
                    float requestedEnergy = consumer.ElectricConsumer.Consume_request();      //этому потребителю нужно столько энергии

                    var consumPos = consumer.ElectricConsumer.Pos;
                    startPositions = startPositions.AddToArray<BlockPos>(consumPos);     //сохраняем начальные позиции потребителей
                    if (this.parts.TryGetValue(consumPos, out var part))
                    {

                        float recieveEnergy = Math.Min(this.parts[consumPos].eparams[1], requestedEnergy); //выдаем энергии сколько есть сейчас внутри
                        this.parts[consumPos].eparams[1] -= recieveEnergy;                                 //обновляем пакет
                        this.parts[consumPos].eparams[6] += requestedEnergy;                                //делаем в линию запрос необходимой энергии
                        consumer.ElectricConsumer.Consume_receive(recieveEnergy);                          //выдаем потребителю

                    }
                }

                //if (startPositions == null)                                                         //если нет потребителей, то зачем считать вообще?
                //    return;

                if (startPositions != null)
                {
                    var tree = ChainTreeBuilder.BuildTree(network, startPositions);                     //строит дерево для цепи, где в корнях потребители

                    // работаем с деревом
                    for (int level = 0; level < tree.Count; level++)                                    //перебор по уровням удаления
                    {
                        //Console.WriteLine($"Level {level}:");
                        foreach (var node in tree[level])                                               //перебор по нодам на этом уровне
                        {
                            if (this.parts.TryGetValue(node.Position, out var part))                        //извлекаем элемент цепи нода
                            {
                                if (node.Children.Count == 0)                                                 //в этом ноде нет соседей - идем к следующему
                                    continue;

                                float requestedEnergy = part.eparams[6];                                    //сколько же нужно энергии этому ноду?

                                if (requestedEnergy == 0)                                                 //в этом ноде нет потребления? идем к следующему
                                    continue;

                                float avalaibleEnergy = 0;

                                var avalaibleChildren = new float[node.Children.Count];                       //храним доступные энергии соседей
                                int k = 0;
                                //считаем сколько у соседей нода доступные энергии
                                foreach (var child in node.Children)                                        //перебор по соседям, конкретно этого нода
                                {
                                    if (this.parts.TryGetValue(child.Position, out part))
                                    {
                                        avalaibleEnergy += part.eparams[1];
                                        avalaibleChildren[k] = part.eparams[1];
                                        k++;
                                    }
                                }

                                if (avalaibleEnergy < requestedEnergy || avalaibleEnergy == 0) //если энергии у соседей меньше, чем надо
                                {
                                    this.parts[node.Position].eparams[1] += avalaibleEnergy; //отдаем ноду сразу все доступное
                                    this.parts[node.Position].eparams[6] -= avalaibleEnergy; //уменьшаем ноду запрос

                                    //получаем энергию и обновляем пакеты
                                    foreach (var child in node.Children)                                        //перебор по соседям, конкретно этого нода
                                    {
                                        //this.parts[child.Position].eparams[6] += requestedEnergy/ (float)node.Children.Count; //одинаковые запросы детям
                                        this.parts[child.Position].eparams[1] = 0;                                              //мы и так все забрали
                                        this.parts[child.Position].eparams[6] += requestedEnergy;                               //просим у детей по максимуму

                                    }
                                }
                                if (avalaibleEnergy >= requestedEnergy)
                                {
                                    this.parts[node.Position].eparams[1] += requestedEnergy; //отдаем ноду сразу все запрашиваемое
                                    this.parts[node.Position].eparams[6] -= requestedEnergy; //уменьшаем ноду запрос
                                    k = 0;

                                    //получаем энергию и обновляем пакеты
                                    foreach (var child in node.Children)                                        //перебор по соседям, конкретно этого нода
                                    {


                                        this.parts[child.Position].eparams[1] -= requestedEnergy * avalaibleChildren[k] / avalaibleEnergy; //забираем энергию пропорционально доступности                                           //мы и так все забрали

                                        this.parts[child.Position].eparams[6] += requestedEnergy * avalaibleChildren[k] / avalaibleEnergy; //запрос пропорционально потреблению
                                        k++;
                                    }
                                }
                            }

                        }
                    }
                }
                this.producers.Clear();                         //очистка списка всех производителей, потому как для каждой network список свой

                foreach (var producer in network.Producers.Select(electricProducer => new Producer(electricProducer)))  //выбираем всех производителей из этой сети
                {
                    this.producers.Add(producer);      //создаем список с производителями
                }

                if (startPositions != null)
                {
                    foreach (var producer in this.producers)     //работаем со всеми производителями в этой сети
                    {
                        var producePos = producer.ElectricProducer.Pos;
                        if (this.parts.TryGetValue(producePos, out var part))
                        {
                            float orderEnergy = this.parts[producePos].eparams[6];                              //берем запрос энергии
                            producer.ElectricProducer.Produce_order(orderEnergy);                               //передаем запрос производителю
                            float giveEnergy = producer.ElectricProducer.Produce_give();                        //проивзодитель выдает энергию
                            this.parts[producePos].eparams[1] += giveEnergy;                                     //обновляем пакет
                            this.parts[producePos].eparams[6] -= giveEnergy;                                    //обновляем запрос необходимой энергии в линии

                        }

                    }

                }
                else
                {
                    foreach (var producer in this.producers)     //работаем со всеми производителями в этой сети
                    {
                        var producePos = producer.ElectricProducer.Pos;
                        if (this.parts.TryGetValue(producePos, out var part))
                        {                            
                            producer.ElectricProducer.Produce_order(0.0F);                               //передаем запрос производителю
                            float giveEnergy = producer.ElectricProducer.Produce_give();                 //проивзодитель выдает энергию                            
                        }

                    }
                }

                /*

                                ////////////////////////////////////////////////////////////////////
                                this.consumers.Clear();           //очистка всех потребителей

                                int production = network.Producers.Sum(producer => producer.Produce());   //собирает сумму производства всей энергии в цепи

                                int totalRequiredEnergy = 0;    //необходимо энергии потребителям

                                foreach (var consumer in network.Consumers.Select(electricConsumer => new Consumer(electricConsumer)))  //выбираем всех потребителей из этйо сети
                                {
                                    totalRequiredEnergy += consumer.Consumption.Max;
                                    this.consumers.Add(consumer);
                                }

                                if (production < totalRequiredEnergy)   //если производится меньше, чем потребляется
                                {
                                    do
                                    {
                                        accumulators.Clear();
                                        accumulators.AddRange(network.Accumulators.Where(accumulator => accumulator.GetCapacity() > 0));

                                        if (accumulators.Count > 0)   //есть ли подключенные аккумуляторы в этой цепи
                                        {
                                            int rest = (totalRequiredEnergy - production) / accumulators.Count;

                                            if (rest == 0)
                                            {
                                                break;
                                            }

                                            foreach (var accumulator in accumulators)
                                            {
                                                var capacity = Math.Min(accumulator.GetCapacity(), rest);

                                                if (capacity > 0)
                                                {
                                                    production += capacity;
                                                    accumulator.Release(capacity);
                                                }
                                            }
                                        }
                                    } while (accumulators.Count > 0 && totalRequiredEnergy - production > 0);
                                }

                                var availableEnergy = production;

                                var activeConsumers = this.consumers
                                    .OrderBy(consumer => consumer.Consumption.Min)
                                    .GroupBy(consumer => consumer.Consumption.Min)
                                    .Where(
                                        grouping =>
                                        {
                                            var range = grouping.First().Consumption;
                                            var totalMinConsumption = range.Min * grouping.Count();

                                            if (totalMinConsumption <= availableEnergy)
                                            {
                                                availableEnergy -= totalMinConsumption;

                                                foreach (var consumer in grouping)
                                                {
                                                    consumer.GivenEnergy += range.Min;
                                                }

                                                return true;
                                            }

                                            return false;
                                        }
                                    )
                                    .SelectMany(grouping => grouping)
                                    .ToArray();

                                int requiredEnergy = int.MaxValue;

                                while (availableEnergy > 0 && requiredEnergy != 0)
                                {
                                    requiredEnergy = 0;

                                    var dissatisfiedConsumers = activeConsumers
                                        .Where(consumer => consumer.Consumption.Max > consumer.GivenEnergy)
                                        .ToArray();

                                    var numberOfDissatisfiedConsumers = dissatisfiedConsumers.Count();

                                    if (numberOfDissatisfiedConsumers == 0)
                                    {
                                        break;
                                    }

                                    int distributableEnergy = Math.Max(1, availableEnergy / numberOfDissatisfiedConsumers);

                                    foreach (var consumer in dissatisfiedConsumers)
                                    {
                                        if (availableEnergy == 0)
                                        {
                                            break;
                                        }

                                        var giveableEnergy = Math.Min(distributableEnergy, consumer.Consumption.Max - consumer.GivenEnergy);

                                        availableEnergy -= giveableEnergy;
                                        consumer.GivenEnergy += giveableEnergy;

                                        requiredEnergy += consumer.Consumption.Max - consumer.GivenEnergy;
                                    }
                                }

                                foreach (var consumer in this.consumers)
                                {
                                    consumer.ElectricConsumer.Consume(consumer.GivenEnergy);
                                }

                                network.Production = production;
                                network.Consumption = production - availableEnergy;

                                StoreOverflowInAccumulators(network);

                                */
            }


            speedOfElectricity--;   //временно тут??
        }

    }

    private static void StoreOverflowInAccumulators(Network network)
    {
        var availableEnergy = network.Overflow = network.Production - network.Consumption;
        var desiredEnergy = 0; // Energy the Accumulators can store
        var accumulatorEnergy = new List<AccumulatorTuple>();

        // Build a list of accumulators with available capacity
        foreach (var accumulator in network.Accumulators)
        {
            var tuple = new AccumulatorTuple(
                accumulator,
                accumulator.GetMaxCapacity(),
                accumulator.GetCapacity()
            );

            if (tuple.AvailableCapacity <= 0)
            {
                continue;
            }

            accumulatorEnergy.Add(tuple);
            desiredEnergy += tuple.AvailableCapacity;
        }

        // Nothing to do
        if (desiredEnergy <= 0)
        {
            return;
        }

        // Sort accumulators by available capacity (Important for Remainder Algorithm)
        accumulatorEnergy.Sort((a, b) => a.AvailableCapacity.CompareTo(b.AvailableCapacity));

        // Available Energy is less than desired energy
        // So we need to evenly distribute the available energy to all accumulators
        if (availableEnergy < desiredEnergy)
        {
            var count = accumulatorEnergy.Count;

            foreach (var tuple in accumulatorEnergy)
            {
                // Calculate the average energy to give to each accumulator (this will account for remainders)
                var avgEnergyToGiveAccumulator = availableEnergy / count;

                // Minimum of the average energy or just the available capacity
                var energy = Math.Min(tuple.AvailableCapacity, avgEnergyToGiveAccumulator);

                // Update loop values
                availableEnergy -= energy;
                count--;

                // Update the Accumulator and Network
                tuple.Accumulator.Store(energy);
                network.Consumption += energy;
            }
        }

        // Available Energy is greater than desired energy
        // So fill up the accumulators with the most available capacity
        else if (availableEnergy >= desiredEnergy)
        {
            foreach (var tuple in accumulatorEnergy)
            {
                // Add All the available capacity to the accumulator
                var energy = tuple.AvailableCapacity;
                tuple.Accumulator.Store(energy);
                network.Consumption += energy;
            }
        }

        network.Overflow = network.Production - network.Consumption;
    }

    private Network MergeNetworks(HashSet<Network> networks)
    {
        Network? outNetwork = null;

        foreach (var network in networks)
        {
            if (outNetwork == null || outNetwork.PartPositions.Count < network.PartPositions.Count)
            {
                outNetwork = network;
            }
        }

        if (outNetwork != null)
        {
            foreach (var network in networks)
            {
                if (outNetwork == network)
                {
                    continue;
                }

                foreach (var position in network.PartPositions)
                {
                    var part = this.parts[position];

                    foreach (var face in BlockFacing.ALLFACES)
                    {
                        if (part.Networks[face.Index] == network)
                        {
                            part.Networks[face.Index] = outNetwork;
                        }
                    }

                    if (part.Consumer is { } consumer)
                    {
                        outNetwork.Consumers.Add(consumer);
                    }

                    if (part.Producer is { } producer)
                    {
                        outNetwork.Producers.Add(producer);
                    }

                    if (part.Accumulator is { } accumulator)
                    {
                        outNetwork.Accumulators.Add(accumulator);
                    }

                    outNetwork.PartPositions.Add(position);
                }

                network.PartPositions.Clear();
                this.networks.Remove(network);
            }
        }

        return outNetwork ?? this.CreateNetwork();
    }

    //удаляем сеть
    private void RemoveNetwork(ref Network network)
    {
        var partPositions = new BlockPos[network.PartPositions.Count];
        network.PartPositions.CopyTo(partPositions);
        this.networks.Remove(network);                                  //удаляем цепь из списка цепей

        foreach (var position in partPositions)                         //перебираем по всем бывшим элементам этой цепи
        {
            if (this.parts.TryGetValue(position, out var part))         //есть такое соединение?
            {
                foreach (var face in BlockFacing.ALLFACES)              //перебираем по всем 6 направлениям
                {
                    if (part.Networks[face.Index] == network)           //если нашли привязку к этой цепи
                    {
                        part.Networks[face.Index] = null;               //обнуляем ее
                    }
                }
            }
        }

        foreach (var position in partPositions)                                 //перебираем по всем бывшим элементам этой цепи
        {
            if (this.parts.TryGetValue(position, out var part))                 //есть такое соединение?
            {
                this.AddConnections(ref part, part.Connection, null);     //добавляем соединения???
            }
        }
    }


    //создаем новую цепь
    private Network CreateNetwork()
    {
        var network = new Network();
        this.networks.Add(network);

        return network;
    }



    private void AddConnections(ref NetworkPart part, Facing addedConnections, float[] setEparams)
    {
        //if (addedConnections == Facing.None)
        //{
        //    return;
        //}

        var networksByFace = new[]
        {
            new HashSet<Network>(),
            new HashSet<Network>(),
            new HashSet<Network>(),
            new HashSet<Network>(),
            new HashSet<Network>(),
            new HashSet<Network>()
        };

        foreach (var face in FacingHelper.Faces(part.Connection))           //ищет к каким сетям эти провода могут относиться
        {
            networksByFace[face.Index].Add(part.Networks[face.Index] ?? this.CreateNetwork());
        }

        foreach (var direction in FacingHelper.Directions(addedConnections))
        {
            var directionFilter = FacingHelper.FromDirection(direction);
            var neighborPosition = part.Position.AddCopy(direction);

            if (this.parts.TryGetValue(neighborPosition, out var neighborPart))         //проверяет, если в той стороне сосед
            {
                foreach (var face in FacingHelper.Faces(addedConnections & directionFilter))
                {
                    if ((neighborPart.Connection & FacingHelper.From(face, direction.Opposite)) != 0)
                    {
                        if (neighborPart.Networks[face.Index] is { } network)
                        {
                            networksByFace[face.Index].Add(network);

                        }
                    }

                    if ((neighborPart.Connection & FacingHelper.From(direction.Opposite, face)) != 0)
                    {
                        if (neighborPart.Networks[direction.Opposite.Index] is { } network)
                        {
                            networksByFace[face.Index].Add(network);
                        }
                    }
                }
            }
        }

        foreach (var direction in FacingHelper.Directions(addedConnections))
        {
            var directionFilter = FacingHelper.FromDirection(direction);

            foreach (var face in FacingHelper.Faces(addedConnections & directionFilter))
            {
                var neighborPosition = part.Position.AddCopy(direction).AddCopy(face);

                if (this.parts.TryGetValue(neighborPosition, out var neighborPart))
                {
                    if ((neighborPart.Connection & FacingHelper.From(direction.Opposite, face.Opposite)) != 0)
                    {
                        if (neighborPart.Networks[direction.Opposite.Index] is { } network)
                        {
                            networksByFace[face.Index].Add(network);
                        }
                    }

                    if ((neighborPart.Connection & FacingHelper.From(face.Opposite, direction.Opposite)) != 0)
                    {
                        if (neighborPart.Networks[face.Opposite.Index] is { } network)
                        {
                            networksByFace[face.Index].Add(network);
                        }
                    }
                }
            }
        }

        foreach (var face in FacingHelper.Faces(part.Connection))
        {
            var network = this.MergeNetworks(networksByFace[face.Index]);

            if (part.Consumer is { } consumer)
            {
                network.Consumers.Add(consumer);
            }

            if (part.Producer is { } producer)
            {
                network.Producers.Add(producer);
            }

            if (part.Accumulator is { } accumulator)
            {
                network.Accumulators.Add(accumulator);
            }

            network.PartPositions.Add(part.Position);

            part.Networks[face.Index] = network;             //присваиваем в этой точке эту цепь
            if (setEparams != null)
                part.eparams = setEparams;       //аналогично с параметрами электричества
        }

        foreach (var direction in FacingHelper.Directions(part.Connection))
        {
            var directionFilter = FacingHelper.FromDirection(direction);

            foreach (var face in FacingHelper.Faces(part.Connection & directionFilter))
            {
                if ((part.Connection & FacingHelper.From(direction, face)) != 0)
                {
                    if (part.Networks[face.Index] is { } network1 && part.Networks[direction.Index] is { } network2)
                    {
                        var networks = new HashSet<Network>
                        {
                            network1, network2
                        };

                        this.MergeNetworks(networks);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
        }
    }

    private void RemoveConnections(ref NetworkPart part, Facing removedConnections)
    {
        //if (removedConnections == Facing.None)
        //{
        //    return;
        //}

        foreach (var blockFacing in FacingHelper.Faces(removedConnections))
        {
            if (part.Networks[blockFacing.Index] is { } network)
            {
                this.RemoveNetwork(ref network);
            }
        }
    }

    public void SetConsumer(BlockPos position, IElectricConsumer? consumer)
    {
        if (!this.parts.TryGetValue(position, out var part))
        {
            if (consumer == null)
            {
                return;
            }

            part = this.parts[position] = new NetworkPart(position);
        }

        if (part.Consumer != consumer)
        {
            foreach (var network in part.Networks)
            {
                if (part.Consumer is not null)
                {
                    network?.Consumers.Remove(part.Consumer);
                }

                if (consumer is not null)
                {
                    network?.Consumers.Add(consumer);
                }
            }

            part.Consumer = consumer;
        }
    }



    public void SetProducer(BlockPos position, IElectricProducer? producer)
    {
        if (!this.parts.TryGetValue(position, out var part))
        {
            if (producer == null)
            {
                return;
            }

            part = this.parts[position] = new NetworkPart(position);
        }

        if (part.Producer != producer)
        {
            foreach (var network in part.Networks)
            {
                if (part.Producer is not null)
                {
                    network?.Producers.Remove(part.Producer);
                }

                if (producer is not null)
                {
                    network?.Producers.Add(producer);
                }
            }

            part.Producer = producer;
        }
    }

    public void SetAccumulator(BlockPos position, IElectricAccumulator? accumulator)
    {
        if (!this.parts.TryGetValue(position, out var part))
        {
            if (accumulator == null)
            {
                return;
            }

            part = this.parts[position] = new NetworkPart(position);
        }

        if (part.Accumulator != accumulator)
        {
            foreach (var network in part.Networks)
            {
                if (part.Accumulator is not null)
                {
                    network?.Accumulators.Remove(part.Accumulator);
                }

                if (accumulator is not null)
                {
                    network?.Accumulators.Add(accumulator);
                }
            }

            part.Accumulator = accumulator;
        }
    }

    //собирает информацию по цепям
    public NetworkInformation GetNetworks(BlockPos position, Facing facing)
    {
        var result = new NetworkInformation();

        if (this.parts.TryGetValue(position, out var part))
        {
            var networks = new HashSet<Network>();

            foreach (var blockFacing in FacingHelper.Faces(facing))
            {
                if (part.Networks[blockFacing.Index] is { } networkk)
                {
                    networks.Add(networkk);                                     //выдаем найденную цепь
                    result.Facing |= FacingHelper.FromFace(blockFacing);        //выдаем ее направления?
                    result.eParamsInNetwork = part.eparams;                     //выдаем ее текущие параметры
                }
            }

            foreach (var network in networks)
            {
                result.NumberOfBlocks += network.PartPositions.Count;
                result.NumberOfConsumers += network.Consumers.Count;
                result.NumberOfProducers += network.Producers.Count;
                result.NumberOfAccumulators += network.Accumulators.Count;
                result.Production += network.Production;
                result.Consumption += network.Consumption;
                result.Overflow += network.Overflow;
            }
        }

        return result;
    }

    // Local Struct to Store Accumulator Data for Overflow
    // Note: Will NOT Update Capacity Fields
    private struct AccumulatorTuple
    {
        public readonly IElectricAccumulator Accumulator; // Accumulator Object
        public readonly int MaxCapacity; // Max Capacity of Accumulator
        public readonly int CurrentCapacity; // Current Capacity of Accumulator
        public readonly int AvailableCapacity; // Available Capacity of Accumulator

        public AccumulatorTuple(IElectricAccumulator accumulator, int maxCapacity, int currentCapacity)
        {
            this.Accumulator = accumulator;
            this.MaxCapacity = maxCapacity;
            this.CurrentCapacity = currentCapacity;
            this.AvailableCapacity = this.MaxCapacity - this.CurrentCapacity;
        }
    }
}

public class Network
{
    public readonly HashSet<IElectricAccumulator> Accumulators = new();
    public readonly HashSet<IElectricConsumer> Consumers = new();
    public readonly HashSet<BlockPos> PartPositions = new();
    public readonly HashSet<IElectricProducer> Producers = new();

    public int Consumption;
    public int Overflow;
    public int Production;
}

internal class NetworkPart                       //элемент цепи
{
    public readonly Network?[] Networks = {      //в какие стороны провода направлены
            null,
            null,
            null,
            null,
            null,
            null
        };

    public float[] eparams = null;         //похоже тут хватит одного

    /*
        {
            0,                                  //максимальный размер пакета энергии (максим ток), которое может пройти по одной линии этого элемента цепи
            0,                                  //текущий размер энергии в пакете/ах (ток), который проходит в элементе цепи
            0,                                  //потери энергии в элементе цепи
            0,                                  //количество линий элемента цепи/провода
            0,                                  //напряжение макс (возможно будет про запас)
            0,                                  //сгорел или нет
            0                                   //сколько этот элемент цепи хочет энергии
        },

    */

    public readonly BlockPos Position;           //позиция
    public IElectricAccumulator? Accumulator;    //поведение аккумулятора?
    public Facing Connection = Facing.None;
    public IElectricConsumer? Consumer;          //поведение потребителя?
    public IElectricProducer? Producer;          //поведение источнрка?

    public NetworkPart(BlockPos position)
    {
        this.Position = position;
    }
}

public class NetworkInformation             //информация о конкретной цепи
{
    public int Consumption;                 //потреблении
    public Facing Facing = Facing.None;     //направлений
    public int NumberOfAccumulators;        //аккумуляторах
    public int NumberOfBlocks;              //блоков
    public int NumberOfConsumers;           //потребителй
    public int NumberOfProducers;           //источников
    public int Overflow;                    //перепроизводстве
    public int Production;                  //проивзодстве
    public float[] eParamsInNetwork = new float[7];       //параметрах конкретно этого блока в этой цепи
}

internal class Consumer
{
    public readonly ConsumptionRange Consumption;       //возможно удалим
    public readonly IElectricConsumer ElectricConsumer;
    public int GivenEnergy;                               //возможно удалим

    public Consumer(IElectricConsumer electricConsumer)
    {
        this.ElectricConsumer = electricConsumer;
        this.Consumption = electricConsumer.ConsumptionRange;   //возможно удалим
    }
}

internal class Producer
{
    public readonly IElectricProducer ElectricProducer;

    public Producer(IElectricProducer electricProducer)
    {
        this.ElectricProducer = electricProducer;
    }
}