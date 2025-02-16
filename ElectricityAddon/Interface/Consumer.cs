using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;

public struct ConsumptionRange {                                 //���� ����� �������
    public readonly int Min;
    public readonly int Max;

    public ConsumptionRange(int min, int max) {
        this.Min = min;
        this.Max = max;
    }

}                                                               //���� ����� �������



public interface IElectricConsumer
{
    /// <summary>
    /// ���������� ������������
    /// </summary>
    public BlockPos Pos { get; }
    /// <summary>
    /// ������� ����������� � ����������� ������� �� ����� � ������ ������ �������
    /// </summary>
    public float Consume_request();

    /// <summary>
    /// ������� ������ ������� ����������� 
    /// </summary>
    public void Consume_receive(float amount);


    /// <summary>
    /// ��������� Entity
    /// </summary>
    public void Update();


    /// <summary>
    /// ������� �������� � ������ ������ �����������
    /// </summary>
    /// <returns></returns>
    public float getPowerReceive();


    /// <summary>
    /// ������� ������� � ������ ������ �����������
    /// </summary>
    /// <returns></returns>
    public float getPowerRequest();



    public void Consume(int amount);                //������� ����� 
    public ConsumptionRange ConsumptionRange { get; } // �������!



}
