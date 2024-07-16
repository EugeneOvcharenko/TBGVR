using System.Collections.Generic;

namespace Tropicana.Models
{
    public class ProductIDComparer : IComparer <CProduct>
    {
        public int Compare(CProduct x, CProduct y) => Comparer<string>.Default.Compare(x?.Prefab, y?.Prefab);
    }
}