namespace ElectricityAddon.Interface;

public interface IElectricProducer
{
    /// <summary>
    /// ������� ����������� � ���������� ������� �� ����� � ������ ������ ������
    /// </summary>
    public void Produce_order(float amount);

    /// <summary>
    /// ��������� ������ ������� � �������
    /// </summary>
    public float Produce_give();


    public int Produce();                    //����� �������
}
