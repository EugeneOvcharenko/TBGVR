using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Tropicana.Models
{
    public class PlantTourVideoProvider
    {
        public static PlantTourVideoProvider Instance {get; private set;} = new PlantTourVideoProvider();

        public static void ResetData()
        {
            Instance.TourPlantVideos = new();
        }

        public List<PlantTourVideo> TourPlantVideos = new();

        public IEnumerator LoadPlantTourVideosFromJson(bool useLocalJsonFiles = true, string apiUri = "", System.Action Callback = null)
        {
            string json = "";
            // placeholder for web based json loading
            if (!useLocalJsonFiles)
            {
                string uri = apiUri + "tourplantvideos";
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
                    Debug.LogError("Error downloading global banners from API. Falling back to local test JSON. Error was: " + request.error);
                }
            }
            if(useLocalJsonFiles || string.IsNullOrWhiteSpace(json))
            {
                string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "banners.json");
                json = System.IO.File.ReadAllText(filePath);
            }

            var provider = JsonUtility.FromJson<PlantTourVideoProvider>(json);
            TourPlantVideos = provider.TourPlantVideos;
            
            Debug.LogError("Plant Tour Videos were found in JSON: " + TourPlantVideos.Count);

            if(Callback != null)
            {
                Callback();
            }
        }

        
    }
}