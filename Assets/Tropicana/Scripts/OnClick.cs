using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tropicana
{
    public class OnClick : MonoBehaviour
    {
        public event System.Action OnClicked; 

        void OnMouseUpAsButton()
        {
            OnClicked?.Invoke();
        }
    }
}
