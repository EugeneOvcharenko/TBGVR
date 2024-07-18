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
            _player.trackingSpace.position = new Vector3(1013, 996, 988);
            _player.trackingSpace.rotation = Quaternion.Euler(10, 50, 0);
        }

        public void TeleportToPCW()
        {
            _player.trackingSpace.position = new Vector3(0, 0, 0);
            _player.trackingSpace.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}