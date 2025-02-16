using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityAddon.Utils
{
    public class Simulation
    {
        public List<Customer> Customers { get; } = new List<Customer>();
        public List<Store> Stores { get; } = new List<Store>();
        
        public void Run()
        {
            foreach (var store in Stores)                //работаем со всеми генераторами в этой сети                    
            {
                store.totalRequest = 0;                 //инициализируем суммарное выпрашенное число энергии
            }


            do
            {
                Customers.ForEach(c => c.UpdateOrderedStores());        //обновляет пройденный путь и купленное уже покупателями
                Stores.ForEach(s => s.ResetRequests());                 //сбрасывает список запросов магазинам

                foreach (var customer in Customers.Where(c => c.Remaining > 0))     //в цикле выбирает только тех покупателей, кому еще нужна энергия
                {
                    float remaining = customer.Remaining;
                    foreach (var store in customer.GetAvailableStores()) //вынести перед циклом выборку !!!
                    {
                        if (store.Stock <= 0.001F && store.ImNull)           //если у магазина уже ноль и он был обработан, то пропускаем
                            continue;

                        float requested = remaining;
                        store.CurrentRequests[customer] = requested;        //покупатель просит столько
                        remaining -= requested;

                        if (remaining <= 0)    //покупатель отправил запросы везде?
                            break;
                    }
                }


                Stores.ForEach(s => s.ProcessRequests());


            } while (Stores.Any(s => !s.ImNull) && Customers.Any(c => c.Remaining > 0));
        }


    }
}
