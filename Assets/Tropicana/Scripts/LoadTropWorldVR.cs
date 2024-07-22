using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tropicana.Models;

namespace Tropicana
{
    public class LoadTropWorldVR : MonoBehaviour
    {

        [SerializeField] private string _apiUri = "https://tbgdev.eugeneovcharenko.com/api/";
        private OVRCameraRig _player;

        private void Awake()
        {
            _player = FindObjectOfType<OVRCameraRig>();
            StartCoroutine(ProductsProvider.Instance.LoadProductsFromJson(false, _apiUri));
            StartCoroutine(ShelfUnitProvider.Instance.LoadShelfUnitsFromJson(false, _apiUri));
            StartCoroutine(PlantTourVideoProvider.Instance.LoadPlantTourVideosFromJson(false, _apiUri, FindObjectOfType<PlantTour>().SetGroupButtons));
        }

        public void TeleportToPlantTour()
        {
            // _player.trackingSpace.position = new Vector3(1000f, 996f, 1018f);
            _player.trackingSpace.position = new Vector3(1000f, 996f, 981f);
        }

        public void TeleportToPCW()
        {
            _player.trackingSpace.position = new Vector3(0, 0, 0);
        }
    }
}