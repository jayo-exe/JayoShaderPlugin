﻿using System;
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
using JayoShaderPlugin.UI;

namespace JayoShaderPlugin
{
    public class JayoShaderPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler
    {
        public GameObject windowPrefab;
        public GameObject window;

        private MainThreadDispatcher mainThread;
        private JayoShaderManager shaderManager;
        private AnimatedPropertiesList PropertiesList;
        private Dictionary<string, string> HexToDec;

        private VNyanHelper _VNyanHelper;
        private VNyanPluginUpdater updater;

        private GameObject instructionsTab;
        private GameObject instructionsButton;
        private GameObject propertyListTab;
        private GameObject propertyListButton;
        private GameObject propertyListSearch;
        private GameObject propertyListFilter;
        private GameObject propertyListRefetch;
        private GameObject pageOnePanel;
        private GameObject pageOneButton;
        private GameObject pageTwoPanel;
        private GameObject pageTwoButton;

        private string currentVersion = "v0.1.0";
        private string repoName = "jayo-exe/JayoShaderPlugin";
        private string updateLink = "https://jayo-exe.itch.io/shader-plugin-for-vnyan";

        private void OnApplicationQuit()
        {
            // Save settings
            savePluginSettings();
        }

        public void Awake()
        {

            Debug.Log($"Shader Plugin is Awake!");
            _VNyanHelper = new VNyanHelper();

            updater = new VNyanPluginUpdater(repoName, currentVersion, updateLink);
            updater.OpenUrlRequested += (url) => mainThread.Enqueue(() => { Application.OpenURL(url); });

            Debug.Log($"Loading Settings");
            // Load settings
            loadPluginSettings();
            updater.CheckForUpdates();

            Debug.Log($"Beginning Plugin Setup");

            mainThread = gameObject.AddComponent<MainThreadDispatcher>();
            shaderManager = gameObject.AddComponent<JayoShaderManager>();
            shaderManager.PropertiesListUpdated += handleShaderPropertiesUpdate;
            
            _VNyanHelper.registerTriggerListener(shaderManager);
            
            try
            {
                window = _VNyanHelper.pluginSetup(this, "Jayo's Basic Shader Plugin", windowPrefab);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

            HexToDec = Enumerable.Range(0, 256).ToDictionary( i => i.ToString("X2"), i => i.ToString());
            foreach(KeyValuePair<string,string> hex in HexToDec)
            {
                _VNyanHelper.setVNyanDictionaryValue("HexToDec", hex.Key, hex.Value);
                _VNyanHelper.setVNyanDictionaryValue("DecToHex", hex.Value, hex.Key);
            }

            // Hide the window by default
            if (window != null)
            {
                propertyListButton = window.transform.Find("Panel/PropertiesButton").gameObject;
                propertyListTab = window.transform.Find("Panel/PropertiesPanel").gameObject;
                propertyListSearch = window.transform.Find("Panel/PropertiesPanel/SearchField").gameObject;
                propertyListFilter = window.transform.Find("Panel/PropertiesPanel/FilterField").gameObject;
                propertyListRefetch = window.transform.Find("Panel/PropertiesPanel/RefetchButton").gameObject;
                instructionsButton = window.transform.Find("Panel/InstructionsButton").gameObject;
                instructionsTab = window.transform.Find("Panel/InstructionsPanel").gameObject;
                pageOnePanel = window.transform.Find("Panel/InstructionsPanel/Page1").gameObject;
                pageOneButton = window.transform.Find("Panel/InstructionsPanel/Page1/PageNumber/Button").gameObject;
                pageTwoPanel = window.transform.Find("Panel/InstructionsPanel/Page2").gameObject;
                pageTwoButton = window.transform.Find("Panel/InstructionsPanel/Page2/PageNumber/Button").gameObject;
                window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                window.SetActive(false);

                try
                {
                    Debug.Log($"Preparing Plugin Window");

                    updater.PrepareUpdateUI(
                        window.transform.Find("Panel/VersionText").gameObject,
                        window.transform.Find("Panel/UpdateText").gameObject,
                        window.transform.Find("Panel/UpdateButton").gameObject
                    );

                    PropertiesList = window.transform.Find("Panel/PropertiesPanel/Scroll View/Viewport/PropertiesLayoutGroup").gameObject.GetComponent<AnimatedPropertiesList>();
                    window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(() => { closePluginWindow(); });
                    propertyListButton.GetComponent<Button>().onClick.AddListener(() => { tabToPropertyList(); });
                    instructionsButton.GetComponent<Button>().onClick.AddListener(() => { tabToInstructions(); });
                    propertyListRefetch.GetComponent<Button>().onClick.AddListener(() => { shaderManager.findNonPoiMaterials(true); });
                    pageOneButton.GetComponent<Button>().onClick.AddListener(() => { toInstructionsPageTwo(); });
                    pageTwoButton.GetComponent<Button>().onClick.AddListener(() => { toInstructionsPageOne(); });
                    
                    Dropdown filterDropdown = propertyListFilter.GetComponent<Dropdown>();
                    filterDropdown.onValueChanged.AddListener((v) => {
                        PropertiesList.TypeFilter = (v == 0) ? "" : filterDropdown.options[v].text;
                        PropertiesList.RebuildList();
                    });

                    propertyListSearch.GetComponent<InputField>().onValueChanged.AddListener((v) => { 
                        PropertiesList.SearchTerm = v; 
                        PropertiesList.RebuildList(); 
                    });

                    shaderManager.findNonPoiMaterials(true);
                    toInstructionsPageOne();
                    tabToPropertyList();
                }
                catch (Exception e)
                {
                    Debug.Log($"Couldn't prepare Plugin Window: {e.Message}");
                }
            }
        }

        private void handleShaderPropertiesUpdate(ShaderPropertyListData propData)
        {
            if (PropertiesList == null) return;
            PropertiesList.PropData = propData;
            PropertiesList.RebuildList();
        }

        public void loadPluginSettings()
        {
            //no settings for this plugin
        }

        public void savePluginSettings()
        {
            //no settings for this plugin
        }

        public void pluginButtonClicked()
        {
            // Flip the visibility of the window when plugin window button is clicked
            if (window != null)
            {
                window.SetActive(!window.activeSelf);
                PropertiesList.RebuildList();
                if (window.activeSelf)
                {
                    window.transform.SetAsLastSibling();
                }
                window.transform.SetAsLastSibling();
            }
        }

        public void closePluginWindow()
        {
            window.SetActive(false);
        }

        public void tabToInstructions()
        {
            instructionsTab.SetActive(true);
            propertyListTab.SetActive(false);
            instructionsButton.GetComponent<Button>().interactable = false;
            propertyListButton.GetComponent<Button>().interactable = true;
        }

        public void tabToPropertyList()
        {
            instructionsTab.SetActive(false);
            propertyListTab.SetActive(true);
            instructionsButton.GetComponent<Button>().interactable = true;
            propertyListButton.GetComponent<Button>().interactable = false;
        }

        public void toInstructionsPageOne()
        {
            pageTwoPanel.SetActive(false);
            pageOnePanel.SetActive(true);
        }

        public void toInstructionsPageTwo()
        {
            pageTwoPanel.SetActive(true);
            pageOnePanel.SetActive(false);
        }

    }
}
