using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace JayoShaderPlugin.UI
{
    public class MaterialListItem : MonoBehaviour
    {
        public GameObject NameTextObject;
        public GameObject CollapseButtonObject;
        public GameObject ExpandButtonObject;
        public GameObject CopyButtonObject;
        public Dictionary<string,GameObject> ChildProperties;

        public event Action MaterialListCollapsed;
        public event Action MaterialListExpanded;

        private bool isCollapsed;
        private string materialName;
        private string materialInstance;

        public void Awake()
        {
            ChildProperties = new Dictionary<string, GameObject>();
        }

        public void OnDisable()
        {
            foreach (GameObject childProperty in ChildProperties.Values)
            {
                childProperty.SetActive(false);
            }
        }

        public void OnEnable()
        {
            foreach (GameObject childProperty in ChildProperties.Values)
            {
                if (isCollapsed) continue;
                childProperty.SetActive(true);
            }
        }

        public void PrepareUI(string matName, string instanceId)
        {
            materialName = matName;
            materialInstance = instanceId;
            NameTextObject.GetComponent<Text>().text = $"{materialName} ({materialInstance})";
            isCollapsed = false;
            CopyButtonObject.GetComponent<Button>().onClick.AddListener(() => { CopyMaterial(); });
            CollapseButtonObject.GetComponent<Button>().onClick.AddListener(() => { CollapseChildren(); });
            ExpandButtonObject.GetComponent<Button>().onClick.AddListener(() => { ExpandChildren(); });
            ExpandButtonObject.SetActive(false);
        }

        public void CollapseChildren()
        {
            if (isCollapsed) return;

            foreach (GameObject childProperty in ChildProperties.Values)
            {
                childProperty.SetActive(false);
            }

            isCollapsed = true;
            CollapseButtonObject.SetActive(false);
            ExpandButtonObject.SetActive(true);
            MaterialListCollapsed.Invoke();
        }

        public void ExpandChildren()
        {
            if (!isCollapsed) return;

            foreach (GameObject childProperty in ChildProperties.Values)
            {
                childProperty.SetActive(true);
            }

            isCollapsed = false;
            CollapseButtonObject.SetActive(true);
            ExpandButtonObject.SetActive(false);
            MaterialListExpanded.Invoke();
        }

        public void DestroyChildren()
        {
            foreach (GameObject childProperty in ChildProperties.Values)
            {
                GameObject.Destroy(childProperty);
            }
        }

        //copy the currently-selected material name to the clipboard
        public void CopyMaterial()
        {
            GUIUtility.systemCopyBuffer = materialName;
        }
    }
}
