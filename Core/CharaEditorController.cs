using Studio;
using AIChara;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PushUpAI;
using HS2_BoobSettings;
using KKABMX.Core;

namespace StudioCharaEditor
{
    class CharaEditorController
    {
        public OCIChar ociTarget;
        public Dictionary<string, CharaDetailInfo> myDetailDict;
        public Dictionary<string, string[]> myDetailSet;
        public List<string> myUpdateSequence;
        public bool hairSameColor;
        public bool hairAutoColor;

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

        public void InitFileData()
        {
            ChaControl chaCtrl = ociTarget.charInfo;
            myDetailDict = new Dictionary<string, CharaDetailInfo>();
            myUpdateSequence = new List<string>();
            Dictionary<string, List<string>> detailSetTemp = new Dictionary<string, List<string>>();
            string[] myExcludeList = CharaDetailSet.ExcludeKeys[chaCtrl.sex];

            void addToDetailSetTemp(string key)
            {
                string[] segs = key.Split(new char[] { '#' });
                string setName = segs[0] + "#" + segs[1];
                if (!detailSetTemp.ContainsKey(setName))
                {
                    detailSetTemp[setName] = new List<string>();
                }
                detailSetTemp[setName].Add(segs[2]);
            }

            // vanilla detail set
            foreach (CharaDetailDefine cdi in CharaDetailSet.Details)
            {
                if (myExcludeList.Contains(cdi.Key))
                {
                    continue;
                }

                CharaDetailInfo myDetail = new CharaDetailInfo(chaCtrl, cdi);
                if (myDetail.RevertValue != null)
                {
                    myDetailDict[cdi.Key] = myDetail;
                    myUpdateSequence.Add(cdi.Key);
                    addToDetailSetTemp(cdi.Key);
                }
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
                foreach (CharaDetailDefine cdi in BoobSettingDetailSet.Details)
                {
                    myDetailDict[cdi.Key] = new CharaDetailInfo(chaCtrl, cdi);
                    myUpdateSequence.Add(cdi.Key);
                    addToDetailSetTemp(cdi.Key);
                }
            }

            // ABMX detail set
            if (HasABMXPlugin)
            {
                foreach (CharaDetailDefine cdi in AMBXSettingDetailSet.Details)
                {
                    myDetailDict[cdi.Key] = new CharaDetailInfo(chaCtrl, cdi);
                    myUpdateSequence.Add(cdi.Key);
                    addToDetailSetTemp(cdi.Key);
                }
            }

            // re-orgnize detail set
            myDetailSet = new Dictionary<string, string[]>();
            foreach (string key in detailSetTemp.Keys)
            {
                myDetailSet[key] = detailSetTemp[key].ToArray();
            }
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
                if (myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SEPERATOR)
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
                if (myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SEPERATOR ||
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
                if (!myDetailDict.ContainsKey(key) || myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SEPERATOR)
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
                if (myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SEPERATOR)
                {
                    continue;
                }

                object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                if (!cValue.Equals(myDetailDict[key].RevertValue))
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
                if (myDetailDict.ContainsKey(key))
                {
                    if (myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SEPERATOR)
                    {
                        continue;
                    }

                    object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                    if (cValue.Equals(refDict[key]))
                    {
                        result[key] = cValue;
                    }
                }
            }
            return result;
        }

        public Dictionary<string, object> GetContinuousDataSet(Dictionary<string, object> orgSet)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in orgSet.Keys)
            {
                if (myDetailDict.ContainsKey(key) && myDetailDict[key].DetailDefine.Type != CharaDetailDefine.CharaDetailDefineType.SEPERATOR && (
                    myDetailDict[key].DetailDefine.Type != CharaDetailDefine.CharaDetailDefineType.SELECTOR &&
                    myDetailDict[key].DetailDefine.Type != CharaDetailDefine.CharaDetailDefineType.TOGGLE))
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
                if (myDetailDict.ContainsKey(key) && myDetailDict[key].DetailDefine.Type != CharaDetailDefine.CharaDetailDefineType.SEPERATOR && (
                    myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SELECTOR ||
                    myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.TOGGLE))
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
            foreach (string key in myUpdateSequence)
            {
                if (data.ContainsKey(key) && myDetailDict.ContainsKey(key))
                {
                    if (force)
                    {
                        myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, data[key]);
                    }
                    else
                    {
                        object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                        if (!cValue.Equals(data[key]))
                        {
                            myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, data[key]);
                        }
                    }
                }
            }
            Reload();
        }

        public void RevertAll()
        {
            foreach (string key in myDetailDict.Keys)
            {
                if (myDetailDict[key].DetailDefine.Type == CharaDetailDefine.CharaDetailDefineType.SEPERATOR)
                {
                    continue;
                }

                object cValue = myDetailDict[key].DetailDefine.Get(ociTarget.charInfo);
                if (!cValue.Equals(myDetailDict[key].RevertValue))
                {
                    myDetailDict[key].DetailDefine.Set(ociTarget.charInfo, myDetailDict[key].RevertValue);
                }
            }
            Reload();
        }
    }
}
