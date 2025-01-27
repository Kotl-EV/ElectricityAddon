using System;
using System.Linq;
using System.Text;
using ElectricityAddon.Interface;
using ElectricityAddon.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace ElectricityAddon.Content.Block.EMotor;

public class BEBehaviorEMotorTier2 : BEBehaviorMPBase, IElectricConsumer
{

    private static CompositeShape? compositeShape;
    private int powerSetting;             //текущее значение потребления

    // Константы двигателя
    private static float I_min;          // Минимальный ток
    private static float I_max;         // Максимальный ток
    private static float torque_max;     // Максимальный крутящий момент
    private static float kpd_max;        // Пиковый КПД
    private static float speed_max;       // Максимальная скорость вращения
    private static float resistance_factor;    // множитель сопротивления

    private float torque;                        // Текущий крутящий момент
    private float I_value;                       // Ток потребления
    public float kpd;                            // КПД

    private float[] def_Params = { 10.0F, 400.0F, 1.0F, 0.85F, 1.0F, 0.1F };   //заглушка
    public float[] Params = { 0, 0, 0, 0, 0, 0 };                              //сюда берем параметры


    //извлекаем параметры
    public void GetParams()
    {
        Params = MyMiniLib.GetAttributeArrayFloat(this.Block, "params", def_Params);
        I_min = Params[0];
        I_max = Params[1];
        torque_max = Params[2];
        kpd_max = Params[3];
        speed_max = Params[4];
        resistance_factor = Params[5];
    }

    public BEBehaviorEMotorTier2(BlockEntity blockEntity) : base(blockEntity)
    {
        GetParams();
    }

    public override BlockFacing OutFacingForNetworkDiscovery
    {
        get
        {
            if (this.Blockentity is BlockEntityEMotor entity && entity.Facing != Facing.None)
            {
                return FacingHelper.Directions(entity.Facing).First();
            }

            return BlockFacing.NORTH;
        }
    }


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
            +1,
            +0
        },
        _ => throw new Exception()
    };

    //диапазон потребления
    public ConsumptionRange ConsumptionRange => new(0, (int)I_max);

    //событие потребления
    public void Consume(int amount)
    {
        if (this.powerSetting != amount)
        {
            this.powerSetting = amount;
            this.Blockentity.MarkDirty(true);
        }
    }

    //событие подключения к механической сети




    //не удалять
    //никто не обращается к этой функции, когда работает GetTorque, но быть должна
    public override float GetResistance()
    {
        return 0;
    }

    //считаем сопротивление самого двигателя
    public float Resistance(float spd)
    {
        return (Math.Abs(spd) > speed_max)                           // Если скорость превышает максимальную, рассчитываем сопротивление как степенную зависимость
            ? resistance_factor * (float)Math.Pow((Math.Abs(spd) / speed_max), 2f)  // Степенная зависимость, если скорость ушла за пределы двигателя   
            : resistance_factor * Math.Abs(spd) / speed_max;                      // Линейное сопротивление для обычных скоростей
    }


    // Рассчитываем КПД
    public float KPD(float tor)
    {
        float b = 0.7f;                             // Положение вершины параболы
        float a = (tor <= torque_max / 2.0F) ?        // левая и права ветвь параболы разные
            2.04F
            : 0.8f;
        float buf = kpd_max * (1 - a * (float)Math.Pow(tor / torque_max - b, 2));   // Параболическая зависимость
        return Math.Max(0.01f, buf);                                             // Минимальное значение КПД
    }


    private static float constanta = (I_max - I_min) / torque_max;

    /// <summary>
    /// Основной метод поведения двигателя
    /// </summary>
    public override float GetTorque(long tick, float speed, out float resistance)
    {

        torque = 0f;        // Текущий крутящий момент
        resistance = Resistance(speed);  //вычисляем текущее сопротивление двигателя    
        I_value = I_min;    // Ток потребления

        float I_amount = this.powerSetting;  //доступно тока 

        // Если ток меньше минимального, двигатель не работает
        if (I_amount < I_min)
            return torque;

        I_value = Math.Min(I_amount, I_max);

        // Рассчитываем момент для компенсации сопротивления
        //torque = Math.Min(Network.NetworkResistance, torque_max);
        //float torque2 = torque_max * (I_value - I_min) / (I_max - I_min);
        //torque =(torque+ torque2)/2;

        //момент линейно от тока
        torque = torque_max * (I_value - I_min) / (I_max - I_min); //берем максимум момента из всей энергии, что нам дают

        // Ток потребления с учетом КПД
        I_value = torque * constanta / KPD(torque) + I_min;

        // Проверка, чтобы ток не превышал максимальное значение I_max и I_amount
        float torque_down = 0;  //понижаем 
        int k = 0;
        while (I_value > Math.Min(I_max, I_amount))
        {
            k++;
            // Пропорционально снижаем крутящий момент
            torque_down = torque * (1 - (0.02F * k));         // Уменьшаем крутящий момент на 2%

            if (torque_down < 0)
            {
                torque_down = 0;
                break;
            }
            // Ток потребления с учетом КПД
            I_value = torque_down * constanta / KPD(torque_down) + I_min;

        }

        if (k > 0)
            torque = torque_down;

        // Возвращаем все значения            
        return this.propagationDir == this.OutFacingForNetworkDiscovery
            ? 1f * torque
            : -1f * torque;
    }


    public override void WasPlaced(BlockFacing connectedOnFacing)
    {
    }


    protected override CompositeShape? GetShape()
    {
        if (this.Api is { } api && this.Blockentity is BlockEntityEMotor entity && entity.Facing != Facing.None)
        {
            var direction = this.OutFacingForNetworkDiscovery;

            if (BEBehaviorEMotorTier2.compositeShape == null)
            {
                var location = this.Block.CodeWithVariant("type", "rotor");
                BEBehaviorEMotorTier2.compositeShape = api.World.BlockAccessor.GetBlock(location).Shape.Clone();
            }

            var shape = BEBehaviorEMotorTier2.compositeShape.Clone();

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

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder)
    {
        base.GetBlockInfo(forPlayer, stringBuilder);

        stringBuilder.AppendLine(StringHelper.Progressbar(powerSetting / I_max * 100));
        stringBuilder.AppendLine("└  " + Lang.Get("Consumption") + this.powerSetting + "/" + I_max + " Eu");
        //stringBuilder.AppendLine("└ " + "КПД " + this.kpd*100F + "%/" + this.kpd_max*100+"%");
        //stringBuilder.AppendLine("└ " + "Реальный ток " + I_value + "/" + this.I_max);
        stringBuilder.AppendLine();

    }
}
