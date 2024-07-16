using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tropicana
{
    public class HighlightWithEmission : MonoBehaviour
    {
        [SerializeField]
        private float _highlightTime = 2;
        // 0 means keep repeating
        [SerializeField]
        private int _repeatTimes = 2;
        private int _repeatTimesRemaining;

        [SerializeField]
        private Color _emissionColor = Color.red;

        private float _timer;

        private Material _mat;

        void Start()
        {
            _repeatTimesRemaining = _repeatTimes;

            Renderer renderer = GetComponentInChildren<Renderer>();
            if(renderer)
            {
                if(renderer.material.HasColor("_Emission"))
                {
                    _mat = renderer.material;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(_mat != null)
            {
                _timer += Time.deltaTime;
                if(_timer >= _highlightTime) {
                    _mat.SetColor("_Emission", Color.black);
                    _timer = 0;
                    if(_repeatTimesRemaining == 1) {
                        Destroy(this);
                    } else if(_repeatTimesRemaining > 1) {
                        _repeatTimesRemaining--;
                    }
                } else {
                    if(_timer > _highlightTime/2)
                    {
                        _mat.SetColor("_Emission", Color.Lerp(_emissionColor, Color.black, (_timer - _highlightTime/2)/(_highlightTime/2)));
                    }
                    else
                    {
                        _mat.SetColor("_Emission", Color.Lerp(Color.black, _emissionColor, _timer/(_highlightTime/2)));
                    }
                }
            }
        }

        public void StopHighlight()
        {
            _mat.SetColor("_Emission", Color.black);
            _mat = null;
            Destroy(this);
        }
    }
}