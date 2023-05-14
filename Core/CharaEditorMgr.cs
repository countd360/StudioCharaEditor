using AIChara;
using BepInEx;
using KKAPI.Studio;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;
using CharaCustom;
using HarmonyLib;

namespace StudioCharaEditor
{
    class CharaEditorMgr : MonoBehaviour
    {
        public CharaEditorUI gui;
        public Dictionary<OCIChar, CharaEditorController> charaEditorCtrlDict = new Dictionary<OCIChar, CharaEditorController>();
        public Dictionary<string, Dictionary<string, string>> charaEditorLocalizeDict = new Dictionary<string, Dictionary<string, string>>();

        public static CharaEditorMgr Instance { get; private set; }

        public static CharaEditorMgr Install(GameObject container)
        {
            if (CharaEditorMgr.Instance == null)
            {
                CharaEditorMgr.Instance = container.AddComponent<CharaEditorMgr>();
            }
            return CharaEditorMgr.Instance;
        }

        public bool VisibleGUI
        {
            get => gui.VisibleGUI;
            set => gui.VisibleGUI = value;
        }

        private void Awake()
        {
        }

        private void Start()
        {
            StartCoroutine(LoadingCo());
        }

        //[Warning: Unity Log] OnLevelWasLoaded was found on ConsolePlugin
        //This message has been deprecated and will be removed in a later version of Unity.
        //Add a delegate to SceneManager.sceneLoaded instead to get notifications after scene loading has completed
        private IEnumerator LoadingCo()
        {
            yield return new WaitUntil(() => StudioAPI.StudioLoaded);
            // Wait until fully loaded
            yield return null;

            // start ui
            gui = new GameObject("GUI").AddComponent<CharaEditorUI>();
            gui.transform.parent = base.transform;
            gui.VisibleGUI = false;
            //Console.WriteLine("StudioCharaEditor CharaEditorMgr Started.");

            // check extra plugins
        }

        public void ResetGUI()
        {
            gui.ResetGui();
        }

        public void HouseKeeping(bool isVisible)
        {
            // release deleted controller
            if (isVisible)
            {
                foreach (OCIChar ociChar in charaEditorCtrlDict.Keys)
                {
                    if (ociChar.charInfo == null)
                    {
                        Console.WriteLine("Remove controller for deleted chara");
                        charaEditorCtrlDict.Remove(ociChar);
                        return;
                    }
                }
            }

            // housekeeping for controller
            if (isVisible)
            {
                foreach (var ctrl in charaEditorCtrlDict.Values)
                {
                    ctrl.RefreshAccessoriesListIfExpired();
                }
            }
        }

        public CharaEditorController GetEditorController(OCIChar ociTarget)
        {
            if (ociTarget == null)
            {
                return null;
            }
            if (!charaEditorCtrlDict.ContainsKey(ociTarget))
            {
                charaEditorCtrlDict[ociTarget] = new CharaEditorController(ociTarget);
                charaEditorCtrlDict[ociTarget].Initialize();
            }
            return charaEditorCtrlDict[ociTarget];
        }

        public CharaEditorController GetEditorController(ChaControl chaCtrl)
        {
            foreach (OCIChar ociChar in charaEditorCtrlDict.Keys)
            {
                if (ociChar.charInfo == chaCtrl)
                {
                    return charaEditorCtrlDict[ociChar];
                }
            }
            return null;
        }

        public void ReloadDictionary()
        {
            LoadExtendSetting();
            if (gui != null)
            {
                gui.curLocalizationDict = AssignLocalizeDict();
            }
        }

        public void LoadExtendSetting()
        {
            try
            {
                string xmlFilename = Path.Combine(GetDllPath(), "HS2StudioCharaEditor.xml");
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(xmlFilename);

                XmlNode rootNode = xDoc.DocumentElement;
                if (!rootNode.Name.Equals("StudioCharaEditorSetting"))
                {
                    throw new Exception("Root element missed!?");
                }

                charaEditorLocalizeDict.Clear();
                foreach (XmlNode sNode in rootNode.ChildNodes)
                {
                    if (sNode.Name.Equals("LocalizeDictionary"))
                    {
                        string dicName;
                        XmlAttribute attr = (XmlAttribute)sNode.Attributes.GetNamedItem("language");
                        if (attr == null || string.IsNullOrWhiteSpace(attr.Value))
                            dicName = "default";
                        else
                            dicName = attr.Value;
                        charaEditorLocalizeDict[dicName] = new Dictionary<string, string>();

                        foreach (XmlNode ssNode in sNode.ChildNodes)
                        {
                            if (ssNode.Name.Equals("DictPair"))
                            {
                                XmlAttribute srcAttr = (XmlAttribute)ssNode.Attributes.GetNamedItem("source");
                                if (srcAttr == null || string.IsNullOrWhiteSpace(srcAttr.Value))
                                    continue;
                                string srcText = srcAttr.Value;
                                string toText = ssNode.InnerText;
                                charaEditorLocalizeDict[dicName][srcText] = toText;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void SaveExtendSetting()
        {
            try
            {
                string xmlFilename = Path.Combine(GetDllPath(), "HS2StudioCharaEditor.xml");
                XmlDocument xDoc = new XmlDocument();

                XmlElement rootNode = xDoc.CreateElement("StudioCharaEditorSetting");
                xDoc.AppendChild(rootNode);

                if (charaEditorLocalizeDict != null && charaEditorLocalizeDict.Count > 0)
                {
                    foreach (string dicName in charaEditorLocalizeDict.Keys)
                    {
                        XmlElement dicRoot = xDoc.CreateElement("LocalizeDictionary");
                        dicRoot.SetAttribute("language", dicName);
                        rootNode.AppendChild(dicRoot);

                        foreach (string srcText in charaEditorLocalizeDict[dicName].Keys)
                        {
                            XmlElement dicItem = xDoc.CreateElement("DictPair");
                            dicItem.SetAttribute("source", srcText);
                            dicItem.InnerText = charaEditorLocalizeDict[dicName][srcText];
                            dicRoot.AppendChild(dicItem);
                        }
                    }
                }

                xDoc.Save(xmlFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private Dictionary<string, string> AssignLocalizeDict()
        {
            string tgtDicName = StudioCharaEditor.UILanguage.Value;

            if (charaEditorLocalizeDict != null)
            {
                if (charaEditorLocalizeDict.ContainsKey(tgtDicName))
                {
                    return charaEditorLocalizeDict[tgtDicName];
                }
                else if (charaEditorLocalizeDict.ContainsKey("default"))
                {
                    return charaEditorLocalizeDict["default"];
                }
            }
            return null;
        }

        static public string GetDllPath()
        {
            //string dllPath = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).AbsolutePath);
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string dllPath = Path.GetDirectoryName(path);
            return dllPath;
        }

        static public string GetExportCharaPath(byte sex)
        {
            string exportPath = StudioCharaEditor.CharaExportPath.Value;
            string defPath = Path.Combine(Paths.GameRootPath, sex == 0 ? "UserData\\chara\\male" : "UserData\\chara\\female");
            if (exportPath.Contains(StudioCharaEditor.DefaultPathMacro))
            {
                exportPath = exportPath.Replace(StudioCharaEditor.DefaultPathMacro, defPath);
            }
            return exportPath;
        }

        static public string GetExportCoordPath(byte sex)
        {
            string exportPath = StudioCharaEditor.CoordExportPath.Value;
            string defPath = Path.Combine(Paths.GameRootPath, sex == 0 ? "UserData\\coordinate\\male" : "UserData\\coordinate\\female");
            if (exportPath.Contains(StudioCharaEditor.DefaultCoordMacro))
            {
                exportPath = exportPath.Replace(StudioCharaEditor.DefaultCoordMacro, defPath);
            }
            return exportPath;
        }

        static public bool SetCustomBase(ChaControl chaCtrl)
        {
            // check and init CustomBase
            if (Singleton<CustomBase>.Instance == null)
            {
                try
                {
                    CustomBase dummyCustomBase = CharaEditorMgr.Instance.gameObject.AddComponent<CustomBase>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("This is an expected exception for creating a CustomBase in studio: " + ex.Message);
                }

                // re-check
                if (Singleton<CustomBase>.Instance == null)
                {
                    StudioCharaEditor.Logger.LogError("Fail to create CustomBase.");
                    return false;
                }
            }

            try
            {
                Singleton<CustomBase>.Instance.chaCtrl = chaCtrl;
                return true;
            }
            catch (Exception ex)
            {
                StudioCharaEditor.Logger.LogError("Fail to set CustomBase.chaCtrl: " + ex.Message);
                return false;
            }
        }

        static public bool SetMakerApiInsideMaker(bool insideMaker)
        {
            try
            {
                //KKAPI.Maker.MakerAPI.InsideMaker = insideMaker;
                Traverse makerApiCls = Traverse.CreateWithType("KKAPI.Maker.MakerAPI, HS2API");
                Traverse insideMakerField = makerApiCls.Field("_insideMaker");
                if (!insideMakerField.FieldExists())
                {
                    Console.WriteLine("_insideMaker not found in KKAPI.Maker.MakerAPI");
                    return false;
                }
                insideMakerField.SetValue(insideMaker);
                Console.WriteLine("Set KKAPI.Maker.MakerAPI._insideMaker = {0}", insideMaker);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("This is an expected exception for set MakerAPI.InsiderMaker in studio: " + ex.Message);
                return false;
            }
        }
    }
}
