using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq.Expressions;
using VNyanInterface;
using JayoShaderPlugin.VNyanPluginHelper;
using JayoShaderPlugin.LerpManager;



namespace JayoShaderPlugin
{

    public class JayoShaderManager : MonoBehaviour, VNyanInterface.ITriggerHandler
    {

        public GameObject windowPrefab;
        public GameObject window;

        public MainThreadDispatcher mainThread;
        public JayoShaderLerpManager lerpManager;
        public Dictionary<string, string> HexToDec;

        
        public event Action<ShaderPropertyListData> PropertiesListUpdated;

        private string[] sep;
        private VNyanHelper _VNyanHelper;
        private GameObject lastAvatar;
        private Dictionary<string,Material> materials;
        private ShaderPropertyListData propData;

        public void Awake()
        {
            materials = new Dictionary<string, Material>();
            propData = new ShaderPropertyListData();
            sep = new string[] { ";;" };
            _VNyanHelper = new VNyanHelper();
            lerpManager = gameObject.AddComponent<JayoShaderLerpManager>();
            lerpManager.IntLerpCalculated += setShaderInt;
            lerpManager.FloatLerpCalculated += setShaderFloat;
            lerpManager.ColorLerpCalculated += setShaderColor;
            lerpManager.Vector4LerpCalculated += setShaderVector;
            lerpManager.TextureScaleLerpCalculated += setShaderTextureScale;
            lerpManager.TextureOffsetLerpCalculated += setShaderTextureOffset;
        }

        public void Update()
        {
            findNonPoiMaterials();
        }

        public void triggerCalled(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            if (!triggerName.StartsWith("_xjs_")) return;

            //check if this is using the legacy structured trigger names to pass arguments, and pass the the legacy hander if so
            if(triggerName.Contains(";;"))
            {
                handleLegacyTriggers(triggerName);
                return;
            }
            //Debug.Log($"Trigger Details. name: {triggerName} | v1: {value1}, v2: {value2}, v3: {value3} | t1: {text1}, t2: {text2}, t3: {text3}");
            switch (triggerName)
            {
                case "_xjs_refetch":
                    findNonPoiMaterials(true);
                    break;
                case "_xjs_setfloat":
                    setShaderFloat(_VNyanHelper.parseStringArgument(text1), _VNyanHelper.parseStringArgument(text2), _VNyanHelper.parseFloatArgument(text3), value1);
                    break;
                case "_xjs_settexscale":
                    string[] tileValues = _VNyanHelper.parseStringArgument(text3).Split(new string[] { "," }, StringSplitOptions.None);
                    Vector2 newScaleValue = new Vector2(1, 1);
                    if (tileValues.Length == 2)
                    {
                        float x = _VNyanHelper.parseFloatArgument(tileValues[0]);
                        float y = _VNyanHelper.parseFloatArgument(tileValues[1]);
                        newScaleValue = new Vector2(x, y);
                    }
                    setShaderTextureScale(_VNyanHelper.parseStringArgument(text1), _VNyanHelper.parseStringArgument(text2), newScaleValue, value1);
                    break;
                case "_xjs_settexoffset":
                    string[] locValues = _VNyanHelper.parseStringArgument(text3).Split(new string[] { "," }, StringSplitOptions.None);
                    Vector2 newOffsetValue = new Vector2(1, 1);
                    if (locValues.Length == 2)
                    {
                        float x = _VNyanHelper.parseFloatArgument(locValues[0]);
                        float y = _VNyanHelper.parseFloatArgument(locValues[1]);
                        newOffsetValue = new Vector2(x, y);
                    }
                    setShaderTextureOffset(_VNyanHelper.parseStringArgument(text1), _VNyanHelper.parseStringArgument(text2), newOffsetValue, value1);
                    break;
                case "_xjs_setvector":
                    string[] vecValues = _VNyanHelper.parseStringArgument(text3).Split(new string[] { "," }, StringSplitOptions.None);
                    Vector4 newVectorValue = new Vector4(0, 0, 0, 0);
                    if (vecValues.Length >= 2)
                    {
                        newVectorValue.x = _VNyanHelper.parseFloatArgument(vecValues[0]);
                        newVectorValue.y = _VNyanHelper.parseFloatArgument(vecValues[1]);
                    }
                    if (vecValues.Length >= 3)
                    {
                        newVectorValue.z = _VNyanHelper.parseFloatArgument(vecValues[2]);
                    }
                    if (vecValues.Length >= 4)
                    {
                        newVectorValue.w = _VNyanHelper.parseFloatArgument(vecValues[3]);
                    }
                    setShaderVector(_VNyanHelper.parseStringArgument(text1), _VNyanHelper.parseStringArgument(text2), newVectorValue, value1);
                    break;
                case "_xjs_setint":
                    setShaderInt(_VNyanHelper.parseStringArgument(text1), _VNyanHelper.parseStringArgument(text2), (int)_VNyanHelper.parseFloatArgument(text3), value1);
                    break;
                case "_xjs_setcolor":
                    string[] colorValues = _VNyanHelper.parseStringArgument(text3).Split(new string[] { "," }, StringSplitOptions.None);
                    Color newColorValue = new Color();
                    if (colorValues.Length >= 3)
                    {
                        newColorValue.r = _VNyanHelper.parseFloatArgument(colorValues[0]);
                        newColorValue.g = _VNyanHelper.parseFloatArgument(colorValues[1]);
                        newColorValue.b = _VNyanHelper.parseFloatArgument(colorValues[2]);
                    }
                    if (colorValues.Length >= 4)
                    {
                        newColorValue.a = _VNyanHelper.parseFloatArgument(colorValues[3]);
                    }

                    setShaderColor(_VNyanHelper.parseStringArgument(text1), _VNyanHelper.parseStringArgument(text2), newColorValue, value1);
                    break;
                case "_xjs_setcolorhex":
                    Color newHexColorValue = new Color();
                    ColorUtility.TryParseHtmlString(_VNyanHelper.parseStringArgument(text3), out newHexColorValue);

                    setShaderColor(_VNyanHelper.parseStringArgument(text1), _VNyanHelper.parseStringArgument(text2), newHexColorValue, value1);
                    break;
                default:
                    break;
            }

        }

        public void handleLegacyTriggers(string triggerName)
        {
            try
            {
                _VNyanHelper.setVNyanParameterString("_xjs_last_trigger", triggerName);
                string[] triggerParts = triggerName.Split(sep, StringSplitOptions.None);
                string actionName = triggerParts.Length > 0 ? triggerParts[0] : "";
                string matName = triggerParts.Length > 1 ? triggerParts[1] : "";
                string propName = triggerParts.Length > 2 ? triggerParts[2] : "";
                string args = triggerParts.Length > 3 ? triggerParts[3] : "";
                int lerpTime = triggerParts.Length > 4 ? Int32.Parse(triggerParts[4]) : 0;

                //Debug.Log($"Trigger Details. action: {actionName} | mat: {matName} | prop: {propName} | args: {args}");
                switch (actionName)
                {
                    case "_xjs_refetch":
                        findNonPoiMaterials(true);
                        break;
                    case "_xjs_setfloat":
                        setShaderFloat(_VNyanHelper.parseStringArgument(matName), _VNyanHelper.parseStringArgument(propName), _VNyanHelper.parseFloatArgument(args), lerpTime);
                        break;
                    case "_xjs_settexscale":
                        string[] tileValues = _VNyanHelper.parseStringArgument(args).Split(new string[] { "," }, StringSplitOptions.None);
                        Vector2 newScaleValue = new Vector2(1, 1);
                        if (tileValues.Length == 2)
                        {
                            float x = _VNyanHelper.parseFloatArgument(tileValues[0]);
                            float y = _VNyanHelper.parseFloatArgument(tileValues[1]);
                            newScaleValue = new Vector2(x, y);
                        }
                        setShaderTextureScale(_VNyanHelper.parseStringArgument(matName), _VNyanHelper.parseStringArgument(propName), newScaleValue, lerpTime);
                        break;
                    case "_xjs_settexoffset":
                        string[] locValues = _VNyanHelper.parseStringArgument(args).Split(new string[] { "," }, StringSplitOptions.None);
                        Vector2 newOffsetValue = new Vector2(1, 1);
                        if (locValues.Length == 2)
                        {
                            float x = _VNyanHelper.parseFloatArgument(locValues[0]);
                            float y = _VNyanHelper.parseFloatArgument(locValues[1]);
                            newOffsetValue = new Vector2(x, y);
                        }
                        setShaderTextureOffset(_VNyanHelper.parseStringArgument(matName), _VNyanHelper.parseStringArgument(propName), newOffsetValue, lerpTime);
                        break;
                    case "_xjs_setvector":
                        string[] vecValues = _VNyanHelper.parseStringArgument(args).Split(new string[] { "," }, StringSplitOptions.None);
                        Vector4 newVectorValue = new Vector4(0, 0, 0, 0);
                        if (vecValues.Length >= 2)
                        {
                            newVectorValue.x = _VNyanHelper.parseFloatArgument(vecValues[0]);
                            newVectorValue.y = _VNyanHelper.parseFloatArgument(vecValues[1]);
                        }
                        if (vecValues.Length >= 3)
                        {
                            newVectorValue.z = _VNyanHelper.parseFloatArgument(vecValues[2]);
                        }
                        if (vecValues.Length >= 4)
                        {
                            newVectorValue.w = _VNyanHelper.parseFloatArgument(vecValues[3]);
                        }
                        setShaderVector(_VNyanHelper.parseStringArgument(matName), _VNyanHelper.parseStringArgument(propName), newVectorValue, lerpTime);
                        break;
                    case "_xjs_setint":
                        setShaderInt(_VNyanHelper.parseStringArgument(matName), _VNyanHelper.parseStringArgument(propName), (int)_VNyanHelper.parseFloatArgument(args), lerpTime);
                        break;
                    case "_xjs_setcolor":
                        string[] colorValues = _VNyanHelper.parseStringArgument(args).Split(new string[] { "," }, StringSplitOptions.None);
                        Color newColorValue = new Color();
                        if (colorValues.Length >= 3)
                        {
                            newColorValue.r = _VNyanHelper.parseFloatArgument(colorValues[0]);
                            newColorValue.g = _VNyanHelper.parseFloatArgument(colorValues[1]);
                            newColorValue.b = _VNyanHelper.parseFloatArgument(colorValues[2]);
                        }
                        if (colorValues.Length >= 4)
                        {
                            newColorValue.a = _VNyanHelper.parseFloatArgument(colorValues[3]);
                        }

                        setShaderColor(_VNyanHelper.parseStringArgument(matName), _VNyanHelper.parseStringArgument(propName), newColorValue, lerpTime);
                        break;
                    case "_xjs_setcolorhex":
                        Color newHexColorValue = new Color();

                        ColorUtility.TryParseHtmlString(_VNyanHelper.parseStringArgument(args), out newHexColorValue);

                        setShaderColor(_VNyanHelper.parseStringArgument(matName), _VNyanHelper.parseStringArgument(propName), newHexColorValue, lerpTime);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Unable to process trigger in JayoShaderPlugin: {e.Message} ; {e.StackTrace}");
            }
        }

        public void setShaderFloat(string matName, string propName, float newValue, int lerpTime)
        {
             //Debug.Log($"Setting Shader Float Value for {matName} | {propName} to {newValue} over {lerpTime}ms");
            if (newValue == null) return;
            foreach (Material material in materials.Values)
            {
                if (material.name != matName) continue;
                if (material.shader.FindPropertyIndex(propName) == -1) continue;
                if (lerpTime > 0)
                {
                    lerpManager.startLerp(matName, propName, material.GetFloat(propName), newValue, (float)lerpTime);
                }
                else
                {
                    material.SetFloat(propName, (float)newValue);
                }

            }
        }

        public void setShaderInt(string matName, string propName, int newValue, int lerpTime)
        {
            //Debug.Log($"Setting Shader Int Value for {propName} to {newValue}");
            if (newValue == null) return;
            foreach (Material material in materials.Values)
            {
                if (material.name != matName) continue;
                if (material.shader.FindPropertyIndex(propName) == -1) continue;
                if (lerpTime > 0)
                {
                    lerpManager.startLerp(matName, propName, material.GetInt(propName), newValue, (float)lerpTime);
                }
                else
                {
                    material.SetInt(propName, (int)newValue);
                }
            }
        }

        public void setShaderColor(string matName, string propName, Color newValue, int lerpTime)
        {
            //Debug.Log($"Setting Shader Color Value for {propName} to {newValue.ToString()}");
            if (newValue == null) return;
            foreach (Material material in materials.Values)
            {
                if (material.name != matName) continue;
                if (material.shader.FindPropertyIndex(propName) == -1) continue; 
                if (lerpTime > 0)
                {
                    lerpManager.startLerp(matName, propName, material.GetColor(propName), newValue, (float)lerpTime);
                }
                else
                {
                    material.SetColor(propName, (Color)newValue);
                }

            }
        }

        public void setShaderVector(string matName, string propName, Vector4 newValue, int lerpTime)
        {
            //Debug.Log($"Setting Shader Vector Value for {propName} to {newValue.x}, {newValue.y}, {newValue.z}, {newValue.w},");
            if (newValue == null) return;
            foreach (Material material in materials.Values)
            {
                if (material.name != matName) continue; 
                if (material.shader.FindPropertyIndex(propName) == -1) continue; 
                if (lerpTime > 0)
                {
                    lerpManager.startLerp(matName, propName, material.GetVector(propName), newValue, (float)lerpTime);
                }
                else
                {
                    material.SetVector(propName, newValue);
                }

            }
        }

        public void setShaderTextureScale(string matName, string propName, Vector2 newValue, int lerpTime)
        {
            //Debug.Log($"Setting Shader texture Tiling Value for {propName} to {newValue.x}, {newValue.y}");
            foreach (Material material in materials.Values)
            {
                if (material.name != matName) continue;
                if (material.shader.FindPropertyIndex(propName) == -1) continue; 
                if (lerpTime > 0)
                {
                    lerpManager.startLerp(matName, propName, "scale", material.GetTextureScale(propName), newValue, (float)lerpTime);
                }
                else
                {
                    material.SetTextureScale(propName, newValue);
                }

            }
        }

        public void setShaderTextureOffset(string matName, string propName, Vector2 newValue, int lerpTime)
        {
            //Debug.Log($"Setting Shader texture Offset Value for {propName} to {newValue.x} , {newValue.y}");
            foreach (Material material in materials.Values)
            {
                if (material.name != matName) continue;
                if (material.shader.FindPropertyIndex(propName) == -1) continue; 
                if (lerpTime > 0)
                {
                    lerpManager.startLerp(matName, propName, "offset", material.GetTextureOffset(propName), newValue, (float)lerpTime);
                }
                else
                {
                    material.SetTextureOffset(propName, newValue);
                }
            }
        }

        public void findNonPoiMaterials(bool force = false)
        {
            GameObject avatar = _VNyanHelper.getAvatarObject();
            if (!force)
            {
                if (avatar == null) return;
                if (avatar == lastAvatar) return;
            }
            lastAvatar = avatar;

            
            List<string> materialsToRemove = new List<string>(propData.Keys);

            foreach (Renderer renderer in GameObject.FindObjectsOfType<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    string materialKey = $"{material.name} ({material.GetInstanceID()})";

                    materialsToRemove.Remove(materialKey);
                    if (propData.ContainsKey(materialKey)) continue;

                    Debug.Log($"Checking Material {material.name}");
                    Debug.Log($"Shader is {material.shader.name}");
                    
                    if (!material.shader.name.StartsWith(".poiyomi/") && !material.shader.name.StartsWith("Hidden/Locked/.poiyomi/"))
                    {
                        Debug.Log($"Non-Poi shader found! {material.shader.name} on material {material.name}");
                        materials.Add(materialKey, material);
                        propData.Add(materialKey, new ShaderPropertyListItem());
                        findAnimatedParameters(material);
                    }
                }
            }

            foreach (string materialName in materialsToRemove)
            {
                propData.Remove(materialName);
                materials.Remove(materialName);
            }

            PropertiesListUpdated.Invoke(propData);
        }

        private List<string> findAnimatedParameters(Material material)
        {
            List<string> parameterList = new List<string>();
            Shader shader = material.shader;
            int propertyCount = shader.GetPropertyCount();
            Debug.Log($"Shader {shader.name} has {propertyCount} properties");
            for (int i = 0; i <= propertyCount - 1; i++)
            {
                
                propData[$"{material.name} ({material.GetInstanceID()})"].Add(shader.GetPropertyName(i), new ShaderPropertyDetails
                {
                    ["materialName"] = material.name,
                    ["materialInstance"] = material.GetInstanceID().ToString(),
                    ["name"] = shader.GetPropertyName(i),
                    ["type"] = shader.GetPropertyType(i).ToString(),
                    ["flag"] = "animated"
                });
            }
            return parameterList;
        }

    }
}
