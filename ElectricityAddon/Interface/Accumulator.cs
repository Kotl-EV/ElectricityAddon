using System.Runtime.InteropServices;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;

public interface IElectricAccumulator
{
    public BlockPos Pos { get; }

    /// <summary>
    /// ������������ ��� ������/����������
    /// </summary>
    public float maxCurrent { get; }

    /// <summary>
    /// ������������ ������� ������������
    /// </summary>
    /// <returns></returns>
    public float GetMaxCapacity();

    /// <summary>
    /// ������� ������� ������������
    /// </summary>
    /// <returns></returns>
    public float GetCapacity();

    /// <summary>
    /// ��������� �������
    /// </summary>
    /// <param name="amount"></param>
    public void Store(float amount);

    /// <summary>
    /// ����� ��������� ������� �� ���
    /// </summary>
    /// <param name="amount"></param>
    public float canStore();

    /// <summary>
    /// ������ �������
    /// </summary>
    /// <param name="amount"></param>
    public float Release(float amount);


    /// <summary>
    /// ����� ������ ������� �� ���
    /// </summary>
    /// <param name="amount"></param>
    public float canRelease();


    public void Update();
}

