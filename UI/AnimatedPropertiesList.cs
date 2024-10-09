using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace JayoShaderPlugin.UI
{
    public class AnimatedPropertiesList : MonoBehaviour
    {
        public GameObject MaterialItemPrefab;
        public GameObject ListItemPrefab;

        public ShaderPropertyListData PropData;
        public string SearchTerm = "";
        public string TypeFilter = "";

        private string ExpandedMaterial;
        private Dictionary<string, GameObject> MaterialItems;
        private Coroutine FilterCoroutine;

        public void Awake()
        {
            ExpandedMaterial = "";
            MaterialItems = new Dictionary<string, GameObject>();
        }

        public void ClearList()
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            children.ForEach(child => Destroy(child));
        }
        public void RebuildList()
        {
            Dictionary<string, ShaderPropertyListItem> materialsToAdd = new Dictionary<string, ShaderPropertyListItem>();
            List<string> materialsToRemove = new List<string>(MaterialItems.Keys);

            foreach (KeyValuePair<string, ShaderPropertyListItem> materialItem in PropData)
            {
                materialsToRemove.Remove(materialItem.Key);
                if (MaterialItems.ContainsKey(materialItem.Key)) continue;
                materialsToAdd.Add(materialItem.Key, materialItem.Value);
            }

            StartCoroutine(CheckMaterials(materialsToAdd, materialsToRemove));
        }

        IEnumerator CheckMaterials(Dictionary<string, ShaderPropertyListItem> materialsToAdd, List<string> materialsToRemove)
        {
            foreach (string materialName in materialsToRemove)
            {
                MaterialItems[materialName].GetComponent<MaterialListItem>().DestroyChildren();
                Destroy(MaterialItems[materialName]);
                MaterialItems.Remove(materialName);
                yield return null;
            }

            foreach (KeyValuePair<string, ShaderPropertyListItem> materialItem in materialsToAdd)
            {
                bool addedMaterialItem = false;
                GameObject matLabel = null;

                foreach (KeyValuePair<string, ShaderPropertyDetails> propertyItem in materialItem.Value)
                {

                    if (!addedMaterialItem)
                    {
                        matLabel = GameObject.Instantiate(MaterialItemPrefab);
                        matLabel.transform.SetParent(transform);
                        var materialListItem = matLabel.GetComponent<MaterialListItem>();
                        materialListItem.PrepareUI(propertyItem.Value["materialName"], propertyItem.Value["materialInstance"]);
                        materialListItem.MaterialListCollapsed += () => { ExpandedMaterial = ""; };
                        materialListItem.MaterialListExpanded += () => {
                            if(ExpandedMaterial != "") MaterialItems[ExpandedMaterial].GetComponent<MaterialListItem>().CollapseChildren();
                            ExpandedMaterial = materialItem.Key;
                        };
                        addedMaterialItem = true;
                        MaterialItems.Add(materialItem.Key, matLabel);
                    }

                    GameObject newLabel = GameObject.Instantiate(ListItemPrefab);
                    newLabel.transform.SetParent(transform);
                    newLabel.GetComponent<AnimatedPropertyListItem>().PrepareUI(propertyItem.Value["name"], propertyItem.Value["type"]);
                    
                    if (addedMaterialItem && matLabel != null)
                    {
                        matLabel.GetComponent<MaterialListItem>().ChildProperties.Add(propertyItem.Value["name"], newLabel);
                    }

                }
                matLabel.GetComponent<MaterialListItem>().CollapseChildren();
                yield return null;
            }

            FilterList();
        }

        public void FilterList()
        {
            if (FilterCoroutine != null) StopCoroutine(FilterCoroutine);
            FilterCoroutine = StartCoroutine(DoFilter());
        }

        IEnumerator DoFilter()
        {   
            foreach (KeyValuePair<string, ShaderPropertyListItem> materialItem in PropData)
            {
                bool materialMatch = false;
                

                foreach (KeyValuePair<string, ShaderPropertyDetails> propertyItem in materialItem.Value)
                {
                    if (SearchTerm != "" && !propertyItem.Value["materialName"].ToLowerInvariant().Contains(SearchTerm.ToLowerInvariant()))
                    {
                        materialMatch = true;
                        break;
                    }
                    bool itemMatch = true;
                    if (SearchTerm != "" && !propertyItem.Value["name"].ToLowerInvariant().Contains(SearchTerm.ToLowerInvariant())) itemMatch = false;
                    if (TypeFilter != "" && propertyItem.Value["type"] != TypeFilter) itemMatch = false;
                    if (itemMatch)
                    {
                        materialMatch = true;
                        break;
                    }
                }
                MaterialItems[materialItem.Key].SetActive(materialMatch);
                yield return null;
            }
        }


    }
}
