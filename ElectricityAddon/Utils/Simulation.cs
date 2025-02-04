using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityUnofficial.Utils
{
    public class Simulation
    {
        public List<Customer> Customers { get; } = new List<Customer>();
        public List<Store> Stores { get; } = new List<Store>();

        public void Run()
        {
            bool hasChanges;
            do
            {
                Customers.ForEach(c => c.UpdateOrderedStores());
                Stores.ForEach(s => s.ResetRequests());

                foreach (var customer in Customers.Where(c => c.Remaining > 0))
                {
                    int remaining = customer.Remaining;
                    foreach (var store in customer.GetAvailableStores())
                    {
                        if (store.Stock <= 0) continue;

                        int requested = Math.Min(remaining, store.Stock);
                        store.CurrentRequests[customer] = requested;
                        remaining -= requested;

                        if (remaining <= 0) break;
                    }
                }

                int prevStock = Stores.Sum(s => s.Stock);
                Stores.ForEach(s => s.ProcessRequests());
                hasChanges = Stores.Sum(s => s.Stock) != prevStock;

            } while (hasChanges && Customers.Any(c => c.Remaining > 0));

            //PrintResults();
        }

        private void PrintResults()
        {
            Console.WriteLine("\nFinal results:");
            foreach (var customer in Customers)
            {
                Console.WriteLine($"Customer {customer.Id}: " +
                    $"{customer.Received.Sum(r => r.Value)}/{customer.Required}, " +
                    $"Distance: {customer.TotalDistance:F1} km");
            }

            Console.WriteLine("\nStore statistics:");
            foreach (var store in Stores)
            {
                Console.WriteLine($"Store {store.Id}: " +
                    $"{store.FailedRequests} failed requests, " +
                    $"{store.Stock} remaining stock");
            }
        }
    }
}
