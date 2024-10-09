using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace JayoShaderPlugin.LerpManager
{
    public class JayoShaderLerpManager : MonoBehaviour
    {

        public event Action<string, string, int, int> IntLerpCalculated;
        public event Action<string, string, float, int> FloatLerpCalculated;
        public event Action<string, string, Color, int> ColorLerpCalculated;
        public event Action<string, string, Vector4, int> Vector4LerpCalculated;
        public event Action<string, string, Vector2, int> TextureScaleLerpCalculated;
        public event Action<string, string, Vector2, int> TextureOffsetLerpCalculated;

        private List<string> finishedLerps = new List<string>();
        private Dictionary<string, ILerpItem> propertyLerps = new Dictionary<string, ILerpItem>();

        public void startLerp(string materialName, string propertyName, int startValue, int targetValue, float lerpTime)
        {
            IntLerpItem item = new IntLerpItem()
            {
                materialName = materialName,
                propertyName = propertyName,
                startValue = startValue,
                currentValue = startValue,
                targetValue = targetValue,
                lerpTime = lerpTime,
                currentLerpTime = 0f
            };
            item.LerpCalculated += (m, p, v, l) => IntLerpCalculated.Invoke(m, p, v, l);
            propertyLerps[$"{materialName};;{propertyName}"] = item;
        }

        public void startLerp(string materialName, string propertyName, float startValue, float targetValue, float lerpTime)
        {
            FloatLerpItem item = new FloatLerpItem()
            {
                materialName = materialName,
                propertyName = propertyName,
                startValue = startValue,
                currentValue = startValue,
                targetValue = targetValue,
                lerpTime = lerpTime,
                currentLerpTime = 0f
            };
            item.LerpCalculated += (m, p, v, l) => FloatLerpCalculated.Invoke(m, p, v, l);
            propertyLerps[$"{materialName};;{propertyName}"] = item;
        }

        public void startLerp(string materialName, string propertyName, Color startValue, Color targetValue, float lerpTime)
        {
            ColorLerpItem item = new ColorLerpItem()
            {
                materialName = materialName,
                propertyName = propertyName,
                startValue = startValue,
                currentValue = startValue,
                targetValue = targetValue,
                lerpTime = lerpTime,
                currentLerpTime = 0f
            };
            item.LerpCalculated += (m, p, v, l) => ColorLerpCalculated.Invoke(m, p, v, l);
            propertyLerps[$"{materialName};;{propertyName}"] = item;
        }

        public void startLerp(string materialName, string propertyName, string subType, Vector2 startValue, Vector2 targetValue, float lerpTime)
        {
            if(subType == "scale")
            {
                TextureScaleLerpItem item = new TextureScaleLerpItem()
                {
                    materialName = materialName,
                    propertyName = propertyName,
                    startValue = startValue,
                    currentValue = startValue,
                    targetValue = targetValue,
                    lerpTime = lerpTime,
                    currentLerpTime = 0f
                };
                item.LerpCalculated += (m, p, v, l) => TextureScaleLerpCalculated.Invoke(m, p, v, l);
                propertyLerps[$"{materialName};;{propertyName}"] = item;
            } else if (subType == "offset")
            {
                TextureOffsetLerpItem item = new TextureOffsetLerpItem()
                {
                    materialName = materialName,
                    propertyName = propertyName,
                    startValue = startValue,
                    currentValue = startValue,
                    targetValue = targetValue,
                    lerpTime = lerpTime,
                    currentLerpTime = 0f
                };
                item.LerpCalculated += (m, p, v, l) => TextureOffsetLerpCalculated.Invoke(m, p, v, l);
                propertyLerps[$"{materialName};;{propertyName}"] = item;
            }
            
            
        }

        public void startLerp(string materialName, string propertyName, Vector4 startValue, Vector4 targetValue, float lerpTime)
        {
            Vector4LerpItem item = new Vector4LerpItem()
            {
                materialName = materialName,
                propertyName = propertyName,
                startValue = startValue,
                currentValue = startValue,
                targetValue = targetValue,
                lerpTime = lerpTime,
                currentLerpTime = 0f
            };
            item.LerpCalculated += (m, p, v, l) => Vector4LerpCalculated.Invoke(m, p, v, l);
            propertyLerps[$"{materialName};;{propertyName}"] = item;
        }

        private void checkLerps()
        {
            if (propertyLerps.Count == 0) return;

            foreach (KeyValuePair<string, ILerpItem> lerpItem in propertyLerps)
            {
                lerpItem.Value.currentLerpTime += (Time.deltaTime * 1000);
                lerpItem.Value.DoLerp();
                if (lerpItem.Value.currentLerpTime >= lerpItem.Value.lerpTime)
                {
                    finishedLerps.Add(lerpItem.Key);
                }
            }
        }

        private void clearFinishedLerps()
        {
            if (finishedLerps.Count == 0) return;

            foreach(string lerpKey in finishedLerps)
            {
                propertyLerps.Remove(lerpKey);
            }
            finishedLerps = new List<string>();
        }

        public void Update()
        {
            checkLerps();
            clearFinishedLerps();
        }
    }
}
