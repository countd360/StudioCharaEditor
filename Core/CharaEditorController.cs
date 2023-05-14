using Studio;
using AIChara;
using System;
using System.Collections.Generic;
using UnityEngine;
using PushUpAI;
using HS2_BoobSettings;
using KKABMX.Core;
using KoiSkinOverlayX;
using KoiClothesOverlayX;
using MessagePack;
using CharaCustom;
using static Illusion.Utils;

namespace StudioCharaEditor
{
    class CharaEditorController
    {
        public delegate object Category2GetFunc(CharaEditorController cec);
        public delegate void Category2SetFunc(CharaEditorController cec, object value);
        public delegate CharaDetailInfo[] GetClothesDetailInfoListFunc(string category2);

        static public readonly string CT1_BODY = "Body";
        static public readonly string CT1_FACE = "Face";
        static public readonly string CT1_HAIR = "Hair";
        static public readonly string CT1_CTHS = "Clothes";
        static public readonly string CT1_ACCS = "Accessories";
        static public readonly string[] CATEGORY1 = { CT1_BODY, CT1_FACE, CT1_HAIR, CT1_CTHS, CT1_ACCS };
        static public readonly string[] MALE_CLOTHES_NAME = { "Top", "Bot", "Gloves", "Shoes" };
        static public readonly string[] FEMALE_CLOTHES_NAME = { "Top", "Bot", "Inner_t", "Inner_b", "Gloves", "Panst", "Socks", "Shoes" };
        static public readonly char[] KEY_SEP_CHAR = new char[] { '#' };
        static private readonly Dictionary<string, string[]> CATEGORY2_BASE_MALE = new Dictionary<string, string[]>
        {
            {CT1_BODY, new string[] {
                    "==SHAPE==",
                    "ShapeWhole",
                    "ShapeBreast",
                    "ShapeUpper",
                    "ShapeLower",
                    "ShapeArm",
                    "ShapeLeg",
                    "==SKIN==",
                    "Skin",
                    "Sunburn",
                    "Nip",
                    "Underhair",
                    "Nail",
                    "Paint1",
                    "Paint2",
                }
            },
            {CT1_FACE, new string[] {
                    "==FACE==",
                    "FaceType",
                    "ShapeWhole",
                    "ShapeChin",
                    "ShapeCheek",
                    "ShapeEyebrow",
                    "ShapeEyes",
                    "ShapeNose",
                    "ShapeMouth",
                    "ShapeEar",
                    "Mole",
                    "Bread",
                    "==EYES==",
                    "++EyesSameSetting",
                    "EyeL",
                    "EyeR",
                    "EyeEtc",
                    "EyeHL",
                    "Eyebrow",
                    "Eyelashes",
                    "==MAKEUP==",
                    "MakeupEyeshadow",
                    "MakeupCheek",
                    "MakeupLip",
                    "MakeupPaint1",
                    "MakeupPaint2",
                }
            },
            {CT1_HAIR, new string[] {
                    "++ColorAutoSetting",
                    "++ColorSameSetting",
                    "BackHair",
                    "FrontHair",
                    "SideHair",
                    "ExtensionHair",
                }
            },
            {CT1_CTHS, MALE_CLOTHES_NAME
            },
            {CT1_ACCS, new string[] { }
            },
        };
        static private readonly Dictionary<string, string[]> CATEGORY2_BASE_FEMALE = new Dictionary<string, string[]>
        {
            {CT1_BODY, new string[] {
                    "==SHAPE==",
                    "ShapeWhole",
                    "ShapeBreast",
                    "ShapeUpper",
                    "ShapeLower",
                    "ShapeArm",
                    "ShapeLeg",
                    "==SKIN==",
                    "Skin",
                    "Sunburn",
                    "Nip",
                    "Underhair",
                    "Nail",
                    "Paint1",
                    "Paint2",
                }
            },
            {CT1_FACE, new string[] {
                    "==FACE==",
                    "FaceType",
                    "ShapeWhole",
                    "ShapeChin",
                    "ShapeCheek",
                    "ShapeEyebrow",
                    "ShapeEyes",
                    "ShapeNose",
                    "ShapeMouth",
                    "ShapeEar",
                    "Mole",
                    "==EYES==",
                    "++EyesSameSetting",
                    "EyeL",
                    "EyeR",
                    "EyeEtc",
                    "EyeHL",
                    "Eyebrow",
                    "Eyelashes",
                    "==MAKEUP==",
                    "MakeupEyeshadow",
                    "MakeupCheek",
                    "MakeupLip",
                    "MakeupPaint1",
                    "MakeupPaint2",
                }
            },
            {CT1_HAIR, new string[] {
                    "++ColorAutoSetting",
                    "++ColorSameSetting",
                    "BackHair",
                    "FrontHair",
                    "SideHair",
                    "ExtensionHair",
                }
            },
            {CT1_CTHS, FEMALE_CLOTHES_NAME
            },
            {CT1_ACCS, new string[] { }
            },
        };

        public Dictionary<string, Category2GetFunc> Category2GetFuncDict = new Dictionary<string, Category2GetFunc>
        {
            { "Face#EyesSameSetting", (cec) => { return cec.ociTarget.charInfo.fileFace.pupilSameSetting; } },
            { "Hair#ColorAutoSetting", (cec) => { return cec.hairAutoColor; } },
            { "Hair#ColorSameSetting", (cec) => { return cec.hairSameColor; } },
        };
        public Dictionary<string, Category2SetFunc> Category2SetFuncDict = new Dictionary<string, Category2SetFunc>
        {
            { "Face#EyesSameSetting", (cec, v) => { cec.ociTarget.charInfo.fileFace.pupilSameSetting = (bool)v; } },
            { "Hair#ColorAutoSetting", (cec, v) => { cec.hairAutoColor = (bool)v; } },
            { "Hair#ColorSameSetting", (cec, v) => { cec.hairSameColor = (bool)v; } },
        };

        public OCIChar ociTarget;
        public Dictionary<string, List<string>> myCategorySet;
        public Dictionary<string, CharaDetailInfo> myDetailDict;
        public Dictionary<string, List<CharaDetailInfo>> myDetailSet;
        public List<AccessoryInfo> myAccessoriesInfo;
        public List<string> myUpdateSequence;
        public bool hairSameColor;
        public bool hairAutoColor;
        public bool textureInited;
        public bool accSortByParent;

        // extend plugins
        public object PushUpController { get; private set; }
        public bool HasPushUpPlugin { 
            get
            {
                return PushUpController != null;
            } 
        }
        public object BoobController { get; private set; }
        public bool HasBoobSettingPlugin
        {
            get
            {
                return BoobController != null;
            }
        }
        public object BoneController { get; private set; }
        public bool HasABMXPlugin
        {
            get
            {
                return BoneController != null;
            }
        }
        public object SkinOverlayContrller { get; private set; }
        public object ClothOverlayContrller { get; private set; }
        public bool HasOverlayPlugin
        {
            get
            {
                return SkinOverlayContrller != null && ClothOverlayContrller != null;
            }
        }

        public CharaEditorController(OCIChar target)
        {
            ociTarget = target;
        }

        public void Initialize()
        { 
            // check plugins
            try
            {
                InitPushUpCtrl();
            }
            catch (Exception)
            {
                PushUpController = null;
            }
            try
            {
                InitBoobCtrl();
            }
            catch (Exception)
            {
                BoobController = null;
            }
            try
            {
                InitABMXCtrl();
            }
            catch (Exception)
            {
                BoneController = null;
            }
            try
            {
                InitOverlayCtrl();
            }
            catch (Exception)
            {
                SkinOverlayContrller = null;
                ClothOverlayContrller = null;
            }

            InitFileData();
            CheckHairColor();
        }

        #region PushUpPlugin
        private void InitPushUpCtrl()
        {
            GameObject gameObject = ociTarget.charInfo.gameObject;
            PushUpController = ((gameObject != null) ? gameObject.GetComponent<PushUpController>() : null);
        }

        private void InvokeSetPushUpBreastSoftness(float value)
        {
            (PushUpController as PushUpController).SetBreastSoftness(value);
        }

        public void SetPushUpBreastSoftness(float value)
        {
            if (HasPushUpPlugin)
            {
                InvokeSetPushUpBreastSoftness(value);
            }
        }
        #endregion

        #region BoobSettingPlugin
        private void InitBoobCtrl()
        {
            BoobController = ociTarget.charInfo.GetComponent<BoobController>();
        }
        #endregion

        #region ABMXPlugin
        private void InitABMXCtrl()
        {
            GameObject gameObject = ociTarget.charInfo.gameObject;
            BoneController = ((gameObject != null) ? gameObject.GetComponent<BoneController>() : null);
        }
        #endregion

        #region OverlayPlugin
        private void InitOverlayCtrl()
        {
            GameObject gameObject = ociTarget.charInfo.gameObject;
            SkinOverlayContrller = ((gameObject != null) ? gameObject.GetComponent<KoiSkinOverlayController>() : null);
            ClothOverlayContrller = ((gameObject != null) ? gameObject.GetComponent<KoiClothesOverlayController>() : null);
        }
        #endregion

        public void InitTexture(bool init)
        {
            // TODO: MeterialEditor issue
            ChaControl chaCtrl = ociTarget.charInfo;
            if (init && !textureInited)
            {
                chaCtrl.releaseCustomInputTexture = false;
                chaCtrl.loadWithDefaultColorAndPtn = false;
                chaCtrl.ChangeClothes(false);
                textureInited = true;
            }
            else if (!init && textureInited)
            {
                chaCtrl.releaseCustomInputTexture = true;
                chaCtrl.loadWithDefaultColorAndPtn = false;
                textureInited = false;
            }
        }

        public void InitFileData()
        {
            ChaControl chaCtrl = ociTarget.charInfo;
            myCategorySet = new Dictionary<string, List<string>>();
            myDetailDict = new Dictionary<string, CharaDetailInfo>();
            myDetailSet = new Dictionary<string, List<CharaDetailInfo>>();
            myUpdateSequence = new List<string>();

            bool isDetailInCategory(string cdiKey)
            {
                string[] segs = cdiKey.Split(KEY_SEP_CHAR);
                if (segs.Length != 3)
                    return false;
                if (!myCategorySet.ContainsKey(segs[0]))
                    return false;
                return myCategorySet[segs[0]].Contains(segs[1]);
            }

            void addToDetailSet(CharaDetailInfo cdi)
            {
                string key = cdi.DetailDefine.Key;
                string[] segs = key.Split(KEY_SEP_CHAR);
                string setName = segs[0] + "#" + segs[1];
                if (!myDetailSet.ContainsKey(setName))
                {
                    myDetailSet[setName] = new List<CharaDetailInfo>();
                }
                myDetailSet[setName].Add(cdi);
                myDetailDict[key] = cdi;
            }
            
            void addToUpdateSequence(CharaDetailInfo cdi)
            {
                if (!cdi.DetailDefine.IsData)
                {
                    return;
                }
                if (!myUpdateSequence.Contains(cdi.DetailDefine.Key))
                {
                    myUpdateSequence.Add(cdi.DetailDefine.Key);
                }
            }

            void addToUpdateSequenceList(string[] seqKeys)
            {
                foreach (string key in seqKeys)
                {
                    if (!myUpdateSequence.Contains(key))
                    {
                        myUpdateSequence.Add(key);
                    }
                }
            }

            // category set
            foreach (string category in CATEGORY1)
            {
                // base
                List<string> cset = new List<string>();
                cset.AddRange(chaCtrl.sex == 0 ? CATEGORY2_BASE_MALE[category] : CATEGORY2_BASE_FEMALE[category]);

                // body overlay
                if (category == CT1_BODY && HasOverlayPlugin)
                {
                    cset.Add("==OVERLAY==");
                    cset.Add("Overlay");
                }

                // face overlay
                if (category == CT1_FACE && HasOverlayPlugin)
                {
                    cset.Add("==OVERLAY==");
                    cset.Add("Overlay");
                }

                // clothes
                if (category == CT1_CTHS)
                {
                    // Nothing here
                }

                // accessories
                if (category == CT1_ACCS)
                {
                    cset = BuildAccessoriesList();
                }

                myCategorySet[category] = cset;
            }

            // vanilla chara detail set
            foreach (CharaDetailDefine cdd in CharaDetailSet.Details)
            {
                if (!isDetailInCategory(cdd.Key))
                {
                    continue;
                }

                CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                addToDetailSet(cdi);
                addToUpdateSequence(cdi);
            }

            // clothes
            foreach (string clothName in myCategorySet[CT1_CTHS])
            {
                //string setName = CT1_CTHS + "#" + clothName;
                int clothIndex = myCategorySet[CT1_CTHS].IndexOf(clothName);
                foreach (CharaDetailDefine cdd in CharaDetailSet.ClothDetailBuilder(chaCtrl, clothIndex))
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                    addToDetailSet(cdi);
                }
                // update sequence key
                addToUpdateSequenceList(CharaDetailSet.ClothUpdateSequenceKeyBuilder(chaCtrl, clothIndex));
            }

            // accessories
            foreach (string accKey in myCategorySet[CT1_ACCS])
            {
                foreach (CharaDetailDefine cdd in CharaDetailSet.AccessoryDetailBuilder(chaCtrl, accKey))
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                    addToDetailSet(cdi);
                }
                // update sequence key
                addToUpdateSequenceList(CharaDetailSet.AccessoryUpdateSequenceKeyBuilder(chaCtrl, accKey));
            }

            // pushup detail set
            /*
            if (HasPushUpPlugin && false)
            {
                foreach (CharaDetailDefine cdi in PushUpDetailSet.Details)
                {
                    myDetailDict[cdi.Key] = new CharaDetailInfo(chaCtrl, cdi);
                    addToDetailSetTemp(cdi.Key);
                }
            }
            */

            // Boob Setting detail set
            if (HasBoobSettingPlugin)
            {
                foreach (CharaDetailDefine cdd in BoobSettingDetailSet.Details)
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                    addToDetailSet(cdi);
                    addToUpdateSequence(cdi);
                }
            }

            // ABMX detail set
            if (HasABMXPlugin)
            {
                foreach (CharaDetailDefine cdd in AMBXSettingDetailSet.Details)
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                    addToDetailSet(cdi);
                    addToUpdateSequence(cdi);
                }
            }

            // OVERLAY detail set
            if (HasOverlayPlugin)
            {
                // body
                foreach (CharaDetailDefine cdd in PluginOverlayDetailSet.BuildSkinOverlayDefine(CT1_BODY))
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                    addToDetailSet(cdi);
                    addToUpdateSequence(cdi);
                }
                // face
                foreach (CharaDetailDefine cdd in PluginOverlayDetailSet.BuildSkinOverlayDefine(CT1_FACE))
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                    addToDetailSet(cdi);
                    addToUpdateSequence(cdi);
                }
                // clothes, update sequence keys only
                List<string> clothOverlaySK = new List<string>();
                foreach (string clothName in myCategorySet[CT1_CTHS])
                {
                    clothOverlaySK.AddRange(PluginOverlayDetailSet.ClothOverlayUpdateSequenceKey(clothName));
                }
                addToUpdateSequenceList(clothOverlaySK.ToArray());
            }

            // DONE
            InitTexture(true);
        }

        /// <summary>
        /// build myAccessoriesInfo data, which is always slot number sorted,
        /// and return a string list of valid acc keys, which affected by sort mode
        /// </summary>
        /// <returns>list of valid acc keys</returns>
        public List<string> BuildAccessoriesList()
        {
            ChaControl chaCtrl = ociTarget.charInfo;
            myAccessoriesInfo = new List<AccessoryInfo>();
            List<string> accKeys = new List<string>();
            int accCount = PluginMoreAccessories.GetAccessoryCount(chaCtrl);
            // build myAccessoriesInfo
            for (int slotNo = 0; slotNo < accCount; slotNo ++)
            {
                var ai = new AccessoryInfo(chaCtrl, slotNo);
                myAccessoriesInfo.Add(ai);
                if (!accSortByParent) accKeys.Add(ai.AccKey);
            }
            // sort by parent
            if (accSortByParent)
            {
                List<CustomSelectInfo> parentSelectList = CharaDetailSet.GetAccessoryParentSelectList();
                foreach (CustomSelectInfo parentInfo in parentSelectList)
                {
                    bool hadChild = false;
                    for (int slotNo = 0; slotNo < accCount; slotNo++)
                    {
                        AccessoryInfo ai = myAccessoriesInfo[slotNo];
                        if (!ai.IsEmptySlot && ChaAccessoryDefine.GetAccessoryParentInt(ai.partsInfo.parentKey) == parentInfo.id)
                        {
                            if (!hadChild)
                            {
                                accKeys.Add("==" + parentInfo.name + "==");
                                hadChild = true;
                            }
                            accKeys.Add(ai.AccKey);
                        }
                    }
                }
            }
 
            return accKeys;
        }

        public void RefreshAccessoriesListIfExpired()
        {
            if (myAccessoriesInfo == null || myAccessoriesInfo.Count < PluginMoreAccessories.VANILLA_ACC_NUM)
                return;
            if (ociTarget == null || ociTarget.charInfo == null)
                return;
            if (myAccessoriesInfo[0].partsInfo == ociTarget.charInfo.nowCoordinate.accessory.parts[0])
                return;
            Console.WriteLine("Auto refresh accessories for {0}", ociTarget.treeNodeObject.textName);
            RefreshAccessoriesList();
        }

        public void RefreshAccessoriesList()
        {
            ChaControl chaCtrl = ociTarget.charInfo;
            // rebuild list
            myCategorySet[CT1_ACCS] = BuildAccessoriesList();
            // rebuild details
            foreach (var accInfo in myAccessoriesInfo)
            {
                string accKey = accInfo.AccKey;
                // clear old set if exist, or create a new one
                string setName = CT1_ACCS + "#" + accKey;
                Dictionary<string, object> oldRevValue = new Dictionary<string, object>();
                if (myDetailSet.ContainsKey(setName))
                {
                    foreach (var oldcdi in myDetailSet[setName])
                    {
                        oldRevValue[oldcdi.DetailDefine.Key] = oldcdi.RevertValue;
                        myDetailDict.Remove(oldcdi.DetailDefine.Key);
                    }
                    myDetailSet[setName].Clear();
                    // leave sequence list no change 
                }
                else
                {
                    myDetailSet[setName] = new List<CharaDetailInfo>();
                    foreach (string key in CharaDetailSet.AccessoryUpdateSequenceKeyBuilder(chaCtrl, accKey))
                    {
                        if (!myUpdateSequence.Contains(key))
                        {
                            myUpdateSequence.Add(key);
                        }
                    }
                }
                // create and update new detail define info
                foreach (CharaDetailDefine cdd in CharaDetailSet.AccessoryDetailBuilder(chaCtrl, accKey))
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(chaCtrl, cdd);
                    if (oldRevValue.ContainsKey(cdi.DetailDefine.Key))
                    {
                        cdi.RevertValue = oldRevValue[cdi.DetailDefine.Key];
                    }
                    myDetailSet[setName].Add(cdi);
                    myDetailDict[cdi.DetailDefine.Key] = cdi;
                }
            }
        }

        public AccessoryDetailInfo GetAccessoryDetailData(string accKey)
        {
            try
            {
                AccessoryDetailInfo accDetail = new AccessoryDetailInfo();
                // Accessory info
                accDetail.accInfo = GetAccessoryInfoByKey(accKey);
                if (accDetail.accInfo == null)
                {
                    throw new Exception("Fail to get accessory info");
                }
                // Accessory info list
                accDetail.accDetails = GetDetailInfoList(CT1_ACCS, accKey);
                // Accessory data
                accDetail.accData = GetDataDictByCategory(CT1_ACCS, accKey);
                return accDetail;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAccessoryDetail failed: accKey = '" + accKey + "': " + ex.Message);
                return null;
            }
        }

        public bool SetAccessoryDetailData(string tgtSlot, AccessoryDetailInfo accDetail, bool mirrorParent, bool mirrorAdjust)
        {
            //Console.WriteLine($"Copy {accDetail.accInfo.AccName} to {tgtSlot}. mirror parent = {mirrorParent}, mirror adjust = {mirrorAdjust}");
            const string PARENT_DATA_NAME = "Acc Parent";
            const string PREFIX_POSITION = "Position ";
            const string PREFIX_ROTATION = "Rotation ";
            //string PREFIX_SCALE = "Scale ";
            const string SURFIX_X = " X";
            const string SURFIX_Y = " Y";
            const string SURFIX_Z = " Z";

            try
            {
                // build a data dict to new slot
                Dictionary<string, object> newSlotData = new Dictionary<string, object>();
                foreach (var pair in accDetail.accData)
                {
                    // change key
                    string dataName = pair.Key.Split(KEY_SEP_CHAR)[2];
                    string newKey = CT1_ACCS + KEY_SEP_CHAR[0] + tgtSlot + KEY_SEP_CHAR[0] + dataName;
                    object newVal = pair.Value;

                    // mirror parent
                    if (mirrorParent && dataName.Equals(PARENT_DATA_NAME))
                    {
                        newVal = CharaDetailSet.GetAccessoryMirrorParentId((int)newVal);
                    }

                    // mirror adjustment
                    if (mirrorAdjust && dataName.StartsWith(PREFIX_POSITION) && dataName.EndsWith(SURFIX_X))
                    {
                        newVal = -(float)newVal;
                    }
                    if (mirrorAdjust && dataName.StartsWith(PREFIX_ROTATION) && (dataName.EndsWith(SURFIX_Y) || dataName.EndsWith(SURFIX_Z)))
                    {
                        newVal = -(float)newVal;
                    }

                    // add to new slot data
                    newSlotData.Add(newKey, newVal);
                }

                SetDataDict(newSlotData);
                //Console.WriteLine(" - Done!");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($" - Failed: {e.Message}");
                return false;
            }
        }

        public void ClearClothSlot(string clothName)
        {
            string key = CT1_CTHS + KEY_SEP_CHAR[0] + clothName + KEY_SEP_CHAR[0] + clothName + " Type";
            if (myDetailDict.ContainsKey(key))
            {
                myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, 0);
            }
            else
            {
                Console.WriteLine("ClearClothSlot failed for known cloth name: " + clothName);
            }
        }

        public void ClearAccessorySlot(string accKey)
        {
            string key = CT1_ACCS + KEY_SEP_CHAR[0] + accKey + KEY_SEP_CHAR[0] + "Acc Category";
            if (myDetailDict.ContainsKey(key))
            {
                myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, 350);
            }
            else
            {
                Console.WriteLine("ClearAccessorySlot failed for known accKey: " + accKey);
            }
        }

        public string[] GetCategoryList(string category1)
        {
            if (myCategorySet.ContainsKey(category1))
            {
                return myCategorySet[category1].ToArray();
            }

            return new string[] { };
        }

        public CharaDetailInfo[] GetDetailInfoList(string category1, string category2)
        {
            if (myCategorySet.ContainsKey(category1))
            {
                if (myCategorySet[category1].Contains(category2))
                {
                    // pre-setted detail info
                    string setName = category1 + "#" + category2;
                    if (myDetailSet.ContainsKey(setName))
                    {
                        return myDetailSet[setName].ToArray();
                    }
                }
            }
            return new CharaDetailInfo[] { };
        }

        public CharaDetailInfo GetDetailInfo(string category1, string category2, string category3)
        {
            string key = category1 + KEY_SEP_CHAR[0] + category2 + KEY_SEP_CHAR[0] + category3;
            if (myDetailDict.ContainsKey(key))
            {
                return myDetailDict[key];
            }
            else
            {
                return null;
            }
        }

        public void UpdateDetailInfo_ClothType(string category2)
        {
            // when cloth type changed, 
            if (myCategorySet.ContainsKey(CT1_CTHS) && myCategorySet[CT1_CTHS].Contains(category2))
            {
                string setName = CT1_CTHS + "#" + category2;
                Dictionary<string, object> revData = new Dictionary<string, object>();
                // delete old entry form myDetailDict
                foreach (var cdi in myDetailSet[setName])
                {
                    string key = cdi.DetailDefine.Key;
                    revData[key] = myDetailDict[key].RevertValue;
                    myDetailDict.Remove(key);
                }
                // create new ones
                myDetailSet[setName].Clear();
                int clothIndex = myCategorySet[CT1_CTHS].IndexOf(category2);
                foreach (CharaDetailDefine cdd in CharaDetailSet.ClothDetailBuilder(ociTarget.charInfo, clothIndex))
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(ociTarget.charInfo, cdd);
                    if (revData.ContainsKey(cdd.Key))
                    {
                        cdi.RevertValue = revData[cdd.Key];
                    }
                    myDetailSet[setName].Add(cdi);
                    myDetailDict[cdd.Key] = cdi;
                }
            }
        }

        public bool RestoreClothDefaultColor(string category2, int partIndex, int colorIndex)
        {
            ChaControl chaCtrl = ociTarget.charInfo;
            CmpClothes cmpCloth = chaCtrl.cmpClothes[partIndex];
            bool updateColor = false;
            bool updatePtn1 = false;
            bool updatePtn2 = false;
            bool updatePtn3 = false;

            void setColorByIndex(int i)
            {
                var ci = chaCtrl.GetClothesDefaultSetting(partIndex, i);
                
                chaCtrl.nowCoordinate.clothes.parts[partIndex].colorInfo[i].baseColor = ci.baseColor;
                chaCtrl.chaFile.coordinate.clothes.parts[partIndex].colorInfo[i].baseColor = ci.baseColor;

                chaCtrl.nowCoordinate.clothes.parts[partIndex].colorInfo[i].glossPower = ci.glossPower;
                chaCtrl.chaFile.coordinate.clothes.parts[partIndex].colorInfo[i].glossPower = ci.glossPower;

                chaCtrl.nowCoordinate.clothes.parts[partIndex].colorInfo[i].metallicPower = ci.metallicPower;
                chaCtrl.chaFile.coordinate.clothes.parts[partIndex].colorInfo[i].metallicPower = ci.metallicPower;

                chaCtrl.nowCoordinate.clothes.parts[partIndex].colorInfo[i].pattern = ci.pattern;
                chaCtrl.chaFile.coordinate.clothes.parts[partIndex].colorInfo[i].pattern = ci.pattern;

                chaCtrl.nowCoordinate.clothes.parts[partIndex].colorInfo[i].patternColor = ci.patternColor;
                chaCtrl.chaFile.coordinate.clothes.parts[partIndex].colorInfo[i].patternColor = ci.patternColor;

                chaCtrl.nowCoordinate.clothes.parts[partIndex].colorInfo[i].layout = ci.layout;
                chaCtrl.chaFile.coordinate.clothes.parts[partIndex].colorInfo[i].layout = ci.layout;

                chaCtrl.nowCoordinate.clothes.parts[partIndex].colorInfo[i].rotation = ci.rotation;
                chaCtrl.chaFile.coordinate.clothes.parts[partIndex].colorInfo[i].rotation = ci.rotation;
            }

            if (cmpCloth != null)
            {
                if ((colorIndex == 0 || colorIndex == -1) && (cmpCloth.useColorA01 || cmpCloth.useColorN01))
                {
                    setColorByIndex(0);
                    updateColor = true;
                    updatePtn1 = true;
                }
                if ((colorIndex == 1 || colorIndex == -1) && (cmpCloth.useColorA02 || cmpCloth.useColorN02))
                {
                    setColorByIndex(1);
                    updateColor = true;
                    updatePtn2 = true;
                }
                if ((colorIndex == 2 || colorIndex == -1) && (cmpCloth.useColorA03 || cmpCloth.useColorN03))
                {
                    setColorByIndex(2);
                    updateColor = true;
                    updatePtn3 = true;
                }
            }

            if (updateColor)
            {
                chaCtrl.ChangeCustomClothes(partIndex, true, updatePtn1, updatePtn2, updatePtn3);
                UpdateDetailInfo_ClothType(category2);
            }

            return updateColor;
        }

        public string GetClothDispName(string category2)
        {
            return category2;
        }

        public AccessoryInfo GetAccessoryInfoByKey(string accKey)
        {
            try
            {
                int slotNo = int.Parse(accKey);
                if (slotNo >= myAccessoriesInfo.Count)
                {
                    return null;
                }
                if (!myAccessoriesInfo[slotNo].AccKey.Equals(accKey))
                {
                    throw new InvalidOperationException("Accessory key not match!?");
                }
                return myAccessoriesInfo[slotNo];
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAccessoryInfoByKey failed: accKey = '" + accKey + "': " + ex.Message);
                return null;
            }
        }

        public void UpdateDetailInfo_AccType(string category2)
        {
            // when cloth type changed, 
            if (myCategorySet.ContainsKey(CT1_ACCS) && myCategorySet[CT1_ACCS].Contains(category2))
            {
                string setName = CT1_ACCS + "#" + category2;
                Dictionary<string, object> revData = new Dictionary<string, object>();
                // delete old entry form myDetailDict
                foreach (var cdi in myDetailSet[setName])
                {
                    string key = cdi.DetailDefine.Key;
                    revData[key] = myDetailDict[key].RevertValue;
                    myDetailDict.Remove(key);
                }
                // create new ones
                myDetailSet[setName].Clear();
                foreach (CharaDetailDefine cdd in CharaDetailSet.AccessoryDetailBuilder(ociTarget.charInfo, category2))
                {
                    CharaDetailInfo cdi = new CharaDetailInfo(ociTarget.charInfo, cdd);
                    if (revData.ContainsKey(cdd.Key))
                    {
                        cdi.RevertValue = revData[cdd.Key];
                    }
                    myDetailSet[setName].Add(cdi);
                    myDetailDict[cdd.Key] = cdi;
                }
            }
        }

        public bool RestoreAccessoryDefaultColor(string accKey, int colorIndex)
        {
            Color color = Color.black;
            float gloss = 0.5f;
            float metallic = 0.5f;
            ChaControl chaCtrl = ociTarget.charInfo;
            AccessoryInfo accInfo = GetAccessoryInfoByKey(accKey);
            bool updateColor = false;

            void setColorByIndex(int i)
            {
                accInfo.partsInfo.colorInfo[i].color = color;
                accInfo.partsInfo.colorInfo[i].glossPower = gloss;
                accInfo.partsInfo.colorInfo[i].metallicPower = metallic;
                if (accInfo.IsVanillaSlot)
                {
                    accInfo.orgPartsInfo.colorInfo[i].color = color;
                    accInfo.orgPartsInfo.colorInfo[i].glossPower = gloss;
                    accInfo.orgPartsInfo.colorInfo[i].metallicPower = metallic;
                }
            }

            if (colorIndex == -1)
            {
                for (int j = 0; j < 4; j ++)
                {
                    bool res = PluginMoreAccessories.GetAccessoryDefaultColor(ref color, ref gloss, ref metallic, chaCtrl, accInfo.slotNo, j);
                    if (res)
                    {
                        setColorByIndex(j);
                        updateColor = true;
                    }
                }
            }
            else
            {
                updateColor = PluginMoreAccessories.GetAccessoryDefaultColor(ref color, ref gloss, ref metallic, chaCtrl, accInfo.slotNo, colorIndex);
                if (updateColor)
                {
                    setColorByIndex(colorIndex);
                }
            }

            if (updateColor)
                chaCtrl.ChangeAccessoryColor(accInfo.slotNo);

            return updateColor;
        }

        public bool AlignAccessoryColorWithHair(string accKey, int hairIndex)
        {
            ChaControl chaCtrl = ociTarget.charInfo;
            AccessoryInfo accInfo = GetAccessoryInfoByKey(accKey);
            bool updateColor = false;
            
            if (accInfo.accCmp != null && accInfo.accCmp.typeHair)
            {
                accInfo.partsInfo.colorInfo[0].color = chaCtrl.fileHair.parts[hairIndex].baseColor;
                accInfo.partsInfo.colorInfo[1].color = chaCtrl.fileHair.parts[hairIndex].topColor;
                accInfo.partsInfo.colorInfo[2].color = chaCtrl.fileHair.parts[hairIndex].underColor;
                accInfo.partsInfo.colorInfo[3].color = chaCtrl.fileHair.parts[hairIndex].specular;
                accInfo.partsInfo.colorInfo[0].smoothnessPower = chaCtrl.fileHair.parts[hairIndex].smoothness;
                accInfo.partsInfo.colorInfo[0].metallicPower = chaCtrl.fileHair.parts[hairIndex].metallic;
                if (accInfo.IsVanillaSlot)
                {
                    for (int i = 0; i < 4; i ++)
                    {
                        byte[] bytes = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo.ColorInfo>(accInfo.partsInfo.colorInfo[i]);
                        accInfo.orgPartsInfo.colorInfo[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo.ColorInfo>(bytes);
                    }
                }
                chaCtrl.ChangeHairTypeAccessoryColor(accInfo.slotNo);
                updateColor = true;
            }

            return updateColor;
        }

        public void CheckHairColor()
        {
            ChaControl chaCtrl = ociTarget.charInfo;
            Color refBaseColor = Color.clear;
            Color refTopColor = Color.clear;
            Color refUnderColor = Color.clear;
            Color refSpecular = Color.clear;
            bool tempSameColor = true;
            bool tempAutoColor = true;
            for (int i = 0; i < chaCtrl.fileHair.parts.Length; i++)
            {
                // same check
                if (i == 0)
                {
                    refBaseColor = chaCtrl.fileHair.parts[i].baseColor;
                    refTopColor = chaCtrl.fileHair.parts[i].topColor;
                    refUnderColor = chaCtrl.fileHair.parts[i].underColor;
                    refSpecular = chaCtrl.fileHair.parts[i].specular;
                }
                else
                {
                    if (chaCtrl.fileHair.parts[i].baseColor != refBaseColor)
                        tempSameColor = false;
                    if (chaCtrl.fileHair.parts[i].topColor != refTopColor)
                        tempSameColor = false;
                    if (chaCtrl.fileHair.parts[i].underColor != refUnderColor)
                        tempSameColor = false;
                    if (chaCtrl.fileHair.parts[i].specular != refSpecular)
                        tempSameColor = false;
                }
                // auto check
                Color topColor;
                Color underColor;
                Color specular;
                chaCtrl.CreateHairColor(chaCtrl.fileHair.parts[i].baseColor, out topColor, out underColor, out specular);
                if (chaCtrl.fileHair.parts[i].topColor != topColor)
                    tempAutoColor = false;
                if (chaCtrl.fileHair.parts[i].underColor != underColor)
                    tempAutoColor = false;
                if (chaCtrl.fileHair.parts[i].specular != specular)
                    tempAutoColor = false;
            }
            hairSameColor = tempSameColor;
            hairAutoColor = tempAutoColor;
        }

        public void Reload()
        {
            ChaControl chaCtrl = ociTarget.charInfo;

            bool noChangeHead = true;
            foreach (bool f in chaCtrl.updateCMFaceTex)
            {
                if (f) noChangeHead = false;
            }
            foreach (bool f in chaCtrl.updateCMFaceGloss)
            {
                if (f) noChangeHead = false;
            }
            foreach (bool f in chaCtrl.updateCMFaceColor)
            {
                if (f) noChangeHead = false;
            }
            foreach (bool f in chaCtrl.updateCMFaceLayout)
            {
                if (f) noChangeHead = false;
            }
            if (!noChangeHead)
            {
                chaCtrl.CreateFaceTexture();
                //Console.WriteLine("Reload CreateFaceTexture");
            }

            bool noChangeBody = true;
            foreach (bool f in chaCtrl.updateCMBodyTex)
            {
                if (f) noChangeBody = false;
            }
            foreach (bool f in chaCtrl.updateCMBodyGloss)
            {
                if (f) noChangeBody = false;
            }
            foreach (bool f in chaCtrl.updateCMBodyColor)
            {
                if (f) noChangeBody = false;
            }
            foreach (bool f in chaCtrl.updateCMBodyLayout)
            {
                if (f) noChangeBody = false;
            }
            if (!noChangeBody)
            {
                chaCtrl.CreateBodyTexture();
                //Console.WriteLine("Reload CreateBodyTexture");
            }

        }

        public Dictionary<string, object> GetDataDictFull()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in myDetailDict.Keys)
            {
                if (!myDetailDict[key].DetailDefine.IsData)
                {
                    continue;
                }

                result[key] = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
            }
            return result;
        }

        public Dictionary<string, object> GetDataDictByCategory(string category1, string category2 = null)
        {
            string starter = category1 + "#";
            if (!string.IsNullOrWhiteSpace(category2))
            {
                starter += category2 + "#";
            }

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in myDetailDict.Keys)
            {
                if (!myDetailDict[key].DetailDefine.IsData || !key.StartsWith(starter))
                {
                    continue;
                }

                result[key] = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
            }
            return result;
        }

        public Dictionary<string, object> GetDataDictByCatelog(CharaDetailDefine.CharaDetailDefineCatelog catelog)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in myDetailDict.Keys)
            {
                if (!myDetailDict[key].DetailDefine.IsData ||
                    myDetailDict[key].DetailDefine.Catelog != catelog)
                {
                    continue;
                }

                result[key] = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
            }
            return result;
        }

        public Dictionary<string, object> GetDataDictVanilla()
        {
            return GetDataDictByCatelog(CharaDetailDefine.CharaDetailDefineCatelog.VANILLA);
        }

        public Dictionary<string, object> GetDataDictABMX()
        {
            return GetDataDictByCatelog(CharaDetailDefine.CharaDetailDefineCatelog.ABMX);
        }

        public Dictionary<string, object> GetDataDictByKeys(string[] tgtKeys)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in tgtKeys)
            {
                if (!myDetailDict.ContainsKey(key) || !myDetailDict[key].DetailDefine.IsData)
                {
                    continue;
                }

                result[key] = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
            }
            return result;
        }

        public Dictionary<string, object> GetDataDictDiff()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in myDetailDict.Keys)
            {
                if (!myDetailDict[key].DetailDefine.IsData)
                {
                    continue;
                }

                object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                if (!DataValueEqual(cValue, myDetailDict[key].RevertValue))
                {
                    result[key] = cValue;
                }
            }
            return result;
        }

        public Dictionary<string, object> GetDataDictDiff(Dictionary<string, object> refDict)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in refDict.Keys)
            {
                if (!myDetailDict.ContainsKey(key) || !myDetailDict[key].DetailDefine.IsData)
                {
                    continue;
                }

                object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                if (!DataValueEqual(cValue, refDict[key]))
                {
                    result[key] = cValue;
                }
            }
            return result;
        }

        public Dictionary<string, object> GetContinuousDataSet(Dictionary<string, object> orgSet)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in orgSet.Keys)
            {
                if (myDetailDict.ContainsKey(key) && myDetailDict[key].DetailDefine.IsContinuousData)
                {
                    result[key] = orgSet[key];
                }
            }
            return result;
        }

        public Dictionary<string, object> GetDiscreteDataSet(Dictionary<string, object> orgSet)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in orgSet.Keys)
            {
                if (myDetailDict.ContainsKey(key) && myDetailDict[key].DetailDefine.IsDiscreteData)
                {
                    result[key] = orgSet[key];
                }
            }
            return result;
        }

        public void SetDataDict(Dictionary<string, object> data, bool force = false)
        {
            if (data == null)
            {
                return;
            }

            // clear remain
            bool verbose = StudioCharaEditor.VerboseMessage.Value;
            int filteredDataCount = 0;
            int settedDataCount = 0;
            int failedDataCount = 0;
            if (verbose)
            {
                Console.WriteLine($"CharaEditorController.SetDataDict start: inputed {data.Count} data");
                foreach (string key in data.Keys)
                {
                    if (!myUpdateSequence.Contains(key))
                    {
                        Console.WriteLine($"- Key '{key}' filtered for not existed in update sequence");
                        filteredDataCount++;
                    }
                }
            }

            // update by sequence
            foreach (string key in myUpdateSequence)
            {
                if (!data.ContainsKey(key))
                {
                    continue;   // not in data, skip
                }
                if (!myDetailDict.ContainsKey(key))
                {
                    if (verbose)
                    {
                        Console.WriteLine($"- Key '{key}' filtered for detail not defined");
                        filteredDataCount++;
                    }
                    continue;   // unknown detail
                }
                // do set data
                try
                {
                    if (force)
                    {
                        //Console.WriteLine("SetData " + key);
                        myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, data[key]);
                    }
                    else
                    {
                        object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                        if (!DataValueEqual(cValue, data[key]))
                        {
                            //Console.WriteLine("SetData " + key);
                            myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, data[key]);
                        }
                    }
                    if (verbose)
                    {
                        Console.WriteLine($"- Key '{key}' updated");
                        settedDataCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"- Key '{key}' fail to update: {ex.Message}");
                    failedDataCount++;
                }
            }

            // reload
            Reload();

            // report
            if (verbose)
            {
                Console.WriteLine($"CharaEditorController.SetDataDict end: set {settedDataCount}, filtered {filteredDataCount}, failed {failedDataCount}\n");
            }
        }

        public void RevertAll()
        {
            bool verbose = StudioCharaEditor.VerboseMessage.Value;
            foreach (string key in myUpdateSequence)
            {
                if (!myDetailDict.ContainsKey(key) || !myDetailDict[key].DetailDefine.IsData)
                {
                    continue;
                }

                try
                {
                    object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                    if (!DataValueEqual(cValue, myDetailDict[key].RevertValue))
                    {
                        if (verbose) Console.WriteLine("Revert " + key);
                        myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, myDetailDict[key].RevertValue);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Revert value failed for key <" + key + ">, value <" + myDetailDict[key].RevertValue.ToString() + ">: " + ex.Message);
                }
            }
            Reload();
        }

        static public bool DataValueEqual(object val1, object val2)
        {
            if (val1 == null) 
            {
                return val2 == null;
            }
            else
            {
                if (val2 == null)
                {
                    return false;
                }

                // for ABMX and other array
                if (val1 is Array)
                {
                    int len = (val1 as Array).Length;
                    for (int i = 0; i < len; i++)
                    {
                        bool rql = DataValueEqual((val1 as Array).GetValue(i), (val2 as Array).GetValue(i));
                        if (!rql) return false;
                    }
                    return true;
                }

                // for hair bundle
                if (val1 is Dictionary<int, float[]>)
                {
                    foreach (int k1 in (val1 as Dictionary<int, float[]>).Keys)
                    {
                        if (!(val2 as Dictionary<int, float[]>).ContainsKey(k1))
                        {
                            return false;
                        }
                        bool rql = DataValueEqual((val1 as Dictionary<int, float[]>)[k1], (val2 as Dictionary<int, float[]>)[k1]);
                        if (!rql) return false;
                    }
                    return true;
                }

                return val1.Equals(val2);
            }
        }
    }
}
