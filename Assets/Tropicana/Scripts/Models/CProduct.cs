using System.Collections.Generic;

namespace Tropicana.Models
{
    [System.Serializable]
    public class CProduct
    {
        public string Prefab;
        public string FullName;
        public string ShortName;
        public string SKU;
        public string UPC;
        public string GTIN;
        public string Description;
        public string Volume;
        public string Brand;
        public string Category;
        public string PackageType;
        public bool CannotBePickedUp;
        public string LabelImage;

        public List<CProductSales> Sales = new();
    }
}