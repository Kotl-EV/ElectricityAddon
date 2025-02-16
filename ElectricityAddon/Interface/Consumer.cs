using Vintagestory.API.MathTools;

namespace ElectricityAddon.Interface;

public struct ConsumptionRange {                                 //тоже потом удалить
    public readonly int Min;
    public readonly int Max;

    public ConsumptionRange(int min, int max) {
        this.Min = min;
        this.Max = max;
    }

}                                                               //тоже потом удалить



public interface IElectricConsumer
{
    /// <summary>
    /// Координата аккумулятора
    /// </summary>
    public BlockPos Pos { get; }
    /// <summary>
    /// Система запрашивает у потребителя сколько ей нужно в данный момент энергии
    /// </summary>
    public float Consume_request();

    /// <summary>
    /// Система выдает энергию потребителю 
    /// </summary>
    public void Consume_receive(float amount);


    /// <summary>
    /// Обновляем Entity
    /// </summary>
    public void Update();


    /// <summary>
    /// Сколько получает в данный момент потребитель
    /// </summary>
    /// <returns></returns>
    public float getPowerReceive();


    /// <summary>
    /// Сколько требует в данный момент потребитель
    /// </summary>
    /// <returns></returns>
    public float getPowerRequest();



    public void Consume(int amount);                //удалить можно 
    public ConsumptionRange ConsumptionRange { get; } // удалить!



}
