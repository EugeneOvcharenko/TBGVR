using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HighlightPlus;

namespace Tropicana
{
    public class PlantTourSphere : MonoBehaviour
    {
        [SerializeField] string _videoUri;

        private HighlightEffect _highlightEffect;
        private PlantTour _plantTour;

        void Awake()
        {
            _highlightEffect = GetComponent<HighlightEffect>();
            _plantTour = FindObjectOfType<PlantTour>();
        }

        void OnMouseEnter()
        {
            _highlightEffect.highlighted = true;
        }

        void OnMouseExit()
        {
            _highlightEffect.highlighted = false;
        }

        void OnMouseUpAsButton()
        {
            //_plantTour.PlayVideo(_videoUri);
        }
    }
}