using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Tropicana.Models
{
    public class ProductsProvider 
    {
        public static ProductsProvider Instance {get; private set;} = new ProductsProvider();

        public static void ResetData()
        {
            Instance.jsonLoaded = false;
            Instance.Products = new();
        }

        readonly ProductIDComparer _productIDComparer = new ProductIDComparer();
        public List<CProduct> Products = new();

        private bool jsonLoaded = false;
        public bool productsLoaded
        {
            get { return jsonLoaded; }
        }

        public IEnumerator LoadProductsFromJson(bool useLocalJsonFiles = true, string apiUri = "", System.Action Callback = null)
        {
            jsonLoaded = false;
            string json = "";
            // placeholder for web based json loading
            if (!useLocalJsonFiles)
            {
                string uri = apiUri + "products";
                UnityWebRequest request = UnityWebRequest.Get(uri);

                string authKey = "Authorization";
                string authToken = "APIToken abcde12345qwert3";
                request.SetRequestHeader(authKey, authToken);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    json = request.downloadHandler.text;
                }
                else
                {
                    Debug.LogError("Error downloading products from API. Falling back to local test JSON. Error was: " + request.error);
                }
            }
            if(useLocalJsonFiles || string.IsNullOrWhiteSpace(json))
            {
                string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "products.json");
                json = System.IO.File.ReadAllText(filePath);
            }

            var provider = JsonUtility.FromJson<ProductsProvider>(json);
            Products = provider.Products;
            
            jsonLoaded = true;
            Debug.LogError("Products were found in JSON: " + Products.Count);

            PrepareForSearch();

            if(Callback != null)
            {
                Callback();
            }
        }

        public void PrepareForSearch()
        {
            Products.Sort(_productIDComparer);
            // AddDummySalesToJson(); // uncomment to override json with dummy sales data
        }

        public void AddDummySalesToJson()
        {
            var regions = new[]
            {
                "North America", "EMEA", "APAC", "Latin America"
            };
            
            foreach (var product in Products)
            {
                product.Sales = new List<CProductSales>();
                for (int i = 0; i < regions.Length; i++)
                {
                    CProductSales sale = new CProductSales();
                    sale.Region = regions[i];
                    
                    for (int s = 0; s < 4; s++)
                    {
                        sale.Sales.Add(Random.Range(0.1f,100000000f).ToString("C0"));
                        sale.QoQ.Add(Random.Range(1,Random.Range(2,100)).ToString());
                    }
                    product.Sales.Add(sale);
                }
            }
            
            var filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "products.json");
            System.IO.File.WriteAllText(filePath, JsonUtility.ToJson(this));
        }

        public List<CProduct> GetProducts()
        {
            return Products;
        }
        
        public CProduct GetProduct(string id)
        {
            if(jsonLoaded)
            {
                // Debug.Log("Looking up product " + id);
                var dummy = new CProduct()
                {
                    Prefab = id
                };
                var index = Products.BinarySearch(dummy, _productIDComparer);
                if (index >= 0) 
                {
                    // Debug.Log("Product [" + id + "] found - " + index);
                    return Products[index];
                }
                Debug.LogWarning("product [" + id + "] not found" + index);
                return null;
            }
            else
            {
                Debug.LogError("Product json has not been loaded");
                return null;
            }
        }
    }
}