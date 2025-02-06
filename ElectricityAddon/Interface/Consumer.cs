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
    public ConsumptionRange ConsumptionRange { get; } // �������!

    public BlockPos Pos { get; }
    /// <summary>
    /// ������� ����������� � ����������� ������� �� ����� � ������ ������ �������
    /// </summary>
    public float Consume_request();

    /// <summary>
    /// ������� ������ ������� ����������� 
    /// </summary>
    public void Consume_receive(float amount);      



    public void Consume(int amount);                //������� ����� 
}
