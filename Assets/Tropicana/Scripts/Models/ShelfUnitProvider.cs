using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Tropicana.Models
{
    public class ShelfUnitProvider
    {
        public static ShelfUnitProvider Instance {get; private set;} = new ShelfUnitProvider();
        
        public static void ResetData()
        {
            Instance.jsonLoaded = false;
            Instance.ShelfUnits = new();
        }

        public List<ShelfUnit> ShelfUnits = new();

        private bool jsonLoaded = false;
        public bool shelfUnitsLoaded
        {
            get { return jsonLoaded; }
        }

        public static event System.Action OnShelfUnitsLoaded;

        private string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "planograms.json");

        public IEnumerator LoadShelfUnitsFromJson(bool useLocalJsonFiles = true, string apiUri = "", System.Action Callback = null)
        {
            jsonLoaded = false;
            string json = "";

            // Variables for storing the local ShelfUnits as well even when loading from backend
            // Will not be needed when CMS is complete
            string json2 = "";
            List<ShelfUnit> ShelfUnits2 = null;

            if (!useLocalJsonFiles)
            {
                string uri = apiUri + "shelves";
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
                    Debug.LogError("Error downloading shelf units from API. Falling back to local test JSON. Error was: " + request.error);
                }

                // Get the local ShelfUnits as well even when loading from backend
                // Will not be needed when CMS is complete
                /*json2 = System.IO.File.ReadAllText(filePath);
                ShelfUnits2 = JsonUtility.FromJson<ShelfUnitProvider>(json2).ShelfUnits;*/
            }
            if(useLocalJsonFiles || string.IsNullOrWhiteSpace(json))
            {
                json = System.IO.File.ReadAllText(filePath);
            }

            var provider = JsonUtility.FromJson<ShelfUnitProvider>(json);
            ShelfUnits = provider.ShelfUnits;

            // START: TEMP CODE WHILE WORKING ON BACKEND
            // Use the local JSON, but replace any shelf units with those on backend
            // But, keep local Banners, Top Blocks while that is being added to CMS
            if(ShelfUnits2 != null)
            {
                foreach(ShelfUnit shelfUnitBackend in ShelfUnits)
                {
                    foreach(ShelfUnit shelfUnitLocal in ShelfUnits2)
                    {
                        if(shelfUnitLocal.Id == shelfUnitBackend.Id)
                        {
                            if(shelfUnitBackend.MenuButtons.Count == 0)
                            {
                                shelfUnitBackend.MenuButtons = shelfUnitLocal.MenuButtons;
                            }

                            for(int i=0; i<shelfUnitBackend.Planograms.Count && i<shelfUnitLocal.Planograms.Count; i++)
                            {
                                if(shelfUnitBackend.Planograms[i].Banners.Count == 0)
                                {
                                    shelfUnitBackend.Planograms[i].Banners = shelfUnitLocal.Planograms[i].Banners;
                                }
                                if(shelfUnitBackend.Planograms[i].TopBlocks.Count == 0)
                                {
                                    shelfUnitBackend.Planograms[i].TopBlocks = shelfUnitLocal.Planograms[i].TopBlocks;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            // END: TEMP CODE WHILE WORKING ON BACKEND

            jsonLoaded = true;
            
            Debug.LogError("Shelf Units were found in JSON: " + ShelfUnits.Count);

            if(OnShelfUnitsLoaded != null)
            {
                OnShelfUnitsLoaded();
            }

            if(Callback != null)
            {
                Callback();
            }
        }

        public List<Planogram> GetPlanograms(int shelfUnitId)
        {
            if(jsonLoaded)
            {
                foreach(ShelfUnit shelfUnit in ShelfUnits)
                {
                    if(shelfUnit.Id == shelfUnitId)
                    {
                        // When loading in editor mode, json could update so unmark loaded so it reloads next time
                        if(!Application.isPlaying)
                        {
                            jsonLoaded = false;
                        }
                        return shelfUnit.Planograms;
                    }
                }
            }
            else
            {
                Debug.LogError("Cannot get planograms as planogram JSON has not been loaded");
            }

            return new List<Planogram>();
        }

        public List<ShelfUnitMenuButton> GetMenuButtons(int shelfUnitId)
        {
            if(jsonLoaded)
            {
                foreach(ShelfUnit shelfUnit in ShelfUnits)
                {
                    if(shelfUnit.Id == shelfUnitId)
                    {
                        // When loading in editor mode, json could update so unmark loaded so it reloads next time
                        if(!Application.isPlaying)
                        {
                            jsonLoaded = false;
                        }
                        return shelfUnit.MenuButtons;
                    }
                }
            }
            else
            {
                Debug.LogError("Cannot get menu buttons as planogram JSON has not been loaded");
            }

            return new List<ShelfUnitMenuButton>();
        }

        public void SaveShelfUnitPlanograms(int shelfUnitId, List<Planogram> planograms)
        {
            foreach(ShelfUnit shelfUnit in ShelfUnits)
            {
                if(shelfUnit.Id == shelfUnitId)
                {
                    shelfUnit.Planograms = planograms;

                    string jsonString = JsonUtility.ToJson(this);
                    System.IO.File.WriteAllText(filePath, jsonString);
                    break;
                }
            }
        }

        public void SaveShelfUnitPlanogram(int shelfUnitId, Planogram planogram, int planogramIndex)
        {
            Debug.Log("SaveShelfUnitPlanogram with "+ ShelfUnits.Count + " ShelfUnits. Saving " + shelfUnitId + " using index " + planogramIndex + " and name " + planogram.Name);
            bool found = false;
            foreach(ShelfUnit shelfUnit in ShelfUnits)
            {
                Debug.Log("shelfUnit " + shelfUnit.Id + "comparing to " + shelfUnitId);
                if(shelfUnit.Id == shelfUnitId)
                {
                    found = true;
                    if(planogramIndex >= 0)
                    {
                        
                        shelfUnit.Planograms[planogramIndex] = planogram;
                    }
                    else
                    {
                        shelfUnit.Planograms.Add(planogram);
                    }

                    string jsonString = JsonUtility.ToJson(this);
                    System.IO.File.WriteAllText(filePath, jsonString);
                    break;
                }
            }
            if(!found && shelfUnitId >= 0)
            {
                ShelfUnit shelfUnit = new ShelfUnit();
                shelfUnit.Id = shelfUnitId;
                shelfUnit.Planograms = new List<Planogram>() { planogram };
                ShelfUnits.Add(shelfUnit);

                string jsonString = JsonUtility.ToJson(this);
                System.IO.File.WriteAllText(filePath, jsonString);
            }
        }
    }
}