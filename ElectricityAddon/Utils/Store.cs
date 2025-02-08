using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Utils
{
    public class Store
    {
        public int Id { get; }
        public float Stock { get; set; }
        public Dictionary<Customer, float> CurrentRequests { get; } = new Dictionary<Customer, float>();

        //public Dictionary<Customer, float> AllGives { get; } = new Dictionary<Customer, float>();
        public bool ImNull { get; private set; }

        public Dictionary<Store, float> StoresOrders { get; } = new Dictionary<Store, float>();

        public float totalRequest;
        public Store(int id, float stock) => (Id, Stock) = (id, stock);
        public double DistanceTo(Customer customer) => customer.StoreDistances[this];

        public void ResetRequests()
        {
            CurrentRequests.Clear();
        }

        public void ProcessRequests()  
        {
            

            float totalRequested = CurrentRequests.Sum(r => r.Value);   //обязательно говорим сумму попрошенного у каждого магазина

            totalRequest += totalRequested;
            if (Stock <= 0.001F)
            {

                Stock = 0.0F;
                ImNull = true;   //если магазин был изначально нулевой и принял запросы
                ResetRequests();
                return;
            }

            
            if (totalRequested == 0) return;

            if (Stock >= totalRequested)
            {
                foreach (var (customer, amount) in CurrentRequests)
                {
                    customer.Received[this] = amount;
                    
                }
                Stock -= totalRequested;
            }
            else
            {
                float ratio = Stock / totalRequested;
                foreach (var (customer, amount) in CurrentRequests.ToList())
                {
                    float allocated = amount * ratio;
                    customer.Received[this] = allocated;
                    Stock -= allocated;
                }

                
            }

            if (Stock <= 0.001F) //если после всех раздач имеется ноль
            {
                Stock = 0.0F;
                ImNull = true;   //магазин теперь пуст  и он принимал запросы
            }

            // Console.WriteLine($"Store {Id}: Processed {CurrentRequests.Count} requests. Remaining: {Stock}");
            ResetRequests();
        }
    }
}
