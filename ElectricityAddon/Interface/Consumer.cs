using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;
public struct ConsumptionRange {                                 //тоже потом отвалится за ненадобностью
    public readonly int Min;
    public readonly int Max;

    public ConsumptionRange(int min, int max) {
        this.Min = min;
        this.Max = max;
    }

}



public interface IElectricConsumer
{
    public ConsumptionRange ConsumptionRange { get; }

    public BlockPos Pos { get; }
    /// <summary>
    /// Система запрашивает у потребителя сколько ей нужно в данный момент энергии
    /// </summary>
    public float Consume_request();

    /// <summary>
    /// Система выдает энергию потребителю 
    /// </summary>
    public void Consume_receive(float amount);      

    public void Consume(int amount);                //удалить можно 
}
