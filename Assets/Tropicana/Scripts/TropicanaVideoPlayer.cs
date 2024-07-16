using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using Tropicana.Models;
using ReadyPlayerMe.Samples;

namespace Tropicana
{
    public class TropicanaVideoPlayer : MonoBehaviour
    {
        private Material _skybox360Mat;
        private Material _initialSkybox;
        private int _initialCullingMask;
        private MediaPlayer _mediaPlayer;
        private ResolveToRenderTexture _resolveToRenderTexture;
        private RenderTexture _rt;
        Tropicana.Models.MediaType _mediaType;

        private Transform _player;
        private CameraOrbit _cameraOrbit;

        private Vector3 _playerPositionBeforeVideoStart;
        private Quaternion _playerRotationBeforeVideoStart;
        private Quaternion _cameraRigRotationBeforeVideoStart;
        private float _pitchBeforeVideoStart;
        private float _yawBeforeVideoStart;

        UnityEngine.UI.RawImage _rawImage;
        System.Action<Texture, UnityEngine.UI.RawImage> _CallbackStart = null;
        System.Action _CallbackEnd = null;
        System.Action<string> _CallbackError = null;

        public MediaPlayer mediaPlayer
        {
            get { return _mediaPlayer; }
        }

        private void Awake()
        {
            _mediaPlayer = GetComponent<MediaPlayer>();
            _resolveToRenderTexture = GetComponent<ResolveToRenderTexture>();
            _mediaPlayer.Events.AddListener(HandleEvent);

            TropicanaVideoPlayer[] tropicanaVideoPlayers = FindObjectsOfType<TropicanaVideoPlayer>();
            foreach(TropicanaVideoPlayer tropicanaVideoPlayer in tropicanaVideoPlayers)
            {
                if(tropicanaVideoPlayer != this)
                {
                    Destroy(tropicanaVideoPlayer.gameObject);
                }
            }

            PrefabList prefabList = FindObjectOfType<PrefabList>();
            _skybox360Mat = prefabList.skybox360Mat;

            _initialSkybox = RenderSettings.skybox;
            _initialCullingMask = Camera.main.cullingMask;
            SetPlayerObjects();
        }

        private void SetPlayerObjects()
        {
            if(_player == null)
            {
                GameObject[] _playerObjects = GameObject.FindGameObjectsWithTag("Player");
                if(_playerObjects.Length == 1)
                {
                    _player = _playerObjects[0].transform;
                }
            }
            if(_cameraOrbit == null)
            {
                _cameraOrbit = FindObjectOfType<CameraOrbit>();
            }
        }

        public void PlayVideo(string uri, Tropicana.Models.MediaType mediaType, bool loop = true, UnityEngine.UI.RawImage rawImage = null, System.Action<string> CallbackError = null, System.Action<Texture, UnityEngine.UI.RawImage> CallbackStart = null, System.Action CallbackEnd = null)
        {
            _mediaType = mediaType;
            _rawImage = rawImage;
            _CallbackError = CallbackError;
            _CallbackStart = CallbackStart;
            _CallbackEnd = CallbackEnd;

            _mediaPlayer.Loop = loop;

            /*string username = "jose";
            string password = "j0s3";
            string base64token = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" +
            password));
            Debug.Log(base64token);
            _mediaPlayer.GetCurrentPlatformOptions().httpHeaders.Add("Authorization", "Basic " + base64token);*/

            switch(_mediaType)
            {
                case Tropicana.Models.MediaType.Video:
                case Tropicana.Models.MediaType.Video360:
                    GetComponent<ResolveToRenderTexture>().enabled = true;
                    break;
                case Tropicana.Models.MediaType.VideoFullScreen:
                    GetComponent<DisplayIMGUI>().enabled = true;
                    break;
            }

            bool isOpening = _mediaPlayer.OpenMedia(new MediaPath(uri, MediaPathType.AbsolutePathOrURL), autoPlay:false);
        }

        void HandleEvent(MediaPlayer mp, MediaPlayerEvent.EventType eventType, ErrorCode code)
        {
            //Debug.Log("MediaPlayer " + mp.name + " generated event: " + eventType.ToString());
            if (eventType == MediaPlayerEvent.EventType.Error)
            {
                ErrorReceived();
            }
            else if(eventType == MediaPlayerEvent.EventType.ReadyToPlay)
            {
                StartPlaying();
            }
            else if(eventType == MediaPlayerEvent.EventType.FinishedPlaying)
            {
                Destroy(gameObject);
            }
        }

        private void ErrorReceived()
        {
            if(_CallbackError != null)
            {
                _CallbackError("Error: Cannot load video");
                Destroy(gameObject);
            }
        }

        private void StartPlaying()
        {
            if(_mediaType != Tropicana.Models.MediaType.VideoFullScreen)
            {
                _rt = new RenderTexture(_mediaPlayer.Info.GetVideoWidth(), _mediaPlayer.Info.GetVideoHeight(), 16, RenderTextureFormat.ARGB32);
                _rt.Create();
                _resolveToRenderTexture.ExternalTexture = _rt;
            }
            else
            {
                //TODO: This commented out code was for the Unity Video Player, revisit it for the AVPro Video Player

                // Disabling setting aspect radio because Screen Space Outlines are causing
                // the camera backgrond color to be the outline color rather than the one in camera settings
                // So leaving the video to stretched instead, so it covers the entire screen
                /*if(_videoPlayer.texture.width >= _videoPlayer.texture.height)
                {
                    _videoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitHorizontally;
                }
                else
                {
                    _videoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitVertically;
                }*/
            }

            HandlePlayVideoStart();

            _CallbackStart?.Invoke(_rt, _rawImage);

            _mediaPlayer.Play();
        }

        private void Update()
        {
            // Pause functionality only for Full Screen and 360 videos
            /*if(_mediaType != Tropicana.Models.MediaType.Video && Input.GetKeyDown(KeyCode.Space))
            {
                if(_mediaPlayer.Control.IsPlaying())
                {
                    _mediaPlayer.Pause();
                }
                else
                {
                    _mediaPlayer.Play();
                }
            }*/
        }

        private void HandlePlayVideoStart()
        {
            if(Application.isPlaying)
            {
                SetPlayerObjects();
                
                if(_mediaType == MediaType.VideoFullScreen ||
                    _mediaType == MediaType.Video360)
                {
                    int videoLayerOnly = 1 << LayerMask.NameToLayer("Video");
                    Camera.main.cullingMask = videoLayerOnly;

                    if(_mediaType == MediaType.Video360)
                    {
                        _skybox360Mat.mainTexture = _rt;
                        RenderSettings.skybox = _skybox360Mat;
                    }
                    else
                    {
                        Camera.main.clearFlags = CameraClearFlags.SolidColor;
                    }

                    /*if(_cameraOrbit == null)
                    {
                        _cameraOrbit = FindObjectOfType<CameraOrbit>();
                    }
                    _playerPositionBeforeVideoStart = _player.localPosition;
                    _playerRotationBeforeVideoStart = _player.GetChild(1).localRotation;
                    _cameraRigRotationBeforeVideoStart = Camera.main.transform.parent.localRotation;
                    if (_cameraOrbit != null)
                    {
                        _pitchBeforeVideoStart = _cameraOrbit.pitch;
                        _yawBeforeVideoStart = _cameraOrbit.yaw;
                    }*/

                    UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                    if (eventSystem)
                    {
                        eventSystem.enabled = false;
                    }
                }
            }
        }

        private void OnVideoFinished()
        {
            if(Camera.main != null)
            {
                Camera.main.cullingMask = _initialCullingMask;
                Camera.main.clearFlags = CameraClearFlags.Skybox;
            }

            RenderSettings.skybox = _initialSkybox;

            if (_mediaType == Tropicana.Models.MediaType.Video360 || _mediaType == Tropicana.Models.MediaType.VideoFullScreen)
            {
                UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if(eventSystem != null)
                {
                    eventSystem.enabled = true;
                }

                /*if(_player != null && Camera.main != null)
                {
                    _player.localPosition = _playerPositionBeforeVideoStart;
                    _player.GetChild(1).localRotation = _playerRotationBeforeVideoStart;
                    Camera.main.transform.parent.localRotation = _cameraRigRotationBeforeVideoStart;
                    if (_cameraOrbit != null)
                    {
                        _cameraOrbit.pitch = _pitchBeforeVideoStart;
                        _cameraOrbit.yaw = _yawBeforeVideoStart;
                    }
                    _player.GetComponent<CharacterController>().enabled = false;
                    LoadMall.Instance.EnableCharacterController();
                }*/
            }
        }

        public void CloseVideo()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if(_rt != null)
            {
                _rt.Release();
            }

            OnVideoFinished();

            if(_CallbackEnd != null)
            {
                _CallbackEnd();
            }
        }
    }
}