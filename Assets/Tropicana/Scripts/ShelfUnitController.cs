using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tropicana.Models;
using TMPro;
using UnityEngine.Networking;
using System.Linq;
using HighlightPlus;

namespace Tropicana {

    public enum AlignProductEdgeType {
        AlignFrontEdge,
        AlignMiddle,
        AlignBackEdge
    };

    public class ShelfUnitController : MonoBehaviour
    {
        private enum ShelfUnitState {
            None,
            Banners,
            Overlays,
            TopProducts,
            Innovations
        };

        private ShelfUnitState _shelfUnitState = ShelfUnitState.None;
        private bool _mediaOpened = false;
        private bool _disableAllExtrasWhenMediaLoads = false;

        private Transform _productsParent;
        private Transform _productsParentBackup;
        private Transform _bannersParent;
        private Transform _overlaysParent;
        private Transform _infoParent;

        [SerializeField] private bool _leftToRight = true;
        [SerializeField] private bool _showDebug = false;
        // Positions pointing to outer-bottom point of shelf start;
        [SerializeField] private Vector3[] _shelfStartPositions;
        public Vector3[] shelfStartPositions
        {
            get { return _shelfStartPositions; }
        }
        // Positions pointing to outer-bottom point of shelf end;
        [SerializeField] private Vector3[] _shelfEndPositions;
        public Vector3[] shelfEndPositions
        {
            get { return _shelfEndPositions; }
        }

        [SerializeField] private float[] _shelfGapsStartDistances;
        public float[] shelfGapsStartDistances
        {
            get { return _shelfGapsStartDistances; }
        }
        [SerializeField] private float[] _shelfGapsEndDistances;
        public float[] shelfGapsEndDistances
        {
            get { return _shelfGapsEndDistances; }
        }

        [SerializeField] private float _productXRot;
        public bool isSlanted
        {
            get { return _productXRot != 0; }
        }
        [SerializeField] private float _productYRot;
        public float productYRot
        {
            get { return _productYRot; }
        }

        [SerializeField] private int _numCopyRows = 0;
        [SerializeField] private float _rowSpacing = 0.05f;
        [SerializeField] private AlignProductEdgeType _alignProductEdgeType = AlignProductEdgeType.AlignFrontEdge;

        [SerializeField] private Vector3 _topCorner;
        [SerializeField] private float _overlayInfoBoxXOffset = 0.0f;
        [SerializeField] private float _overlayInfoBoxZOffset = -0.16f;
        
        [SerializeField] private float _topBlockSpacing = 0.05f;
        [SerializeField] private float _topBlockOverhangPerSide = 0f;

        [SerializeField] private bool _createUI = true;
        [SerializeField] private bool _hideProductsPastShelfEnd = true;
        [SerializeField] private bool _useFadeToBlack = true;

        private float _uiButtonYSpacing = 26;
        private float _uiButtonXSpacing = 50;

        [SerializeField] private List<ShelfUnitController> _shelfUnitsToToggle;
        
        // Prefabs
        private GameObject _shelfUnitUIPrefab;
        private GameObject _topProductsUIPrefab;
        private GameObject _bannerPrefab;
        private GameObject _overlayPrefab;
        private GameObject _overlayTextPrefab;
        private GameObject _topBlockPrefab;
        private Texture _defaultBannerTexture;
        private Texture _defaultTopBlockTexture;
        private Shader _grayscaleShader;

        private GameObject _UI;
        private GameObject _topProductsUI;
        private GameObject _mediaPanel;
        private GameObject _innovationsButton;
        private GameObject _topProductsButton;
        private GameObject _overlaysButton;
        private GameObject _bannersButton;
        private List<GameObject> _planogramIdToButton = new List<GameObject>();

        private TropicanaMediaPlayer _mediaPlayer;
        private AssetDownloader _assetDownloader;
        
        private FadeToBlack _fadeToBlack;

        private Planogram _currentPlanogram;
        private int _currentPlanogramId = -1;
        private int _initialPlanogramIdToLoad = 0;

        private List<GameObject> _selectedTopProducts = new List<GameObject>();
        private List<GameObject> _selectedInnovations = new List<GameObject>();
        private Dictionary<GameObject, PlanogramOverlay> _productToTopProduct = new Dictionary<GameObject, PlanogramOverlay>();
        private Dictionary<GameObject, PlanogramOverlay> _productToInnovation = new Dictionary<GameObject, PlanogramOverlay>();
        private Dictionary<string, Color> _topProductsColorDict = new Dictionary<string, Color>();
        private Dictionary<string, Color> _innovationsColorDict = new Dictionary<string, Color>();
        private Dictionary<Material, Shader> _materialToOriginalShader = new Dictionary<Material, Shader>();
        private LayerMask _oldLayer;
        private PlanogramOverlay _selectedTopProductsPrefab;
        private GameObject _lastCreatedTextBox = null;
        private PlanogramOverlay _lastCreatedTextBoxOverlay = null;

        [SerializeField] [HideInInspector] private int _shelfUnitId = -1;
        [SerializeField] private List<Planogram> _planograms;
        private List<ShelfUnitMenuButton> _menuButtons;
        private int _uiSetsToggleMenuButtonColumn = -1;

        private bool initWhenProductsLoaded = false;

        private int numProductsPerFrame = 0;
        private int numProducts = 0;
        
        [SerializeField] GameObject testGrayscale;

        private void OnDrawGizmosSelected()
        {
            if (!_showDebug)
            {
                return;
            }
            
            Color[] row_colors = new []
            {
                new Color(.5f,.5f,.25f),
                new Color(.5f,.25f,.25f),
                new Color(.25f,.25f,.25f),
                new Color(.25f,.5f,.25f),
                new Color(.25f,.25f,.5f),
                new Color(.25f,.5f,.5f),
                new Color(.55f,.5f,.5f),
            };
            
            for (int i = 0; i < Mathf.Min(_shelfStartPositions.Length,_shelfEndPositions.Length); i++)
            {
                if (row_colors.Length > i)
                {
                    Gizmos.color = row_colors[i];
                }

                int steps = 16;
                float size = .025f;
                for (int j = 0; j < steps; j++) {
                    Vector3 center = transform.TransformPoint(Vector3.Lerp(_shelfStartPositions[i], _shelfEndPositions[i], (float)j/(float)(steps - 1) ));
                    Gizmos.DrawCube(center, new Vector3(size,size,size));
                }

                if (_shelfGapsStartDistances.Length > i && _shelfGapsEndDistances.Length > i)
                {
                    Vector3 _gapA = transform.TransformPoint(new Vector3(_shelfGapsStartDistances[i], _shelfStartPositions[i].y, _shelfStartPositions[i].z));
                    Vector3 _gapB = transform.TransformPoint(new Vector3(_shelfGapsEndDistances[i], _shelfEndPositions[i].y, _shelfEndPositions[i].z));
                    Gizmos.DrawLine(_gapA, _gapB);
                }
            }
        }

        public int currentPlanogramId
        {
            get { return _currentPlanogramId; }
        }

        public int initialPlanogramIdToLoad
        {
            get { return _initialPlanogramIdToLoad; }
            set { _initialPlanogramIdToLoad = value; }
        }

        public int shelfUnitId
        {
            get { return _shelfUnitId; }
        }

        public List<Planogram> planograms
        {
            get { return _planograms; }
        }

        public bool AllowPickup
        {
            get
            {
                return !_mediaOpened;
            }
        }

        private Vector3 shelfDirection {
            get {
                return (_shelfEndPositions[0] - _shelfStartPositions[0]).normalized;
            }
        }

        private Vector3 depthDirection {
            get {
                // depthDirection is calculated from _productYRot
                return Quaternion.AngleAxis(_productYRot, Vector3.up) * -Vector3.forward;
            }
        }

        private float shelfLength {
            get {
                return Vector3.Distance(_shelfEndPositions[0], _shelfStartPositions[0]);
            }
        }

        private float shelfLengthWithoutGap {
            get
            {
                float totalGapLength = 0;
                for(int i=0; i<shelfGapsStartDistances.Length; i++)
                {
                    totalGapLength += Mathf.Abs(shelfGapsEndDistances[i] - shelfGapsStartDistances[i]);
                }

                return Vector3.Distance(_shelfEndPositions[0], _shelfStartPositions[0]) - totalGapLength;
            }
        }

        private bool inputDataIsLeftToRight {
            get
            {
                Vector3 crossProduct  = Vector3.Cross(shelfDirection, depthDirection);
                if(crossProduct.y > 0)
                {
                    return false;
                }
                return true;
            }
        }

        public void Awake()
        {
            _assetDownloader = FindObjectOfType<AssetDownloader>();
            SetPrefabs();
            SetParents();
            CheckProductDirection();
        }

        private void SetPrefabs()
        {
            PrefabList prefabList = FindObjectOfType<PrefabList>();
            _shelfUnitUIPrefab = prefabList.shelfUnitUIPrefab;
            _topProductsUIPrefab = prefabList.topProductsUIPrefab;
            _bannerPrefab = prefabList.bannerPrefab;
            _overlayPrefab = prefabList.overlayPrefab;
            _overlayTextPrefab = prefabList.overlayTextPrefab;
            _topBlockPrefab = prefabList.topBlockPrefab;
            _defaultBannerTexture = prefabList.defaultBannerTexture;
            _defaultTopBlockTexture = prefabList.defaultTopBlockTexture;
            _grayscaleShader = prefabList.grayscaleShader;
        }

        private void SetParents()
        {
            _productsParent = transform.Find("ProductsParent");
            _bannersParent = transform.Find("BannersParent");
            _overlaysParent = transform.Find("OverlaysParent");
            _infoParent = transform.Find("InfoParent");
        }

        private void CheckProductDirection()
        {
            if(_shelfStartPositions.Length > 0 && _leftToRight != inputDataIsLeftToRight)
            {
                _topCorner += shelfDirection * shelfLength;

                Vector3[] temp = _shelfStartPositions;
                _shelfStartPositions = _shelfEndPositions;
                _shelfEndPositions = temp;

                if(_shelfGapsStartDistances.Length == _shelfGapsEndDistances.Length)
                {
                    for(int i=0; i<_shelfGapsStartDistances.Length; i++)
                    {
                        float shelfGapStartDistance =  _shelfGapsStartDistances[i];
                        float shelfGapEndDistance =  _shelfGapsEndDistances[i];

                        _shelfGapsStartDistances[i] = shelfLength - shelfGapEndDistance;
                        _shelfGapsEndDistances[i] = shelfLength - shelfGapStartDistance;
                    }
                }
            }
        }

        public void Start()
        {
            if(this.enabled)
            {
                if(GetComponent<ShelfUnitId>() != null)
                {
                    _shelfUnitId = GetComponent<ShelfUnitId>().shelfUnitId;
                }
                if(_shelfUnitId > -1)
                {
                    if(ProductsProvider.Instance.productsLoaded || !Application.isPlaying)
                    {
                        if(ShelfUnitProvider.Instance.shelfUnitsLoaded)
                        {
                            Init();
                        }
                        else
                        {
                            ShelfUnitProvider.OnShelfUnitsLoaded += Init;
                            if(!Application.isPlaying)
                            {
                                ShelfUnitProvider.Instance.LoadShelfUnitsFromJson();
                            }
                        }
                    }
                    else
                    {
                        initWhenProductsLoaded = true;
                    }
                }
            }
        }

        private void Update()
        {
            if (testGrayscale != null)
            {
                GrayscaleProduct(testGrayscale);
                testGrayscale = null;
            }
            
            if(initWhenProductsLoaded)
            {
                if(ProductsProvider.Instance.productsLoaded)
                {
                    initWhenProductsLoaded = false;
                    if(ShelfUnitProvider.Instance.shelfUnitsLoaded)
                    {
                        Init();
                    }
                    else
                    {
                        ShelfUnitProvider.OnShelfUnitsLoaded += Init;
                    }
                }
            }
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                _mediaPlayer?.CloseMedia();
            }
        }

        // Start is called before the first frame update
        void Init()
        {
            ShelfUnitProvider.OnShelfUnitsLoaded -= Init;
            _planograms = ShelfUnitProvider.Instance.GetPlanograms(_shelfUnitId);
            _menuButtons = ShelfUnitProvider.Instance.GetMenuButtons(_shelfUnitId);
            if(_planograms.Count > 0 && Application.isPlaying)
            {
                _currentPlanogram = _planograms[initialPlanogramIdToLoad];
                if (shelfEndPositions.Length > 0)
                {
                    SetUpUI();
                    StartCoroutine(LoadInitialPlanogram());
                }
            }
        }

        void OnDestroy()
        {
            ShelfUnitProvider.OnShelfUnitsLoaded -= Init;
        }

        private void SetUpUI()
        {
            if(Application.isPlaying)
            {
                // In case of hot reload, delete the old UI
                if(_UI != null)
                {
                    Destroy(_UI);
                    _uiSetsToggleMenuButtonColumn = -1;
                }
                
                _UI = Instantiate(_shelfUnitUIPrefab, transform.position, Quaternion.identity, transform);
                //_UI.GetComponent<UIContextualMenu>().OnMenuHidden += ResetUI;

                GameObject mediaPanel = _UI.transform.GetChild(0).GetChild(1).gameObject;

                PrefabList prefabList = FindObjectOfType<PrefabList>();

                _mediaPlayer = new TropicanaMediaPlayer(
                    _assetDownloader,
                    prefabList.videoPlayerPrefab,
                    prefabList.skybox360Mat,
                    FindObjectOfType<UnityEngine.EventSystems.EventSystem>(),

                    mediaPanel,
                    null,
                    mediaPanel.GetComponent<UnityEngine.UI.RawImage>(),
                    mediaPanel.transform.GetChild(0).Find("PrevButton").gameObject,
                    mediaPanel.transform.GetChild(0).Find("NextButton").gameObject,
                    mediaPanel.transform.GetChild(0).Find("OpenButton").gameObject,
                    mediaPanel.transform.GetChild(0).Find("DownloadButton").gameObject,
                    mediaPanel.transform.GetChild(0).Find("CloseButton").gameObject,
                    shelfLength*200,
                    MediaLoaded,
                    MediaClosed,
                    null,
                    _UI.transform.GetChild(0).GetChild(0).gameObject
                );

                if(_createUI && (_planograms.Count > 1 || _currentPlanogram.TopProducts.Count + _currentPlanogram.Banners.Count + _currentPlanogram.TopBlocks.Count + _currentPlanogram.Overlays.Count + _shelfUnitsToToggle.Count > 0))
                {
                    _UI.transform.position = transform.TransformPoint(_topCorner);
                    _UI.transform.localPosition += shelfDirection * 0.65f;
                    _UI.transform.localPosition -= depthDirection * 0.1f;
                    Vector3 localPos = _UI.transform.localPosition;
                    localPos.y = 0.25f;
                    _UI.transform.localPosition = localPos;
                    _UI.transform.localRotation = Quaternion.Euler(0, _productYRot+180, 0);

                    if(_menuButtons.Count > 0)
                    {
                        Destroy(_UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject);
                        Destroy(_UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject);
                        Destroy(_UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(3).gameObject);
                        Destroy(_UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).gameObject);

                        GameObject buttonPrefab = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;

                        for(int i=0; i<_menuButtons.Count; i++)
                        {
                            GameObject button = buttonPrefab;
                            if(i != 0)
                            {
                                button = Instantiate(buttonPrefab, _UI.transform.GetChild(0).GetChild(0).GetChild(0));
                            }

                            button.transform.GetChild(0).GetComponent<TMP_Text>().text = _menuButtons[i].Text;
                            Vector3 localButtonPos = button.transform.localPosition;
                            localButtonPos.x = (i * _uiButtonXSpacing) - 40;
                            button.transform.localPosition = localButtonPos;

                            int column = i;

                            switch(_menuButtons[i].Action)
                            {
                                case ShelfUnitButtonAction.PlayMedia:
                                    button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => PlayMedia(_menuButtons[column].MediaType, _menuButtons[column].ActionData, _menuButtons[column].OpenLinkUri, _menuButtons[column].DownloadFileUri, true) );
                                    break;
                                case ShelfUnitButtonAction.ToggleButtonsAbove:
                                    StartCoroutine(CreateSecondaryButtons(i, _menuButtons[i].SecondaryButtons));
                                    button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => ToggleSubMenu(column));
                                    break;
                                case ShelfUnitButtonAction.TogglePlanograms:
                                    StartCoroutine(CreateSetButtons(i));
                                    button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => ToggleUISets());
                                    break;
                                case ShelfUnitButtonAction.ToggleBanners:
                                    _bannersButton = button;
                                    button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleBanners(); });
                                    break;
                                case ShelfUnitButtonAction.ToggleOverlays:
                                    _overlaysButton = button;
                                    button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleOverlay(); });
                                    break;
                                case ShelfUnitButtonAction.ToggleTopProducts:
                                    _topProductsButton = button;
                                    button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleTopProducts(); });
                                    break;
                                case ShelfUnitButtonAction.ToggleInnovations:
                                    _innovationsButton = button;
                                    button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleInnovations(); });
                                    break;
                            }
                        }
                    }
                    else
                    {
                        GameObject setsButton = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;
                        _overlaysButton = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject;
                        _bannersButton = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject;
                        _topProductsButton = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(3).gameObject;
                        _innovationsButton = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).gameObject;
                        
                        setsButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => ToggleUISets());
                        _overlaysButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleOverlay(); });
                        _bannersButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleBanners(); });
                        _topProductsButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleTopProducts(); });
                        _innovationsButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleInnovations(); });

                        StartCoroutine(CreateSetButtons(0));
                    }
                }
            }
        }

        IEnumerator CreateSecondaryButtons(int column, List<ShelfUnitMenuSecondaryButton> buttons)
        {
            yield return null;
            // In case multiple buttons want to toggle sets, create the set buttons only the first time
            if(buttons.Count > 0)
            {
                int buttonNum = 1;

                GameObject buttonPrefab = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(column).gameObject;
                Transform parent = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(column).GetChild(1);

                for(int i=0; i<buttons.Count; i++)
                {
                    ShelfUnitMenuSecondaryButton button = buttons[i];

                    GameObject newButton = Instantiate(buttonPrefab, parent);
                    newButton.transform.GetChild(0).GetComponent<TMP_Text>().text = button.Text;

                    switch(button.Action)
                    {
                        case ShelfUnitButtonAction.PlayMedia:

                            newButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); PlayMedia(button.MediaType, button.ActionData, button.OpenLinkUri, button.DownloadFileUri, true); } );
                            break;
                        case ShelfUnitButtonAction.ToggleBanners:
                            _bannersButton = newButton;
                            newButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleBanners(); } );
                            break;
                        case ShelfUnitButtonAction.ToggleOverlays:
                            _overlaysButton = newButton;
                            newButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleOverlay(); } );
                            break;
                        case ShelfUnitButtonAction.ToggleTopProducts:
                            _topProductsButton = newButton;
                            newButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleTopProducts(); } );
                            break;
                        case ShelfUnitButtonAction.ToggleInnovations:
                            _innovationsButton = newButton;
                            newButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { CloseAllSubMenus(); ToggleInnovations(); } );
                            break;
                    }

                    Vector3 localButtonPos = newButton.transform.localPosition;
                    localButtonPos.y = buttonNum * _uiButtonYSpacing;
                    localButtonPos.x = 0;
                    newButton.transform.localPosition = localButtonPos;
                    buttonNum++;
                }

                parent.gameObject.SetActive(false);
            }
        }

        IEnumerator CreateSetButtons(int column)
        {
            // Allow the buttons to be destroyed/created before instanting from them
            yield return null;
            // In case multiple buttons want to toggle sets, create the set buttons only the first time
            if(_uiSetsToggleMenuButtonColumn < 0)
            {
                _uiSetsToggleMenuButtonColumn = column;
                int buttonNum = 1;

                GameObject buttonPrefab = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(column).gameObject;
                Transform parent = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(column).GetChild(1);

                _planogramIdToButton = new List<GameObject>();

                if(_shelfUnitsToToggle.Count == 0)
                {
                    _shelfUnitsToToggle.Add(this);
                }

                for(int i=0; i<_shelfUnitsToToggle.Count; i++)
                {
                    ShelfUnitController shelfUnitController = _shelfUnitsToToggle[i];
                    // Must get the number of planograms and planogram name like this
                    // Because if this shelf unit controller has not been started it has not yet loaded it's own planograms
                    ShelfUnitId otherShelfUnitId = shelfUnitController.GetComponent<ShelfUnitId>();
                    int id = otherShelfUnitId.shelfUnitId;
                    List<Planogram> otherShelfUnitPlanograms = ShelfUnitProvider.Instance.GetPlanograms(id);
                    for(int j=0; j<otherShelfUnitPlanograms.Count; j++)
                    {
                        string buttonName = "";
                        string setName = otherShelfUnitPlanograms[j].Name;
                        if(!string.IsNullOrWhiteSpace(setName))
                        {
                            buttonName = setName;
                        }
                        else
                        {
                            buttonName = "Set #" + buttonNum.ToString();
                        }

                        GameObject newShowSetButton = Instantiate(buttonPrefab, parent);
                        newShowSetButton.transform.GetChild(0).GetComponent<TMP_Text>().text = buttonName;
                        int shelfUnitId = i;
                        int planogramId = j;
                        newShowSetButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => ShowShelfUnit(shelfUnitId, planogramId));

                        Vector3 localButtonPos = newShowSetButton.transform.localPosition;
                        localButtonPos.y = buttonNum * _uiButtonYSpacing;
                        localButtonPos.x = 0;
                        newShowSetButton.transform.localPosition = localButtonPos;
                        buttonNum++;

                        if(shelfUnitController == this)
                        {
                            _planogramIdToButton.Add(newShowSetButton);
                        }
                    }
                }

                parent.gameObject.SetActive(false);
            }
        }

        public void ResetUI()
        {
            _mediaPlayer.CloseMedia();
            CloseAllSubMenus();
            DisableAllExtras();
        }

        private void CloseAllSubMenus()
        {
            for(int i=0; i<_UI.transform.GetChild(0).GetChild(0).GetChild(0).childCount; i++)
            {
                if(_UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(i).GetChild(1).gameObject.activeSelf)
                {
                    ToggleSubMenu(i, true);
                }
            }
        }

        private void ToggleSubMenu(int column, bool forceDisable = false)
        {
            bool enabling = false;
            Debug.Log("toggling menu!");
            GameObject subMenu = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(column).GetChild(1).gameObject;
            if(forceDisable)
            {
                subMenu.SetActive(false);
            }
            else
            {
                if(!subMenu.activeSelf)
                {
                    CloseAllSubMenus();
                    enabling = true;
                }
                
                subMenu.SetActive(!subMenu.activeSelf);
            }
            ToggleButtonColor(_UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(column).gameObject, enabling);
        }

        private void ToggleButtonColor(GameObject button, bool enabling)
        {
            if(button != null)
            {
                button.GetComponent<UnityEngine.UI.Image>().color = enabling? new Color(244/255f, 203/255f, 123/255f) : Color.white;
            }
        }

        public void ToggleUISets(bool forceDisable = false)
        {
            Debug.Log("TOGGLING");
            ToggleSubMenu(_uiSetsToggleMenuButtonColumn, forceDisable);
        }

        private IEnumerator LoadInitialPlanogram()
        {
            // hack to allow Set UI buttons to be created first
            yield return null;
            if(_currentPlanogram != null)
            {
                CreatePlanogram(_currentPlanogram.Name);
            }
        }

        private bool IsCBetweenAB ( Vector3 A , Vector3 B , Vector3 C ) {
            return Vector3.Dot( (B-A).normalized , (C-B).normalized )<0f && Vector3.Dot( (A-B).normalized , (C-A).normalized )<0f;
        }

        public void ShowShelfUnit(int shelfUnitIndex, int planogramIndex = 0)
        {
            if(!(_shelfUnitsToToggle[shelfUnitIndex] == this && planogramIndex == _currentPlanogramId))
            {
                if(_fadeToBlack == null)
                {
                    _fadeToBlack = Camera.main.transform.parent.GetComponent<FadeToBlack>();
                }
                if(_fadeToBlack != null && _useFadeToBlack) {
                    _fadeToBlack.ToggleFade(true, true, () => EnableShelfUnit(shelfUnitIndex, planogramIndex));
                } else {
                    EnableShelfUnit(shelfUnitIndex, planogramIndex);
                }
            }

        }

        private void EnableShelfUnit(int shelfUnitIndex, int planogramIndex = 0)
        {
            ToggleUISets(true);
            gameObject.SetActive(false);
            ShelfUnitController shelfUnitController = _shelfUnitsToToggle[shelfUnitIndex];
            bool hasNotInited = false;
            if(shelfUnitController.currentPlanogramId == -1)
            {
                hasNotInited = true;
                shelfUnitController.initialPlanogramIdToLoad = planogramIndex;
            }
            _shelfUnitsToToggle[shelfUnitIndex].gameObject.SetActive(true);
            if(!hasNotInited)
            {
                _shelfUnitsToToggle[shelfUnitIndex].CreatePlanogram(planogramIndex);
            }
        }

        public void CreatePlanogram(string planogramName)
        {
            for(int i = 0; i<_planograms.Count; i++)
            {
                if(planogramName == _planograms[i].Name)
                {
                    CreatePlanogram(i);
                    return;
                }
            }
            Debug.LogError("Cannot find planogram with name " + planogramName);
        }

        public void CreatePlanogram(int planogramId)
        {
            if(_planograms.Count > 0 )
            {
                if(_shelfStartPositions.Length != _shelfEndPositions.Length)
                {
                    Debug.LogError("Planogram must have equal number of shelf start positions and end positions");
                    return;
                }

                if(_shelfGapsStartDistances.Length != _shelfGapsEndDistances.Length)
                {
                    Debug.LogError("Planogram must have equal number of shelf gap start distances and end distances");
                    return;
                }

                _currentPlanogramId = planogramId;

                if(Application.isPlaying && _currentPlanogram != _planograms[planogramId])
                {
                    _currentPlanogram = _planograms[planogramId];
                    if(_fadeToBlack == null)
                    {
                        _fadeToBlack = Camera.main.transform.parent.GetComponent<FadeToBlack>();
                    }
                    if(_useFadeToBlack)
                    {
                        _fadeToBlack.ToggleFade(true, true, (() => CreateAllPlanogramComponents()));
                    }
                }
                else
                {
                    _currentPlanogram = _planograms[planogramId];
                    CreateAllPlanogramComponents(true);
                }

                if(_uiSetsToggleMenuButtonColumn >= 0 && _planogramIdToButton.Count > planogramId)
                {
                    Transform subMenu = _UI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(_uiSetsToggleMenuButtonColumn).GetChild(1);
                    foreach(Transform button in subMenu)
                    {
                        ToggleButtonColor(button.gameObject, false);
                    }
                    ToggleButtonColor(_planogramIdToButton[planogramId], true);
                }
            }
        }

        public void ToggleOverlay()
        {
            if(!_overlaysParent.gameObject.activeSelf)
            {
                DisableAllExtras();
                _overlaysParent.gameObject.SetActive(true);
                _infoParent.gameObject.SetActive(true);

                _shelfUnitState = ShelfUnitState.Overlays;
                ToggleButtonColor(_overlaysButton, true);
            }
            else
            {
                DisableAllExtras();
            }
        }

        public void ToggleBanners()
        {
            if(!_bannersParent.gameObject.activeSelf)
            {
                DisableAllExtras();
                _bannersParent.gameObject.SetActive(true);
                _shelfUnitState = ShelfUnitState.Banners;
                ToggleButtonColor(_bannersButton, true);
            }
            else
            {
                DisableAllExtras();
            }
        }

        // Old version with top products menu and one set of products highlighted at a time
        /*public void ToggleTopProducts()
        {
            if(_topProductsUI == null)
            {
                DisableAllExtras();

                _topProductsColorDict = CreateColorDict(_currentPlanogram.TopProducts);

                _overlaysParent.gameObject.SetActive(false);
                _infoParent.gameObject.SetActive(false);
                _bannersParent.gameObject.SetActive(false);

                Transform canvas = Camera.main.transform.parent.GetComponentInChildren<Canvas>().transform;
                _topProductsUI = GameObject.Instantiate(_topProductsUIPrefab, canvas);

                _topProductsUI.transform.GetChild(1).GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ToggleTopProducts);

                for(int i=0; i<_currentPlanogram.TopProducts.Count; i++)
                {
                    PlanogramOverlay topProduct = _currentPlanogram.TopProducts[i];
                    string prefabName = topProduct.Prefabs[0];
                    _topProductsUI.transform.GetChild(0).GetChild(i).gameObject.SetActive(true);
                    _topProductsUI.transform.GetChild(0).GetChild(i).GetComponent<TMP_Text>().text = topProduct.Text;

                    // try TBG assets first
                    GameObject resource = Resources.Load("Drink_Assets/" + prefabName, typeof(GameObject)) as GameObject;
                    // try third party assets next
                    if (resource== null)
                    {
                        resource = Resources.Load("ThirdPartyAssets/" + prefabName, typeof(GameObject)) as GameObject;
                        //Debug.Log("[creation] Trying ThirdPartyAssets: " + prefabName + " found: " + resource);
                    }
                    
                    if(resource == null) {
                        Debug.LogError("CANNOT FIND: " + prefabName + " in Shelf Unit Id " + _shelfUnitId);
                        continue;
                    }

                    GameObject product = Instantiate(resource, _topProductsUI.transform.GetChild(0).GetChild(i)) as GameObject;
                    product.transform.localPosition = new Vector3(400, -40, -5);
                    product.transform.localRotation = Quaternion.Euler(0, 200, 0);
                    product.transform.localScale = new Vector3(300, 300, 300);

                    int layer = LayerMask.NameToLayer("DynamicProductPickedUp");
                    product.layer = layer;

                    _topProductsUI.transform.GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectTopProducts(topProduct));

                    _shelfUnitState = ShelfUnitState.TopProducts;
                    ToggleButtonColor(_topProductsButton, true);
                }

                _topProductsUI.SetActive(false);
            }
            else
            {
                DisableAllExtras();
            }
        }*/

        public void ToggleTopProducts()
        {
            if(_shelfUnitState != ShelfUnitState.TopProducts)
            {
                DisableAllExtras();
                StartCoroutine(ToggleOverlaysCoroutine(ShelfUnitState.TopProducts));

            }
            else
            {
                DisableAllExtras();
            }
        }

        public void ToggleInnovations()
        {
            if(_shelfUnitState != ShelfUnitState.Innovations)
            {
                DisableAllExtras();

                StartCoroutine(ToggleOverlaysCoroutine(ShelfUnitState.Innovations));
            }
            else
            {
                DisableAllExtras();
            }
        }

        private IEnumerator ToggleOverlaysCoroutine(ShelfUnitState shelfUnitState)
        {
            // Wait for a frame for any Highlight objects to be destroyed
            yield return null;
            List<PlanogramOverlay> overlays;

            if(shelfUnitState == ShelfUnitState.TopProducts)
            {
                overlays = _currentPlanogram.TopProducts;
                _topProductsColorDict = CreateColorDict(_currentPlanogram.TopProducts);
            }
            else if(shelfUnitState == ShelfUnitState.Innovations)
            {
                overlays = _currentPlanogram.Innovations;
                _innovationsColorDict = CreateColorDict(_currentPlanogram.Innovations);
            }
            else
            {
                Debug.LogError("ToggleOverlaysCoroutine must be called with either TopProducts or Innovations ShelfUnitState");
                yield break;
            }
            
            foreach(PlanogramOverlay overlay in overlays)
            {
                Color color;
                if(!ColorUtility.TryParseHtmlString(overlay.Color, out color))
                {
                    Debug.LogError("Cannot parse overlay color: " + overlay.Color);
                    continue;
                }

                foreach(string prefabName in overlay.Prefabs.Distinct().ToList())
                {
                    // The first child is the first row, so only front row items will have the highlight and outline
                    foreach(Transform product in _productsParent.GetChild(0))
                    {
                        if(product.gameObject.name == prefabName + "(Clone)")
                        {
                            if(shelfUnitState == ShelfUnitState.TopProducts)
                            {
                                _selectedTopProducts.Add(product.gameObject);
                                _productToTopProduct.Add(product.gameObject, overlay);
                            }
                            else if(shelfUnitState == ShelfUnitState.Innovations)
                            {
                                _selectedInnovations.Add(product.gameObject);
                                _productToInnovation.Add(product.gameObject, overlay);
                            }
                            HighlightProduct(product, color, false);
                        }
                    }
                }
            }

            // Add grayscale effect to all other products
            foreach(Transform row in _productsParent)
            {
                foreach(Transform product in row)
                {
                    if(shelfUnitState == ShelfUnitState.TopProducts)
                    {
                        if(!_selectedTopProducts.Contains(product.gameObject))
                        {
                            GrayscaleProduct(product.gameObject);
                        }
                    }
                    else if(shelfUnitState == ShelfUnitState.Innovations)
                    {
                        if(!_selectedInnovations.Contains(product.gameObject))
                        {
                            GrayscaleProduct(product.gameObject);
                        }
                    }
                }
            }

            _shelfUnitState = shelfUnitState;
            if(shelfUnitState == ShelfUnitState.TopProducts)
            {
                ToggleButtonColor(_topProductsButton, true);
            }
            else if(shelfUnitState == ShelfUnitState.Innovations)
            {
                ToggleButtonColor(_innovationsButton, true);
            }
        }

        private void DisableAllExtras()
        {
            if(_shelfUnitState == ShelfUnitState.Innovations)
            {
                // Remove Outline Highlights
                foreach(GameObject product in _selectedInnovations)
                {
                    if(product != null)
                    {
                        product.gameObject.layer = _oldLayer;
                        Destroy(product.gameObject.GetComponent<HighlightEffect>());
                    }
                }

                // Revert grayscale effect
                foreach(Material mat in _materialToOriginalShader.Keys)
                {
                    mat.shader = _materialToOriginalShader[mat];
                }

                _selectedInnovations = new List<GameObject>();
                _productToInnovation = new Dictionary<GameObject, PlanogramOverlay>();
                _materialToOriginalShader = new Dictionary<Material, Shader>();
            }

            if(_shelfUnitState == ShelfUnitState.TopProducts)
            {
                // Remove Outline Highlights
                foreach(GameObject product in _selectedTopProducts)
                {
                    if(product != null)
                    {
                        product.gameObject.layer = _oldLayer;
                        Destroy(product.gameObject.GetComponent<HighlightEffect>());
                    }
                }

                // Revert grayscale effect
                foreach(Material mat in _materialToOriginalShader.Keys)
                {
                    mat.shader = _materialToOriginalShader[mat];
                }

                _selectedTopProducts = new List<GameObject>();
                _productToTopProduct = new Dictionary<GameObject, PlanogramOverlay>();
                _materialToOriginalShader = new Dictionary<Material, Shader>();
            }

            _overlaysParent.gameObject.SetActive(false);
            _infoParent.gameObject.SetActive(false);
            _bannersParent.gameObject.SetActive(false);

            if(_lastCreatedTextBox != null)
            {
                Destroy(_lastCreatedTextBox);
                _lastCreatedTextBox = null;
                _lastCreatedTextBoxOverlay = null;
            }

            _shelfUnitState = ShelfUnitState.None;
            ToggleButtonColor(_bannersButton, false);
            ToggleButtonColor(_overlaysButton, false);
            ToggleButtonColor(_topProductsButton, false);
            ToggleButtonColor(_innovationsButton, false);
        }

        private void CreateAllPlanogramComponents(bool isInitialCreate = false)
        {
            if(Application.isPlaying)
            {
                CreateParents();
            }
            // If we are changing planogram
            DisableAllExtras();
            CreateOverlays();
            StartCoroutine(CreateProducts(isInitialCreate));
            CreateBanners();
        }

        private Dictionary<string, Color> CreateColorDict(List<PlanogramOverlay> planogramOverlays)
        {
            Dictionary<string, Color> prefabsToColors = new Dictionary<string, Color>();
            foreach(PlanogramOverlay planogramOverlay in planogramOverlays)
            {
                Color overlayColor;
                if(!ColorUtility.TryParseHtmlString(planogramOverlay.Color, out overlayColor))
                {
                    Debug.LogError("Cannot parse overlay color: " + planogramOverlay.Color);
                    continue;
                }
                overlayColor.a = 0.75f;

                foreach(string prefab in planogramOverlay.Prefabs)
                {
                    if(prefabsToColors.ContainsKey(prefab))
                    {
                        if(prefabsToColors[prefab] != overlayColor)
                        {
                            Debug.LogWarning("Ignoring duplicate overlay color for " + prefab);
                        }
                    }
                    else
                    {
                        prefabsToColors.Add(prefab, overlayColor);
                    }
                }
            }

            return prefabsToColors;
        }

        IEnumerator CreateProducts(bool isInitialCreate)
        {
            List<Transform> rowParents = new List<Transform>();

            float[] overlayYPositions = new float[_shelfStartPositions.Length + 1];
            overlayYPositions[0] = _topCorner.y;
            for(int i = 0; i<_shelfStartPositions.Length; i++)
            {
                overlayYPositions[i+1] = _shelfStartPositions[i].y;
            }

            Dictionary<string, Color> prefabsToOverlayColors = CreateColorDict(_currentPlanogram.Overlays);

            for(int i = 0; i<_currentPlanogram.Shelves.Count && i <  _shelfStartPositions.Length; i++)
            {
                PlanogramShelf shelf = _currentPlanogram.Shelves[i];
                int numRows = shelf.Rows.Count;
                bool creatingCopies = false;

                if(numRows > 1 && _numCopyRows > 0)
                {
                    Debug.LogError("Num copy rows > 0 but planogram already has multiple rows defined. Ignoring copy rows");
                }
                else if(_numCopyRows > 0)
                {
                    numRows = _numCopyRows + 1;
                    creatingCopies = true;
                }

                float maxDepth = 0;

                for(int j = 0; j<numRows; j++)
                {
                    if(rowParents.Count == j) {
                        GameObject rowParent = new GameObject();
                        rowParent.name = "Row " + (j+1).ToString();
                        rowParent.transform.parent = _productsParent;
                        rowParent.transform.localPosition = Vector3.zero;
                        rowParent.transform.localRotation = Quaternion.identity;
                        rowParent.transform.localScale = Vector3.one;
                        rowParents.Add(rowParent.transform);
                    }

                    PlanogramShelfRow row;
                    if(creatingCopies)
                    {
                        row = shelf.Rows[0];
                    }
                    else
                    {
                        row = shelf.Rows[j];
                    }
                    float prevProductExtents = 0f;
                    Vector3 productPos = _shelfStartPositions[i];
                    // move the position back depending on what row it's in
                    productPos += (maxDepth + _rowSpacing) * j * depthDirection;
                    // Change the y of the row if this is a slanted shelf
                    productPos.y += j * (maxDepth + _rowSpacing) * Mathf.Tan(Mathf.Deg2Rad * _productXRot);
                    Vector3 rowStartPos = productPos;

                    maxDepth = 0;

                    float productSpacing = _currentPlanogram.ProductSpacing;
                    if(productSpacing == 0)
                    {
                        float totalWidth = 0;
                        int numProducts = 0;
                        foreach(string prefabName in row.Products)
                        {
                            // try TBG assets first
                            GameObject resource = Resources.Load("Drink_Assets/" + prefabName, typeof(GameObject)) as GameObject;
                            // try third party assets next
                            if (resource== null)
                            {
                                resource = Resources.Load("ThirdPartyAssets/" + prefabName, typeof(GameObject)) as GameObject;
                            }
                            
                            if(resource == null) {
                                continue;
                            }

                            MeshFilter meshFilter = resource.GetComponentInChildren<MeshFilter>();
                            if(!meshFilter)
                            {
                                continue;
                            }
                            Mesh mesh = meshFilter.sharedMesh;
                            if(!mesh)
                            {
                                continue;
                            }

                            numProducts++;
                            totalWidth += mesh.bounds.size.x;
                        }

                        // If the total width is greater than the shelf length, spacing remains 0
                        if(totalWidth < shelfLengthWithoutGap)
                        {
                            productSpacing = (shelfLengthWithoutGap - totalWidth) / (numProducts-1);
                        }
                    }

                    bool isFirstProduct = true;
                    
                    foreach(string prefabName in row.Products)
                    {
                        // try TBG assets first
                        GameObject resource = Resources.Load("Drink_Assets/" + prefabName, typeof(GameObject)) as GameObject;
                        // try third party assets next
                        if (resource== null)
                        {
                            resource = Resources.Load("ThirdPartyAssets/" + prefabName, typeof(GameObject)) as GameObject;
                            //Debug.Log("[creation] Trying ThirdPartyAssets: " + prefabName + " found: " + resource);
                        }
                        
                        if(resource == null) {
                            Debug.LogError("CANNOT FIND: " + prefabName + " in Shelf Unit Id " + _shelfUnitId);
                            continue;
                        }
                        GameObject product = Instantiate(resource, rowParents[j]) as GameObject;

                        MeshRenderer[] mrs = product.GetComponentsInChildren<MeshRenderer>();
                        foreach(MeshRenderer mr in mrs)
                        {
                            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                        }
                        
                        product.transform.localRotation = Quaternion.Euler(new Vector3(_productXRot, _productYRot, 0));

                        int layer = LayerMask.NameToLayer("DynamicProductBack");
                        if(j == 0)
                        {
                            layer = LayerMask.NameToLayer("DynamicProduct");
                        }
                        if(layer >= 0)
                        {
                            product.layer = layer;
                            foreach(Transform child in product.transform)
                            {
                                child.gameObject.layer = layer;
                            }
                        }

                        MeshFilter meshFilter = product.GetComponentInChildren<MeshFilter>();
                        if(!meshFilter)
                        {
                            Debug.LogError("Skipping Prefab " + prefabName + " because the Mesh Filter was not found");
                            continue;
                        }
                        Mesh mesh = meshFilter.sharedMesh;
                        if(!mesh)
                        {
                            Debug.LogError("Skipping Prefab " + prefabName + " because the Mesh Filter is empty");
                            continue;
                        }

                        productPos = productPos + shelfDirection * (prevProductExtents + mesh.bounds.extents.x + productSpacing);
                        // Don't add the product spacing to the first product
                        if(isFirstProduct)
                        {
                            isFirstProduct = false;
                            productPos -= shelfDirection * productSpacing;
                        }
                        
                        // Check if the product pos is in the gaps and if so, move it out of the gap
                        for(int k=0; k<_shelfGapsStartDistances.Length; k++)
                        {
                            // Checks if the start or end point of the product is within the gap, OR (for larger products) if the start of end point of the gap is within the product
                            if(IsCBetweenAB(productPos - shelfDirection*mesh.bounds.extents.x, productPos + shelfDirection*mesh.bounds.extents.x, rowStartPos + _shelfGapsStartDistances[k] * shelfDirection) ||
                               IsCBetweenAB(productPos - shelfDirection*mesh.bounds.extents.x, productPos + shelfDirection*mesh.bounds.extents.x, rowStartPos + _shelfGapsEndDistances[k] * shelfDirection) ||
                               IsCBetweenAB(rowStartPos + _shelfGapsStartDistances[k] * shelfDirection, rowStartPos + _shelfGapsEndDistances[k] * shelfDirection, productPos - shelfDirection*mesh.bounds.extents.x) ||
                               IsCBetweenAB(rowStartPos + _shelfGapsStartDistances[k] * shelfDirection, rowStartPos + _shelfGapsEndDistances[k] * shelfDirection, productPos + shelfDirection*mesh.bounds.extents.x))
                            {
                                // Move the product to just after the gap
                                productPos = rowStartPos + shelfDirection * (_shelfGapsEndDistances[k] +  mesh.bounds.extents.x);
                                break;
                            }
                        }

                        Vector3 productPosWithDepth = productPos;
                        if(_alignProductEdgeType != AlignProductEdgeType.AlignMiddle)
                        {
                            productPosWithDepth += _alignProductEdgeType == AlignProductEdgeType.AlignFrontEdge? mesh.bounds.extents.z * depthDirection : -mesh.bounds.extents.z * depthDirection;
                        }
                        
                        product.transform.localPosition = productPosWithDepth;

                        prevProductExtents = mesh.bounds.extents.x;

                        // calculate the max depth of the current row to use for spacing the next row
                        float depth = mesh.bounds.size.z;
                        if(depth > maxDepth)
                        {
                            maxDepth = depth;
                        }

                        // Checks if the end of the current product is within the line between the shelf start pos and end pos
                        if(_hideProductsPastShelfEnd && !IsCBetweenAB(_shelfEndPositions[i], _shelfStartPositions[i], productPos + shelfDirection*prevProductExtents)) {
                            if(Application.isPlaying)
                            {
                                Destroy(product);
                            }
                            else
                            {
                                DestroyImmediate(product);
                            }
                            
                            break;
                        }

                        // Only add interactivity to the front row
                        if(Application.isPlaying && j == 0) {
                            AddProductFascade(product);
                        }

                        if(j == 0)
                        {
                            if(prefabsToOverlayColors.ContainsKey(prefabName))
                            {
                                CreateOverlay(_overlayPrefab, prefabsToOverlayColors[prefabName], _overlaysParent, productPos, mesh, overlayYPositions, i);
                            }
                        }

                        numProducts++;
                        if(numProductsPerFrame > 0 && isInitialCreate && numProducts % numProductsPerFrame == 0)
                        {
                            yield return null;
                        }
                    }
                }
            }
        }

        private void AddProductFascade(GameObject productObj)
        {
            string ProductID = productObj.name.Replace("(Clone)", "");
            
            var product = ProductsProvider.Instance.GetProduct(ProductID);

            if (product != null)
            {
                Renderer renderer = productObj.GetComponentInChildren<Renderer>();
                if(renderer != null)
                {
                    var facade = renderer.gameObject.AddComponent<ProductFacade>();
                    facade.Product = product;
                }
            }
        }

        private void CreateOverlay(GameObject overlayPrefab, Color color, Transform overlaysParent, Vector3 productPos, Mesh mesh, float[] overlayYPositions, int i)
        {
            GameObject overlay = Instantiate(overlayPrefab, overlaysParent) as GameObject;

            Vector3 overlayPos = productPos - shelfDirection * mesh.bounds.extents.x - depthDirection * 0.001f;
            if(_alignProductEdgeType == AlignProductEdgeType.AlignMiddle)
            {
                overlayPos -= depthDirection * mesh.bounds.size.z/2;
            }
            else if(_alignProductEdgeType == AlignProductEdgeType.AlignBackEdge)
            {
                overlayPos -= depthDirection * mesh.bounds.size.z;
            }
            overlayPos.y = overlayYPositions[i];

            overlay.transform.localPosition = overlayPos;

            overlay.transform.localRotation = Quaternion.Euler(0, _productYRot+180, 0);

            float xScale = mesh.bounds.size.x;
            if(!inputDataIsLeftToRight)
            {
                xScale = -xScale;
            }
            float yScale = overlayYPositions[i] - overlayYPositions[i+1];
            overlay.transform.localScale = new Vector3(xScale, yScale, 1);

            var tempMaterial = new Material(overlay.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial);
            tempMaterial.SetColor("_BaseColor", color);
            tempMaterial.SetColor("_Color", color);
            overlay.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial = tempMaterial;
        }

        private void CreateBanners()
        {
            Vector3 bannerEndPos = _topCorner + shelfDirection * shelfLength;

            for(int i = 0; i<_currentPlanogram.Banners.Count; i++)
            {
                PlanogramBanner planogramBanner = _currentPlanogram.Banners[i];

                if(!IsCBetweenAB(bannerEndPos, _topCorner, _topCorner + shelfDirection * planogramBanner.HorizontalPosition)) {
                    break;
                }
                
                GameObject banner = Instantiate(_bannerPrefab, _bannersParent) as GameObject;
                banner.transform.localPosition = _topCorner + shelfDirection * planogramBanner.HorizontalPosition - Vector3.up * planogramBanner.VerticalPosition;
                if(planogramBanner.IsParallel)
                {
                    banner.transform.localRotation = Quaternion.Euler(0, _productYRot-180, 0);
                }
                else
                {
                    banner.transform.localRotation = Quaternion.Euler(0, _productYRot-90, 0);
                }
                banner.transform.localScale = new Vector3(planogramBanner.Width, planogramBanner.Height, 1);

                var tempMaterial = new Material(banner.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial);
                if(!string.IsNullOrEmpty(planogramBanner.Image))
                {
                    StartCoroutine(_assetDownloader.GetTexture<Material>(planogramBanner.Image, tempMaterial, SetTextureToMaterial));
                }
                else
                {
                    tempMaterial.SetTexture("_BaseMap", _defaultBannerTexture);
                }
                banner.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial = tempMaterial;

                tempMaterial = new Material(banner.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial);
                if(!string.IsNullOrEmpty(planogramBanner.Image))
                {
                    StartCoroutine(_assetDownloader.GetTexture<Material>(planogramBanner.Image, tempMaterial, SetTextureToMaterial));
                }
                else
                {
                    tempMaterial.SetTexture("_BaseMap", _defaultBannerTexture);
                }
                banner.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial = tempMaterial;
            }

            _bannersParent.gameObject.SetActive(false);
        }

        private void SetTextureToMaterial(Texture texture, Material mat)
        {
            if(texture != null)
            {
                mat.SetTexture("_BaseMap", texture);
            }
        }

        private void CreateOverlays()
        {
            int numTopBlocks = _currentPlanogram.TopBlocks.Count;

            // 0.4 = 1m, because the top block child object has x scale of 2.5
            float topBlockScale = (shelfLength + _topBlockOverhangPerSide*2 - _topBlockSpacing*(numTopBlocks-1)) / Mathf.Max(2, numTopBlocks) * 0.4f;

            Vector3 topBlockStartPos = _topCorner;
            topBlockStartPos.y += 0.1f;
            topBlockStartPos -= shelfDirection * _topBlockOverhangPerSide;
            // If there's only one top-block, it will take up half the size of the shelf unit, so centre it
            if(numTopBlocks == 1)
            {
                topBlockStartPos += shelfDirection * shelfLength/4;
            }

            for(int i = 0; i<numTopBlocks; i++)
            {
                PlanogramTopBlock planogramTopBlock = _currentPlanogram.TopBlocks[i];
                
                GameObject topBlock = Instantiate(_topBlockPrefab, _infoParent) as GameObject;
                topBlock.transform.localPosition = topBlockStartPos + (shelfDirection * (topBlockScale * 2.5f + _topBlockSpacing) * i);
                topBlock.transform.localRotation = Quaternion.Euler(0, _productYRot+180, 0);
                topBlock.transform.localScale = new Vector3(topBlockScale, topBlockScale, 1);

                string topBlockText = planogramTopBlock.Text;
                topBlockText = topBlockText.Replace("•", "\n•").Trim();

                topBlock.transform.GetChild(0).GetComponent<TMP_Text>().text = topBlockText;
                Color color;
                if(ColorUtility.TryParseHtmlString(planogramTopBlock.FontColor, out color))
                {
                    topBlock.transform.GetChild(0).GetComponent<TMP_Text>().color = color;
                }
                if(ColorUtility.TryParseHtmlString(planogramTopBlock.BackgroundColor, out color))
                {
                    var tempColorMaterial = new Material(topBlock.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial);
                    tempColorMaterial.SetColor("_BaseColor", color);
                    tempColorMaterial.SetColor("_Color", color);
                    topBlock.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial = tempColorMaterial;
                }

                var tempMaterial = new Material(topBlock.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial);
                if(!string.IsNullOrEmpty(planogramTopBlock.Image))
                {
                    StartCoroutine(_assetDownloader.GetTexture<Material>(planogramTopBlock.Image, tempMaterial, SetTextureToMaterial));
                }
                else
                {
                    tempMaterial.SetTexture("_BaseMap", _defaultTopBlockTexture);
                }
                topBlock.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial = tempMaterial;

                if(!inputDataIsLeftToRight)
                {
                    topBlock.transform.localScale = new Vector3(-topBlockScale, topBlockScale, 1);
                    topBlock.transform.GetChild(0).localScale = new Vector3(-topBlock.transform.GetChild(0).localScale.x, topBlock.transform.GetChild(0).localScale.y, topBlock.transform.GetChild(0).localScale.z);
                }
            }

            foreach(PlanogramOverlay planogramOverlay in _currentPlanogram.Overlays)
            {
                CreateOverlayText(planogramOverlay, _overlaysParent);
            }

            _overlaysParent.gameObject.SetActive(false);
            _infoParent.gameObject.SetActive(false);
        }

        private GameObject CreateOverlayText(PlanogramOverlay planogramOverlay, Transform parent)
        {
            if (string.IsNullOrEmpty(planogramOverlay.Text))
            {
                return null;
            }
            
            GameObject overlayText = Instantiate(_overlayTextPrefab, parent) as GameObject;

            float hp = planogramOverlay.TextBoxHorizontalPosition;
            float vp = planogramOverlay.TextBoxVerticalPosition;

            Vector3 textPos = _topCorner + shelfDirection * hp;
            textPos.x += _overlayInfoBoxXOffset;
            textPos.y -= vp;
            textPos.z += _overlayInfoBoxZOffset;
            overlayText.transform.localPosition = textPos;

            overlayText.transform.localRotation = Quaternion.Euler(0, _productYRot+180, 0);

            float xScaleText = planogramOverlay.TextBoxWidth;
            float yScaleText = planogramOverlay.TextBoxHeight;
            overlayText.transform.GetChild(0).localScale = new Vector3(xScaleText, yScaleText, 1);
            overlayText.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(xScaleText * 10 + 0.5f, yScaleText * 10 + 1f);

            string text = planogramOverlay.Text.Replace("•", "\n•").Trim();
            overlayText.transform.GetChild(1).GetComponent<TMP_Text>().text = text;
            Color color;
            if(ColorUtility.TryParseHtmlString(planogramOverlay.FontColor, out color))
            {
                overlayText.transform.GetChild(1).GetComponent<TMP_Text>().color = color;
            }
            if(ColorUtility.TryParseHtmlString(planogramOverlay.TextBoxBackgroundColor, out color))
            {
                var tempMaterial = new Material(overlayText.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial);
                tempMaterial.SetColor("_BaseColor", color);
                tempMaterial.SetColor("_Color", color);
                overlayText.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial = tempMaterial;
            }

            if(!inputDataIsLeftToRight)
            {
                overlayText.transform.GetChild(0).localScale = new Vector3(-overlayText.transform.localScale.x, overlayText.transform.localScale.y, overlayText.transform.localScale.z);

                RectTransform rectTransform = overlayText.transform.GetChild(0).GetComponent<RectTransform>();
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(xScaleText/2, -yScaleText/2);

                overlayText.transform.GetChild(1).localScale = new Vector3(-overlayText.transform.GetChild(1).localScale.x, overlayText.transform.GetChild(1).localScale.y, overlayText.transform.GetChild(1).localScale.z);
            }

            if(!string.IsNullOrEmpty(planogramOverlay.MediaUri))
            {
                overlayText.transform.GetChild(0).GetChild(0).GetComponent<OnClick>().OnClicked += () => OverlayTextClicked(overlayText, planogramOverlay);
            }

            return overlayText;
        }

        private void OverlayTextClicked(GameObject overlayText, PlanogramOverlay planogramOverlay)
        {
            PlayMedia(planogramOverlay.MediaType, planogramOverlay.MediaUri, planogramOverlay.OpenLinkUri, planogramOverlay.DownloadFileUri);
        }

        private void PlayMedia(MediaType mediaType, string mediaUri, string openLinkUri = "", string downloadFileUri = "", bool disableAllExtras = false)
        {
            _disableAllExtrasWhenMediaLoads = disableAllExtras;
            _mediaPlayer.PlayMedia(mediaType, mediaUri, openLinkUri, downloadFileUri);
        }

        private void MediaLoaded()
        {
            _mediaOpened = true;
            if(_shelfUnitState == ShelfUnitState.Overlays)
            {
                _overlaysParent.gameObject.SetActive(false);
            }
            else if(_shelfUnitState == ShelfUnitState.TopProducts)
            {
                _topProductsUI?.SetActive(false);
            }
            HighlightEffect[] highlightEffects = GetComponentsInChildren<HighlightEffect>();
            foreach(HighlightEffect highlightEffect in highlightEffects)
            {
                highlightEffect.enabled = false;
            }
            if(_disableAllExtrasWhenMediaLoads)
            {
                DisableAllExtras();
            }
            if(_lastCreatedTextBox != null)
            {
                _lastCreatedTextBox.SetActive(false);
            }
        }

        private void MediaClosed()
        {
            _mediaOpened = false;
            if(_shelfUnitState == ShelfUnitState.Overlays)
            {
                _overlaysParent.gameObject.SetActive(true);
            }
            else if(_shelfUnitState == ShelfUnitState.TopProducts)
            {
                //_topProductsUI?.SetActive(true);
            }
            HighlightEffect[] highlightEffects = GetComponentsInChildren<HighlightEffect>();
            foreach(HighlightEffect highlightEffect in highlightEffects)
            {
                highlightEffect.enabled = true;
            }
            if(_lastCreatedTextBox != null)
            {
                _lastCreatedTextBox.SetActive(true);
            }
            
        }

        public void SelectTopProducts(int index)
        {
            if(_currentPlanogram != null)
            {
                if(_currentPlanogram.TopProducts.Count >= index+1)
                {
                    SelectTopProducts(_currentPlanogram.TopProducts[index]);
                }
            }
        }

        private void SelectTopProducts(PlanogramOverlay topProduct)
        {
            foreach(GameObject product in _selectedTopProducts)
            {
                product.gameObject.layer = _oldLayer;
                HighlightWithEmission highlightWithEmission = product.GetComponent<HighlightWithEmission>();
                if(highlightWithEmission)
                {
                    highlightWithEmission.StopHighlight();
                }

                Destroy(product.gameObject.GetComponent<HighlightEffect>());
            }

            foreach(Material mat in _materialToOriginalShader.Keys)
            {
                mat.shader = _materialToOriginalShader[mat];
            }

            _selectedTopProducts = new List<GameObject>();
            _materialToOriginalShader = new Dictionary<Material, Shader>();

            if(_selectedTopProductsPrefab != topProduct && topProduct != null)
            {
                string prefabName = topProduct.Prefabs[0];
                Color color = new Color(226/255f,125/255f,44/255f);

                if(!ColorUtility.TryParseHtmlString(topProduct.Color, out color))
                {
                    Debug.LogError("Cannot parse top product color: " + topProduct.Color + ". Using default.");
                }

                // The first child is the first row, so only front row items will have the highlight and outline
                foreach(Transform product in _productsParent.GetChild(0))
                {
                    if(product.gameObject.name == prefabName + "(Clone)")
                    {
                        _selectedTopProducts.Add(product.gameObject);
                        
                        HighlightProduct(product, color, true);
                    }
                }

                // Add grayscale effect to all other products
                foreach(Transform row in _productsParent)
                {
                    foreach(Transform product in row)
                    {
                        if(product.gameObject.name != prefabName + "(Clone)")
                        {
                            Renderer renderer = product.GetComponentInChildren<Renderer>();
                            if(renderer != null)
                            {
                                Material mat = renderer.material;
                                Shader shader = mat.shader;
                                if(!_materialToOriginalShader.ContainsKey(mat))
                                {
                                    _materialToOriginalShader.Add(mat, shader);
                                }
                                
                                mat.shader = _grayscaleShader;
                            }
                        }
                    }
                }

                _selectedTopProductsPrefab = topProduct;
            } else {
                _selectedTopProductsPrefab = null;
            }

            if(_lastCreatedTextBox != null)
            {
                Destroy(_lastCreatedTextBox);
                _lastCreatedTextBox = null;
                _lastCreatedTextBoxOverlay = null;
            }
        }

        private void HighlightProduct(Transform product, Color color, bool addTemporaryEmissionHighlight = false)
        {
            if(addTemporaryEmissionHighlight)
            {
                product.gameObject.AddComponent<HighlightWithEmission>();
            }

            _oldLayer = product.gameObject.layer;
            product.gameObject.layer = LayerMask.NameToLayer("DynamicProductOutlined");

            product.gameObject.AddComponent<HighlightEffect>();
            HighlightEffect highlightEffect = product.gameObject.GetComponent<HighlightEffect>();
            
            /*highlightEffect.outline = 0;
            highlightEffect.glow = 2;
            highlightEffect.glowWidth = 0.5f;
            highlightEffect.glowQuality = HighlightPlus.QualityLevel.Highest;
            highlightEffect.glowHQColor = color;
            highlightEffect.glowBlurMethod = BlurMethod.Kawase;
            highlightEffect.glowDownsampling = 1;*/

            highlightEffect.outline = 1;
            highlightEffect.outlineWidth = 0.2f;
            highlightEffect.glow = 0;
            highlightEffect.outlineQuality = HighlightPlus.QualityLevel.High;
            highlightEffect.outlineColor = color;

            highlightEffect.highlighted = true;
        }

        private void GrayscaleProduct(GameObject product)
        {
            Renderer renderer = product.GetComponentInChildren<Renderer>();
            if(renderer != null)
            {
                Material mat = renderer.material;
                Shader shader = mat.shader;
                if(!_materialToOriginalShader.ContainsKey(mat))
                {
                    _materialToOriginalShader.Add(mat, shader);
                }
                
                mat.shader = _grayscaleShader;
            }
        }

        public void PickUpProduct(ProductFacade product)
        {
            _topProductsUI?.SetActive(false);
            if(product.GetComponent<HighlightEffect>() != null)
            {
                Destroy(product.GetComponent<HighlightEffect>());
            }

            HighlightWithEmission highlightWithEmission = product.GetComponent<HighlightWithEmission>();
            if(highlightWithEmission)
            {
                highlightWithEmission.StopHighlight();
            }

            Renderer renderer = product.GetComponentInChildren<Renderer>();
            if(renderer != null)
            {
                Material mat = renderer.material;
                if(mat.shader == _grayscaleShader)
                {
                    if(_materialToOriginalShader.ContainsKey(mat))
                    {
                        mat.shader = _materialToOriginalShader[mat];
                    }
                }
            }

        }

        public void PutBackProduct(ProductFacade product)
        {
            //_topProductsUI?.SetActive(true);
            if(_selectedTopProducts.Contains(product.gameObject))
            {
                HighlightProduct(product.transform, _topProductsColorDict[product.gameObject.name.Replace("(Clone)", "")]);
            }
            else if(_selectedInnovations.Contains(product.gameObject))
            {
                HighlightProduct(product.transform, _innovationsColorDict[product.gameObject.name.Replace("(Clone)", "")]);
            }
            else if(_shelfUnitState == ShelfUnitState.TopProducts || _shelfUnitState == ShelfUnitState.Innovations)
            {
                GrayscaleProduct(product.gameObject);
            }
        }

        public void SelectProduct(ProductFacade productFacade)
        {
            if(_selectedTopProducts.Contains(productFacade.gameObject))
            {
                foreach(GameObject product in _selectedTopProducts)
                {
                    if(product.gameObject.GetComponent<HighlightWithEmission>() != null)
                    {
                        break;
                    }
                }
            }
        }

        public void HoverProduct(ProductFacade product)
        {
            if(_selectedTopProducts.Contains(product.gameObject))
            {
                if(_lastCreatedTextBoxOverlay != _selectedTopProductsPrefab)
                {
                    if(_lastCreatedTextBox != null)
                    {
                        Destroy(_lastCreatedTextBox);
                    }
                    
                    _lastCreatedTextBox = CreateOverlayText(_selectedTopProductsPrefab, transform);
                    _lastCreatedTextBoxOverlay = _selectedTopProductsPrefab;
                }
            }
            else if(_selectedInnovations.Contains(product.gameObject))
            {
                if(_lastCreatedTextBoxOverlay != _productToInnovation[product.gameObject])
                {
                    if(_lastCreatedTextBox != null)
                    {
                        Destroy(_lastCreatedTextBox);
                    }

                    _lastCreatedTextBox = CreateOverlayText(_productToInnovation[product.gameObject], transform);
                    _lastCreatedTextBoxOverlay = _productToInnovation[product.gameObject];
                }
            }
        }

        public void EndHoverProduct(ProductFacade product)
        {

        }

        private void CreateParent(ref Transform parent, string name)
        {
            if(parent != null)
            {
                if(Application.isPlaying)
                {
                    Destroy(parent.gameObject);
                }
                else
                {
                    DestroyImmediate(parent.gameObject);
                }
            }

            parent = (new GameObject()).transform;
            parent.gameObject.name = name;
            parent.parent = transform;
            parent.localPosition = Vector3.zero;
            parent.localRotation = Quaternion.identity;
            parent.localScale = Vector3.one;
        }

        public void BackupAndDestroyParents()
        {
            SetParents();
            if(_currentPlanogram == null && _productsParent != null)
            {
                _productsParentBackup = _productsParent;
                _productsParentBackup.gameObject.name = _productsParentBackup.gameObject.name + " backup";
                _productsParentBackup.gameObject.SetActive(false);
                _productsParent = null;
            }
        }

        public void CreateParents()
        {
            CreateParent(ref _productsParent, "ProductsParent");
            CreateParent(ref _bannersParent, "BannersParent");
            CreateParent(ref _overlaysParent, "OverlaysParent");
            CreateParent(ref _infoParent, "InfoParent");
        }

        public void UnloadPlanogram()
        {
            if(_productsParent != null)
            {
                if(Application.isPlaying){Destroy(_productsParent.gameObject);}else{DestroyImmediate(_productsParent.gameObject);};
            }
            if(_bannersParent != null)
            {
                if(Application.isPlaying){Destroy(_bannersParent.gameObject);}else{DestroyImmediate(_bannersParent.gameObject);};
            }
            if(_overlaysParent != null)
            {
                if(Application.isPlaying){Destroy(_overlaysParent.gameObject);}else{DestroyImmediate(_overlaysParent.gameObject);};
            }
            _currentPlanogram = null;
            _currentPlanogramId = -1;
        }

        public void RestoreBackup()
        {
            if(_productsParentBackup != null)
            {
                _productsParentBackup.gameObject.name = "ProductsParent";
                _productsParentBackup.gameObject.SetActive(true);
            }
            SetParents();
        }

        public List<PlanogramShelf> SavePlanogramShelves(List<GameObject> products)
        {
            List<PlanogramShelf> shelves = new List<PlanogramShelf>();
            
            Dictionary<GameObject, float> productToRowHeight = new Dictionary<GameObject, float>();

            foreach(GameObject product in products)
            {
                float productHeight = product.transform.position.y;

                bool foundMatchingHeight = false;
                foreach(float height in productToRowHeight.Values)
                {
                    if(Mathf.Abs(productHeight - height) < 0.1f)
                    {
                        productHeight = height;
                        foundMatchingHeight = true;
                        break;
                    }
                }

                productToRowHeight.Add(product, productHeight);
            }

            List<float> heights = productToRowHeight.Values.Distinct().OrderByDescending(v => v).ToList();

            if(heights.Count > _shelfStartPositions.Length)
            {
                Debug.LogError("Cannot save planogram: more heights detected than shelves defined");
                return shelves;
            }
            if(heights.Count < _shelfStartPositions.Length)
            {
                Debug.LogError("Less heights ("+ heights.Count +") detected than shelves ("+ _shelfStartPositions.Length + ") defined. Assuming products are for the highest shelves.");
            }

            List<List<GameObject>> productsInOrder = new List<List<GameObject>>();
            // Debug.Log("Processing heights: " + heights.Count); 
            for(int i=0; i<heights.Count; i++)
            {
                productsInOrder.Add(new List<GameObject>());
                foreach(GameObject product in productToRowHeight.Keys)
                {
                    if(productToRowHeight[product] == heights[i])
                    {
                        productsInOrder[i].Add(product);
                        Debug.Log("Adding product :" + product.name + " to height " + i);
                    }
                }
            }

            for(int i=0; i<heights.Count; i++)
            {
                PlanogramShelf shelf = new PlanogramShelf();
                shelves.Add(shelf);
                shelf.Rows = new List<PlanogramShelfRow>();
                PlanogramShelfRow row = new PlanogramShelfRow();
                shelf.Rows.Add(row);
                row.Products = new List<string>();

                Vector3 productStartPos = transform.TransformPoint(_shelfStartPositions[i]);
                if(_leftToRight != inputDataIsLeftToRight)
                {
                    productStartPos = transform.TransformPoint(_shelfEndPositions[i]);
                }
                productsInOrder[i] = productsInOrder[i].OrderBy(v => Vector3.Distance(v.transform.position, productStartPos)).ToList();

                foreach(GameObject product in productsInOrder[i])
                {
                    row.Products.Add(product.name);
                }
            }

            return shelves;
        }
    }

}