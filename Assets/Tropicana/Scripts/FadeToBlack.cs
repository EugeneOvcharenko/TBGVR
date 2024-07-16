using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeToBlack : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image _fadeImage;
    private float _fadeSpeedSeconds = 0.5f;
    private float _timer;

    public void ToggleFade(bool toBlack, bool immediatelyUndo = false, System.Action callback = null)
    {
        _timer = 0;
        StartCoroutine(Fade(toBlack, immediatelyUndo, callback));
    }

    IEnumerator Fade(bool toBlack, bool immediatelyUndo, System.Action callback)
    {
        Color color = _fadeImage.color;
        float alpha = toBlack? 1 : 0;
        while(_fadeImage.color.a != alpha)
        {
            color.a = System.Math.Max(System.Math.Min((toBlack? _timer / _fadeSpeedSeconds : 1 - _timer / _fadeSpeedSeconds), 1), 0);
            _fadeImage.color = color;
            _timer += Time.deltaTime;
            yield return null;
        }

        if(callback != null)
        {
            try
            {
                callback();
            }
            catch(System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
            
        }

        if(immediatelyUndo)
        {
            ToggleFade(!toBlack);
        }
    }
}
