using System.Runtime.InteropServices;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;

public interface IElectricAccumulator
{
    public BlockPos Pos { get; }

    /// <summary>
    /// Максимальный ток отдачи/сохранения
    /// </summary>
    public float maxCurrent { get; }

    /// <summary>
    /// Максимальная емкость аккумулятора
    /// </summary>
    /// <returns></returns>
    public float GetMaxCapacity();

    /// <summary>
    /// Текущая емкость аккумулятора
    /// </summary>
    /// <returns></returns>
    public float GetCapacity();

    /// <summary>
    /// Сохранить энергию
    /// </summary>
    /// <param name="amount"></param>
    public void Store(float amount);

    /// <summary>
    /// Выдать энергию
    /// </summary>
    /// <param name="amount"></param>
    public float Release();
}

