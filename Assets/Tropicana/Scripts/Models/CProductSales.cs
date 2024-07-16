using System.Collections.Generic;

namespace Tropicana.Models
{
    [System.Serializable]
    public class CProductSales
    {
        public string Region;
        public List<string> Sales = new();
        public List<string> QoQ = new();
    }
}