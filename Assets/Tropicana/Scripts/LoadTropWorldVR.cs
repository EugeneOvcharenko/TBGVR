using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tropicana.Models;

namespace Tropicana
{
    public class LoadTropWorldVR : MonoBehaviour
    {

        [SerializeField] private string _apiUri = "https://tbgdev.eugeneovcharenko.com/api/";

        private void Awake()
        {
            StartCoroutine(ProductsProvider.Instance.LoadProductsFromJson(false, _apiUri));
            StartCoroutine(ShelfUnitProvider.Instance.LoadShelfUnitsFromJson(false, _apiUri));
        }
    }
}