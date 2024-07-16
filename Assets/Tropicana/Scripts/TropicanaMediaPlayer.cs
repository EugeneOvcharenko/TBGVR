using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tropicana.Models;
using TMPro;
using System;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

namespace Tropicana
{
    public class TropicanaMediaPlayer
    {
        private AssetDownloader _assetDownloader;
        private GameObject _videoPlayerPrefab;
        private Material _skybox360Mat;
        private EventSystem _eventSystem;
        
        private GameObject _mediaPanel;
        private TMP_Text _infoText;
        private UnityEngine.UI.RawImage _mediaRawImage;
        private GameObject _prevButton;
        private GameObject _nextButton;
        private GameObject _openLinkButton;
        private GameObject _downloadFileButton;
        private GameObject _closeMediaButton;
        private float _mediaMaxWidth;
        private Action _mediaLoadedCallback;
        private Action _mediaClosedCallback;
        private Action _resetTextCallback;
        private GameObject _hideDuringMediaObj;

        private Material _initialSkybox;
        private int _initialCullingMask;

        public TropicanaMediaPlayer(
            AssetDownloader assetDownloader,
            GameObject videoPlayerPrefab,
            Material skybox360Mat,
            EventSystem eventSystem,

            GameObject mediaPanel = null,
            TMP_Text infoText = null,
            UnityEngine.UI.RawImage mediaRawImage = null,
            GameObject prevButton = null,
            GameObject nextButton = null,
            GameObject openLinkButton = null,
            GameObject downloadFileButton = null,
            GameObject closeMediaButton = null,
            float mediaMaxWidth = 0,
            Action mediaLoadedCallback = null,
            Action mediaClosedCallback = null,
            Action resetTextCallback = null,
            GameObject hideDuringMediaObj = null
        )
        {
            _assetDownloader = assetDownloader;
            _videoPlayerPrefab = videoPlayerPrefab;
            _skybox360Mat = skybox360Mat;
            _eventSystem = eventSystem;

            _mediaPanel = mediaPanel;
            _infoText = infoText;
            _mediaRawImage = mediaRawImage;
            _prevButton = prevButton;
            _nextButton = nextButton;
            _openLinkButton = openLinkButton;
            _downloadFileButton = downloadFileButton;
            _closeMediaButton = closeMediaButton;
            _mediaMaxWidth = mediaMaxWidth;
            _mediaLoadedCallback = mediaLoadedCallback;
            _mediaClosedCallback = mediaClosedCallback;
            _resetTextCallback = resetTextCallback;
            _hideDuringMediaObj = hideDuringMediaObj;

            _prevButton?.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => NextImage(false));
            _nextButton?.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => NextImage(true));
            _openLinkButton?.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OpenLink());
            _downloadFileButton?.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => DownloadFile());
            _closeMediaButton?.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => CloseMedia());

            _initialSkybox = RenderSettings.skybox;
            _initialCullingMask = Camera.main.cullingMask;
        }

        private List<string> _slideshowUris;
        private int _currentSlideShowIndex = 0;
        private GameObject _currentVideoPlayer;
        private List<string> _openLinkUris;
        private List<string> _downloadFileUris;

        private MediaType _mediaType;

        private bool _360ImageOpen = false;

        public void PlayMedia(MediaType mediaType, string mediaUri, string openLinkUri = "", string downloadFileUri = "", bool loopVideo = true)
        {
            if(_infoText != null)
            {
                _infoText.text = "Loading Media...";
            }

            _mediaType = mediaType;

            _slideshowUris = new List<string>();

            switch(mediaType)
            {
                case MediaType.Image:
                    _assetDownloader.GetTextureCoroutine<UnityEngine.UI.RawImage>(mediaUri, null, SetTextureToPanel);
                    break;
                case MediaType.Image360:
                    _assetDownloader.GetTextureCoroutine<UnityEngine.UI.RawImage>(mediaUri, null, SetTextureToSkybox);
                    break;
                case MediaType.Video:
                case MediaType.VideoFullScreen:
                case MediaType.Video360:
                    _currentVideoPlayer = GameObject.Instantiate(_videoPlayerPrefab);
                    _currentVideoPlayer.GetComponent<TropicanaVideoPlayer>().PlayVideo(mediaUri, mediaType, loopVideo, _mediaRawImage, SetTextToTemp, SetTextureToPanel, VideoClosed);
                    break;
                case MediaType.ImageSlideshow:
                    _currentSlideShowIndex = 0;
                    _slideshowUris = new List<string>(mediaUri.Split("|"));
                    _assetDownloader.GetTextureCoroutine<UnityEngine.UI.RawImage>(_slideshowUris[0], null, SetTextureToPanelWithArrows);
                    break;
            }

            if(_openLinkButton != null)
            {
                _openLinkButton.SetActive(!IsNullOrWhiteSpace(openLinkUri));
                if(_openLinkButton.activeSelf)
                {
                    _openLinkUris = new List<string>(openLinkUri.Split("|"));
                }
            }

            if(_downloadFileButton != null)
            {
                _downloadFileButton.SetActive(!IsNullOrWhiteSpace(openLinkUri));
                if(_downloadFileButton.activeSelf)
                {
                    _downloadFileUris = new List<string>(downloadFileUri.Split("|"));
                }
            }
        }

        private void SetTextureToPanelWithArrows(Texture texture, UnityEngine.UI.RawImage rawImage)
        {
            SetTextureToPanel(texture, null);
            if(texture != null)
            {
                _prevButton.SetActive(true);
                _nextButton.SetActive(true);
            }
        }

        private void SetTextureToPanel(Texture texture, UnityEngine.UI.RawImage rawImage)
        {
            if(texture != null && _mediaRawImage != null)
            {
                _mediaRawImage.texture = texture;

                RectTransform rectTransform = _mediaRawImage.GetComponent<RectTransform>();
                float width = rectTransform.rect.height * texture.width/texture.height;
                if(_mediaMaxWidth > 0)
                {
                    width = Mathf.Min(_mediaMaxWidth, width);
                }
                float height = width * texture.height/texture.width;
                rectTransform.sizeDelta = new Vector2(width, height);
                _mediaPanel.SetActive(true);
                // disable arrows
                _prevButton.SetActive(false);
                _nextButton.SetActive(false);
                _resetTextCallback?.Invoke();
                if(_hideDuringMediaObj != null)
                {
                    _hideDuringMediaObj.SetActive(false);
                }

                _mediaLoadedCallback?.Invoke();
            }
            else if(texture != null)
            {
                _mediaLoadedCallback?.Invoke();
            }
            else
            {
                SetTextToTemp("Error: Cannot load media");
            }
        }

        private void SetTextureToSkybox(Texture texture, UnityEngine.UI.RawImage rawImage)
        {
            if(texture != null)
            {
                _skybox360Mat.mainTexture = texture;
                RenderSettings.skybox = _skybox360Mat;

                Camera.main.cullingMask = 0;
                _eventSystem.enabled = false;
                _resetTextCallback?.Invoke();
                if(_hideDuringMediaObj != null)
                {
                    _hideDuringMediaObj.SetActive(false);
                }

                _360ImageOpen = true;
                _mediaLoadedCallback?.Invoke();
            }
            else
            {
                SetTextToTemp("Error: Cannot load media");
            }
        }

        private void NextImage(bool forward)
        {
            _currentSlideShowIndex += forward? 1 : -1;
            if(_currentSlideShowIndex < 0)
            {
                _currentSlideShowIndex = _slideshowUris.Count - 1;
            }
            else if(_currentSlideShowIndex >= _slideshowUris.Count)
            {
                _currentSlideShowIndex = 0;
            }

            if(_infoText != null)
            {
                _infoText.text = "Loading Media...";
            }
            _assetDownloader.GetTextureCoroutine<UnityEngine.UI.RawImage>(_slideshowUris[_currentSlideShowIndex], null, SetTextureToPanelWithArrows);
        }

        private void OpenLink()
        {
            int index = 0;
            if(_slideshowUris.Count > 0)
            {
                int maxIndex = Mathf.Min(_slideshowUris.Count-1, _openLinkUris.Count-1);
                index = Mathf.Min(_currentSlideShowIndex, maxIndex);
            }
            Application.OpenURL(_openLinkUris[index]);
        }

        private void DownloadFile()
        {
            int index = 0;
            if(_slideshowUris.Count > 0)
            {
                int maxIndex = Mathf.Min(_slideshowUris.Count-1, _downloadFileUris.Count-1);
                index = Mathf.Min(_currentSlideShowIndex, maxIndex);
            }
            
            if(_infoText != null)
            {
                _infoText.text = "Downloading File... 0%";
            }
            
            Debug.Log("Downloading " + _downloadFileUris[index]);
           _assetDownloader.DownloadFileToDownloadsFolderCoroutine(_downloadFileUris[index], SetText, SetTextToTemp, SetTextToTemp);
        }

        private void VideoClosed()
        {
            _mediaClosedCallback?.Invoke();
        }

        public void CloseMedia()
        {
            _mediaPanel?.SetActive(false);
            if(_currentVideoPlayer != null)
            {
                _currentVideoPlayer.GetComponent<TropicanaVideoPlayer>().CloseVideo();
                _currentVideoPlayer = null;
            }
            else if(_360ImageOpen)
            {
                RenderSettings.skybox = _initialSkybox;
                Camera.main.cullingMask = _initialCullingMask;
                _eventSystem.enabled = true;
                _360ImageOpen = false;
            }
            _resetTextCallback?.Invoke();
            if(_hideDuringMediaObj != null)
            {
                _hideDuringMediaObj.SetActive(true);
            }
            // If it's a video, the video player will call the callback
            if(_mediaType != MediaType.Video && _mediaType != MediaType.VideoFullScreen && _mediaType != MediaType.Video360)
            {
                _mediaClosedCallback?.Invoke();
            }
        }

        private void SetText(string text)
        {
            if(_infoText != null)
            {
                _infoText.text = text;
            }
        }

        private void SetTextToTemp(string text)
        {
            if(_infoText != null)
            {
                _infoText.text = text;
                HideText(text, 5);
            }
        }

        private async void HideText(string text, int seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            if(_infoText.text == text) {
                _infoText.text = "";
                _resetTextCallback?.Invoke();
            }
        }

        // Workaround for a backend quirk that some empty strings are becoming a string of "null"
        private bool IsNullOrWhiteSpace(string str)
        {
            if(string.IsNullOrWhiteSpace(str) || str == "null")
            {
                return true;
            }
            return false;
        }

        public RenderHeads.Media.AVProVideo.MediaPlayer GetVideoPlayer()
        {
            return _currentVideoPlayer.GetComponent<TropicanaVideoPlayer>().mediaPlayer;
        }
    }
}