using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;

namespace Tropicana
{
    public class AssetDownloader: MonoBehaviour
    {
        [SerializeField] private string assetUri = "https://tbgdev.eugeneovcharenko.com";

        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();

        private string AddHostToUri(string uri)
        {
            if(!(uri.StartsWith("http") || uri.StartsWith("//")))
            {
                if(uri.StartsWith("/"))
                {
                    return assetUri + uri; 
                }
                else
                {
                    return assetUri + "/" + uri;
                }
            }
            return uri;
        }

        public void GetTextureCoroutine<T>(string uri, T arg, Action<Texture, T> Callback = null, bool trimWhitespace = false)
        {
            StartCoroutine(GetTexture(uri, arg, Callback, trimWhitespace));
        }

        public IEnumerator GetTexture<T>(string uri, T arg, Action<Texture, T> Callback = null, bool trimWhitespace = false)
        {
            uri = AddHostToUri(uri);

            if (!textures.ContainsKey(uri))
            {
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error + " - " + uri);
                }
                else
                {
                    Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    if (trimWhitespace)
                    {
                        texture = TrimWhitespace(texture);
                    }
                    textures[uri] = texture;
                }
            }

            if (Callback != null)
            {
                if (textures.ContainsKey(uri))
                {
                    Callback(textures[uri], arg);
                }
                else
                {
                    Callback(null, arg);
                }
            }
        }

        public IEnumerator GetAudioClip(string uri, System.Action<AudioClip> Callback = null) {
            uri = AddHostToUri(uri);
            if(!audioClips.ContainsKey(uri))
            {
                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success) {
                    Debug.Log(www.error);
                }
                else
                {
                    AudioClip audioClip = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
                    audioClips[uri] = audioClip;
                }
            }
            if(audioClips.ContainsKey(uri) && Callback != null)
            {
                Callback(audioClips[uri]);
            }
        }

        public void DownloadFileToDownloadsFolderCoroutine(string uri, Action<string> ProgressCallback = null, Action<string> CompletedCallback = null, Action<string> FailedCallback = null)
        {
            StartCoroutine(DownloadFileToDownloadsFolder(uri, ProgressCallback, CompletedCallback, FailedCallback));
        }

        public IEnumerator DownloadFileToDownloadsFolder(string uri, Action<string> ProgressCallback = null, Action<string> CompletedCallback = null, Action<string> FailedCallback = null)
        {
            const float updateInterval = 0.1f;

            UnityWebRequest www = UnityWebRequest.Get(uri);

            // Use DownloadHandlerFile to download large files
            string tempFilePath = Path.Combine(Application.persistentDataPath, "temp_download_file");
            DownloadHandlerFile downloadHandlerFile = new DownloadHandlerFile(tempFilePath);
            www.downloadHandler = downloadHandlerFile;

            // Start the download
            www.SendWebRequest();


            float lastProgress = 0f;

            while (!www.isDone)
            {
                // Calculate progress percentage
                float progress = www.downloadProgress * 100f;

                // Log progress if it has changed significantly since the last time
                if (progress - lastProgress >= 1f)
                {
                    ProgressCallback?.Invoke("Downloading File... " + progress.ToString("F2") + "%");
                    lastProgress = progress;
                }

                yield return new WaitForSeconds(updateInterval);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                FailedCallback?.Invoke("Downloading file failed: " + www.error);
            }
            else
            {
                // Once download is finished, save the downloaded file to the final location
                string finalFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", GetFileNameFromURL(uri, www.GetResponseHeader("Content-Type").Split('/')[1]));
                int i = 1;
                while(File.Exists(finalFilePath))
                {
                    finalFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", GetFileNameFromURL(uri, www.GetResponseHeader("Content-Type").Split('/')[1], i));
                    i++;
                }
                File.Move(tempFilePath, finalFilePath);
                CompletedCallback?.Invoke("File downloaded and saved to: " + finalFilePath);
            }
        }

        private string GetFileNameFromURL(string url, string fileType, int i=0)
        {
            string fileName = "downloaded-file";
            if(!string.IsNullOrEmpty(fileType))
            {
                fileName += "." + fileType;
            }

            Uri uri;
            try
            {
                uri = new Uri(url);
                if(!string.IsNullOrEmpty(Path.GetFileName(uri.LocalPath)))
                {
                    fileName = Path.GetFileName(uri.LocalPath);
                }
            }
            catch (UriFormatException)
            {

            }

            if(i > 0)
            {
                string[] filenameSplit = fileName.Split(".");
                fileName = "";
                for(int j = 0; j<filenameSplit.Length-1; j++)
                {
                    fileName += filenameSplit[j];
                }
                fileName += "(" + i.ToString() + ")";
                fileName += "." + filenameSplit[filenameSplit.Length-1];
            }

            return fileName;
        }

        private Texture2D TrimWhitespace(Texture2D original, int border = 5)
        {
            int width = original.width;
            int height = original.height;

            // Determine the bounds of the non-whitespace area
            int minX = width, minY = height, maxX = 0, maxY = 0;
            bool foundNonWhite = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = original.GetPixel(x, y);
                    if (pixel.a > 0.01f && !(pixel.r == 1 && pixel.g == 1 && pixel.b == 1)) // You can adjust the threshold as needed
                    {
                        foundNonWhite = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (!foundNonWhite)
            {
                // No non-whitespace found, return the original texture or handle as needed
                return original;
            }

            // Adjust bounds to include border, and clamp to texture size
            minX = Mathf.Max(0, minX - border);
            minY = Mathf.Max(0, minY - border);
            maxX = Mathf.Min(width - 1, maxX + border);
            maxY = Mathf.Min(height - 1, maxY + border);

            // Calculate width and height of the new texture
            int newWidth = maxX - minX + 1;
            int newHeight = maxY - minY + 1;

            // Create the new texture and copy the relevant pixels
            Texture2D newTexture = new Texture2D(newWidth, newHeight);
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    Color pixel = original.GetPixel(minX + x, minY + y);
                    newTexture.SetPixel(x, y, pixel);
                }
            }

            newTexture.Apply();
            return newTexture;
        }
    }
}