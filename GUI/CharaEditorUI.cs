using AIChara;
using BepInEx.Logging;
using CharaCustom;
using HarmonyLib;
using KKAPI.Utilities;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StudioCharaEditor
{
    class CharaEditorUI : MonoBehaviour
    {
        enum SelectMode
        {
            Normal,
            ForCopy,
            ForPaste,
        };

        private readonly int windowID = 10123;
        private readonly string windowTitle = "Studio Charactor Editor";
        private Rect windowRect = new Rect(0f, 300f, 600f, 400f);
        private bool mouseInWindow = false;

        private TreeNodeObject lastSelectedTreeNode;
        private OCIChar ociTarget;
        private Dictionary<string, object> clipboard;
        private bool renameMode = false;
        private bool searchingMode = false;
        private string tempCharaName;
        private int catelogIndex1;
        private int[] catelogIndex2 = new int[] { 1, 1, 2, 0, 0 };
        private SelectMode detailPageSelect = SelectMode.Normal;
        private Dictionary<string, bool> selectBuffer = new Dictionary<string, bool>();
        private Dictionary<string, Vector2> scrollPool = new Dictionary<string, Vector2>();
        private Dictionary<string, bool> expandPool = new Dictionary<string, bool>();
        private Dictionary<string, Dictionary<string, Texture2D>> thumbPool = new Dictionary<string, Dictionary<string, Texture2D>>();
        private Dictionary<string, string> searchWordPool = new Dictionary<string, string>();

        // save
        private OCIChar savingChara;
        private string savingPath;
        private string savingFilename;
        private Texture2D savingTexture;

        // GUI
        private GUIStyle largeLabel;
        private GUIStyle btnstyle;
        private Vector2 leftScroll = Vector2.zero;
        private Vector2 rightScroll = Vector2.zero;
        private int namew = 100;
        private float thumbSize = 100;
        private float thumbSizeSmall = 70;
        private float thumbBtnHeight = 40;

        // work
        public static Queue<Action> ToDoQueue = new Queue<Action>();

        enum GuiModeType
        {
            MAIN,
            SAVE,
        };
        private GuiModeType guiMode = GuiModeType.MAIN;

        // Localize
        public Dictionary<string, string> curLocalizationDict;

        // Control flag
        public bool VisibleGUI { get; set; }
        public bool LaterUpdate { get; set; }

        public void ResetGui()
        {
            guiMode = GuiModeType.MAIN;
            ociTarget = null;
            renameMode = false;
            searchingMode = false;
            tempCharaName = null;
            catelogIndex1 = 0;
            catelogIndex2 = new int[] { 1, 1, 2, 0, 0 };
            detailPageSelect = SelectMode.Normal;
        }

    private void Start()
        {
            largeLabel = new GUIStyle("label");
            largeLabel.fontSize = 16;
            btnstyle = new GUIStyle("button");
            btnstyle.fontSize = 16;

            //Console.WriteLine("StudioCharaEditor CharaEditorUI started.");
        }

        private void OnGUI()
        {
            if (VisibleGUI)
            {
                try
                {
                    GUIStyle guistyle = new GUIStyle(GUI.skin.window);
                    windowRect = GUI.Window(windowID, windowRect, new GUI.WindowFunction(FuncWindowGUI), windowTitle, guistyle);

                    mouseInWindow = windowRect.Contains(Event.current.mousePosition);
                    if (mouseInWindow)
                    {
                        Studio.Studio.Instance.cameraCtrl.noCtrlCondition = (() => mouseInWindow && VisibleGUI);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private void Update()
        {
            // hotkey check
            if (StudioCharaEditor.KeyShowUI.Value.IsDown())
            {
                VisibleGUI = !VisibleGUI;
                if (VisibleGUI)
                {
                    CharaEditorMgr.Instance.ReloadDictionary();
                    windowRect = new Rect(StudioCharaEditor.UIXPosition.Value, StudioCharaEditor.UIYPosition.Value, Math.Max(600, StudioCharaEditor.UIWidth.Value), Math.Max(400, StudioCharaEditor.UIHeight.Value));
                }
                else
                {
                    StudioCharaEditor.UIXPosition.Value = (int)windowRect.x;
                    StudioCharaEditor.UIYPosition.Value = (int)windowRect.y;
                    StudioCharaEditor.UIWidth.Value = (int)windowRect.width;
                    StudioCharaEditor.UIHeight.Value = (int)windowRect.height;
                }
            }

            // change select check
            if (VisibleGUI)
            {
                TreeNodeObject curSel = GetCurrentSelectedNode();
                if (curSel != lastSelectedTreeNode)
                {
                    OnSelectChange(curSel);
                }
            }

            // house keeping
            CharaEditorMgr.Instance.HouseKeeping(VisibleGUI);

            // check todo queue
            if (ToDoQueue.Count > 0)
            {
                Action p = ToDoQueue.Dequeue();
                p();
            }
        }

        private void FuncWindowGUI(int winID)
        {
            try
            {
                if (GUIUtility.hotControl == 0)
                {

                }
                if (Event.current.type == EventType.MouseDown)
                {
                    GUI.FocusControl("");
                    GUI.FocusWindow(winID);

                }
                GUI.enabled = true;

                switch (guiMode)
                {
                    case GuiModeType.MAIN:
                        guiEditorMain();
                        break;
                    case GuiModeType.SAVE:
                        guiSave();
                        break;
                    default:
                        throw new Exception("Unknown gui mode");
                }

                GUI.DragWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ResetGui();                
            }
            finally
            {

            }
        }

        private void guiEditorMain()
        {
            float fullw = windowRect.width - 20;
            float fullh = windowRect.height - 20;
            float leftw = 150;
            float rightw = fullw - 8 - leftw - 5;
            GUIStyle cat1btnstyle = new GUIStyle("button");

            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(ociTarget);
            if (ociTarget == null || cec == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#00ffff>" + LC("Please select a charactor to edit.") + "</color>", largeLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            else
            {
                // charactor selected
                string curDetailSetKey = null;

                GUILayout.BeginHorizontal();

                // LEFT area
                GUILayout.BeginVertical(GUILayout.Width(leftw + 8));

                // catelog1 select
                GUILayout.BeginHorizontal();
                for (int c1 = 0; c1 < CharaEditorController.CATEGORY1.Length; c1++)
                {
                    if (c1 == 3)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                    string title = LC(CharaEditorController.CATEGORY1[c1]);
                    Color color = GUI.color;
                    if (catelogIndex1 == c1)
                        GUI.color = Color.green;
                    if (GUILayout.Button(title))
                    {
                        catelogIndex1 = c1;
                        detailPageSelect = SelectMode.Normal;
                    }
                    GUI.color = color;
                }
                GUILayout.EndHorizontal();

                // catelog2 select
                leftScroll = GUILayout.BeginScrollView(leftScroll, GUI.skin.box);
                string category1 = CharaEditorController.CATEGORY1[catelogIndex1];
                string category2 = null;
                string[] category2List = cec.GetCategoryList(category1);
                for (int c2 = 0; c2 < category2List.Length; c2++)
                {
                    string title = category2List[c2];
                    if (title.StartsWith("=="))
                    {
                        // seperator
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(LC(title));
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    else if (title.StartsWith("++"))
                    {
                        // toggle
                        string cTitle = title.Substring(2);
                        string checkKey = category1 + "#" + cTitle;
                        if (cec.Category2GetFuncDict.ContainsKey(checkKey))
                        {
                            bool oldV = (bool)cec.Category2GetFuncDict[checkKey](cec);
                            bool newV = GUILayout.Toggle(oldV, LC(title));
                            if (oldV != newV)
                            {
                                cec.Category2SetFuncDict[checkKey](cec, newV);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unknown ++{0}", checkKey);
                        }
                    }
                    else
                    {
                        // selectable button
                        string detailSetKey = category1 + "#" + title;
                        Color color = GUI.color;
                        if (catelogIndex2[catelogIndex1] == c2)
                        {
                            GUI.color = Color.green;
                            category2 = title;
                            curDetailSetKey = detailSetKey;
                        }
                        if (catelogIndex1 == 3)
                        {
                            title = cec.GetClothDispName(title);
                            cat1btnstyle.alignment = TextAnchor.MiddleCenter;                            
                        }
                        else if (catelogIndex1 == 4)
                        {
                            title = cec.GetAccessoryInfoByKey(title)?.AccName;
                            cat1btnstyle.alignment = TextAnchor.MiddleLeft;
                        }
                        else
                        {
                            cat1btnstyle.alignment = TextAnchor.MiddleCenter;
                        }
                        if (GUILayout.Button(LC(title), cat1btnstyle))
                        {
                            catelogIndex2[catelogIndex1] = c2;
                            detailPageSelect = SelectMode.Normal;
                        }
                        GUI.color = color;
                    }
                }
                GUILayout.EndScrollView();

                // category operation button
                GUILayout.BeginVertical(GUI.skin.box);
                if (catelogIndex1 == 4 && PluginMoreAccessories.HasMoreAccessories)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(LC("+1 Slot")))
                    {
                        PluginMoreAccessories.AddOneAccessorySlot(cec.ociTarget.charInfo);
                        cec.RefreshAccessoriesList();
                    }
                    if (GUILayout.Button(LC("+10 Slots")))
                    {
                        PluginMoreAccessories.AddTenAccessorySlots(cec.ociTarget.charInfo);
                        cec.RefreshAccessoriesList();
                    }
                    GUILayout.EndHorizontal();
                }
                if (GUILayout.Button(LC("Copy") + " " + LC(category1)))
                {
                    List<string> tgtKeys = new List<string>();
                    for (int c2 = 0; c2 < category2List.Length; c2++)
                    {
                        string c2name = category2List[c2];
                        if (c2name.StartsWith("==") || c2name.StartsWith("++")) continue;
                        foreach (CharaDetailInfo cdi in cec.GetDetaiInfoList(category1, c2name))
                        {
                            tgtKeys.Add(cdi.DetailDefine.Key);
                        }
                    }
                    clipboard = cec.GetDataDictByKeys(tgtKeys.ToArray());
                }
                GUILayout.EndVertical();
                GUILayout.EndVertical();

                // RIGHT area
                if (curDetailSetKey != null)
                {
                    GUILayout.BeginVertical(GUILayout.Width(rightw));

                    // chara name editor line
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (renameMode)
                        tempCharaName = GUILayout.TextField(tempCharaName, GUILayout.Width(200));
                    else
                        GUILayout.Label(magentaText(ociTarget.treeNodeObject.textName) + greenText(" > " + LC(category1) + " > " + LC(category2)));
                    GUILayout.FlexibleSpace();
                    if (renameMode)
                    {
                        if (GUILayout.Button(LC("OK")))
                        {
                            ociTarget.treeNodeObject.textName = tempCharaName;
                            ociTarget.charInfo.chaFile.parameter.fullname = tempCharaName;
                            renameMode = false;
                        }
                        if (GUILayout.Button(LC("Cancel")))
                        {
                            renameMode = false;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(LC("Rename Chara")))
                        {
                            tempCharaName = ociTarget.treeNodeObject.textName;
                            renameMode = true;
                        }
                    }
                    GUILayout.EndHorizontal();

                    // chara detials editor
                    if (cec.myDetailSet.ContainsKey(curDetailSetKey))
                    {
                        CharaDetailInfo[] detailInfoSet = cec.GetDetaiInfoList(category1, category2);
                        Dictionary<string, object> pageClipboard = new Dictionary<string, object>();
                        // detail page scroll view
                        rightScroll = GUILayout.BeginScrollView(rightScroll, GUI.skin.box);
                        foreach (CharaDetailInfo dInfo in detailInfoSet)
                        {
                            string dkey = dInfo.DetailDefine.Key;
                            string dname = dkey.Split(CharaEditorController.KEY_SEP_CHAR)[2];
                            if (detailPageSelect == SelectMode.Normal)
                            {
                                // Setting mode
                                ChaControl chaCtrl = ociTarget.charInfo;
                                switch (dInfo.DetailDefine.Type)
                                {
                                    case CharaDetailDefine.CharaDetailDefineType.SLIDER:
                                        guiRenderSlider(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.COLOR:
                                        guiRenderColor(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.SELECTOR:
                                        guiRenderSelector(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.SEPERATOR:
                                        guiRenderSeperator(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.TOGGLE:
                                        guiRenderToggle(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.VALUEINPUT:
                                        guiRenderValueInput(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.INT_STATUS:
                                        guiRenderIntStatus(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.HAIR_BUNDLE:
                                        guiRenderHairBundle(chaCtrl, curDetailSetKey, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.BUTTON:
                                        guiRenderButton(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.ABMXSET1:
                                    case CharaDetailDefine.CharaDetailDefineType.ABMXSET2:
                                    case CharaDetailDefine.CharaDetailDefineType.ABMXSET3:
                                        guiRenderABMXSet(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.SKIN_OVERLAY:
                                        guiRenderSkinOverlay(chaCtrl, dname, dInfo);
                                        break;
                                    case CharaDetailDefine.CharaDetailDefineType.CLOTH_OVERLAY:
                                        guiRenderClothOverlay(chaCtrl, dname, dInfo);
                                        break;
                                    default:
                                        GUILayout.Label(dname + ": UNKNOWN type not implemented");
                                        break;
                                }
                            }
                            else
                            {
                                // selecting mode
                                if (dInfo.DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SEPERATOR)
                                {
                                    continue;
                                }
                                if (selectBuffer.ContainsKey(dkey))
                                {
                                    selectBuffer[dkey] = GUILayout.Toggle(selectBuffer[dkey], LC(dname));
                                }
                                else
                                {
                                    GUILayout.Label(greyText("    " + LC(dname)));
                                }
                            }

                            if (clipboard != null && clipboard.ContainsKey(dkey))
                            {
                                pageClipboard[dkey] = clipboard[dkey];
                            }
                        }
                        GUILayout.EndScrollView();

                        // detail page copy/paste
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        if (detailPageSelect == SelectMode.Normal)
                        {
                            if (GUILayout.Button(LC("Copy Page")))
                            {
                                List<string> tgtKeys = new List<string>();
                                foreach (CharaDetailInfo dInfo in detailInfoSet)
                                {
                                    tgtKeys.Add(dInfo.DetailDefine.Key);
                                }
                                clipboard = cec.GetDataDictByKeys(tgtKeys.ToArray());
                            }
                            if (GUILayout.Button(LC("Copy Select")))
                            {
                                detailPageSelect = SelectMode.ForCopy;
                                selectBuffer.Clear();
                                foreach (CharaDetailInfo dInfo in detailInfoSet)
                                {
                                    selectBuffer[dInfo.DetailDefine.Key] = true;
                                }
                            }
                            if (pageClipboard.Count > 0 && GUILayout.Button(LC("Paste Page")))
                            {
                                cec.SetDataDict(pageClipboard);
                            }
                            if (pageClipboard.Count > 0 && GUILayout.Button(LC("Paste Select")))
                            {
                                detailPageSelect = SelectMode.ForPaste;
                                selectBuffer.Clear();
                                foreach (string dkey in pageClipboard.Keys)
                                {
                                    selectBuffer[dkey] = true;
                                }
                            }
                        }
                        else if (detailPageSelect == SelectMode.ForCopy)
                        {
                            if (GUILayout.Button(LC("Copy Selected Data")))
                            {
                                List<string> tgtKeys = new List<string>();
                                foreach (string dkey in selectBuffer.Keys)
                                {
                                    if (selectBuffer[dkey])
                                    {
                                        tgtKeys.Add(dkey);
                                    }
                                }
                                clipboard = cec.GetDataDictByKeys(tgtKeys.ToArray());
                                detailPageSelect = SelectMode.Normal;
                            }
                            if (GUILayout.Button(LC("Cancel")))
                            {
                                detailPageSelect = SelectMode.Normal;
                            }
                        }
                        else if (detailPageSelect == SelectMode.ForPaste)
                        {
                            if (GUILayout.Button(LC("Paste To Selected Data")))
                            {
                                Dictionary<string, object> pageSelClipboard = new Dictionary<string, object>();
                                foreach (string dkey in selectBuffer.Keys)
                                {
                                    if (selectBuffer[dkey])
                                    {
                                        pageSelClipboard[dkey] = pageClipboard[dkey];
                                    }
                                }
                                cec.SetDataDict(pageSelClipboard);
                                detailPageSelect = SelectMode.Normal;
                            }
                            if (GUILayout.Button(LC("Cancel")))
                            {
                                detailPageSelect = SelectMode.Normal;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.Label("Detail of " + greenText(curDetailSetKey) + " is not defined");
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                // control buttons
                float cbwidth = (fullw - 4 * 3) / 4;
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(LC("Copy All"), btnstyle, GUILayout.Width(cbwidth)))
                {
                    clipboard = cec.GetDataDictFull();
                }
                bool canPaste = clipboard != null;
                if (GUILayout.Button(canPaste ? LC("Paste All") : " ", btnstyle, GUILayout.Width(cbwidth)) && canPaste)
                {
                    cec.SetDataDict(clipboard);
                }
                if (GUILayout.Button(LC("Revert All"), btnstyle, GUILayout.Width(cbwidth)))
                {
                    cec.RevertAll();
                }
                if (GUILayout.Button(LC("Save"), btnstyle, GUILayout.Width(cbwidth)))
                {
                    savingChara = ociTarget;
                    ChaFile savingChaFile = savingChara.charInfo.chaFile;
                    savingPath = CharaEditorMgr.Instance.GetExportPath(savingChaFile.parameter.sex);
                    savingFilename = string.Format("CharaEditor_{0:yyyy-MM-dd-HH-mm-ss}_{1}_{2}.png", DateTime.Now, savingChaFile.parameter.sex == 0 ? "male" : "female", savingChaFile.parameter.fullname);
                    if (savingChaFile.pngData != null)
                    {
                        savingTexture = new Texture2D(2, 2);
                        ImageConversion.LoadImage(savingTexture, savingChaFile.pngData);
                    }
                    else
                    {
                        savingTexture = null;
                    }
                    guiMode = GuiModeType.SAVE;
                }
                GUILayout.EndHorizontal();
            }

            // close btn
            Rect cbRect = new Rect(windowRect.width - 16, 3, 13, 13);
            Color oldColor = GUI.color;
            GUI.color = Color.red;
            if (GUI.Button(cbRect, ""))
            {
                VisibleGUI = false;
            }
            GUI.color = oldColor;
        }

        private void guiRenderSlider(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            float oldV = (float)dInfo.DetailDefine.Get(chaCtrl);
            float newV = oldV;
            bool preciseMode = StudioCharaEditor.PreciseInputMode.Value;
            bool unlimitMode = StudioCharaEditor.UnlimitedSlider.Value;

            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUILayout.Width(namew));
            string txtV;
            int inputw;
            if (preciseMode)
            {
                txtV = string.Format("{0:F3}", oldV * 100.0);
                inputw = 60;
            }
            else
            {
                txtV = string.Format("{0:F0}", oldV * 100.0);
                inputw = 35;
            }
            string newTxtV = GUILayout.TextField(txtV, GUILayout.Width(inputw));
            if (!newTxtV.Equals(txtV))
            {
                if (float.TryParse(newTxtV, out float outV))
                {
                    newV = outV / 100.0f;
                }
            }
            if (preciseMode)
            {
                if (GUILayout.Button("-0.1", GUILayout.Width(37)))
                    newV -= 0.001f;
                if (GUILayout.Button("-0.01", GUILayout.Width(43)))
                    newV -= 0.0001f;
            }
            else
            {
                if (GUILayout.Button("-10", GUILayout.Width(35)))
                    newV -= 0.1f;
                if (GUILayout.Button("-1", GUILayout.Width(30)))
                    newV -= 0.01f;
            }
            float sldMax = 2;
            float sldMin = -1;
            if (unlimitMode)
            {
                sldMax = Math.Max(2, newV);
                sldMin = Math.Min(-1, newV);
            }
            float sldV = GUILayout.HorizontalSlider(newV, sldMin, sldMax);
            if (sldV != newV)
                newV = sldV;
            if (preciseMode)
            {
                if (GUILayout.Button("+0.01", GUILayout.Width(43)))
                    newV += 0.0001f;
                if (GUILayout.Button("+0.1", GUILayout.Width(37)))
                    newV += 0.001f;
            }
            else
            {
                if (GUILayout.Button("+1", GUILayout.Width(30)))
                    newV += 0.01f;
                if (GUILayout.Button("+10", GUILayout.Width(35)))
                    newV += 0.1f;
            }
            if (dInfo.RevertValue != null && GUILayout.Button("R", GUILayout.Width(25)))
                newV = (float)dInfo.RevertValue;
            if (!unlimitMode)
            {
                if (newV > 2)
                    newV = 2f;
                if (newV < -1)
                    newV = -1f;
            }
            if (newV != oldV)
            {
                dInfo.DetailDefine.Set(chaCtrl, newV);
                if (dInfo.DetailDefine.Upd != null && !LaterUpdate) dInfo.DetailDefine.Upd(chaCtrl);
            }
            GUILayout.EndHorizontal();
        }

        private void guiRenderColor(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            Color oldC = (Color)dInfo.DetailDefine.Get(chaCtrl);

            string formatColor(Color color)
            {
                return string.Format("R:{0:F0} G:{1:F0} B:{2:F0} A:{3:F0}", color.r * 255, color.g * 255, color.b * 255, color.a * 100);
            }
            Color[] getColors(int size, Color fillColor)
            {
                List<Color> cList = new List<Color>();
                for (int i = 0; i < size; i++)
                {
                    cList.Add(fillColor);
                }
                return cList.ToArray();
            }
            void onChangeColor(Color color)
            {
                if (color != oldC)
                {
                    dInfo.DetailDefine.Set(chaCtrl, color);
                    if (dInfo.DetailDefine.Upd != null && !LaterUpdate) dInfo.DetailDefine.Upd(chaCtrl);
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUILayout.Width(namew));
            int colorw = 74;
            Texture2D colorTex = new Texture2D(colorw, 20);
            colorTex.SetPixels(getColors(colorw * 20, oldC));
            colorTex.Apply();
            if (GUILayout.Button(colorTex, GUILayout.Height(20), GUILayout.Width(colorw)))
            {
                Studio.Studio studio = Studio.Studio.Instance;
                studio.colorPalette.Setup(LC(name), oldC, new Action<Color>(onChangeColor), true);
                studio.colorPalette.visible = true;
            }
            GUILayout.Space(4);
            GUILayout.Label(formatColor(oldC));
            GUILayout.FlexibleSpace();
            if (dInfo.RevertValue != null && GUILayout.Button("R", GUILayout.Width(25)))
                onChangeColor((Color)dInfo.RevertValue);
            GUILayout.EndHorizontal();
        }

        private void guiRenderSelector(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            float fullw = windowRect.width - 20;
            float fullh = windowRect.height - 20;
            float leftw = 150;
            float rightw = fullw - 8 - leftw - 5;
            float thumbBtnWidth = rightw - namew - thumbSize - 60;
            float thumbVSpace = (thumbSize - thumbBtnHeight) / 2;
            float thumbListMinH = thumbSize * 2 + 20;// 130;
            float thumbListMaxH = fullh * 0.7f;// 350;
            int thumbShowBefore = 1;
            int thumbShowAfter = (int)(thumbListMaxH / thumbSize) + 1;
            bool thumbList = name != "Acc Parent" && name != "Acc Category";
            bool showSmallThumbMode = StudioCharaEditor.ShowSelectedThumb.Value;
            bool unexpandOnSelect = StudioCharaEditor.CloseListAfterSelect.Value;
            bool inSearching = false;

            // Get list and current info
            int oldId = (int)dInfo.DetailDefine.Get(chaCtrl);
            string oldName = "!!Unknown!!";
            int oldIndex = -1;
            List<CustomSelectInfo> infoLst = dInfo.DetailDefine.SelectorList(chaCtrl);
            for (int i = 0; i < infoLst.Count; i++)
            {
                if (infoLst[i].id == oldId)
                {
                    oldName = infoLst[i].name;
                    oldIndex = i;
                    break;
                }
            }

            // initialize pool
            if (!scrollPool.ContainsKey(name))
            {
                scrollPool[name] = Vector2.zero;
                expandPool[name] = false;
                thumbPool[name] = new Dictionary<string, Texture2D>();
                searchWordPool[name] = string.Empty;
            }

            void onChangeId(int id)
            {
                if (unexpandOnSelect)
                {
                    expandPool[name] = false;   // no matter changed or not
                }
                if (id != oldId)
                {
                    dInfo.DetailDefine.Set(chaCtrl, id);
                    if (dInfo.DetailDefine.Upd != null && !LaterUpdate) dInfo.DetailDefine.Upd(chaCtrl);
                }
            }

            Texture2D getThumbTex(CustomSelectInfo info)
            {
                if (info.assetBundle != null && info.assetName != null)
                {
                    string texKey = info.assetBundle + "+" + info.assetName;
                    if (!thumbPool[name].ContainsKey(texKey))
                    {
                        thumbPool[name][texKey] = CommonLib.LoadAsset<Texture2D>(info.assetBundle, info.assetName, false, ""); ;
                    }
                    return thumbPool[name][texKey];
                }
                else
                {
                    return Texture2D.blackTexture;
                }
            }

            // title line
            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUILayout.Width(namew));
            if (thumbList && showSmallThumbMode && !expandPool[name])
            {
                Texture2D tex = oldIndex >= 0 ? getThumbTex(infoLst[oldIndex]) : Texture2D.blackTexture;
                GUILayout.Box(tex, GUILayout.Width(thumbSizeSmall), GUILayout.Height(thumbSizeSmall));
                GUILayout.Label(string.Format("#{0}\n{1}", oldId, oldName));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    expandPool[name] = true;
                    scrollPool[name] = new Vector2(0, oldIndex * (thumbSize + 4) + 4);
                }
                if (dInfo.RevertValue != null && GUILayout.Button("R", GUILayout.Width(25)))
                    onChangeId((int)dInfo.RevertValue);
            }
            else
            {
                GUILayout.Label(string.Format("#{0}: {1}", oldId, oldName));
                GUILayout.FlexibleSpace();
                if (expandPool[name])
                {
                    // search button
                    var oldColor = GUI.color;
                    if (searchingMode)
                        GUI.color = Color.yellow;
                    if (GUILayout.Button(LC("Search")))
                        searchingMode = !searchingMode;
                    GUI.color = oldColor;
                    // - button
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        expandPool[name] = false;
                    }
                }
                else
                {
                    // + button
                    if (GUILayout.Button("+", GUILayout.Width(25)))
                    {
                        expandPool[name] = true;
                        if (thumbList)
                            scrollPool[name] = new Vector2(0, oldIndex * (thumbSize + 3) + 4);
                        else
                            scrollPool[name] = new Vector2(0, oldIndex * (20 + 4) + 4);
                    }
                }
                // R button
                if (dInfo.RevertValue != null && GUILayout.Button("R", GUILayout.Width(25)))
                    onChangeId((int)dInfo.RevertValue);
            }
            GUILayout.EndHorizontal();

            // expandable list
            if (expandPool[name])
            {
                // search box
                if (searchingMode && thumbList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" ", GUILayout.Width(namew));
                    GUILayout.Label(LC("Search"), GUILayout.Width(namew));
                    searchWordPool[name] = GUILayout.TextField(searchWordPool[name]);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        searchWordPool[name] = string.Empty;
                    }
                    GUILayout.EndHorizontal();
                    inSearching = !string.IsNullOrWhiteSpace(searchWordPool[name]);
                }

                // draw drop list
                GUILayout.BeginHorizontal();
                GUILayout.Label(" ", GUILayout.Width(namew));
                scrollPool[name] = GUILayout.BeginScrollView(scrollPool[name], GUI.skin.box, GUILayout.MinHeight(thumbListMinH), GUILayout.MaxHeight(thumbListMaxH));
                int fi = 0;
                for (int i = 0; i < infoLst.Count; i++)
                {
                    CustomSelectInfo info = infoLst[i];
                    Color color = GUI.color;
                    if (thumbList)
                    {
                        // search filter
                        if (inSearching && !info.name.ToLower().Contains(searchWordPool[name].ToLower()))
                        {
                            continue;
                        }
                        // load thumb tex
                        int curDispIndex = (int)(scrollPool[name].y / (thumbSize + 4));
                        bool needThumb = fi >= curDispIndex - thumbShowBefore && fi <= curDispIndex + thumbShowAfter;
                        fi++;
                        Texture2D tex = needThumb ? getThumbTex(info) : Texture2D.blackTexture;
                        // show thumb and button
                        GUILayout.BeginHorizontal();
                        GUILayout.Box(tex, GUILayout.Width(thumbSize), GUILayout.Height(thumbSize));
                        GUILayout.BeginVertical();
                        GUILayout.Space(thumbVSpace);
                        if (info.id == oldId)
                            GUI.color = Color.green;
                        if (GUILayout.Button(string.Format("#{0}:\n{1}", info.id, info.name), GUILayout.Width(thumbBtnWidth), GUILayout.Height(thumbBtnHeight)))
                            onChangeId(info.id);
                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        // button only
                        if (info.id == oldId)
                            GUI.color = Color.green;
                        if (GUILayout.Button(string.Format("#{0}: {1}", info.id, info.name)))
                            onChangeId(info.id);
                    }
                    GUI.color = color;
                }
                GUILayout.EndScrollView();
                GUILayout.EndHorizontal();
            }
        }

        private void guiRenderSeperator(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            GUILayout.BeginHorizontal();
            if (dInfo.DetailDefine.Get != null)
                GUILayout.Label(LC((string)dInfo.DetailDefine.Get(chaCtrl)), GUI.skin.box);
            else
                GUILayout.Label(LC(name), GUI.skin.box);
            GUILayout.EndHorizontal();
        }

        private void guiRenderToggle(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            bool newV, oldV;
            object curV = dInfo.DetailDefine.Get(chaCtrl);
            if (curV.GetType() == typeof(bool))
            {
                oldV = (bool)curV;
            }
            else
            {
                oldV = (float)curV == 1;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.Width(namew));
            newV = GUILayout.Toggle(oldV, LC(name));
            GUILayout.FlexibleSpace();
            if (dInfo.RevertValue != null && GUILayout.Button("R", GUILayout.Width(25)))
            {
                if (dInfo.RevertValue.GetType() == typeof(bool))
                {
                    newV = (bool)dInfo.RevertValue;
                }
                else
                {
                    newV = (float)dInfo.RevertValue == 1;
                }
            }
            if (newV != oldV)
            {
                dInfo.DetailDefine.Set(chaCtrl, newV);
                if (dInfo.DetailDefine.Upd != null && !LaterUpdate) dInfo.DetailDefine.Upd(chaCtrl);
            }
            GUILayout.EndHorizontal();
        }

        private void guiRenderValueInput(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            float oldV = (float)dInfo.DetailDefine.Get(chaCtrl);
            float newV = oldV;
            bool preciseMode = StudioCharaEditor.PreciseInputMode.Value;
            CharaValueDetailDefine vDefine = (CharaValueDetailDefine)dInfo.DetailDefine;
            float dim1 = preciseMode ? vDefine.DimStep1 / 10 : vDefine.DimStep1;
            float dim2 = preciseMode ? vDefine.DimStep2 / 10 : vDefine.DimStep2;

            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUILayout.Width(namew));
            // dec buttons
            if (GUILayout.RepeatButton("<<", GUILayout.Width(30)))
                newV -= dim2;
            if (GUILayout.RepeatButton("<", GUILayout.Width(25)))
                newV -= dim1;
            // value input
            string txtV;
            int inputw;
            if (preciseMode)
            {
                txtV = string.Format("{0:F5}", oldV);
                inputw = 70;
            }
            else
            {
                txtV = string.Format("{0:F3}", oldV);
                inputw = 60;
            }
            string newTxtV = GUILayout.TextField(txtV, GUILayout.Width(inputw));
            if (!newTxtV.Equals(txtV))
            {
                if (float.TryParse(newTxtV, out float outV))
                {
                    newV = outV;
                }
            }
            // inc buttons
            if (GUILayout.RepeatButton(">", GUILayout.Width(25)))
                newV += dim1;
            if (GUILayout.RepeatButton(">>", GUILayout.Width(30)))
                newV += dim2;
            // def button
            if (!float.IsNaN(vDefine.DefValue) && GUILayout.Button(vDefine.DefValue.ToString()))
                newV = vDefine.DefValue;
            // revert
            GUILayout.FlexibleSpace();
            if (dInfo.RevertValue != null && GUILayout.Button("R", GUILayout.Width(25)))
                newV = (float)dInfo.RevertValue;
            GUILayout.EndHorizontal();

            if (newV != oldV)
            {
                if (vDefine.LoopValue && !float.IsNaN(vDefine.MinValue) && !float.IsNaN(vDefine.MaxValue))
                {
                    while (newV < vDefine.MinValue)
                        newV = vDefine.MaxValue - (vDefine.MinValue - newV);
                    while (newV > vDefine.MaxValue)
                        newV = vDefine.MinValue + (newV - vDefine.MaxValue);
                }
                else
                {
                    if (!float.IsNaN(vDefine.MinValue) && newV < vDefine.MinValue)
                        newV = vDefine.MinValue;
                    if (!float.IsNaN(vDefine.MaxValue) && newV > vDefine.MaxValue)
                        newV = vDefine.MaxValue;
                }
                dInfo.DetailDefine.Set(chaCtrl, newV);
                if (dInfo.DetailDefine.Upd != null && !LaterUpdate) dInfo.DetailDefine.Upd(chaCtrl);
            }
        }

        private void guiRenderIntStatus(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            int oldV = Convert.ToInt32(dInfo.DetailDefine.Get(chaCtrl));
            int newV = oldV;
            int btnWidth = 50;
            CharaIntStatusDetailDefine vDefine = (CharaIntStatusDetailDefine)dInfo.DetailDefine;

            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUILayout.Width(namew));
            // int selector
            int num = vDefine.IntStatus.Length;
            if (true)
            {
                // select buttons
                for (int i = 0; i < num; i++)
                {
                    Color oldColor = GUI.color;
                    if (oldV == vDefine.IntStatus[i])
                        GUI.color = Color.green;
                    if (GUILayout.Button(LC(vDefine.IntStatusName[i]), GUILayout.Width(btnWidth)))
                        newV = vDefine.IntStatus[i];
                    GUI.color = oldColor;
                }
            }
            // revert
            GUILayout.FlexibleSpace();
            if (dInfo.RevertValue != null && GUILayout.Button("R", GUILayout.Width(25)))
                newV = Convert.ToInt32(dInfo.RevertValue);
            GUILayout.EndHorizontal();

            // update
            if (newV != oldV)
            {
                dInfo.DetailDefine.Set(chaCtrl, newV);
                if (dInfo.DetailDefine.Upd != null && !LaterUpdate) dInfo.DetailDefine.Upd(chaCtrl);
            }
        }

        private void guiRenderButton(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.Width(namew));
            // a button
            if (GUILayout.Button(LC(name)) && dInfo.DetailDefine.Upd != null)
                dInfo.DetailDefine.Upd(chaCtrl);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void guiRenderHairBundle(ChaControl chaCtrl, string setKey, CharaDetailInfo dInfo)
        {
            // get hair PartsNo
            //Console.WriteLine("\nStart render hair bundle, setKey = {0}", setKey);
            List<string> setKeyToPartsNo = new List<string>() { "Hair#BackHair", "Hair#FrontHair", "Hair#SideHair", "Hair#ExtensionHair" };
            HairBundleDetailSet.PartsNo = setKeyToPartsNo.IndexOf(setKey);
            if (HairBundleDetailSet.PartsNo == -1)
            {
                return;
            }
            //Console.WriteLine("Parts no = {0}, dInfo Key = {1}", HairBundleDetailSet.PartsNo, dInfo.DetailDefine.Key);

            // current
            Dictionary<int, float[]> bundleSetDict = (Dictionary<int, float[]>)dInfo.DetailDefine.Get(chaCtrl);
            if (bundleSetDict == null)
            {
                return;
            }

            // revert
            Dictionary<int, float[]> revBundleSetDict = (Dictionary<int, float[]>)dInfo.RevertValue;
            //Console.WriteLine("Parts revBundleSetDict = {0}", revBundleSetDict);

            foreach (int i in bundleSetDict.Keys)
            {
                HairBundleDetailSet.BundleKey = i;
                string bundlename = string.Format("Bundle {0} Adjust", i);
                float[] revValues = null;
                if (revBundleSetDict != null && revBundleSetDict.ContainsKey(i))
                {
                    revValues = revBundleSetDict[i];
                }
                //Console.WriteLine("bundle key = {0} revValues = {1}", i, revValues);

                // render bundle detail
                foreach (CharaHairBundleDetailDefine cDef in HairBundleDetailSet.Details)
                {
                    CharaDetailInfo cInfo = new CharaDetailInfo(chaCtrl, cDef);
                    //Console.WriteLine("rendering {0}", cDef.Key);
                    switch (cDef.Type)
                    {
                        case CharaDetailDefine.CharaDetailDefineType.SEPERATOR:
                            guiRenderSeperator(chaCtrl, bundlename, cInfo);
                            break;
                        case CharaDetailDefine.CharaDetailDefineType.TOGGLE:
                            cInfo.RevertValue = revValues != null ? cDef.GetRevertValue(revValues) : null;
                            guiRenderToggle(chaCtrl, cDef.Key, cInfo);
                            break;
                        case CharaDetailDefine.CharaDetailDefineType.SLIDER:
                            cInfo.RevertValue = revValues != null ? cDef.GetRevertValue(revValues) : null;
                            guiRenderSlider(chaCtrl, cDef.Key, cInfo);
                            break;
                        default:
                            GUILayout.Label(bundlename + cDef.Key + ": UNKNOWN type not implemented");
                            break;
                    }
                }
            }
        }

        private void guiRenderABMXSet(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            object dataset = dInfo.DetailDefine.Get(chaCtrl);
            float[] workSet;
            float[] workRevert;
            // header
            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUI.skin.box);
            GUILayout.EndHorizontal();
            // target selector
            if (dInfo.DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.ABMXSET1)
            {
                workSet = (float[])dataset;
                workRevert = (float[])dInfo.RevertValue;
            }
            else if (dInfo.DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.ABMXSET2)
            {
                CharaABMXDetailDefine2 dd2 = (dInfo.DetailDefine as CharaABMXDetailDefine2);
                GUILayout.BeginHorizontal();
                GUILayout.Label(LC("Side to edit"), GUILayout.Width(namew));
                for (int i = 0; i < dd2.targetNames.Length; i++)
                {
                    Color bkc = GUI.color;
                    if (i == dd2.curTargetIndex)
                    {
                        GUI.color = Color.green;
                    }
                    if (GUILayout.Button(LC(dd2.targetNames[i])))
                    {
                        dd2.curTargetIndex = i;
                    }
                    GUI.color = bkc;
                }
                GUILayout.EndHorizontal();

                workSet = ((float[][])dataset)[dd2.curTargetIndex == 0 ? 0 : dd2.curTargetIndex - 1];
                workRevert = ((float[][])dInfo.RevertValue)[dd2.curTargetIndex == 0 ? 0 : dd2.curTargetIndex - 1];
            }
            else if (dInfo.DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.ABMXSET3)
            {
                CharaABMXDetailDefine3 dd3 = (dInfo.DetailDefine as CharaABMXDetailDefine3);
                GUILayout.BeginHorizontal();
                GUILayout.Label(LC("Hand"), GUILayout.Width(namew));
                for (int i = 0; i < dd3.targetNames.Length; i++)
                {
                    Color bkc = GUI.color;
                    if (i == dd3.curTargetIndex)
                    {
                        GUI.color = Color.green;
                    }
                    if (GUILayout.Button(LC(dd3.targetNames[i])))
                    {
                        dd3.curTargetIndex = i;
                    }
                    GUI.color = bkc;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(LC("Finger"), GUILayout.Width(namew));
                for (int i = 0; i < dd3.fingerNames.Length; i++)
                {
                    Color bkc = GUI.color;
                    if (i == dd3.curFingerIndex)
                    {
                        GUI.color = Color.green;
                    }
                    if (GUILayout.Button(LC(dd3.fingerNames[i])))
                    {
                        dd3.curFingerIndex = i;
                    }
                    GUI.color = bkc;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(LC("Segment"), GUILayout.Width(namew));
                for (int i = 0; i < dd3.segmentNames.Length; i++)
                {
                    Color bkc = GUI.color;
                    if (i == dd3.curSegmentIndex)
                    {
                        GUI.color = Color.green;
                    }
                    if (GUILayout.Button(LC(dd3.segmentNames[i])))
                    {
                        dd3.curSegmentIndex = i;
                    }
                    GUI.color = bkc;
                }
                GUILayout.EndHorizontal();

                workSet = ((float[][][][])dataset)[dd3.curTargetIndex == 0 ? 0 : dd3.curTargetIndex - 1][dd3.curFingerIndex == 0 ? 0 : dd3.curFingerIndex - 1][dd3.curSegmentIndex];
                workRevert = ((float[][][][])dInfo.RevertValue)[dd3.curTargetIndex == 0 ? 0 : dd3.curTargetIndex - 1][dd3.curFingerIndex == 0 ? 0 : dd3.curFingerIndex - 1][dd3.curSegmentIndex];
            }
            else
            {
                throw new ArgumentException("Unexpected DetailDefine.Type for ABMX bone: " + name);
            }
            // sliders
            for (int i = 0; i < workSet.Length; i++)
            {
                float sldMax = 2;
                float sldMin = 0;
                float oldV = workSet[i];
                float newV = oldV;
                bool preciseMode = StudioCharaEditor.PreciseInputMode.Value;
                bool unlimitMode = StudioCharaEditor.UnlimitedSlider.Value;

                string slideName = (dInfo.DetailDefine as CharaABMXDetailDefine1).SubSlidersNames[i];

                GUILayout.BeginHorizontal();
                GUILayout.Label(LC(slideName), GUILayout.Width(namew));
                string txtV;
                int inputw;
                if (preciseMode)
                {
                    txtV = string.Format("{0:F3}", oldV * 100.0);
                    inputw = 60;
                }
                else
                {
                    txtV = string.Format("{0:F0}", oldV * 100.0);
                    inputw = 35;
                }
                string newTxtV = GUILayout.TextField(txtV, GUILayout.Width(inputw));
                if (!newTxtV.Equals(txtV))
                {
                    if (float.TryParse(newTxtV, out float outV))
                    {
                        newV = outV / 100.0f;
                    }
                }
                if (preciseMode)
                {
                    if (GUILayout.Button("-0.1", GUILayout.Width(37)))
                        newV -= 0.001f;
                    if (GUILayout.Button("-0.01", GUILayout.Width(43)))
                        newV -= 0.0001f;
                }
                else
                {
                    if (GUILayout.Button("-10", GUILayout.Width(35)))
                        newV -= 0.1f;
                    if (GUILayout.Button("-1", GUILayout.Width(30)))
                        newV -= 0.01f;
                }
                if (unlimitMode)
                {
                    sldMax = Math.Max(2, newV);
                    sldMin = Math.Min(-1, newV);
                }
                float sldV = GUILayout.HorizontalSlider(newV, sldMin, sldMax);
                if (sldV != newV)
                    newV = sldV;
                if (preciseMode)
                {
                    if (GUILayout.Button("+0.01", GUILayout.Width(43)))
                        newV += 0.0001f;
                    if (GUILayout.Button("+0.1", GUILayout.Width(37)))
                        newV += 0.001f;
                }
                else
                {
                    if (GUILayout.Button("+1", GUILayout.Width(30)))
                        newV += 0.01f;
                    if (GUILayout.Button("+10", GUILayout.Width(35)))
                        newV += 0.1f;
                }
                if (GUILayout.Button("R", GUILayout.Width(25)))
                    newV = workRevert[i];
                if (!unlimitMode)
                {
                    if (newV > sldMax)
                        newV = sldMax;
                    if (newV < sldMin)
                        newV = sldMin;
                }
                if (newV != oldV)
                {
                    workSet[i] = newV;
                    if (dInfo.DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.ABMXSET2 && (dInfo.DetailDefine as CharaABMXDetailDefine2).curTargetIndex == 0)
                    {
                        ((float[][])dataset)[1][i] = newV;
                    }
                    if (dInfo.DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.ABMXSET3)
                    {
                        CharaABMXDetailDefine3 dd3 = dInfo.DetailDefine as CharaABMXDetailDefine3;
                        if (dd3.curTargetIndex == 0)
                        {
                            ((float[][][][])dataset)[1][dd3.curFingerIndex == 0 ? 0 : dd3.curFingerIndex - 1][dd3.curSegmentIndex][i] = newV;
                        }
                        if (dd3.curFingerIndex == 0)
                        {
                            for (int h = 0; h < 2; h++)
                            {
                                if (dd3.curTargetIndex == 0 || dd3.curTargetIndex - 1 == h)
                                {
                                    for (int f = 1; f < 5; f++)
                                    {
                                        ((float[][][][])dataset)[h][f][dd3.curSegmentIndex][i] = newV;
                                    }
                                }
                            }
                        }
                    }
                    dInfo.DetailDefine.Set(chaCtrl, dataset);
                }
                GUILayout.EndHorizontal();
            }

        }

        private void guiRenderSkinOverlay(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            GUIStyle texTextStyle = new GUIStyle("box");
            texTextStyle.alignment = TextAnchor.MiddleCenter;

            // current part texture
            SkinOverlayDetailDefine overlayDefine = (SkinOverlayDetailDefine)dInfo.DetailDefine;
            Texture2D tex = (Texture2D)overlayDefine.GetSkinOverlayTex(chaCtrl);

            // Overlay block
            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUILayout.Width(namew));
            if (tex != null)
                GUILayout.Box(tex, GUILayout.Width(thumbSize), GUILayout.Height(thumbSize));
            else
                GUILayout.Box(LC("No Texture"), texTextStyle, GUILayout.Width(thumbSize), GUILayout.Height(thumbSize));
            GUILayout.BeginVertical();
            if (GUILayout.Button(LC("Load new texture")))
                overlayDefine.LoadNewOverlayTexture(chaCtrl);
            if (tex != null && GUILayout.Button(LC("Clear texture")))
                overlayDefine.SetSkinOverlayTex(chaCtrl, null);
            if (tex != null && GUILayout.Button(LC("Export current texture")))
                overlayDefine.DumpSkinOverlayTexture(chaCtrl);
            if (!CharaEditorController.DataValueEqual(tex, dInfo.RevertValue) && GUILayout.Button(LC("Revert")))
                overlayDefine.SetSkinOverlayTex(chaCtrl, dInfo.RevertValue);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void guiRenderClothOverlay(ChaControl chaCtrl, string name, CharaDetailInfo dInfo)
        {
            GUIStyle texTextStyle = new GUIStyle("box");
            texTextStyle.alignment = TextAnchor.MiddleCenter;

            // current part texture
            ClothOverlayDetailDefine overlayDefine = (ClothOverlayDetailDefine)dInfo.DetailDefine;
            KoiClothesOverlayX.ClothesTexData texData = (KoiClothesOverlayX.ClothesTexData)overlayDefine.GetClothOverlayTexData(chaCtrl);

            // Overlay block
            GUILayout.BeginHorizontal();
            GUILayout.Label(LC(name), GUILayout.Width(namew));
            if (texData != null && texData.Texture != null)
                GUILayout.Box(texData.Texture, GUILayout.Width(thumbSize), GUILayout.Height(thumbSize));
            else
                GUILayout.Box(LC("No Texture"), texTextStyle, GUILayout.Width(thumbSize), GUILayout.Height(thumbSize));
            GUILayout.BeginVertical();
            if (GUILayout.Button(LC("Load new overlay texture")))
                overlayDefine.LoadNewOverlayTexture(chaCtrl);
            if (texData != null && GUILayout.Button(LC("Clear overlay texture")))
                overlayDefine.SetClothOverlayTex(chaCtrl, null);
            if (texData != null && GUILayout.Button(LC("Export overlay texture")))
                overlayDefine.DumpClothOverlayTexture(chaCtrl);
            if (GUILayout.Button(LC("Dump original texture")))
                overlayDefine.DumpClothOrignalTexture(chaCtrl);
            if (texData != null)
            {
                bool newOverride = GUILayout.Toggle(texData.Override, LC("Hide base textrue"));
                if (newOverride != texData.Override) {
                    texData.Override = newOverride;
                    overlayDefine.RefreshClothesTexture(chaCtrl);
                }
            }
            if (overlayDefine.modified && GUILayout.Button(LC("Revert")))
            {
                overlayDefine.SetClothOverlayTex(chaCtrl, dInfo.RevertValue);
                overlayDefine.modified = false;
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void guiSave()
        {
            float fullw = windowRect.width - 20;
            float fullh = windowRect.height - 20;

            float thumbH = fullh - 40;
            float thumbW = fullw - 350;// thumbH * 252.0f / 352.0f;
            ChaFile savingChaFile = savingChara.charInfo.chaFile;

            // save ui
            GUILayout.BeginHorizontal();
            if (savingTexture != null)
            {
                GUILayout.Box(savingTexture, GUILayout.Width(thumbW), GUILayout.Height(thumbH));
            }
            else
            {
                GUIStyle boxStyle = new GUIStyle("box");
                boxStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Box(redText(LC("No Photo")), boxStyle, GUILayout.Width(thumbW), GUILayout.Height(thumbH));
            }
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(LC("Charactor name:"), largeLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(cyanText(savingChaFile.parameter.fullname));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(LC("Output folder:"), largeLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(cyanText(savingPath));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(LC("PNG file name:"), largeLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(cyanText(savingFilename));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(LC("Change export path/filename"), btnstyle))
            {
                OpenFileDialog.Show((files) => {
                    if (files != null && files.Length > 0)
                    {
                        string pathname = files[0];
                        savingPath = Path.GetDirectoryName(pathname);
                        savingFilename = Path.GetFileName(pathname);
                        if (!Path.GetExtension(pathname).ToLower().Equals(".png"))
                        {
                            savingFilename += ".png";
                        }
                    }
                }, "Save Charactor", savingPath, "Images (*.png)|*.png|All files|*.*", ".png", OpenFileDialog.OpenSaveFileDialgueFlags.OFN_EXPLORER | OpenFileDialog.OpenSaveFileDialgueFlags.OFN_LONGNAMES);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(LC("Capture thumbnail photo"), btnstyle))
            {
                int capW = 1280;
                int capH = 720;
                int savW = 504;
                int savH = 704;
                RenderTextureFormat format = RenderTextureFormat.ARGB32;
                int depthBuffer = 0;
                RenderTexture targetTexture = Camera.main.targetTexture;
                Camera.main.targetTexture = RenderTexture.GetTemporary(capW, capH, depthBuffer, format);
                Camera.main.Render();
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = Camera.main.targetTexture;
                savingTexture = new Texture2D(savW, savH);
                savingTexture.ReadPixels(new Rect((capW - savW) / 2.0f, (capH - savH) / 2.0f, (float)savW, (float)savH), 0, 0, false);
                savingTexture.Apply();
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(Camera.main.targetTexture);
                Camera.main.targetTexture = targetTexture;

                // shink size
                if (!StudioCharaEditor.DoubleThumbnailSize.Value)
                {
                    TextureScale.Bilinear(savingTexture, savW / 2, savH / 2);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(LC("Capture thumbnail photo 2"), btnstyle))
            {
                int capW = 1280;
                int capH = 720;
                int savW = 504;
                int savH = 704;

                byte[] capBuf = Studio.Studio.Instance.gameScreenShot.CreatePngScreen(capW, capH);
                Texture2D capTex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                capTex.LoadImage(capBuf);
                Color[] capPixels = capTex.GetPixels((capW - savW) / 2, (capH - savH) / 2, savW, savH, 0);

                savingTexture = new Texture2D(savW, savH);
                savingTexture.SetPixels(capPixels);
                savingTexture.Apply();

                // shink size
                if (!StudioCharaEditor.DoubleThumbnailSize.Value)
                {
                    TextureScale.Bilinear(savingTexture, savW / 2, savH / 2);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // control buttons
            float cbwidth = (fullw - 2 * 4) / 3;
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(LC("Export PNG"), btnstyle, GUILayout.Width(cbwidth)))
            {
                if (savingTexture != null)
                {
                    savingChaFile.pngData = savingTexture.EncodeToPNG();
                }
                string filename = Path.Combine(savingPath, savingFilename);
                try
                {
                    Traverse.Create(savingChaFile).Method("SaveFile", new object[] { filename, 0 }).GetValue();
                    StudioCharaEditor.Logger.Log(LogLevel.Message | LogLevel.Warning, string.Format("Charactor {0} saved to {1}.", savingChaFile.parameter.fullname, savingFilename));
                    guiMode = GuiModeType.MAIN;
                }
                catch (Exception ex)
                {
                    StudioCharaEditor.Logger.LogError(ex.Message);
                }
            }
            if (GUILayout.Button(LC("Save as revert point"), btnstyle, GUILayout.Width(cbwidth)))
            {
                CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(savingChara);
                if (cec != null)
                {
                    cec.InitFileData();
                    StudioCharaEditor.Logger.Log(LogLevel.Message | LogLevel.Warning, string.Format("Charactor {0}'s revert point updated.", savingChaFile.parameter.fullname));
                    guiMode = GuiModeType.MAIN;
                }
                else
                {
                    StudioCharaEditor.Logger.LogError("Fail to get CharaEditorController!");
                }
            }
            if (GUILayout.Button(LC("Cancel"), btnstyle, GUILayout.Width(cbwidth)))
            {
                guiMode = GuiModeType.MAIN;
            }
            GUILayout.EndHorizontal();

            // close btn
            Rect cbRect = new Rect(windowRect.width - 16, 3, 13, 13);
            Color oldColor = GUI.color;
            GUI.color = Color.red;
            if (GUI.Button(cbRect, ""))
            {
                VisibleGUI = false;
            }
            GUI.color = oldColor;
        }

        private void OnSelectChange(TreeNodeObject newSel)
        {
            lastSelectedTreeNode = newSel;
            ociTarget = GetOCICharFromNode(newSel);
            //Console.WriteLine("Select change to {0}", ociTarget);
        }

        protected TreeNodeObject GetCurrentSelectedNode()
        {
            return Studio.Studio.Instance.treeNodeCtrl.selectNode;
        }

        protected OCIChar GetOCICharFromNode(TreeNodeObject node)
        {
            if (node == null) return null;

            var dic = Studio.Studio.Instance.dicInfo;
            if (dic.ContainsKey(node))
            {
                ObjectCtrlInfo oci = dic[node];
                if (oci is OCIChar)
                {
                    return oci as OCIChar;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private string colorText(string text, string color = "ffffff")
        {
            return "<color=#" + color + ">" + text + "</color>";
        }

        private string redText(string text)
        {
            return colorText(text, "ff0000");
        }

        private string greenText(string text)
        {
            return colorText(text, "00ff00");
        }

        private string magentaText(string text)
        {
            return colorText(text, "ff00ff");
        }

        private string cyanText(string text)
        {
            return colorText(text, "00ffff");
        }

        private string greyText(string text)
        {
            return colorText(text, "808080");
        }

        private string LC(string org)
        {
            if (curLocalizationDict != null && curLocalizationDict.ContainsKey(org) && !string.IsNullOrWhiteSpace(curLocalizationDict[org]))
                return curLocalizationDict[org];
            else
                return org;
        }
    }
}
