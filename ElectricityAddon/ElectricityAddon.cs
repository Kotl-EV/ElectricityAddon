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
using ElectricityAddon.Content.Block;
using ElectricityAddon.Content.Block.ESwitch;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using ElectricityAddon.Content.Block.ECable;
using Vintagestory.API.Config;



[assembly: ModDependency("game", "1.20.0")]
[assembly: ModInfo(
    "ElectricityAddon",
    "electricityaddon",
    Website = "https://github.com/Kotl-EV/ElectricityAddon",
    Description = "Brings electricity into the game!",
    Version = "0.0.15",
    Authors = new[] {
        "Kotl",
        "Tehtelev"
    }
)]

namespace ElectricityAddon;

public class ElectricityAddon : ModSystem
{
    private readonly List<Consumer> consumers = new();
    private readonly List<Consumer> consumers2 = new();
    private readonly List<Producer> producers = new();
    private readonly List<Producer> producers2 = new();
    private readonly List<Accumulator> accums = new();
    private readonly List<Accumulator> accums2 = new();
    private readonly HashSet<Network> networks = new();
    private readonly Dictionary<BlockPos, NetworkPart> parts = new(); //хранит все элементы всех цепей
    public static bool combatoverhaul = false;                        //установлен ли combatoverhaul
    public int speedOfElectricity = 1;                                //скорость электричетсва в проводах при одном обновлении сети (блоков в тик)
    public bool instant = false;                                      //расчет мгновенно?
    public bool AltPressed = false;                                   //зажата кнопка Alt
    private PathFinder pathFinder = new PathFinder();                 //инициализация модуля поиска путей
    private ICoreAPI api = null!;
    private ICoreClientAPI capi = null!;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        this.api = api;

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




    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        this.capi = api;
        RegisterAltKeys();

    }


    /// <summary>
    /// Отслеживает заэатую кнопку Alt
    /// </summary>
    /// <param name="comb"></param>
    /// <returns></returns>
    private bool OnAltKeyPressed(KeyCombination comb)
    {
        if (api.Side != EnumAppSide.Client)
            return false;

        AltPressed = true;

        return false;
    }

    /// <summary>
    /// Регистрируем кнопку для подробной информации о сети
    /// </summary>
    private void RegisterAltKeys()
    {
        base.Mod.Logger.VerboseDebug("AltPressForNetwork: hotkey handler for Alt");
        this.capi.Input.RegisterHotKey("AltPressForNetwork", "Output of detailed information about the network", GlKeys.Unknown, HotkeyType.CharacterControls, true);
        this.capi.Input.SetHotKeyHandler("AltPressForNetwork", OnAltKeyPressed);

    }



    /// <summary>
    /// D цепи изменения, значит нужно обновить все соединения
    /// </summary>
    /// <param name="position"></param>
    /// <param name="facing"></param>
    /// <param name="setEparams"></param>
    /// <returns></returns>
    public bool Update(BlockPos position, Facing facing, (EParams, int) setEparams, ref EParams[] Eparams)
    {
        //Eparams = null!;

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

        part.eparams = Eparams;
        part.Connection = facing;                              // раз уж просят, применим направления

        this.AddConnections(ref part, addedConnections, setEparams);         // добавляем новое соединение
        this.RemoveConnections(ref part, removedConnections);  // убираем соединение

        if (part.Connection == Facing.None)                    // если направлений в блоке не осталось, то
        {
            this.parts.Remove(position);                       // вообще удаляем этот элемент из системы
        }

        //тут очистка всех элементов цепи 
        Cleaner(false);

        Eparams = part.eparams;

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


    /// <summary>
    /// Чистка ненужно между прогонами расчета
    /// </summary>
    /// <param name="all"></param>
    public void Cleaner(bool all = false)
    {
        //тут очистка пакетов в parts c током и запросами 
        foreach (var network in this.networks)              //каждую сеть считаем
        {
            foreach (var pos in network.PartPositions)              //каждую позицию подчищаем
            {
                if (this.parts[pos].eparams != null && this.parts[pos].eparams.Length > 0)   //бывает всякое
                {
                    int i = 0;
                    foreach (var ams in this.parts[pos].eparams)
                    {

                        if (ams.Equals(default(EParams)))
                            this.parts[pos].eparams[i] = new EParams();
                        
                        i++;
                    }

                }
                else
                    this.parts[pos].eparams = new EParams[6]
                        {
                        new EParams(),
                        new EParams(),
                        new EParams(),
                        new EParams(),
                        new EParams(),
                        new EParams()
                        };

                if (this.parts[pos].energyPackets == null)   //бывает всякое
                    this.parts[pos].energyPackets = new List<energyPacket>();

                //чистим маркеры пакетам
                if (this.parts[pos].energyPackets.Count > 0)
                {
                    var copyEnergyPackets = this.parts[pos].energyPackets.ToList<energyPacket>();

                    foreach (var item in copyEnergyPackets)  //перебираем все пакеты в этой части
                    {
                        var item2 = new energyPacket();
                        item2 = item.DeepCopy();
                        item2.moved = false;
                        this.parts[pos].energyPackets.Remove(item);
                        this.parts[pos].energyPackets.Add(item2);
                       
                    }
                }



            }


        }
    }




    /// <summary>
    /// Решается задача распределения энергии
    /// </summary>
    private void logisticalTask(Network network, List<BlockPos> consumerPositions, List<float> consumerRequests,
        List<BlockPos> producerPositions, List<float> producerGive, ref Simulation sim, out List<BlockPos>[][] paths, out List<int>[][] facingFrom, out List<bool[]>[][] nowProcessedFaces)
    {
        //ищем все пути и расстояния
        float[][] distances = new float[consumerPositions.Count][];                     //сохраняем сюда расстояния от всех потребителей ко всем источникам 
        paths = new List<BlockPos>[consumerPositions.Count][];                          //сохраняем сюда пути от всех потребителей ко всем источникам
        facingFrom = new List<int>[consumerPositions.Count][];                          //сохраняем сюда грань следующей позиции от всех потребителей ко всем источникам
        nowProcessedFaces = new List<bool[]>[consumerPositions.Count][];                 //сохраняем сюда просчитанные грани


        int i = 0, j;                                                                   //индексы -__-
        foreach (var cP in consumerPositions)                                           //работаем со всеми потребителями в этой сети
        {
            j = 0;
            distances[i] = new float[producerPositions.Count];
            paths[i] = new List<BlockPos>[producerPositions.Count];
            facingFrom[i] = new List<int>[producerPositions.Count];
            nowProcessedFaces[i] = new List<bool[]>[producerPositions.Count];

            foreach (var pP in producerPositions)                                                   //работаем со всеми источниками в этой сети
            {

                var (buf1, buf2, buf3) = pathFinder.FindShortestPath(cP, pP, network, parts);       //извлекаем путь и расстояние

                if (buf1 == null)                                                                   //Путь не найден!
                    return;                                                                         //возможно потом continue тут должно быть

                paths[i][j] = buf1;                                                                 //сохраняем пути
                distances[i][j] = buf1.Count;                                                       //сохраняем длину пути
                facingFrom[i][j] = buf2;                                                            //сохраняем грани соседей
                nowProcessedFaces[i][j] = buf3;                                                     //сохраняем посчитанные грани

                j++;
            }

            i++;
        }


        //Распределение запросов и энергии
        //Инициализация задачи логистики энергии: магазины - покупатели
        Store[] stores = new Store[producerPositions.Count];
        for (j = 0; j < producerPositions.Count; j++)                       //работаем со всеми источниками в этой сети                    
        {
            stores[j] = new Store(j + 1, producerGive[j]);                  //создаем магазин со своими запасами
        }

        Customer[] customers = new Customer[consumerPositions.Count];
        for (i = 0; i < consumerPositions.Count; i++)                       //работаем со всеми потребителями в этой сети
        {
            Dictionary<Store, float> distFromCustomerToStore = new Dictionary<Store, float>();
            for (j = 0; j < producerPositions.Count; j++)                   //работаем со всеми генераторами в этой сети                    
            {
                distFromCustomerToStore.Add(stores[j], distances[i][j]);    //записываем расстояния до каждого магазина от этого потребителя
            }

            customers[i] = new Customer(j + 1, consumerRequests[i], distFromCustomerToStore);        //создаем покупателя со своими потребностями
        }


        //Собственно сама реализация "жадного алгоритма" ---------------------------------------------------------------------------------------//
        List<Customer> Customers = new List<Customer>();
        List<Store> Stores = new List<Store>();

        sim.Stores.AddRange(stores);
        sim.Customers.AddRange(customers);

        sim.Run();                                                          //распределение происходит тут
    }






    /// <summary>
    /// Просчет сетей в этом тике
    /// </summary>
    private void OnGameTick(float _)
    {
        int i = 0, j = 0;

        //разбираемся со скоростью и режимами расчета
        int ticks;
        if (instant)
            ticks = 1;
        else
            ticks = speedOfElectricity;

        while (ticks >= 1)
        {
            Cleaner();   //обязательно чистим 

            foreach (var network in this.networks)              //каждую сеть считаем
            {

                //Этап 1 - Очистка мусора---------------------------------------------------------------------------------------------//    
                this.producers.Clear();                         //очистка списка всех производителей, потому как для каждой network список свой
                this.producers2.Clear();                        //очистка списка всех производителей, потому как для каждой network список свой
                this.consumers.Clear();                         //очистка списка всех потребителей, потому как для каждой network список свой
                this.consumers2.Clear();                        //очистка списка всех ненулевых потребителей, потому как для каждой network список свой
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
                    this.accums.Add(accum);                                            //создаем список с аккумами
                }


                foreach (var accum in this.accums)                                     //работаем со всеми аккумами в этой сети
                {
                    float giveEnergy = accum.ElectricAccum.canRelease();               //этот аккум может выдать столько энергии
                    if (giveEnergy == 0)                                               //если у этого аккума пусто
                        continue;

                    this.accums2.Add(accum);

                    var accumPos = accum.ElectricAccum.Pos;
                    producerPositions.Add(accumPos);                                   //сохраняем позиции аккумов
                    producerGive.Add(giveEnergy);                                      //сохраняем выданную энергию аккумов
                }





                //Этап 4 - Распределяем энергию ----------------------------------------------------------------------//                 
                var sim = new Simulation();
                logisticalTask(network, consumerPositions, consumerRequests, producerPositions, producerGive, ref sim, out var paths, out var facingFrom, out var nowProcessedFaces);



                if (!instant)  // медленная передача
                {
                    //Этап  - выдаем пакеты энергии в сеть ---------------------------------------------------------------------------------------//
                    foreach (var customer in sim.Customers)
                    {
                        foreach (var store in sim.Stores)
                        {
                            if (customer.Received.TryGetValue(store, out var value))
                            {
                                int indexStore = sim.Stores.IndexOf(store);
                                BlockPos posStore = producerPositions[indexStore];
                                int indexCustomer = sim.Customers.IndexOf(customer);


                                if (parts.TryGetValue(posStore, out var part))
                                {
                                    var packet = new energyPacket();
                                    packet.energy = value;
                                    packet.path = paths[indexCustomer][indexStore];
                                    packet.facingFrom = facingFrom[indexCustomer][indexStore];
                                    packet.nowProcessed = nowProcessedFaces[indexCustomer][indexStore];
                                    packet.voltage = part.eparams[packet.facingFrom.Last()].voltage;

                                    parts[posStore].energyPackets.Add(packet);
                                }
                            }
                        }
                    }
                }



                if (instant)  // мгновенная выдача энергии по воздуху минуя провода
                {
                    i = 0;
                    foreach (var consumer in this.consumers2)       //работаем со всеми потребителями в этой сети
                    {
                        var totalGive = sim.Customers[i].Required - sim.Customers[i].Remaining;  //потребитель получил столько энергии
                        consumer.ElectricConsumer.Consume_receive(totalGive);                    //выдаем энергию потребителю 
                        i++;
                    }
                }





                //Этап  - Забираем у аккумов выданное ими ---------------------------------------------------------------------------------------//
                i = 0;
                foreach (var accum in this.accums2)       //работаем со всеми аккумами в этой сети
                {
                    if (sim.Stores[i + producers.Count].Stock < accum.ElectricAccum.canRelease())
                    {
                        accum.ElectricAccum.Release(accum.ElectricAccum.canRelease() - sim.Stores[i + producers.Count].Stock);
                    }
                    i++;
                }





                //Этап 5  - Хотим зарядить аккумы  ---------------------------------------------------------------------------------------//
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
                logisticalTask(network, consumer2Positions, consumer2Requests, producer2Positions, producer2Give, ref sim2, out var paths2, out var facingFrom2, out var nowProcessedFaces2);


                if (!instant)  // медленная передача
                {
                    //Этап  - выдаем пакеты энергии в сеть ---------------------------------------------------------------------------------------//
                    foreach (var customer in sim2.Customers)
                    {
                        foreach (var store in sim2.Stores)
                        {
                            if (customer.Received.TryGetValue(store, out var value))
                            {
                                int indexStore = sim2.Stores.IndexOf(store);
                                BlockPos posStore = producer2Positions[indexStore];
                                int indexCustomer = sim2.Customers.IndexOf(customer);


                                if (parts.TryGetValue(posStore, out var part))
                                {
                                    var packet = new energyPacket();
                                    packet.energy = value;
                                    packet.path = paths2[indexCustomer][indexStore];

                                    packet.path = paths2[indexCustomer][indexStore];
                                    packet.facingFrom = facingFrom2[indexCustomer][indexStore];
                                    packet.nowProcessed = nowProcessedFaces2[indexCustomer][indexStore];
                                    packet.voltage = (int)part.eparams[packet.facingFrom.Last()].voltage;

                                    parts[posStore].energyPackets.Add(packet);
                                }
                            }
                        }
                    }
                }







                if (instant) // мгновенная выдача энергии по воздуху минуя провода
                {
                    //Этап  - Заряжаем аккумы ---------------------------------------------------------------------------------------//
                    i = 0;
                    foreach (var accum in this.accums2)       //работаем со всеми аккумами в этой сети
                    {
                        var totalGive = sim2.Customers[i].Required - sim2.Customers[i].Remaining;       //аккум получил столько энергии                    
                        accum.ElectricAccum.Store(totalGive);                                           //выдаем энергию аккумам 

                        i++;
                    }
                }





                //Этап  - Сообщение генераторам о нужном количестве энергии ---------------------------------------------------------------------------------------//

                j = 0;
                foreach (var producer in this.producers)     //работаем со всеми генераторами в этой сети
                {
                    var totalOrder = sim.Stores[j].totalRequest;  //у генератора все просили столько
                    var totalOrder2 = sim2.Stores[j].totalRequest;  //у генератора все просили столько

                    producer.ElectricProducer.Produce_order(totalOrder + totalOrder2);   //говорим генератору сколько просят с него (нагрузка сети)

                    j++;
                }







                //Этап  - Сбор информации и обновление всего  (порядок не менять)----------------------------------------------------------------------------------------------//
                //обновляем информацию этой цепи

                //потребление
                network.Consumption = consumers.Sum<Consumer>(c => c.ElectricConsumer.getPowerReceive());
                network.Consumption += accums
                    .Sum<Accumulator>(a => Math.Max(a.ElectricAccum.GetCapacity() - a.ElectricAccum.GetLastCapacity(), 0.0F));

                //генерация
                network.Production = producers.Sum<Producer>(p => Math.Min(p.ElectricProducer.getPowerGive(), p.ElectricProducer.getPowerOrder()));

                //дефицит
                network.Lack = Math.Max(consumers.Sum<Consumer>(c => c.ElectricConsumer.getPowerRequest() - c.ElectricConsumer.getPowerReceive()), 0);



                //обновляем энтити все
                accums.ForEach(a => a.ElectricAccum.Update());
                producers.ForEach(a => a.ElectricProducer.Update());
                consumers.ForEach(a => a.ElectricConsumer.Update());



            }






            if (!instant)  // медленная передача
            {
                //Этап  - Потребление энергии уже имеющейся в пакете  ---------------------------------------------------------------------------------------//

                Dictionary<BlockPos, float> sumEnergy = new Dictionary<BlockPos, float>();

                foreach (var part in parts)  //перебираем все элементы
                {
                    if (part.Value.energyPackets != null && part.Value.energyPackets.Count > 0)  //есть ли тут пакеты?
                    {
                        var copyEnergyPackets = part.Value.energyPackets.ToList<energyPacket>();

                        foreach (var item in copyEnergyPackets)  //перебираем все пакеты в этой части
                        {
                            if (item.path[0] == part.Key)  //если первый элемент пути пакета имеет те же координаты, что и текущмй элемент, значит пакет можно забирать
                            {
                                //удаляем пакеты
                                parts[part.Key].energyPackets.Remove(item);

                                //суммируем все полученные пакеты данным потребителем
                                if (sumEnergy.TryGetValue(part.Key, out var value))
                                {
                                    sumEnergy[part.Key] += item.energy;
                                }
                                else
                                {
                                    sumEnergy.Add(part.Key, item.energy);
                                }

                            }

                        }
                    }


                    if (!sumEnergy.ContainsKey(part.Key))    //если в этот тик потребители ничего не получили, то говорим даем им 0
                    {
                        sumEnergy.Add(part.Key, 0.0F);
                    }
                }



                //выдаем каждому потребителю сумму поглощенных пакетов
                foreach (var pair in sumEnergy)
                {
                    if (parts[pair.Key].Consumer != null)                            //это потребитель?
                    {
                        parts[pair.Key].Consumer!.Consume_receive(pair.Value);       //выдали потребителю   
                    }
                    else if (parts[pair.Key].Accumulator != null)                    //это аккумулятор?
                    {
                        parts[pair.Key].Accumulator!.Store(pair.Value);              //выдали аккуму
                    }
                }

                sumEnergy.Clear();






                //Этап  - Двигаем пакеты ---------------------------------------------------------------------------------------//
                parts.Foreach(p => p.Value.current = new float[6]);  //сразу чистим ток (не трогать)

                foreach (var part in parts)  //перебираем все элементы
                {
                    if (part.Value.energyPackets != null && part.Value.energyPackets.Count > 0)
                    {
                        var copyEnergyPackets = part.Value.energyPackets.ToList<energyPacket>();

                        foreach (var item in copyEnergyPackets)  //перебираем все пакеты в этой части
                        {
                            //пропускаем тех, кто уже пришел к получателю
                            //и те пакеты, что уже двинули
                            if (item.path.Count >= 2 && !item.moved)
                            {
                                //делаем глубокую копию пакета
                                var item2 = new energyPacket();
                                item2 = item.DeepCopy();

                                item2.path.RemoveAt(item2.path.Count - 1);                //удаляем последний элемент пути
                                var moveTo = item2.path.Last();                           //координата теперь последнего элемента

                                if (parts.TryGetValue(moveTo, out var partt) && pathFinder.ToGetNeighbor(part.Key, parts, item2.facingFrom.Last(), moveTo)
                                    && !parts[moveTo].eparams[item.facingFrom[item.facingFrom.Count - 2]].burnout)   //копируем пакет, только если элемент там еще есть и пакет может туда пройти
                                {
                                    float resistance = part.Value.eparams[item2.facingFrom.Last()].resisitivity / (part.Value.eparams[item2.facingFrom.Last()].lines * part.Value.eparams[item2.facingFrom.Last()].crossArea);    //сопротивление проводника 
                                    if (part.Value.eparams[item2.facingFrom.Last()].isolated)  //если проводник изолированный
                                    {
                                        resistance /=2.0F;  //снижаем сопротивление в 2 раза
                                    }

                                    float current = item2.energy * (1.0F) / item2.voltage;  //считаем ток
                                    float lossEnergy = current * current * resistance;      //считаем потери в этом вроде по закону Лжоуля-Ленца

                                    item2.energy -= lossEnergy;                             //снижаем энергию пакету
                                    current = item2.energy * (1.0F) / item2.voltage;        //пересчитаем ток

                                    if (item2.energy <= 0.001)                              //если у пакета очень мало энергии - обнуляем
                                    {
                                        item2.energy = 0.0F;
                                    }
                                    else                                                    //двигаем пакеты если энергия не 0
                                    {
                                        i = 0;
                                        foreach (var face in item2.nowProcessed.Last())      //выбираем все просчитанные грани 
                                        {
                                            if (face)
                                            {
                                                parts[moveTo].current[i] += current;        //добавляем просчитанным граням ток
                                            }
                                            i++;
                                        }

                                        item2.facingFrom.RemoveAt(item2.facingFrom.Count - 1);                     //удаляем последний элемент facing
                                        item2.nowProcessed.RemoveAt(item2.nowProcessed.Count - 1);                 //удаляем последний элемент nowProcessed
                                        item2.moved = true;

                                        parts[moveTo].energyPackets.Add(item2);                                    //копируем пакет на новую позицию
                                    }


                                }

                                //пакет в любом случае уничтожится, если не сможет переместиться
                                //удаляем пакеты
                                parts[part.Key].energyPackets.Remove(item);
                            }


                        }
                    }
                }





                //Этап  - Палим провода ---------------------------------------------------------------------------------------//
                foreach (var part in parts)  //перебираем все элементы
                {
                    if (part.Value.energyPackets != null && part.Value.energyPackets.Count > 0)
                    {
                        var copyEnergyPackets = part.Value.energyPackets.ToList<energyPacket>();

                        foreach (var item in copyEnergyPackets)  //перебираем все пакеты в этой части
                        {
                            //пакет превышает напряжение проводника и находится на этой грани?
                            if (item.voltage > part.Value.eparams[item.facingFrom.Last()].voltage)
                            {
                                parts[part.Key].eparams[item.facingFrom.Last()].burnout = true; //проводок сгорел на этой грани

                                var removedFace = FacingHelper.FromFace(FacingHelper.BlockFacingFromIndex(item.facingFrom.Last()));

                                this.parts.TryGetValue(part.Key, out var part2);
                                part2.Connection &= ~removedFace; //вычитаем по сути эти соединения

                                this.RemoveConnections(ref part2, removedFace);  // убираем соединение

                                //уничтожаем все пакеты в этой точке грани
                                parts[part.Key].energyPackets.Remove(item);
                            }
                        }

                        i = 0;

                        foreach (var cur in part.Value.current)
                        {
                            if (cur > part.Value.eparams[i].maxCurrent * part.Value.eparams[i].lines) //ток больше характеристик кабеля?
                            {
                                parts[part.Key].eparams[i].burnout = true; //проводок сгорел на этой грани

                                var removedFace = FacingHelper.FromFace(FacingHelper.BlockFacingFromIndex(i));

                                this.parts.TryGetValue(part.Key, out var part2);
                                part2.Connection &= ~removedFace; //вычитаем по сути эти соединения

                                this.RemoveConnections(ref part2, removedFace);  // убираем соединение

                                //уничтожаем все пакеты в этой точке грани
                                copyEnergyPackets = part.Value.energyPackets.ToList<energyPacket>();

                                foreach (var item in copyEnergyPackets)  //перебираем все пакеты в этой части
                                {
                                    if (item.nowProcessed.Last()[i])
                                    {
                                        parts[part.Key].energyPackets.Remove(item);
                                    }
                                }
                            }
                            i++;
                        }

                    }
                }



            }


            ticks--;                   //временно тут
        }

        AltPressed = false;            //сбрасываем зажатость кнопки альт
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



    /// <summary>
    /// Удаляем сеть
    /// </summary>
    /// <param name="network"></param>
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
                this.AddConnections(ref part, part.Connection, (default, 0));     //добавляем соединения???
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



    private void AddConnections(ref NetworkPart part, Facing addedConnections, (EParams, int) setEparams)
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


        //поиск соседей по граням
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

        //поиск соседей по ребрам
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

            part.Networks[face.Index] = network;            //присваиваем в этой точке эту цепь

            int i = 0;
            if (part.eparams == null)
            {
                part.eparams = new EParams[6];
            }

            foreach (var ams in part.eparams)
            {
                if (ams.Equals(default))
                    part.eparams[i] = new EParams();
                i++;
            }

            if (!setEparams.Item1.Equals(default) && part.eparams[face.Index].maxCurrent == 0)
                part.eparams[face.Index] = setEparams.Item1;      //аналогично с параметрами электричества
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




    /// <summary>
    /// Задать потребителя
    /// </summary>
    /// <param name="position"></param>
    /// <param name="consumer"></param>
    public void SetConsumer(BlockPos position, IElectricConsumer? consumer) =>
    SetComponent(
        position,
        consumer,
        part => part.Consumer,
        (part, c) => part.Consumer = c,
        network => network.Consumers);


    /// <summary>
    /// Задать генератор
    /// </summary>
    /// <param name="position"></param>
    /// <param name="producer"></param>
    public void SetProducer(BlockPos position, IElectricProducer? producer) =>
        SetComponent(
            position,
            producer,
            part => part.Producer,
            (part, p) => part.Producer = p,
            network => network.Producers);


    /// <summary>
    /// Задать аккумулятор
    /// </summary>
    /// <param name="position"></param>
    /// <param name="accumulator"></param>
    public void SetAccumulator(BlockPos position, IElectricAccumulator? accumulator) =>
        SetComponent(
            position,
            accumulator,
            part => part.Accumulator,
            (part, a) => part.Accumulator = a,
            network => network.Accumulators);


    /// <summary>
    /// Задает компоненты разных типов
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="position"></param>
    /// <param name="newComponent"></param>
    /// <param name="getComponent"></param>
    /// <param name="setComponent"></param>
    /// <param name="getCollection"></param>
    private void SetComponent<T>(
        BlockPos position,
        T? newComponent,
        System.Func<NetworkPart, T?> getComponent,
        Action<NetworkPart, T?> setComponent,
        System.Func<Network, ICollection<T>> getCollection)
        where T : class
    {
        if (!this.parts.TryGetValue(position, out var part))
        {
            if (newComponent == null)
            {
                return;
            }

            part = this.parts[position] = new NetworkPart(position);
        }

        var oldComponent = getComponent(part);
        if (oldComponent != newComponent)
        {
            foreach (var network in part.Networks)
            {
                if (network is null) continue;

                var collection = getCollection(network);

                if (oldComponent != null)
                {
                    collection.Remove(oldComponent);
                }

                if (newComponent != null)
                {
                    collection.Add(newComponent);
                }
            }

            setComponent(part, newComponent);
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
                    result.Facing |= FacingHelper.FromFace(blockFacing);        //выдаем ее направления
                    result.eParamsInNetwork = part.eparams[blockFacing.Index];                     //выдаем ее текущие параметры
                    result.current = part.current[blockFacing.Index];
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
                result.Lack += network.Lack;
            }
        }

        return result;
    }


}

/// <summary>
/// Сам пакет с энергией
/// </summary>
public struct energyPacket
{
    public List<BlockPos> path;
    public float energy;
    public int voltage;
    public List<int> facingFrom;
    public List<bool[]> nowProcessed;
    public bool moved;

    // Метод для глубокого копирования
    public energyPacket DeepCopy()
    {
        energyPacket copy = new energyPacket();

        // Копируем списки и массивы
        copy.path = new List<BlockPos>(this.path); // BlockPos — структура, копируется по значению

        copy.energy = this.energy;
        copy.voltage = this.voltage;
        copy.moved = this.moved;

        copy.facingFrom = new List<int>(this.facingFrom); // Копируем список int

        // Глубокое копирование nowProcessed (каждый массив копируется отдельно)
        copy.nowProcessed = this.nowProcessed
            .Select(arr => arr.ToArray()) // Создаем новый массив для каждого bool[]
            .ToList();

        return copy;
    }
}

/// <summary>
/// Параметры проводов/приборов как участников электрической цепи
/// </summary>
public struct EParams
{
    public int voltage;             //напряжение
    public float maxCurrent;        //максим ток, которое может пройти по одной линии этого элемента цепи
    public int indexM;              //индекс материала
    public float resisitivity;      //потери энергии в элементе цепи (удельное сопротивление)
    public byte lines;              //количество линий элемента цепи/провода
    public float crossArea;         //площадь сечения одной жилы
    public bool burnout;            //сгорел или нет?
    public bool isolated;           //изолирован?

    public EParams(int voltage, float maxCurrent, int indexM, float resisitivity, byte lines, float crossArea, bool burnout, bool isolated)
    {
        this.voltage= voltage;
        this.maxCurrent =maxCurrent;
        this.indexM= indexM;
        this.resisitivity= resisitivity;
        this.lines= lines;
        this.crossArea= crossArea;
        this.burnout= burnout;
        this.isolated= isolated;          
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

    public float Consumption;
    public float Overflow;
    public float Production;
    public float Lack;
}

/// <summary>
/// Один элемент цепи
/// </summary>
public class NetworkPart                       //элемент цепи
{
    public readonly Network?[] Networks = {      //в какие стороны провода направлены
            null,
            null,
            null,
            null,
            null,
            null
        };

    public EParams[] eparams = new EParams[] { }; //параметры по граням


    public float[] current = new float[6]
    {
        0.0F,
        0.0F,
        0.0F,
        0.0F,
        0.0F,
        0.0F
    };

    public List<energyPacket> energyPackets;

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
    public float Consumption;                 //потреблении
    public float Overflow;                    //перепроизводстве
    public float Production;                  //проивзодстве
    public float Lack;                        //дефицит

    public Facing Facing = Facing.None;       //направлений
    public int NumberOfAccumulators;          //аккумуляторах
    public int NumberOfBlocks;                //блоков
    public int NumberOfConsumers;             //потребителй
    public int NumberOfProducers;             //источников

    public EParams eParamsInNetwork = new EParams();       //параметрах конкретно этого блока в этой цепи
    internal float current;
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