using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;

public interface IElectricProducer
{
    /// <summary>
    /// ���������� ������������
    /// </summary>
    public BlockPos Pos { get; }


    /// <summary>
    /// ������� ����������� � ���������� ������� �� ����� � ������ ������ ������
    /// </summary>
    public void Produce_order(float amount);


    /// <summary>
    /// ������� ����� ������ ��������� ������ ��������
    /// </summary>
    /// <returns></returns>
    public float getPowerGive();

    /// <summary>
    /// ������� � ������ ������ ������ � ���������� (��������)
    /// </summary>
    /// <returns></returns>
    public float getPowerOrder();

    /// <summary>
    /// ��������� ������ ������� � �������
    /// </summary>
    public float Produce_give();

    /// <summary>
    /// ��������� Entity
    /// </summary>
    public void Update();

}
