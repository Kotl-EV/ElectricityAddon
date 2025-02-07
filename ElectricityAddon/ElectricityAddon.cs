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
using ElectricityUnofficial.Utils;

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
    private readonly List<Consumer> consumers2 = new();
    private readonly List<Consumer> consumers3 = new();
    private readonly List<Producer> producers = new();
    private readonly List<Producer> producers2 = new();
    private readonly List<Accumulator> accums = new();
    private readonly List<Accumulator> accums2 = new();
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


    /// <summary>
    /// D цепи изменения, значит нужно обновить все соединения
    /// </summary>
    /// <param name="position"></param>
    /// <param name="facing"></param>
    /// <param name="setEparams"></param>
    /// <returns></returns>
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



    /// <summary>
    /// Удаляет элемент цепи в этом блоке
    /// </summary>
    /// <param name="position"></param>
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
                if (this.parts[pos].eparams != null && this.parts[pos].eparams.Length>0)   //бывает всякое
                    this.parts[pos].eparams[6] = 0;         //сделать eparams лампам
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




    /// <summary>
    /// Решается задача распределения энергии
    /// </summary>
    private void logisticalTask(Network network, List<BlockPos>  consumerPositions, List<float> consumerRequests, List<BlockPos> producerPositions, List<float> producerGive, ref Simulation sim)
    { 
        //ищем все пути и расстояния
        var pathFinder = new PathFinder();

        float[][] distances = new float[consumerPositions.Count][];                    //сохраняем сюда расстояния от всех потребителей ко всем источникам 
        List<BlockPos>[][] paths = new List<BlockPos>[consumerPositions.Count][];      //сохраняем сюда пути от всех потребителей ко всем источникам
        int i = 0, j;                                                                   //индексы -__-

        foreach (var cP in consumerPositions)                                           //работаем со всеми потребителями в этой сети
        {
            j = 0;
            //var start = cP;
            
            distances[i] = new float[producerPositions.Count];
            paths[i] = new List<BlockPos>[producerPositions.Count];

            foreach (var pP in producerPositions)                                    //работаем со всеми источниками в этой сети
            {
                //var end = producerPositions[j];

                //var path = pathFinder.FindShortestPath(start, end, network.PartPositions);  //извлекаем путь и расстояние

                var path = pathFinder.FindShortestPath(cP, pP, network.PartPositions);       //извлекаем путь и расстояние

                if (path == null)                                                           //Путь не найден!
                    return;                                                                 //возможно потом continue тут должно быть

                distances[i][j] = path.Count;                                               //сохраняем длину пути
                paths[i][j] = path;                                                         //сохраняем пути
                j++;
            }

            i++;
        }


        //Распределение запросов и энергии
        //Инициализация задачи логистики энергии: магазины - покупатели
        Store[] stores = new Store[producerPositions.Count];
        for (j = 0; j < producerPositions.Count; j++)                 //работаем со всеми источниками в этой сети                    
        {
            stores[j] = new Store(j + 1, producerGive[j]);            //создаем магазин со своими запасами
        }

        Customer[] customers = new Customer[consumerPositions.Count];
        for (i = 0; i < consumerPositions.Count; i++)                         //работаем со всеми потребителями в этой сети
        {
            Dictionary<Store, float> distFromCustomerToStore = new Dictionary<Store, float>();
            for (j = 0; j < producerPositions.Count; j++)                     //работаем со всеми генераторами в этой сети                    
            {
                distFromCustomerToStore.Add(stores[j], distances[i][j]);       //записываем расстояния до каждого магазина от этого потребителя
            }

            customers[i] = new Customer(j + 1, consumerRequests[i], distFromCustomerToStore);        //создаем покупателя со своими потребностями
        }





        //Собственно сама реализация "жадного алгоритма" ---------------------------------------------------------------------------------------//
        List<Customer> Customers = new List<Customer>();
        List<Store> Stores = new List<Store>();
        
        sim.Stores.AddRange(stores);
        sim.Customers.AddRange(customers);

        sim.Run();                  //распределение происходит тут

    }






    /// <summary>
    /// Просчет сетей в этом тике
    /// </summary>
    private void OnGameTick(float _)
    {
        //var accumulators = new List<IElectricAccumulator>();          //пригодится


        speedOfElectricity = 1;                                 //временно тут
        while (speedOfElectricity >= 1)
        {
            Cleaner();   //обязательно чистим eparams

            foreach (var network in this.networks)              //каждую сеть считаем
            {
                //Этап 1 - Очистка мусора---------------------------------------------------------------------------------------------//                                                    

                this.producers.Clear();                         //очистка списка всех производителей, потому как для каждой network список свой
                this.producers2.Clear();                        //очистка списка всех производителей, потому как для каждой network список свой
                this.consumers.Clear();                         //очистка списка всех потребителей, потому как для каждой network список свой
                this.consumers2.Clear();                        //очистка списка всех ненулевых потребителей, потому как для каждой network список свой
                this.consumers3.Clear();                        //очистка списка всех ненулевых потребителей после первого распределения, потому как для каждой network список свой
                this.accums.Clear();
                this.accums2.Clear();

                //Этап 2 - Сбор запросов от потребителей---------------------------------------------------------------------------------------//
                foreach (var consumer in network.Consumers.Select(electricConsumer => new Consumer(electricConsumer)))  //выбираем всех потребителей из этой сети
                {
                    this.consumers.Add(consumer);      //создаем список с потребителями
                }


                List<BlockPos> consumerPositions = new List<BlockPos>();
                List<float> consumerRequests = new List<float>();
                foreach (var consumer in this.consumers)     //работаем со всеми потребителями в этой сети
                {
                    float requestedEnergy = consumer.ElectricConsumer.Consume_request();      //этому потребителю нужно столько энергии
                    if (requestedEnergy == 0)                                                 //если ему не надо энергии, то смотрим следующего
                    {
                        continue;
                    }

                    this.consumers2.Add(consumer);                                            //добавляем в список ненулевых потребителей

                    var consumPos = consumer.ElectricConsumer.Pos;
                    consumerPositions.Add(consumPos);         //сохраняем позиции потребителей
                    consumerRequests.Add(requestedEnergy);    //сохраняем запросы потребителей                  

                }


                //Этап 3 - Сбор энергии с генераторов---------------------------------------------------------------------------------------------------//
                foreach (var producer in network.Producers.Select(electricProducer => new Producer(electricProducer)))  //выбираем всех генераторов из этой сети
                {
                    this.producers.Add(producer);      //создаем список с генераторами
                }


                List<BlockPos> producerPositions = new List<BlockPos>();
                List<float> producerGive = new List<float>();
                foreach (var producer in this.producers)     //работаем со всеми генераторами в этой сети
                {
                    float giveEnergy = producer.ElectricProducer.Produce_give();            //этот генератор выдал столько энергии
                    var producePos = producer.ElectricProducer.Pos;
                    producerPositions.Add(producePos);  //сохраняем позиции генераторов
                    producerGive.Add(giveEnergy);       //сохраняем выданную энергию генераторов 
                }


                foreach (var accum in network.Accumulators.Select(electricAccum => new Accumulator(electricAccum)))  //выбираем все аккумы в этой сети
                {
                    this.accums.Add(accum);      //создаем список с аккумами
                }
                
                
                foreach (var accum in this.accums)                                         //работаем со всеми аккумами в этой сети
                {
                    float giveEnergy = accum.ElectricAccum.canRelease();                      //этот аккум может выдать столько энергии
                    if (giveEnergy == 0)                                                   //если у этого аккума пусто
                        continue;

                    this.accums2.Add(accum);

                    var accumPos = accum.ElectricAccum.Pos;
                    producerPositions.Add(accumPos);    //сохраняем позиции аккумов
                    producerGive.Add(giveEnergy);       //сохраняем выданную энергию аккумов
                }





                //Этап 4 - Распределяем энергию ----------------------------------------------------------------------//                 
                var sim = new Simulation();  
                logisticalTask(network,consumerPositions,consumerRequests,producerPositions,producerGive, ref sim);




                //Этап  - Перенос энергии ---------------------------------------------------------------------------------------//




                  
                int i=0,j = 0;
                //Этап  - Получение энергии потребителями ---------------------------------------------------------------------------------------//

                // мгновенная выдача энергии по воздуху минуя провода
                i = 0;
                
                foreach (var consumer in this.consumers2)       //работаем со всеми потребителями в этой сети
                {
                    var totalGive = sim.Customers[i].Required - sim.Customers[i].Remaining;  //потребитель получил столько энергии                    
                    consumer.ElectricConsumer.Consume_receive(totalGive);                    //выдаем энергию потребителю 

                    i++;
                }


                //Этап  - Забираем у аккумов выданное ими ---------------------------------------------------------------------------------------//
                i = 0;
                foreach (var accum in this.accums2)       //работаем со всеми аккумами в этой сети
                {
                    if (sim.Stores[i+producers.Count].Stock < accum.ElectricAccum.canRelease())
                    {
                        accum.ElectricAccum.Release(accum.ElectricAccum.canRelease() - sim.Stores[i + producers.Count].Stock);   
                    }
                    i++;
                }





                //Этап  - Хотим зарядить аккумы  ---------------------------------------------------------------------------------------//
                this.accums2.Clear();

                List<BlockPos> consumer2Positions = new List<BlockPos>();
                List<float> consumer2Requests = new List<float>();
                foreach (var accum in this.accums)     //работаем со всеми потребителями в этой сети
                {
                    float requestedEnergy = accum.ElectricAccum.canStore();      //этот аккум может принять столько
                    if (requestedEnergy == 0)                                    //если ему не надо энергии, то смотрим следующего
                    {
                        continue;
                    }

                    this.accums2.Add(accum);                                     //добавляем в список ненулевых потребителей

                    var accumPos = accum.ElectricAccum.Pos;
                    consumer2Positions.Add(accumPos);         //сохраняем позиции потребителей
                    consumer2Requests.Add(requestedEnergy);    //сохраняем запросы потребителей                  

                }


                //Этап  - высасываем у генераторов остатки ---------------------------------------------------------------------------------------//
                List<BlockPos> producer2Positions = new List<BlockPos>();
                List<float> producer2Give = new List<float>();
                i = 0;
                foreach (var producer in this.producers)     //работаем со всеми генераторами в этой сети
                {
                    float giveEnergy = sim.Stores[i].Stock;            //этот генератор имеет столько
                    var producePos = producer.ElectricProducer.Pos;
                    producer2Positions.Add(producePos);  //сохраняем позиции генераторов
                    producer2Give.Add(giveEnergy);       //сохраняем выданную энергию генераторов 
                    i++;
                }


                //Этап  - Распределяем энергию снова ----------------------------------------------------------------------//                 
                var sim2 = new Simulation();
                logisticalTask(network, consumer2Positions, consumer2Requests, producer2Positions, producer2Give, ref sim2);





                //Этап  - Сообщение генераторам о нужном количестве энергии ---------------------------------------------------------------------------------------//

                j = 0;
                foreach (var producer in this.producers)     //работаем со всеми генераторами в этой сети
                {
                    var totalOrder = sim.Stores[j].totalRequest;  //у генератора все просили столько
                    var totalOrder2 = sim2.Stores[j].totalRequest;  //у генератора все просили столько

                    producer.ElectricProducer.Produce_order(totalOrder+ totalOrder2);   //говорим генератору сколько просят с него (нагрузка сети)

                    j++;
                }


                //Этап  - Заряжаем аккумы ---------------------------------------------------------------------------------------//
                i = 0;
                foreach (var accum in this.accums2)       //работаем со всеми аккумами в этой сети
                {                    
                    var totalGive = sim2.Customers[i].Required - sim2.Customers[i].Remaining;       //аккум получил столько энергии                    
                    accum.ElectricAccum.Store(totalGive);                                           //выдаем энергию аккумам 
                                        
                    i++;
                }


                //*/
                //учесть те генераторы, к которым не обратились, иначе они будут жрать механическую мощность зря???



            }

            speedOfElectricity--;   //временно тут
        }

    }



    /// <summary>
    /// Обьединение цепей
    /// </summary>
    /// <param name="networks"></param>
    /// <returns></returns>
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


    /// <summary>
    /// Cоздаем новую цепь
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Cобирает информацию по цепи
    /// </summary>
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

/// <summary>
/// Электрическая цепь
/// </summary>
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

/// <summary>
/// Один элемент цепи
/// </summary>
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

    public float[] eparams = null;              //похоже тут хватит одного

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

/// <summary>
/// Информация о конкретной сети
/// </summary>
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

/// <summary>
/// Потребитель энергии
/// </summary>
internal class Consumer
{
    public readonly ConsumptionRange Consumption;               //удалим!!!    
    

    public readonly IElectricConsumer ElectricConsumer;

    public Consumer(IElectricConsumer electricConsumer)
    {
        this.ElectricConsumer = electricConsumer;
        this.Consumption = electricConsumer.ConsumptionRange;   //удалим!!
    }
}

/// <summary>
/// Источник энергии
/// </summary>
internal class Producer
{
    public readonly IElectricProducer ElectricProducer;

    public Producer(IElectricProducer electricProducer)
    {
        this.ElectricProducer = electricProducer;
    }
}

/// <summary>
/// Аккумулятор энергии
/// </summary>
internal class Accumulator
{
    public readonly IElectricAccumulator ElectricAccum;

    public Accumulator(IElectricAccumulator electricAccum)
    {
        this.ElectricAccum = electricAccum;
    }
}