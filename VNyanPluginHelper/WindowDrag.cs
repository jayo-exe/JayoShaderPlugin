﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JayoShaderPlugin.VNyanPluginHelper
{
    class WindowFocus : MonoBehaviour, IPointerDownHandler
    {
        public RectTransform focusRect;

        public void OnPointerDown(PointerEventData eventData)
        {
            focusRect.SetAsLastSibling();
            transform.SetAsLastSibling();
        }
    }
}
