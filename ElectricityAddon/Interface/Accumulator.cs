using System.Runtime.InteropServices;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;

public interface IElectricAccumulator
{
    /// <summary>
    /// ���������� ������������
    /// </summary>
    public BlockPos Pos { get; }

    /// <summary>
    /// ������������ ��� ������/����������
    /// </summary>
    public float power { get; }

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
    /// ���������� �������� ������� ������������
    /// </summary>
    /// <returns></returns>
    public float GetLastCapacity();

    /// <summary>
    /// ������ ����� ������� ������������ (�������� ������ ��� ��������� ������������)
    /// </summary>
    /// <returns></returns>
    public void SetCapacity(float value);

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

    /// <summary>
    /// ��������� Entity
    /// </summary>
    public void Update();
}

