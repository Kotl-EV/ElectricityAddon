using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityUnofficial.Utils
{
    public class Customer
    {
        public int Id { get; }
        public float Required { get; }
        public Dictionary<Store, float> StoreDistances { get; }
        public Dictionary<Store, float> Received { get; } = new Dictionary<Store, float>();

        private IEnumerable<Store> _orderedStores;

        public Customer(int id, float required, Dictionary<Store, float> distances)
        {
            Id = id;
            Required = required;
            StoreDistances = distances;
            UpdateOrderedStores();
        }

        public float Remaining => Required - Received.Sum(r => r.Value);
        public double TotalDistance => Received.Sum(kvp => kvp.Key.DistanceTo(this) * kvp.Value);

        public void UpdateOrderedStores()
        {
            _orderedStores = StoreDistances
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public IEnumerable<Store> GetAvailableStores() => _orderedStores;
    }
}
