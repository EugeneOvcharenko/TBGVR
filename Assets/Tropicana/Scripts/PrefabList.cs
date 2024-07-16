using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Tropicana
{
    public class PrefabList: MonoBehaviour
    {
        // For ShelfUnitController
        public GameObject bannerPrefab;
        public GameObject overlayPrefab;
        public GameObject overlayTextPrefab;
        public GameObject topBlockPrefab;
        public GameObject shelfUnitUIPrefab;
        public GameObject topProductsUIPrefab;
        public Texture defaultBannerTexture;
        public Texture defaultTopBlockTexture;
        public Shader grayscaleShader;

        // For TropicanaVideoPlayer
        public Material skybox360Mat;

        // For PlantTourSphere
        public GameObject videoPlayerPrefab;

        // For ProductFascade
        public GameObject productInfoPrefab;

        // For StarbucksProductsUI
        public GameObject starbucksBriefUI;
        public GameObject starbucksFullUI;

        // Scene Objects
        public GameObject blurVolume;
    }
}