﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JayoShaderPlugin.LerpManager
{
    class ColorLerpItem : ILerpItem
    {
        public event Action<string, string, Color, int> LerpCalculated;
        public string materialName { get; set; }
        public string propertyName { get; set; }
        public float lerpTime { get; set; }
        public float currentLerpTime { get; set; }
        public Color startValue { get; set; }
        public Color currentValue { get; set; }
        public Color targetValue { get; set; }

        public void DoLerp()
        {
            float lerpFactor = Math.Min((currentLerpTime / lerpTime), 1.0f);
            currentValue = Color.Lerp(startValue, targetValue, lerpFactor);
            LerpCalculated.Invoke(materialName, propertyName, currentValue, 0);
        }
    }

}
