using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AIChara;
using CharaCustom;
using PushUpAI;
using UnityEngine;


namespace StudioCharaEditor
{
    class CharaDetailInfo
    {
        public CharaDetailDefine DetailDefine;
        public object RevertValue;

        public CharaDetailInfo(ChaControl chaCtrl, CharaDetailDefine detailDefine)
        {
            DetailDefine = detailDefine;
            RevertValue = detailDefine.Get != null ? detailDefine.Get(chaCtrl) : null;
        }
    }

    class CharaDetailDefine
    {
        public enum CharaDetailDefineType
        {
            UNKNOWN,
            SLIDER,
            COLOR,
            SELECTOR,

            SEPERATOR,
            TOGGLE,

            ABMXSET1,
            ABMXSET2,
            ABMXSET3,
        };
        public enum CharaDetailDefineCatelog
        {
            VANILLA,
            ABMX,
            BOOBSETTING,
        };
        public delegate object GetFunc(ChaControl chaCtrl);
        public delegate void SetFunc(ChaControl chaCtrl, object value);
        public delegate void UpdFunc(ChaControl chaCtrl);
        public delegate object DefFunc(ChaControl chaCtrl);
        public delegate List<CustomSelectInfo> LstFunc(ChaControl chaCtrl);

        // define
        public string Key = "";
        public CharaDetailDefineType Type = CharaDetailDefineType.UNKNOWN;
        public CharaDetailDefineCatelog Catelog = CharaDetailDefineCatelog.VANILLA;
        public GetFunc Get;
        public SetFunc Set;
        public UpdFunc Upd;
        public LstFunc SelectorList;
    }

    class CharaDetailSet
    {
        #region UPDATE_UTIL
        public static Vector4 updateLayout(Vector4 orgVec4, string dim, float value)
        {
            switch (dim)
            {
                case "x":
                    return new Vector4(value, orgVec4.y, orgVec4.z, orgVec4.w);
                case "y":
                    return new Vector4(orgVec4.x, value, orgVec4.z, orgVec4.w);
                case "z":
                    return new Vector4(orgVec4.x, orgVec4.y, value, orgVec4.w);
                case "w":
                    return new Vector4(orgVec4.x, orgVec4.y, orgVec4.z, value);
                default:
                    throw new ArgumentException();
            }
        }

        public static void updateHairBaseColor(ChaControl chaCtrl, Color color, int index)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            bool autoSetting = cec.hairAutoColor;
            bool sameSetting = cec.hairSameColor;
            if (autoSetting)
            {
                Color topColor;
                Color underColor;
                Color specular;
                chaCtrl.CreateHairColor(color, out topColor, out underColor, out specular);
                for (int i = 0; i < chaCtrl.fileHair.parts.Length; i++)
                {
                    if (sameSetting || i == index)
                    {
                        chaCtrl.fileHair.parts[i].baseColor = color;
                        chaCtrl.fileHair.parts[i].topColor = topColor;
                        chaCtrl.fileHair.parts[i].underColor = underColor;
                        chaCtrl.fileHair.parts[i].specular = specular;
                        chaCtrl.ChangeSettingHairColor(i, true, autoSetting, autoSetting);
                        chaCtrl.ChangeSettingHairSpecular(i);
                    }
                }
                return;
            }
            for (int j = 0; j < chaCtrl.fileHair.parts.Length; j++)
            {
                if (sameSetting || j == index)
                {
                    chaCtrl.fileHair.parts[j].baseColor = color;
                    chaCtrl.ChangeSettingHairColor(j, true, autoSetting, autoSetting);
                }
            }
        }

        public static void updateHairTopColor(ChaControl chaCtrl, Color color, int index)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            bool sameSetting = cec.hairSameColor;

            for (int i = 0; i < chaCtrl.fileHair.parts.Length; i++)
            {
                if (sameSetting || i == index)
                {
                    chaCtrl.fileHair.parts[i].topColor = color;
                    chaCtrl.ChangeSettingHairColor(i, false, true, false);
                }
            }
        }

        public static void updateHairUnderColor(ChaControl chaCtrl, Color color, int index)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            bool sameSetting = cec.hairSameColor;

            for (int i = 0; i < chaCtrl.fileHair.parts.Length; i++)
            {
                if (sameSetting || i == index)
                {
                    chaCtrl.fileHair.parts[i].underColor = color;
                    chaCtrl.ChangeSettingHairColor(i, false, false, true);
                }
            }
        }

        public static void updateHairSpecular(ChaControl chaCtrl, Color color, int index)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            bool sameSetting = cec.hairSameColor;

            for (int i = 0; i < chaCtrl.fileHair.parts.Length; i++)
            {
                if (sameSetting || i == index)
                {
                    chaCtrl.fileHair.parts[i].specular = color;
                    chaCtrl.ChangeSettingHairSpecular(i);
                }
            }
        }

        public static void updateHairMetallic(ChaControl chaCtrl, float value, int index)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            bool sameSetting = cec.hairSameColor;

            for (int i = 0; i < chaCtrl.fileHair.parts.Length; i++)
            {
                if (sameSetting || i == index)
                {
                    chaCtrl.fileHair.parts[i].metallic = value;
                    chaCtrl.ChangeSettingHairMetallic(i);
                }
            }
        }

        public static void updateHairSmoothness(ChaControl chaCtrl, float value, int index)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            bool sameSetting = cec.hairSameColor;

            for (int i = 0; i < chaCtrl.fileHair.parts.Length; i++)
            {
                if (sameSetting || i == index)
                {
                    chaCtrl.fileHair.parts[i].smoothness = value;
                    chaCtrl.ChangeSettingHairSmoothness(i);
                }
            }
        }

        public static void updatePushUpSoftness(ChaControl chaCtrl, float value)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            cec.SetPushUpBreastSoftness(value);
        }
        #endregion

        public static CharaDetailDefine[] Details =
        {
            #region BODY
            // Body#ShapeWhole
            new CharaDetailDefine
            {
                Key = "Body#ShapeWhole#Height",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(0); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(0, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeWhole#HeadSize",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(9); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(9, (float)v); },
            },
            // Body#ShapeBreast
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustSize",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(1); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(1, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(2); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(2, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustRotY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(3); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(3, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(4); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(4, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustRotX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(5); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(5, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustSharp",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(6); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(6, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustSoftness",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.bustSoftness; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.bustSoftness = (float)v; chaCtrl.UpdateBustSoftness(); updatePushUpSoftness(chaCtrl, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#BustWeight",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.bustWeight; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.bustWeight = (float)v; chaCtrl.UpdateBustGravity(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#AreolaBulge",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(7); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(7, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#AreolaSize",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.areolaSize; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.areolaSize = (float)v; chaCtrl.ChangeNipScale(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#NipWeight",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(8); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(8, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#NipStand",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(32); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(32, (float)v); },
            },
            // Body#ShapeUpper
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#NeckW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(10); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(10, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#NeckZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(11); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(11, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#BodyShoulderW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(12); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(12, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#BodyShoulderZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(13); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(13, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#BodyUpW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(14); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(14, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#BodyUpZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(15); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(15, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#BodyLowW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(16); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(16, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeUpper#BodyLowZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(17); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(17, (float)v); },
            },
            // Body#ShapeLower
            new CharaDetailDefine
            {
                Key = "Body#ShapeLower#WaistY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(18); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(18, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLower#WaistUpW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(19); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(19, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLower#WaistUpZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(20); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(20, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLower#WaistLowW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(21); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(21, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLower#WaistLowZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(22); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(22, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLower#Hip",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(23); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(23, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLower#HipRotX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(24); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(24, (float)v); },
            },
            // Body#ShapeArm
            new CharaDetailDefine
            {
                Key = "Body#ShapeArm#Shoulder",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(29); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(29, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeArm#ArmUp",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(30); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(30, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeArm#ArmLow",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(31); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(31, (float)v); },
            },
            // Body#ShapeLeg
            new CharaDetailDefine
            {
                Key = "Body#ShapeLeg#ThighUp",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(25); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(25, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLeg#ThighLow",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(26); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(26, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLeg#Calf",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(27); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(27, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeLeg#Ankle",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeBodyValue(28); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeBodyValue(28, (float)v); },
            },
            // Body#Skin
            new CharaDetailDefine
            {
                Key = "Body#Skin#SkinType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.skinId; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.skinId = (int)v; chaCtrl.AddUpdateCMBodyTexFlags(true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList((chaCtrl.sex == 0) ? ChaListDefine.CategoryNo.mt_skin_b : ChaListDefine.CategoryNo.ft_skin_b, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Skin#DetailPower",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.detailPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.detailPower = (float)v; chaCtrl.ChangeBodyDetailPower(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Skin#DetailType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.detailId; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.detailId = (int)v; chaCtrl.AddUpdateCMBodyTexFlags(true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList((chaCtrl.sex == 0) ? ChaListDefine.CategoryNo.mt_detail_b : ChaListDefine.CategoryNo.ft_detail_b, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Skin#SkinColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.skinColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.skinColor = (Color)v; chaCtrl.AddUpdateCMBodyTexFlags(true, true, true, true); chaCtrl.AddUpdateCMBodyColorFlags(true, false, false, false); chaCtrl.AddUpdateCMFaceTexFlags(true, true, true, true, true, true, true); chaCtrl.AddUpdateCMFaceColorFlags(true, false, false, false, false, false, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Skin#SkinGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.skinGlossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.skinGlossPower = (float)v; chaCtrl.ChangeBodyGlossPower(); chaCtrl.ChangeFaceGlossPower(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Skin#SkinMetallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.skinMetallicPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.skinMetallicPower = (float)v; chaCtrl.ChangeBodyMetallicPower(); chaCtrl.ChangeFaceMetallicPower(); },
            },
            // Body#Sunburn
            new CharaDetailDefine
            {
                Key = "Body#Sunburn#SunburnType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.sunburnId; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.sunburnId = (int)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList((chaCtrl.sex == 0) ? ChaListDefine.CategoryNo.mt_sunburn : ChaListDefine.CategoryNo.ft_sunburn, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Sunburn#SunburnColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.sunburnColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.sunburnColor = (Color)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyColorFlags(false, false, false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            // Body#Nip
            new CharaDetailDefine
            {
                Key = "Body#Nip#NipType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.nipId; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.nipId = (int)v; chaCtrl.ChangeNipKind(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_nip, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Nip#NipColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.nipColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.nipColor = (Color)v; chaCtrl.ChangeNipColor(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Nip#NipGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.nipGlossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.nipGlossPower = (float)v; chaCtrl.ChangeNipGloss(); },
            },
            // Body#Underhair
            new CharaDetailDefine
            {
                Key = "Body#Underhair#UnderhairType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.underhairId; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.underhairId = (int)v; chaCtrl.ChangeUnderHairKind(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_underhair, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Underhair#UnderhairColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.underhairColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.underhairColor = (Color)v; chaCtrl.ChangeUnderHairColor(); },
            },
            // Body#Nail
            new CharaDetailDefine
            {
                Key = "Body#Nail#NailColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.nailColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.nailColor = (Color)v; chaCtrl.ChangeNailColor(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Nail#NailGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.nailGlossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.nailGlossPower = (float)v; chaCtrl.ChangeNailGloss(); },
            },
            // Body#Paint1
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].id; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].id = (int)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_paint, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].color = (Color)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyColorFlags(false, true, false, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].metallicPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].metallicPower = (float)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintMetallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].glossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].glossPower = (float)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintLayout",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].layoutId; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].layoutId = (int)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyLayoutFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.bodypaint_layout, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].layout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].layout = updateLayout(chaCtrl.fileBody.paintInfo[0].layout, "x", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].layout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].layout = updateLayout(chaCtrl.fileBody.paintInfo[0].layout, "y", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].layout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].layout = updateLayout(chaCtrl.fileBody.paintInfo[0].layout, "z", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].layout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].layout = updateLayout(chaCtrl.fileBody.paintInfo[0].layout, "w", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint1#PaintRot",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[0].rotation; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[0].rotation = (float)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            // Body#Paint2
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].id; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].id = (int)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_paint, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].color = (Color)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyColorFlags(false, false, true, false); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].metallicPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].metallicPower = (float)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintMetallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].glossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].glossPower = (float)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintLayout",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].layoutId; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].layoutId = (int)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyLayoutFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.bodypaint_layout, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].layout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].layout = updateLayout(chaCtrl.fileBody.paintInfo[1].layout, "x", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].layout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].layout = updateLayout(chaCtrl.fileBody.paintInfo[1].layout, "y", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].layout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].layout = updateLayout(chaCtrl.fileBody.paintInfo[1].layout, "z", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].layout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].layout = updateLayout(chaCtrl.fileBody.paintInfo[1].layout, "w", (float)v); chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Body#Paint2#PaintRot",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileBody.paintInfo[1].rotation; },
                Set = (chaCtrl, v) => { chaCtrl.fileBody.paintInfo[1].rotation = (float)v; chaCtrl.AddUpdateCMBodyTexFlags(false, true, true, true); chaCtrl.AddUpdateCMBodyGlossFlags(false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateBodyTexture(); },
            },
            #endregion
            #region FACE
            // Face#FaceType
            new CharaDetailDefine
            {
                Key = "Face#FaceType#FaceType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.headId; },
                Set = (chaCtrl, v) => { chaCtrl.ChangeHead((int)v, false); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList((chaCtrl.sex == 0) ? ChaListDefine.CategoryNo.mo_head : ChaListDefine.CategoryNo.fo_head, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#FaceType#FaceSkinType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.skinId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.skinId = (int)v; chaCtrl.AddUpdateCMFaceTexFlags(true, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
                SelectorList = (chaCtrl) => {
                    List<CustomSelectInfo> list = CvsBase.CreateSelectList((chaCtrl.sex == 0) ? ChaListDefine.CategoryNo.mt_skin_f : ChaListDefine.CategoryNo.ft_skin_f, ChaListDefine.KeyType.HeadID);
                    list = (from x in list where x.limitIndex == chaCtrl.fileFace.headId select x).ToList<CustomSelectInfo>();
                    return list;
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#FaceType#FaceDetailPower",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.detailPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.detailPower = (float)v; chaCtrl.ChangeFaceDetailPower(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#FaceType#FaceDetailType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.detailId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.detailId = (int)v; chaCtrl.ChangeFaceDetailKind(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList((chaCtrl.sex == 0) ? ChaListDefine.CategoryNo.mt_detail_f : ChaListDefine.CategoryNo.ft_detail_f, ChaListDefine.KeyType.Unknown); },
            },
            // Face#ShapeWhole
            new CharaDetailDefine
            {
                Key = "Face#ShapeWhole#FaceBaseW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(0); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(0, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeWhole#FaceUpZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(1); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(1, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeWhole#FaceUpY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(2); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(2, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeWhole#FaceLowZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(3); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(3, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeWhole#FaceLowW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(4); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(4, (float)v); },
            },
            // Face#ShapeChin
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(5); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(5, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(6); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(6, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(7); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(7, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinRot",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(8); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(8, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinLowY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(9); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(9, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinTipW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(10); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(10, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinTipY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(11); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(11, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeChin#ChinTipZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(12); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(12, (float)v); },
            },
            // Face#ShapeCheek
            new CharaDetailDefine
            {
                Key = "Face#ShapeCheek#CheekLowY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(13); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(13, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeCheek#CheekLowZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(14); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(14, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeCheek#CheekLowW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(15); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(15, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeCheek#CheekUpY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(16); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(16, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeCheek#CheekUpZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(17); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(17, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeCheek#sCheekUpW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(18); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(18, (float)v); },
            },
            // Face#ShapeEyebrow
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyebrow#EyebrowW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyebrowLayout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyebrowLayout = updateLayout(chaCtrl.fileFace.eyebrowLayout, "z", (float)v); chaCtrl.ChangeEyebrowLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyebrow#EyebrowH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyebrowLayout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyebrowLayout = updateLayout(chaCtrl.fileFace.eyebrowLayout, "w", (float)v); chaCtrl.ChangeEyebrowLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyebrow#EyebrowX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyebrowLayout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyebrowLayout = updateLayout(chaCtrl.fileFace.eyebrowLayout, "x", (float)v); chaCtrl.ChangeEyebrowLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyebrow#EyebrowY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyebrowLayout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyebrowLayout = updateLayout(chaCtrl.fileFace.eyebrowLayout, "y", (float)v); chaCtrl.ChangeEyebrowLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyebrow#EyebrowTilt",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyebrowTilt; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyebrowTilt = (float)v; chaCtrl.ChangeEyebrowTilt(); },
            },
            // Face#ShapeEyes
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(19); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(19, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(20); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(20, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(21); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(21, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(22); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(22, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(23); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(23, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeRotZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(24); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(24, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeRotY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(25); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(25, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeInX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(26); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(26, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeOutX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(27); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(27, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeInY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(28); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(28, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyeOutY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(29); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(29, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyelidForm01",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(30); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(30, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEyes#EyelidForm02",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(31); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(31, (float)v); },
            },
            // Face#ShapeNose
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseAllY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(32); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(32, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseAllZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(33); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(33, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseAllRotX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(34); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(34, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseAllW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(35); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(35, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseBridgeH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(36); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(36, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseBridgeW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(37); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(37, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseBridgeForm",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(38); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(38, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseWingW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(39); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(39, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseWingY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(40); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(40, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseWingZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(41); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(41, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseWingRotX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(42); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(42, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseWingRotZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(43); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(43, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(44); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(44, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseRotX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(45); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(45, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeNose#NoseSize",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(46); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(46, (float)v); },
            },
            // Face#ShapeMouth
            new CharaDetailDefine
            {
                Key = "Face#ShapeMouth#MouthY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(47); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(47, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeMouth#MouthW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(48); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(48, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeMouth#MouthH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(49); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(49, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeMouth#MouthZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(50); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(50, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeMouth#MouthUpForm",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(51); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(51, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeMouth#MouthLowForm",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(52); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(52, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeMouth#MouthCornerForm",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(53); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(53, (float)v); },
            },
            // Face#ShapeEar
            new CharaDetailDefine
            {
                Key = "Face#ShapeEar#EarSize",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(54); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(54, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEar#EarRotY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(55); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(55, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEar#EarRotZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(56); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(56, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEar#EarUpForm",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(57); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(57, (float)v); },
            },
            new CharaDetailDefine
            {
                Key = "Face#ShapeEar#EarLowForm",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.GetShapeFaceValue(58); },
                Set = (chaCtrl, v) => { chaCtrl.SetShapeFaceValue(58, (float)v); },
            },
            // Face#Mole
            new CharaDetailDefine
            {
                Key = "Face#Mole#MoleType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.moleId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.moleId = (int)v; chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_mole, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Mole#MoleColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.moleColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.moleColor = (Color)v; chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); chaCtrl.AddUpdateCMFaceColorFlags(false, false, false, false, false, false, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Mole#MoleW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.moleLayout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.moleLayout = updateLayout(chaCtrl.fileFace.moleLayout, "x", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(false, false, true); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Mole#MoleH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.moleLayout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.moleLayout = updateLayout(chaCtrl.fileFace.moleLayout, "y", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(false, false, true); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Mole#MoleX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.moleLayout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.moleLayout = updateLayout(chaCtrl.fileFace.moleLayout, "z", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(false, false, true); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Mole#MoleY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.moleLayout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.moleLayout = updateLayout(chaCtrl.fileFace.moleLayout, "w", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(false, false, true); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            // Face#Bread
            new CharaDetailDefine
            {
                Key = "Face#Bread#BreadType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.beardId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.beardId = (int)v; chaCtrl.ChangeBeardKind(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.mt_beard, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Bread#BreadColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.beardColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.beardColor = (Color)v; chaCtrl.ChangeBeardColor(); },
            },
            // Face#EyeL
            new CharaDetailDefine
            {
                Key = "Face#EyeL#PupilType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].pupilId; },
                Set = (chaCtrl, v) => { 
                    chaCtrl.fileFace.pupil[0].pupilId = (int)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].pupilId = (int)v;
                    }
                    chaCtrl.ChangeEyesKind(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eye, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#PupilColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].pupilColor; },
                Set = (chaCtrl, v) => { 
                    chaCtrl.fileFace.pupil[0].pupilColor = (Color)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].pupilColor = (Color)v;
                    }
                    chaCtrl.ChangeEyesColor(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#PupilEmission",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].pupilEmission; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].pupilEmission = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].pupilEmission = (float)v;
                    }
                    chaCtrl.ChangeEyesEmission(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#PupilW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].pupilW; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].pupilW = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].pupilW = (float)v;
                    }
                    chaCtrl.ChangeEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#PupilH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].pupilH; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].pupilH = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].pupilH = (float)v;
                    }
                    chaCtrl.ChangeEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#BlackType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].blackId; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].blackId = (int)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].blackId = (int)v;
                    }
                    chaCtrl.ChangeBlackEyesKind(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eyeblack, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#BlackColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].blackColor; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].blackColor = (Color)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].blackColor = (Color)v;
                    }
                    chaCtrl.ChangeBlackEyesColor(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#BlackW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].blackW; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].blackW = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].blackW = (float)v;
                    }
                    chaCtrl.ChangeBlackEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#BlackH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].blackH; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].blackH = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].blackH = (float)v;
                    }
                    chaCtrl.ChangeBlackEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeL#WhiteColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[0].whiteColor; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[0].whiteColor = (Color)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[1].whiteColor = (Color)v;
                    }
                    chaCtrl.ChangeWhiteEyesColor(chaCtrl.fileFace.pupilSameSetting ? 2 : 0);
                },
            },
            // Face#EyeR
            new CharaDetailDefine
            {
                Key = "Face#EyeR#PupilType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].pupilId; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].pupilId = (int)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].pupilId = (int)v;
                    }
                    chaCtrl.ChangeEyesKind(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eye, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#PupilColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].pupilColor; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].pupilColor = (Color)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].pupilColor = (Color)v;
                    }
                    chaCtrl.ChangeEyesColor(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#PupilEmission",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].pupilEmission; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].pupilEmission = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].pupilEmission = (float)v;
                    }
                    chaCtrl.ChangeEyesEmission(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#PupilW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].pupilW; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].pupilW = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].pupilW = (float)v;
                    }
                    chaCtrl.ChangeEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#PupilH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].pupilH; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].pupilH = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].pupilH = (float)v;
                    }
                    chaCtrl.ChangeEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#BlackType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].blackId; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].blackId = (int)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].blackId = (int)v;
                    }
                    chaCtrl.ChangeBlackEyesKind(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eyeblack, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#BlackColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].blackColor; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].blackColor = (Color)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].blackColor = (Color)v;
                    }
                    chaCtrl.ChangeBlackEyesColor(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#BlackW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].blackW; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].blackW = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].blackW = (float)v;
                    }
                    chaCtrl.ChangeBlackEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#BlackH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].blackH; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].blackH = (float)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].blackH = (float)v;
                    }
                    chaCtrl.ChangeBlackEyesWH(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeR#WhiteColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupil[1].whiteColor; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupil[1].whiteColor = (Color)v;
                    if (chaCtrl.fileFace.pupilSameSetting)
                    {
                        chaCtrl.fileFace.pupil[0].whiteColor = (Color)v;
                    }
                    chaCtrl.ChangeWhiteEyesColor(chaCtrl.fileFace.pupilSameSetting ? 2 : 1);
                },
            },
            // Face#EyeEtc
            new CharaDetailDefine
            {
                Key = "Face#EyeEtc#PupilY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.pupilY; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.pupilY = (float)v;
                    chaCtrl.ChangeEyesBasePosY();
                },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeEtc#ShadowScale",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.whiteShadowScale; },
                Set = (chaCtrl, v) => {
                    chaCtrl.fileFace.whiteShadowScale = (float)v;
                    chaCtrl.ChangeEyesShadowRange();
                },
            },
            // Face#EyeHL
            new CharaDetailDefine
            {
                Key = "Face#EyeHL#EyeHLType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.hlId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.hlId = (int)v; chaCtrl.ChangeEyesHighlightKind(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eye_hl, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeHL#EyeHLColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.hlColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.hlColor = (Color)v; chaCtrl.ChangeEyesHighlightColor(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeHL#HLW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.hlLayout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.hlLayout = updateLayout(chaCtrl.fileFace.hlLayout, "x", (float)v); chaCtrl.ChangeEyesHighlighLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeHL#HLH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.hlLayout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.hlLayout = updateLayout(chaCtrl.fileFace.hlLayout, "y", (float)v); chaCtrl.ChangeEyesHighlighLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeHL#HLX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.hlLayout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.hlLayout = updateLayout(chaCtrl.fileFace.hlLayout, "z", (float)v); chaCtrl.ChangeEyesHighlighLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeHL#HLY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.hlLayout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.hlLayout = updateLayout(chaCtrl.fileFace.hlLayout, "w", (float)v); chaCtrl.ChangeEyesHighlighLayout(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#EyeHL#HLTilt",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.hlTilt; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.hlTilt = (float)v; chaCtrl.ChangeEyesHighlighTilt(); },
            },
            // Face#Eyebrow
            new CharaDetailDefine
            {
                Key = "Face#Eyebrow#EyebrowType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyebrowId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyebrowId = (int)v; chaCtrl.ChangeEyebrowKind(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eyebrow, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Eyebrow#EyebrowColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyebrowColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyebrowColor = (Color)v; chaCtrl.ChangeEyebrowColor(); },
            },
            // Face#Eyelashes
            new CharaDetailDefine
            {
                Key = "Face#Eyelashes#EyelashesType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyelashesId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyelashesId = (int)v; chaCtrl.ChangeEyelashesKind(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eyelash, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#Eyelashes#EyelashesColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.eyelashesColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.eyelashesColor = (Color)v; chaCtrl.ChangeEyelashesColor(); },
            },
            // Face#MakeupEyeshadow
            new CharaDetailDefine
            {
                Key = "Face#MakeupEyeshadow#EyeshadowType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.eyeshadowId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.eyeshadowId = (int)v; chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_eyeshadow, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupEyeshadow#EyeshadowColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.eyeshadowColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.eyeshadowColor = (Color)v; chaCtrl.AddUpdateCMFaceColorFlags(false, true, false, false, false, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupEyeshadow#EyeshadowGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.eyeshadowGloss; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.eyeshadowGloss = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(true, false, false, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            // Face#MakeupCheek
            new CharaDetailDefine
            {
                Key = "Face#MakeupCheek#CheekType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.cheekId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.cheekId = (int)v; chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_cheek, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupCheek#CheekColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.cheekColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.cheekColor = (Color)v; chaCtrl.AddUpdateCMFaceColorFlags(false, false, false, false, true, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupCheek#CheekGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.cheekGloss; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.cheekGloss = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(false, false, false, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            // Face#MakeupLip
            new CharaDetailDefine
            {
                Key = "Face#MakeupLip#LipType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.lipId; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.lipId = (int)v; chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_lip, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupLip#LipColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.lipColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.lipColor = (Color)v; chaCtrl.AddUpdateCMFaceColorFlags(false, false, false, false, false, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupLip#LipGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.lipGloss; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.lipGloss = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(false, false, false, false, true); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            // Face#MakeupPaint1
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].id; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].id = (int)v; chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_paint, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].color = (Color)v; chaCtrl.AddUpdateCMFaceColorFlags(false, false, true, true, false, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].glossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].glossPower = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(false, true, true, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintMetallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].metallicPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].metallicPower = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(false, true, true, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].layout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[0].layout, "x", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].layout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[0].layout, "y", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].layout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[0].layout, "z", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].layout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[0].layout, "w", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint1#PaintRot",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].rotation; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].rotation = (float)v; chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            // Face#MakeupPaint2
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].id; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].id = (int)v; chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_paint, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].color = (Color)v; chaCtrl.AddUpdateCMFaceColorFlags(false, false, true, true, false, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintGloss",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[0].glossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[0].glossPower = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(false, true, true, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintMetallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].metallicPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].metallicPower = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(false, true, true, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].layout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[1].layout, "x", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].layout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[1].layout, "y", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].layout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[1].layout, "z", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].layout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].layout = updateLayout(chaCtrl.fileFace.makeup.paintInfo[1].layout, "w", (float)v); chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            new CharaDetailDefine
            {
                Key = "Face#MakeupPaint2#PaintRot",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].rotation; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].rotation = (float)v; chaCtrl.AddUpdateCMFaceLayoutFlags(true, true, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
                Upd = (chaCtrl) => { chaCtrl.CreateFaceTexture(); },
            },
            #endregion
            #region HAIR
            // Hair#BackHair
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#BackHairType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].id; },
                Set = (chaCtrl, v) => {
                    chaCtrl.ChangeHair(0, (int)v, false);
                    chaCtrl.SetHairAcsDefaultColorParameterOnly(0);
                    chaCtrl.ChangeSettingHairAcsColor(0);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.so_hair_b, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#BaseColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].baseColor; },
                Set = (chaCtrl, v) => { updateHairBaseColor(chaCtrl, (Color)v, 0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#topColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].topColor; },
                Set = (chaCtrl, v) => { updateHairTopColor(chaCtrl, (Color)v, 0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#UnderColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].underColor; },
                Set = (chaCtrl, v) => { updateHairUnderColor(chaCtrl, (Color)v, 0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#Specular",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].specular; },
                Set = (chaCtrl, v) => { updateHairSpecular(chaCtrl, (Color)v, 0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#Metallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].metallic; },
                Set = (chaCtrl, v) => { updateHairMetallic(chaCtrl, (float)v, 0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#Smoothness",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].smoothness; },
                Set = (chaCtrl, v) => { updateHairSmoothness(chaCtrl, (float)v, 0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#AcsColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].acsColorInfo[0].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[0].acsColorInfo[0].color = (Color)v; chaCtrl.ChangeSettingHairAcsColor(0); },
            },
            // Hair#FrontHair
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#BackHairType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].id; },
                Set = (chaCtrl, v) => {
                    chaCtrl.ChangeHair(1, (int)v, false);
                    chaCtrl.SetHairAcsDefaultColorParameterOnly(1);
                    chaCtrl.ChangeSettingHairAcsColor(1);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.so_hair_f, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#BaseColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].baseColor; },
                Set = (chaCtrl, v) => { updateHairBaseColor(chaCtrl, (Color)v, 1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#topColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].topColor; },
                Set = (chaCtrl, v) => { updateHairTopColor(chaCtrl, (Color)v, 1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#UnderColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].underColor; },
                Set = (chaCtrl, v) => { updateHairUnderColor(chaCtrl, (Color)v, 1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#Specular",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].specular; },
                Set = (chaCtrl, v) => { updateHairSpecular(chaCtrl, (Color)v, 1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#Metallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].metallic; },
                Set = (chaCtrl, v) => { updateHairMetallic(chaCtrl, (float)v, 1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#Smoothness",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].smoothness; },
                Set = (chaCtrl, v) => { updateHairSmoothness(chaCtrl, (float)v, 1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#AcsColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].acsColorInfo[0].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[1].acsColorInfo[0].color = (Color)v; chaCtrl.ChangeSettingHairAcsColor(1); },
            },
            // Hair#SideHair
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#BackHairType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].id; },
                Set = (chaCtrl, v) => {
                    chaCtrl.ChangeHair(2, (int)v, false);
                    chaCtrl.SetHairAcsDefaultColorParameterOnly(2);
                    chaCtrl.ChangeSettingHairAcsColor(2);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.so_hair_s, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#BaseColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].baseColor; },
                Set = (chaCtrl, v) => { updateHairBaseColor(chaCtrl, (Color)v, 2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#topColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].topColor; },
                Set = (chaCtrl, v) => { updateHairTopColor(chaCtrl, (Color)v, 2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#UnderColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].underColor; },
                Set = (chaCtrl, v) => { updateHairUnderColor(chaCtrl, (Color)v, 2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#Specular",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].specular; },
                Set = (chaCtrl, v) => { updateHairSpecular(chaCtrl, (Color)v, 2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#Metallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].metallic; },
                Set = (chaCtrl, v) => { updateHairMetallic(chaCtrl, (float)v, 2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#Smoothness",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].smoothness; },
                Set = (chaCtrl, v) => { updateHairSmoothness(chaCtrl, (float)v, 2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#AcsColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].acsColorInfo[0].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[2].acsColorInfo[0].color = (Color)v; chaCtrl.ChangeSettingHairAcsColor(2); },
            },
            // Hair#ExtensionHair
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#BackHairType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].id; },
                Set = (chaCtrl, v) => {
                    chaCtrl.ChangeHair(3, (int)v, false);
                    chaCtrl.SetHairAcsDefaultColorParameterOnly(3);
                    chaCtrl.ChangeSettingHairAcsColor(3);
                },
                SelectorList = (chaCtrl) => {return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.so_hair_o, ChaListDefine.KeyType.Unknown); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#BaseColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].baseColor; },
                Set = (chaCtrl, v) => { updateHairBaseColor(chaCtrl, (Color)v, 3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#topColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].topColor; },
                Set = (chaCtrl, v) => { updateHairTopColor(chaCtrl, (Color)v, 3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#UnderColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].underColor; },
                Set = (chaCtrl, v) => { updateHairUnderColor(chaCtrl, (Color)v, 3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#Specular",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].specular; },
                Set = (chaCtrl, v) => { updateHairSpecular(chaCtrl, (Color)v, 3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#Metallic",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].metallic; },
                Set = (chaCtrl, v) => { updateHairMetallic(chaCtrl, (float)v, 3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#Smoothness",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].smoothness; },
                Set = (chaCtrl, v) => { updateHairSmoothness(chaCtrl, (float)v, 3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#AcsColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].acsColorInfo[0].color; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[3].acsColorInfo[0].color = (Color)v; chaCtrl.ChangeSettingHairAcsColor(3); },
            },
            #endregion
        };

        public static string[][] ExcludeKeys =
        {
            // male
            new string[]
            {
            },
            // female
            new string[]
            {
                "Face#Bread#BreadType",
                "Face#Bread#BreadColor",
            }
        };
    }

    /*
    class PushUpDetailSet
    {
        public static bool CheckPushupEnable(ChaControl chaCtrl)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            return (cec.PushUpController as PushUpController).ChaControl != null;
        }

        public static void SetPushupEnable(ChaControl chaCtrl, bool enable)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            PushUpController pctrl = cec.PushUpController as PushUpController;
            // TODO: Failed
            FieldInfo fi = pctrl.GetType().GetField("ChaControl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fi.SetValue(pctrl, enable ? chaCtrl : null);
        }

        public static CharaDetailDefine[] Details =
        {
            // Body#ShapeBreast
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#PushUp Plugin",
                Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
            },
            new CharaDetailDefine
            {
                Key = "Body#ShapeBreast#PushUpEnable",
                Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                Get = (chaCtrl) => { return CheckPushupEnable(chaCtrl); },
                Set = (chaCtrl, v) => { SetPushupEnable(chaCtrl, (bool)v); },
            },
        };
    }
    */

}
