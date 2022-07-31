using AIChara;
using HS2_BoobSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioCharaEditor
{
    class CharaBoobSettingDetailDefine : CharaDetailDefine
    {
        public CharaBoobSettingDetailDefine()
        {
            Catelog = CharaDetailDefineCatelog.BOOBSETTING;
        }
    }

    class BoobSettingDetailSet
    {
        public static bool GetBoolValue(ChaControl chaCtrl, string key)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            return (cec.BoobController as BoobController).boolData[key];
        }

        public static void SetBoolValue(ChaControl chaCtrl, string key, bool value)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            (cec.BoobController as BoobController).boolData[key] = value;
        }

        public static float GetFloatValue(ChaControl chaCtrl, string key)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            return (cec.BoobController as BoobController).floatData[key];
        }

        public static void SetFloatValue(ChaControl chaCtrl, string key, float value)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            (cec.BoobController as BoobController).floatData[key] = value;
        }

        public static CharaDetailDefine[] Details =
        {
            // Body#ShapeBreast
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#Boob Setting Plugin: Breast",
                Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#OverridePhysics",
                Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                Get = (chaCtrl) => { return GetBoolValue(chaCtrl, "overridePhysics"); },
                Set = (chaCtrl, v) => { SetBoolValue(chaCtrl, "overridePhysics", CharaDetailDefine.ParseBool(v)); chaCtrl.UpdateBustSoftness(); chaCtrl.ChangeBustInert(false); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#OverrideGravity",
                Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                Get = (chaCtrl) => { return GetBoolValue(chaCtrl, "overrideGravity"); },
                Set = (chaCtrl, v) => { SetBoolValue(chaCtrl, "overrideGravity", CharaDetailDefine.ParseBool(v)); chaCtrl.UpdateBustGravity(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#Damping",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, "damping"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, "damping", (float)v); chaCtrl.UpdateBustSoftness(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#Elasticity",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, "elasticity"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, "elasticity", (float)v); chaCtrl.UpdateBustSoftness(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#Stiffness",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, "stiffness"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, "stiffness", (float)v); chaCtrl.UpdateBustSoftness(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#Inert",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, "inert"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, "inert", (float)v); chaCtrl.ChangeBustInert(false); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#GravityX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, "gravityX") * 100.0f; },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, "gravityX", (float)v / 100.0f); chaCtrl.UpdateBustGravity();},
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#GravityY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, "gravityY") * 100.0f; },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, "gravityY", (float)v / 100.0f); chaCtrl.UpdateBustGravity();},
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeBreast#GravityZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, "gravityZ") * 100.0f; },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, "gravityZ", (float)v / 100.0f); chaCtrl.UpdateBustGravity();},
            },
            // Body#ShapeLower
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#Boob Setting Plugin: Butt",
                Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#OverridePhysics",
                Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                Get = (chaCtrl) => { return GetBoolValue(chaCtrl, BoobController.BUTT+"overridePhysics"); },
                Set = (chaCtrl, v) => {
                    bool en = CharaDetailDefine.ParseBool(v);
                    SetBoolValue(chaCtrl, BoobController.BUTT+"overridePhysics", en);
                    if (en)
                    {
                        chaCtrl.UpdateBustSoftness();
                        chaCtrl.ChangeBustInert(false);
                    }
                    else
                    {
                        HS2_BoobSettings.Util.ResetButtPhysics(chaCtrl.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.HipL));
                        HS2_BoobSettings.Util.ResetButtPhysics(chaCtrl.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.HipR));
                    }
                },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#OverrideGravity",
                Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                Get = (chaCtrl) => { return GetBoolValue(chaCtrl, BoobController.BUTT+"overrideGravity"); },
                Set = (chaCtrl, v) => {
                    bool en = CharaDetailDefine.ParseBool(v);
                    SetBoolValue(chaCtrl, BoobController.BUTT+"overrideGravity", en);
                    if (en)
                    {
                        chaCtrl.UpdateBustGravity();
                    }
                    else
                    {
                        HS2_BoobSettings.Util.ResetButtGravity(chaCtrl.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.HipL));
                        HS2_BoobSettings.Util.ResetButtGravity(chaCtrl.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.HipR));
                    }
                },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#Damping",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, BoobController.BUTT+"damping"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, BoobController.BUTT+"damping", (float)v); chaCtrl.UpdateBustSoftness(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#Elasticity",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, BoobController.BUTT+"elasticity"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, BoobController.BUTT+"elasticity", (float)v); chaCtrl.UpdateBustSoftness(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#Stiffness",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, BoobController.BUTT+"stiffness"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, BoobController.BUTT+"stiffness", (float)v); chaCtrl.UpdateBustSoftness(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#Inert",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, BoobController.BUTT+"inert"); },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, BoobController.BUTT+"inert", (float)v); chaCtrl.UpdateBustSoftness(); },
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#GravityX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, BoobController.BUTT+"gravityX") * 100.0f; },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, BoobController.BUTT+"gravityX", (float)v / 100.0f); chaCtrl.UpdateBustGravity();},
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#GravityY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, BoobController.BUTT+"gravityY") * 100.0f; },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, BoobController.BUTT+"gravityY", (float)v / 100.0f); chaCtrl.UpdateBustGravity();},
            },
            new CharaBoobSettingDetailDefine
            {
                Key = "Body#ShapeLower#GravityZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return GetFloatValue(chaCtrl, BoobController.BUTT+"gravityZ") * 100.0f; },
                Set = (chaCtrl, v) => { SetFloatValue(chaCtrl, BoobController.BUTT+"gravityZ", (float)v / 100.0f); chaCtrl.UpdateBustGravity();},
            },
        };
    }
}
