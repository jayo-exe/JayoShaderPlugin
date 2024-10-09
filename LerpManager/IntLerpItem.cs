using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JayoShaderPlugin.LerpManager
{
    class IntLerpItem : ILerpItem
    {
        public event Action<string, string, int, int> LerpCalculated;

        public string materialName { get; set; }
        public string propertyName { get; set; }
        public float lerpTime { get; set; }
        public float currentLerpTime { get; set; }

        public int startValue { get; set; }
        public int currentValue { get; set; }
        public int targetValue { get; set; }

        public void DoLerp()
        {
            float lerpFactor = Math.Min((currentLerpTime / lerpTime), 1.0f);
            currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, lerpFactor));
            LerpCalculated.Invoke(materialName, propertyName, currentValue, 0);
        }
    }
}
