using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ReadyPlayerMe.Samples;
using Tropicana.Models;
using TMPro;

namespace Tropicana
{
    public class PlantTour : MonoBehaviour
    {
        [SerializeField] private Vector3 _plantTourPosition;
        [SerializeField] private float _plantTourOrientation;

        [SerializeField] private Transform _buttonsParent;
        [SerializeField] private GameObject _infoNodePrefab;
        [SerializeField] private GameObject _videoTourGroupButtonPrefab;

        private TropicanaMediaPlayer _mediaPlayer;

        private List<PlantTourVideo> _currentVideos = new List<PlantTourVideo>();
        private int _currentVideoIndex;
        private List<int> _playedVideos = new List<int>();
        private bool _inVideo = false;

        private int _videoAIndex = -1;
        private int _videoBIndex = 1;
        private int _nextVideoindex = -1;

        private GameObject _player;
        private GameObject _plantTourVideoUI;

        private GameObject _loadingScreen;
        private GameObject _quitVideoConfirmationScreen;
        private GameObject _volume;

        private GameObject _previousButton;
        private GameObject _nextAButton;
        private GameObject _nextBButton;
        private GameObject _pauseButton;
        private GameObject _playButton;
        private GameObject _backButton;
        private GameObject _toggleOffButton;
        private GameObject _toggleOnButton;
        private GameObject _lookAroundIcon;
        private GameObject _confirmQuitButton;
        private GameObject _cancelQuitButton;

        private bool inPlant = false;

        private Dictionary<GameObject, InfoNode> _infoNodes = new Dictionary<GameObject, InfoNode>();

        public void SetGroupButtons()
        {
            List<string> groups = new List<string>();
            foreach(PlantTourVideo plantTourVideo in PlantTourVideoProvider.Instance.TourPlantVideos)
            {
                if(!groups.Contains(plantTourVideo.VideoGroup.Name))
                {
                    groups.Add(plantTourVideo.VideoGroup.Name);
                }
            }

            // Destroy any previously created buttons
            for(int i=0; i<_buttonsParent.childCount; i++)
            {
                Destroy(_buttonsParent.GetChild(i).gameObject);
            }

            for(int i=0; i<groups.Count; i++)
            { 
                GameObject groupButton = groupButton = Instantiate(_videoTourGroupButtonPrefab, _buttonsParent);

                string groupName = groups[i];
                groupButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
                groupButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => PlayGroup(groupName));
                groupButton.transform.GetChild(0).GetComponent<TMP_Text>().text = groupName;
            }

            // If we are already in a video (which means hot reload was called)
            if(_inVideo)
            {
                foreach(PlantTourVideo plantTourVideo in PlantTourVideoProvider.Instance.TourPlantVideos)
                {
                    if(plantTourVideo.VideoGroup.Name == _currentVideos[_currentVideoIndex].VideoGroup.Name)
                    {
                        for(int i=0; i<_currentVideos.Count; i++)
                        {
                            if(plantTourVideo.FileName == _currentVideos[i].FileName)
                            {
                                _currentVideos[i] = plantTourVideo;
                                // If we find the currently playing video in the new list of tour plant videos
                                if(i == _currentVideoIndex)
                                {
                                    // Re-create the info nodes, in order to allow quick feedback from CMS to game
                                    DestroyInfoNodes();
                                    CreateInfoNodes();
                                }
                            }
                        }

                    }
                }
            }
        }

        private void PlayGroup(string groupName)
        {
            if(_currentVideos.Count == 0)
            {
                if(_mediaPlayer == null)
                {
                    CreateMediaPlayer();
                }

                SetUpUI();

                _currentVideos = new List<PlantTourVideo>();
                _playedVideos = new List<int>();

                foreach(PlantTourVideo plantTourVideo in PlantTourVideoProvider.Instance.TourPlantVideos)
                {
                    if(plantTourVideo.VideoGroup.Name == groupName)
                    {
                        _currentVideos.Add(plantTourVideo);
                    }
                }

                _currentVideoIndex = 0;
                _playedVideos.Add(0);
                _mediaPlayer.PlayMedia(Tropicana.Models.MediaType.Video360, _currentVideos[_currentVideoIndex].FileName, "", "", false);
            }
        }

        private void OnVideoLoaded()
        {
            // Re-enable event system since it gets disabled by media player (as we have a video canvas UI)
            UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem)
            {
                eventSystem.enabled = true;
            }
            _inVideo = true;
            _loadingScreen?.SetActive(false);

            if(_loadingScreen != null)
            {
                _videoAIndex = GetVideoIndex(_currentVideos[_currentVideoIndex].FileNameLinkA);
                _videoBIndex = GetVideoIndex(_currentVideos[_currentVideoIndex].FileNameLinkB);

                _nextAButton.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = _currentVideos[_currentVideoIndex].TeaserLinkA;
                _nextBButton.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = _currentVideos[_currentVideoIndex].TeaserLinkB;

                _nextAButton.GetComponent<UnityEngine.UI.Button>().interactable = _videoAIndex >= 0;
                _nextBButton.GetComponent<UnityEngine.UI.Button>().interactable = _videoBIndex >= 0;
                _previousButton.GetComponent<UnityEngine.UI.Button>().interactable = _playedVideos.Count > 1;

                CreateInfoNodes();
            }
        }

        private void CreateInfoNodes()
        {
            var video = _currentVideos[_currentVideoIndex];
            // Support Multiple infonodes if/when backend supports it
            List<InfoNode> infoNodes = new List<InfoNode>();

            if(!string.IsNullOrEmpty(video.PopUpTitle))
            {
                // For now, just add the single one on the video if it's been set
                InfoNode singleInfoNode = new InfoNode();
                singleInfoNode.PopUpTitle = video.PopUpTitle;
                singleInfoNode.PopUpBody = video.PopUpBody;
                singleInfoNode.XPosition = video.XPosition;
                singleInfoNode.YPosition = video.YPosition;
                //infoNodes.Add(singleInfoNode);
            }

            foreach(InfoNode infoNode in infoNodes)
            {
                GameObject infoNodeGO = Instantiate(_infoNodePrefab, _plantTourVideoUI.transform.GetChild(0).GetChild(0));
                infoNodeGO.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = infoNode.PopUpTitle;
                infoNodeGO.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = infoNode.PopUpBody;
                _infoNodes.Add(infoNodeGO, infoNode);
            }
        }

        private void DestroyInfoNodes()
        {
            foreach(GameObject infoNodeGO in _infoNodes.Keys)
            {
                if(infoNodeGO != null)
                {
                    Destroy(infoNodeGO);
                }
            }
            _infoNodes = new Dictionary<GameObject, InfoNode>();
        }

        private int GetVideoIndex(string fileName)
        {
            for(int i=0; i<_currentVideos.Count; i++)
            {
                if(fileName == _currentVideos[i].FileName)
                {
                    return i;
                }
            }
            return -1;
        }

        private void OnVideoFinished()
        {
            _inVideo = false;
            _loadingScreen?.SetActive(true);

            if(_loadingScreen != null)
            {
                if(_nextVideoindex == -1)
                {
                    _currentVideoIndex++;
                    if(_currentVideos.Count > _currentVideoIndex)
                    {
                        _mediaPlayer.PlayMedia(Tropicana.Models.MediaType.Video360, _currentVideos[_currentVideoIndex].FileName, "", "", false);
                        _playedVideos.Add(_currentVideoIndex);
                    }
                    else
                    {
                        _plantTourVideoUI?.SetActive(false);
                        _currentVideos = new List<PlantTourVideo>();
                    }
                }
                else
                {
                    ShowNextVideo();
                }
            }

            DestroyInfoNodes();
        }
        
        private void CreateMediaPlayer()
        {
            AssetDownloader assetDownloader = FindObjectOfType<AssetDownloader>();
            PrefabList prefabList = FindObjectOfType<PrefabList>();

            _mediaPlayer = new TropicanaMediaPlayer(
                assetDownloader,
                prefabList.videoPlayerPrefab,
                prefabList.skybox360Mat,
                FindObjectOfType<UnityEngine.EventSystems.EventSystem>(),

                null, null, null, null, null, null, null, null, 0,

                OnVideoLoaded,
                OnVideoFinished
            );
        }

        private void SetUpUI()
        {
            if(_plantTourVideoUI == null)
            {
                //_volume = GameObject.Find("PPV_Indoor");
                _plantTourVideoUI = Camera.main.transform.Find("VideoTourUI").gameObject;

                _previousButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("PreviousButton").gameObject;
                _nextAButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("NextAButton").gameObject;
                _nextBButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("NextBButton").gameObject;
                _pauseButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("PauseButton").gameObject;
                _playButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("PlayButton").gameObject;
                _backButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("BackButton").gameObject;
                _toggleOffButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("ToggleOffButton").gameObject;
                _toggleOnButton = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("ToggleOnButton").gameObject;
                _lookAroundIcon = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("LookAround").gameObject;
                _loadingScreen = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("LoadingScreen").gameObject;
                _quitVideoConfirmationScreen = _plantTourVideoUI.transform.GetChild(0).GetChild(0).Find("QuitVideoConfirmationScreen").gameObject;
                _confirmQuitButton = _quitVideoConfirmationScreen.transform.Find("ConfirmQuitButton").gameObject;
                _cancelQuitButton = _quitVideoConfirmationScreen.transform.Find("CancelQuitButton").gameObject;

                _pauseButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(TogglePauseVideo);
                _playButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(TogglePauseVideo);
                _backButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ToggleConfirmQuit);
                _toggleOffButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ToggleLoopVideo);
                _toggleOnButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ToggleLoopVideo);
                _previousButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ShowPreviousVideoNext);
                _nextAButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ShowVideoANext);
                _nextBButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ShowVideoBNext);

                _confirmQuitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseVideoGroup);
                _cancelQuitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ToggleConfirmQuit);
            }

            _plantTourVideoUI.SetActive(true);
            _volume?.SetActive(false);

            _loadingScreen.SetActive(true);
            _quitVideoConfirmationScreen.SetActive(false);

            //_pauseButton.SetActive(true);
            _playButton.SetActive(false);
            //_toggleOffButton.SetActive(true);
            _toggleOnButton.SetActive(false);

            _nextAButton.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = "";
            _nextBButton.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = "";
            _nextAButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            _nextBButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            _previousButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        }

        private void Update()
        {
            if(_plantTourVideoUI != null && _plantTourVideoUI.activeSelf && OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four))
            {
                CloseVideoGroup();
            }
            if(_plantTourVideoUI != null && _plantTourVideoUI.activeSelf && OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
            {
                TogglePauseVideo();
            }
            if(inPlant)
            {
                /*if(Input.GetKeyDown(KeyCode.Escape))
                {
                    if(_plantTourVideoUI != null && _plantTourVideoUI.activeSelf)
                    {
                        if(!_quitVideoConfirmationScreen.activeSelf)
                        {
                            ToggleConfirmQuit();
                        }
                    }
                    else
                    {
                        inPlant = false;
                        LoadMall.Instance.SetPlayerStartPos();
                    }
                }*/
                if(_inVideo && !_quitVideoConfirmationScreen.activeSelf)
                {
                    /*if(Input.GetKeyDown(KeyCode.Space))
                    {
                        TogglePauseVideo();
                    }
                    if(Input.GetKeyDown(KeyCode.Z))
                    {
                        ShowPreviousVideoNext();
                    }
                    if(Input.GetKeyDown(KeyCode.C))
                    {
                        ShowVideoANext();
                    }
                    if(Input.GetKeyDown(KeyCode.V))
                    {
                        ShowVideoBNext();
                    }*/
                    UpdateInfoNodes();
                }
                //_player.GetComponent<CharacterController>().enabled = false;
            }
            else
            {
                /*if(Input.GetKeyDown(KeyCode.P))
                {
                    inPlant = true;
                    LoadMall.Instance.TeleportPlayer(_plantTourPosition, _plantTourOrientation);
                    SetPlayerObject();
                }*/
            }
        }

        private void UpdateInfoNodes()
        {
            foreach(GameObject infoNodeGO in _infoNodes.Keys)
            {
                InfoNode infoNode = _infoNodes[infoNodeGO];
                Vector3 direction = Quaternion.Euler(-infoNode.YPosition, infoNode.XPosition, 0) * Vector3.forward;
                if(Vector3.Dot(Camera.main.transform.forward, direction) > 0)
                {
                    Vector3 worldPosition = Camera.main.transform.position + direction * 10;
                    Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
                    RectTransform rectTransform = infoNodeGO.GetComponent<RectTransform>();
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(Camera.main.transform.parent.GetChild(2) as RectTransform, screenPosition, Camera.main, out Vector2 localPoint);
                    rectTransform.localPosition = localPoint;
                    infoNodeGO.SetActive(true);
                }
                else
                {
                    infoNodeGO.SetActive(false);
                }
            }
        }

        private void SetPlayerObject()
        {
            if(_player == null)
            {
                GameObject[] _playerObjects = GameObject.FindGameObjectsWithTag("Player");
                if(_playerObjects.Length == 1)
                {
                    _player = _playerObjects[0];
                }
            }
        }

        private void CloseVideoGroup()
        {
            _currentVideos = new List<PlantTourVideo>();
            _mediaPlayer?.CloseMedia();
            _plantTourVideoUI?.SetActive(false);
            _volume?.SetActive(true);
            _inVideo = false;
        }

        private void ToggleConfirmQuit()
        {
            _quitVideoConfirmationScreen.SetActive(!_quitVideoConfirmationScreen.activeSelf);
            if(_quitVideoConfirmationScreen.activeSelf)
            {
                var videoPlayer = _mediaPlayer.GetVideoPlayer();
                bool isPlaying = videoPlayer.Control.IsPlaying();
                if(isPlaying)
                {
                    TogglePauseVideo();
                }
            }
            else
            {
                TogglePauseVideo();
            }
        }

        private bool _videoPausePlayToggledThisFrame = false;
        private void TogglePauseVideo()
        {
            if(_inVideo && !_videoPausePlayToggledThisFrame)
            {
                _videoPausePlayToggledThisFrame = true;

                var videoPlayer = _mediaPlayer.GetVideoPlayer();
                bool isPlaying = videoPlayer.Control.IsPlaying();

                //_pauseButton.SetActive(!isPlaying);
                //_playButton.SetActive(isPlaying);

                if(isPlaying)
                {
                    videoPlayer.Pause();
                }
                else
                {
                    videoPlayer.Play();
                }
            }
        }

        private void ToggleLoopVideo()
        {
            if(_inVideo)
            {
                _videoPausePlayToggledThisFrame = true;

                var videoPlayer = _mediaPlayer.GetVideoPlayer();
                videoPlayer.Loop = !videoPlayer.Loop;

                _toggleOffButton.SetActive(!videoPlayer.Loop);
                _toggleOnButton.SetActive(videoPlayer.Loop);
            }
        }

        private void ShowPreviousVideoNext()
        {
            if(_inVideo && _playedVideos.Count > 1)
            {
                _playedVideos.RemoveAt(_playedVideos.Count - 1);
                _nextVideoindex = _playedVideos[_playedVideos.Count - 1];
                // Remove the previous video as well because it will be readded in the ShowNextVideo function
                _playedVideos.RemoveAt(_playedVideos.Count - 1);
                _mediaPlayer.CloseMedia();
            }
        }

        private void ShowVideoANext()
        {
            if(_inVideo && _videoAIndex >= 0)
            {
                _nextVideoindex = _videoAIndex;
                _mediaPlayer.CloseMedia();
            }
        }

        private void ShowVideoBNext()
        {
            if(_inVideo && _videoBIndex >= 0)
            {
                _nextVideoindex = _videoBIndex;
                _mediaPlayer.CloseMedia();
            }
        }

        private void ShowNextVideo()
        {
            _currentVideoIndex = _nextVideoindex;
            _nextVideoindex = -1;
            _playedVideos.Add(_currentVideoIndex);

            _mediaPlayer.PlayMedia(Tropicana.Models.MediaType.Video360, _currentVideos[_currentVideoIndex].FileName, "", "", _toggleOnButton.activeSelf);
        }

        private void LateUpdate()
        {
            _videoPausePlayToggledThisFrame = false;
        }
    }
}