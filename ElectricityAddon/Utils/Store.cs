using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityUnofficial.Utils
{
    public class Store
    {
        public int Id { get; }
        public int Stock { get; set; }
        public Dictionary<Customer, int> CurrentRequests { get; } = new Dictionary<Customer, int>();
        public int FailedRequests { get; private set; }

        public Store(int id, int stock) => (Id, Stock) = (id, stock);
        public double DistanceTo(Customer customer) => customer.StoreDistances[this];

        public void ResetRequests()
        {
            FailedRequests += CurrentRequests.Count;
            CurrentRequests.Clear();
        }

        public void ProcessRequests()
        {
            if (Stock <= 0)
            {
                if (CurrentRequests.Count > 0)
                {
           //         Console.WriteLine($"Store {Id}: Received {CurrentRequests.Sum(r => r.Value)} requests but has no stock!");
                }
                ResetRequests();
                return;
            }

            int totalRequested = CurrentRequests.Sum(r => r.Value);
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
                double ratio = (double)Stock / totalRequested;
                foreach (var (customer, amount) in CurrentRequests.ToList())
                {
                    int allocated = (int)Math.Floor(amount * ratio);
                    customer.Received[this] = allocated;
                    Stock -= allocated;
                }
            }

           // Console.WriteLine($"Store {Id}: Processed {CurrentRequests.Count} requests. Remaining: {Stock}");
            ResetRequests();
        }
    }
}
