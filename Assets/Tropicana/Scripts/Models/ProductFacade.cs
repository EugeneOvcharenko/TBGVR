using System;
using UnityEngine;
using UnityEngine.Serialization;
using HighlightPlus;
using ReadyPlayerMe.Samples;

namespace Tropicana.Models
{
    public class ProductFacade : MonoBehaviour
    {
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Collider _collider;

        public CProduct Product;
        private ShelfUnitController _shelfUnitController = null;
        private GameObject _productInfoPrefab;
        private CameraFollow _cameraFollow;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshFilter != null)
            {
                if(GetComponent<Collider>() == null)
                {
                    _collider = gameObject.AddComponent<BoxCollider>();
                }
            }
            else
            {
                Debug.LogError("Product is not configured correctly.");
            }

            Transform parent = transform.parent;
            while(parent != null)
            {
                _shelfUnitController = parent.GetComponent<ShelfUnitController>();
                if(_shelfUnitController == null)
                {
                    parent = parent.parent;
                }
                else
                {
                    break;
                }
            }

            _cameraFollow = FindObjectOfType<CameraFollow>();
            PrefabList prefabList = FindObjectOfType<PrefabList>();
            _productInfoPrefab = prefabList.productInfoPrefab;
        }

        public void Hover()
        {
            EnableOutline();
            _shelfUnitController.HoverProduct(this);
        }

        public void EndHover()
        {
            DisableOutline();
            _shelfUnitController.EndHoverProduct(this);
        }

        public void SelectProduct()
        {   
            /*if(GetComponent<StarbucksProductsUI>() != null)
            {
                GetComponent<StarbucksProductsUI>().SelectProduct();
            }
            else
            {
                bool canBePickedUP = !Product.CannotBePickedUp && (_shelfUnitController?.AllowPickup ?? true);
                if(canBePickedUP)
                {
                    _shelfUnitController?.PickUpProduct(this);

                    _cameraFollow.CanChangeMode = false;
                    Transform canvas = Camera.main.transform.parent.GetComponentInChildren<Canvas>().transform;
                    
                    UIProductInfo uiProductInfo = FindObjectOfType<UIProductInfo>();
                    if(uiProductInfo == null)
                    {
                        GameObject productInfoWindow = Instantiate(_productInfoPrefab, canvas, false);
                        uiProductInfo = productInfoWindow.GetComponent<UIProductInfo>();
                    }

                    uiProductInfo.Show(this, (pickingProductToCompare) =>
                    {
                        _cameraFollow.CanChangeMode = true;
                        if(!pickingProductToCompare) {
                            _shelfUnitController?.PutBackProduct(this);
                        }
                        
                    });
                }
                else
                {
                    _shelfUnitController?.SelectProduct(this);
                }
            }*/
        }

        public void EnableOutline()
        {
            if(gameObject.layer != LayerMask.NameToLayer("DynamicProductOutlined"))
            {
                gameObject.AddComponent<HighlightEffect>();
                HighlightEffect highlightEffect = GetComponent<HighlightEffect>();
                
                highlightEffect.outline = 0;
                highlightEffect.glow = 1;
                highlightEffect.glowWidth = 0.5f;
                highlightEffect.glowQuality = HighlightPlus.QualityLevel.Highest;
                highlightEffect.glowHQColor = new Color(226/255f,125/255f,44/255f);
                highlightEffect.glowBlurMethod = BlurMethod.Kawase;
                highlightEffect.glowDownsampling = 1;

                highlightEffect.highlighted = true;
            }
        }

        public void DisableOutline()
        {
            if(gameObject.layer != LayerMask.NameToLayer("DynamicProductOutlined"))
            {
                Destroy(GetComponent<HighlightEffect>());
            }
        }
    }
}