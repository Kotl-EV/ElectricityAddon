using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;

public interface IElectricTransformator
{
    /// <summary>
    /// ���������� ��������������
    /// </summary>
    public BlockPos Pos { get; }

    /// <summary>
    /// ������� ���������� ��������������
    /// </summary>
    public int highVoltage { get; }


    /// <summary>
    /// ������ ���������� ��������������
    /// </summary>
    public int lowVoltage { get; }

    /// <summary>
    /// ������� �������� ������ �� ��������������
    /// </summary>
    /// <returns></returns>
    public float getPower();

    /// <summary>
    /// ��������� �������������� �������� ���
    /// </summary>
    /// <returns></returns>
    public void setPower(float power);



    /// <summary>
    /// ��������� Entity
    /// </summary>
    public void Update();

}
