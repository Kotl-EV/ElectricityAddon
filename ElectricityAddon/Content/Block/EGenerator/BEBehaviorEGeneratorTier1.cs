using System;
using System.Linq;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace ElectricityAddon.Content.Block.EGenerator;

public class BEBehaviorEGeneratorTier1 : BEBehaviorMPBase, IElectricProducer
{
    private static CompositeShape? compositeShape;
    private float powerOrder = 0;           // Просят столько энергии
    private float powerGive = I_max;        // Отдаем столько энергии

    // Константы генератора
    private static float I_max;                 // Максимальный ток
    private static float speed_max;             // Максимальная скорость вращения
    private static float resistance_factor;     // Множитель сопротивления
    private static float resistance_load;       // Сопротивление нагрузки генератора

    private float[] def_Params = { 100.0F, 0.5F, 0.1F, 0.25F };          //заглушка
    public float[] Params = { 0, 0, 0, 0 };                              //сюда берем параметры из ассетов


    /// <summary>
    /// Извлекаем параметры из ассетов
    /// </summary>  
    public void GetParams()
    {
        Params = MyMiniLib.GetAttributeArrayFloat(this.Block, "params", def_Params);

        I_max = Params[0];
        speed_max = Params[1];
        resistance_factor = Params[2];
        resistance_load = Params[3];
    }

    public BEBehaviorEGeneratorTier1(BlockEntity blockEntity) : base(blockEntity)
    {
        GetParams();
    }

    public override BlockFacing OutFacingForNetworkDiscovery
    {
        get
        {
            if (this.Blockentity is BlockEntityEGenerator entity && entity.Facing != Facing.None)
            {
                return FacingHelper.Directions(entity.Facing).First();
            }

            return BlockFacing.NORTH;
        }
    }
    public BlockPos Pos => this.Position;
    public override int[] AxisSign => this.OutFacingForNetworkDiscovery.Index switch
    {
        0 => new[]
        {
            +0,
            +0,
            -1
        },
        1 => new[]
        {
            -1,
            +0,
            +0
        },
        2 => new[]
        {
            +0,
            +0,
            -1
        },
        3 => new[]
        {
            -1,
            +0,
            +0
        },
        4 => new[]
        {
            +0,
            +1,
            +0
        },
        5 => new[]
        {
            +0,
            -1,
            +0
        },
        _ => this.AxisSign
    };


    public int Produce()                        //можно удалять потом----------------------------------
    {
        return 0;
    }

    /// <summary>
    /// Запрашивают энергию у генератора
    /// </summary>
    public void Produce_order(float amount)
    {

        if (this.powerOrder != amount)
        {
            this.powerOrder = amount;
            
                this.Blockentity.MarkDirty(true);
            
        }
    }

    /// <summary>
    /// Генератор отдает энергию
    /// </summary>
    public float Produce_give()
    {
        
        float speed = this.network?.Speed ?? 0.0F;

        float b = 1f;                                                                       // Положение вершины кривой
        float a = 1F;
        float power = (Math.Abs(speed) <= speed_max)                                        // Задаем форму кривых тока(мощности)
            ? (int)((1 - a * (float)Math.Pow(Math.Abs(speed) / speed_max - b, 4F)) * I_max) // Степенная с резким падением ближе к 0
            : (int)(I_max);                                                                 // Линейная горизонтальная

        power = Math.Max(0, power);                                                         // Чтобы уж точно не ниже нуля

        if (power != this.powerGive)
        {
            this.powerGive = power;
           
                this.Blockentity.MarkDirty(true);
            
        }


        //return 100; //временно!!!-----------------------------------------------------
        return power;
    }


    /// </summary>
    /// Механическая сеть берет отсюда сопротивление этого генератора
    /// </summary>
    public override float GetResistance()
    {

        float spd = this.Network.Speed;
        return (Math.Abs(spd) > speed_max)                                                                                      // Если скорость превышает максимальную, рассчитываем сопротивление как квадратичную
           // ? resistance_load * (powerOrder / I_max) + (resistance_factor * (float)Math.Pow((Math.Abs(spd) / speed_max), 2f))   // Степенная зависимость, если скорость ушла за пределы двигателя              
           // : resistance_load * (powerOrder / I_max) + (resistance_factor * Math.Abs(spd) / speed_max);                         // Линейное сопротивление для обычных скоростей
            
            //в таком виде будет лучше, иначе система выработки может встать колом, когда потребления больше выработки
            ? resistance_load * (Math.Min(powerOrder, I_max) / I_max) + (resistance_factor * (float)Math.Pow((Math.Abs(spd) / speed_max), 2f))   // Степенная зависимость, если скорость ушла за пределы двигателя              
            : resistance_load * (Math.Min(powerOrder, I_max) / I_max) + (resistance_factor * Math.Abs(spd) / speed_max);                         // Линейное сопротивление для обычных скоростей

        // сопротивление генератора также напрямую зависит от нагрузки в электрической цепи powerOrder 
    }


    public override void WasPlaced(BlockFacing connectedOnFacing)
    {
    }

    protected override CompositeShape? GetShape()
    {
        if (this.Api is { } api && this.Blockentity is BlockEntityEGenerator entity && entity.Facing != Facing.None)
        {
            var direction = this.OutFacingForNetworkDiscovery;

            if (BEBehaviorEGeneratorTier1.compositeShape == null)
            {
                var location = this.Block.CodeWithVariant("type", "rotor");

                BEBehaviorEGeneratorTier1.compositeShape = api.World.BlockAccessor.GetBlock(location).Shape.Clone();
            }

            var shape = BEBehaviorEGeneratorTier1.compositeShape.Clone();

            if (direction == BlockFacing.NORTH)
            {
                shape.rotateY = 0;
            }

            if (direction == BlockFacing.EAST)
            {
                shape.rotateY = 270;
            }

            if (direction == BlockFacing.SOUTH)
            {
                shape.rotateY = 180;
            }

            if (direction == BlockFacing.WEST)
            {
                shape.rotateY = 90;
            }

            if (direction == BlockFacing.UP)
            {
                shape.rotateX = 90;
            }

            if (direction == BlockFacing.DOWN)
            {
                shape.rotateX = 270;
            }

            return shape;
        }

        return null;
    }

    protected override void updateShape(IWorldAccessor worldForResolve)
    {
        this.Shape = this.GetShape();
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        return false;
    }

    /// <summary>
    /// Подсказка при наведении на блок
    /// </summary>
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(powerGive / I_max * 100));
        stringBuilder.AppendLine("└ " + Lang.Get("Production") + powerGive + "/" + I_max + " Eu");
        stringBuilder.AppendLine();
    }
}
