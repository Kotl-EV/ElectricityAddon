namespace ElectricityAddon.Interface;

public interface IElectricProducer
{
    /// <summary>
    /// Система запрашивает у генератора сколько ей нужно в данный момент выдать
    /// </summary>
    public void Produce_order(float amount);

    /// <summary>
    /// Генератор выдает энергию в систему
    /// </summary>
    public float Produce_give();


    public int Produce();                    //можно удалить
}
