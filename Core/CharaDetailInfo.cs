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
            // non-data
            UNKNOWN,
            SEPERATOR,
            BUTTON,
            // continuous
            SLIDER,
            COLOR,
            HAIR_BUNDLE,
            VALUEINPUT,
            ABMXSET1,
            ABMXSET2,
            ABMXSET3,
            // discrete
            SELECTOR,
            TOGGLE,
            INT_STATUS,
            SKIN_OVERLAY,
            CLOTH_OVERLAY,
        };
        public enum CharaDetailDefineCatelog
        {
            VANILLA,
            ABMX,
            BOOBSETTING,
            OVERLAY,
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

        // helper
        public virtual bool IsData
        {
            get
            {
                return Type != CharaDetailDefineType.UNKNOWN &&
                       Type != CharaDetailDefineType.SEPERATOR &&
                       Type != CharaDetailDefineType.BUTTON;
            }
        }

        public virtual bool IsContinuousData
        {
            get
            {
                return Type == CharaDetailDefineType.SLIDER ||
                       Type == CharaDetailDefineType.COLOR ||
                       Type == CharaDetailDefineType.HAIR_BUNDLE ||
                       Type == CharaDetailDefineType.VALUEINPUT ||
                       Type == CharaDetailDefineType.ABMXSET1 ||
                       Type == CharaDetailDefineType.ABMXSET2 ||
                       Type == CharaDetailDefineType.ABMXSET3;
            }
        }

        public virtual bool IsDiscreteData
        {
            get
            {
                return Type == CharaDetailDefineType.SELECTOR ||
                       Type == CharaDetailDefineType.TOGGLE ||
                       Type == CharaDetailDefineType.INT_STATUS ||
                       Type == CharaDetailDefineType.SKIN_OVERLAY ||
                       Type == CharaDetailDefineType.CLOTH_OVERLAY;
            }
        }

        public static bool ParseBool(object v)
        {
            if (v is bool)
            {
                return (bool)v;
            }
            else if (v is int)
            {
                return ((int)v) != 0;
            }
            else
            {
                bool retb;
                int reti;
                float retf;
                if (bool.TryParse(v.ToString(), out retb))
                    return retb;
                else if (int.TryParse(v.ToString(), out reti))
                    return reti != 0;
                else if (float.TryParse(v.ToString(), out retf))
                    return retf != 0;
                else
                    return false;
            }
        }
    }

    class CharaValueDetailDefine : CharaDetailDefine
    {
        public float MinValue = float.NaN;
        public float MaxValue = float.NaN;
        public float DefValue = float.NaN;
        public bool LoopValue = false;
        public float DimStep1 = 0.1f;
        public float DimStep2 = 1;

        public CharaValueDetailDefine()
        {
            base.Type = CharaDetailDefineType.VALUEINPUT;
        }
    }

    class CharaIntStatusDetailDefine : CharaDetailDefine
    {
        public int[] IntStatus = new int[] { };
        public string[] IntStatusName = new string[] { };

        public CharaIntStatusDetailDefine()
        {
            base.Type = CharaDetailDefineType.INT_STATUS;
        }
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
        
        public static void updateClothType(ChaControl chaCtrl, int id, int clothIndex)
        {
            chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].id = id;
            chaCtrl.ChangeClothes(clothIndex, id, false);
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
                Get = (chaCtrl) => { return chaCtrl.fileFace.makeup.paintInfo[1].glossPower; },
                Set = (chaCtrl, v) => { chaCtrl.fileFace.makeup.paintInfo[1].glossPower = (float)v; chaCtrl.AddUpdateCMFaceGlossFlags(false, true, true, false, false); chaCtrl.AddUpdateCMFaceTexFlags(false, true, true, true, true, true, true); },
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
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#MeshType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].meshType; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[0].meshType = (int)v; chaCtrl.ChangeSettingHairMeshType(0); },
                SelectorList = (chaCtrl) =>
                {
                    Dictionary<int, ListInfoBase> lstDict = chaCtrl.lstCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.st_hairmeshptn);
                    List<CustomSelectInfo> infoList = new List<CustomSelectInfo>();
                    foreach (int i in lstDict.Keys)
                    {
                        CustomSelectInfo csi = new CustomSelectInfo();
                        ListInfoBase lib = lstDict[i];
                        csi.id = lib.Id;
                        csi.name = lib.Name;
                        csi.assetBundle = lib.GetInfo(ChaListDefine.KeyType.ThumbAB);
                        csi.assetName = lib.GetInfo(ChaListDefine.KeyType.ThumbTex);
                        infoList.Add(csi);
                    }
                    return infoList;
                },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#MeshColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].meshColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[0].meshColor = (Color)v; chaCtrl.ChangeSettingHairMeshColor(0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#MeshW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].meshLayout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[0].meshLayout = updateLayout(chaCtrl.fileHair.parts[0].meshLayout, "x", (float)v); chaCtrl.ChangeSettingHairMeshLayout(0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#MeshH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].meshLayout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[0].meshLayout = updateLayout(chaCtrl.fileHair.parts[0].meshLayout, "y", (float)v); chaCtrl.ChangeSettingHairMeshLayout(0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#MeshX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].meshLayout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[0].meshLayout = updateLayout(chaCtrl.fileHair.parts[0].meshLayout, "z", (float)v); chaCtrl.ChangeSettingHairMeshLayout(0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#MeshY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[0].meshLayout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[0].meshLayout = updateLayout(chaCtrl.fileHair.parts[0].meshLayout, "w", (float)v); chaCtrl.ChangeSettingHairMeshLayout(0); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#BackHair#Bundles",
                Type = CharaDetailDefine.CharaDetailDefineType.HAIR_BUNDLE,
                Get = (chaCtrl) => { return HairBundleDetailSet.BuildBundleDataDict(chaCtrl, 0); },
                Set = (chaCtrl, v) => { HairBundleDetailSet.RestoreBundleDataDict(chaCtrl, 0, (Dictionary<int, float[]>)v); },
            },
            // Hair#FrontHair
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#FrontHairType",
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
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#MeshType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].meshType; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[1].meshType = (int)v; chaCtrl.ChangeSettingHairMeshType(1); },
                SelectorList = (chaCtrl) =>
                {
                    Dictionary<int, ListInfoBase> lstDict = chaCtrl.lstCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.st_hairmeshptn);
                    List<CustomSelectInfo> infoList = new List<CustomSelectInfo>();
                    foreach (int i in lstDict.Keys)
                    {
                        CustomSelectInfo csi = new CustomSelectInfo();
                        ListInfoBase lib = lstDict[i];
                        csi.id = lib.Id;
                        csi.name = lib.Name;
                        csi.assetBundle = lib.GetInfo(ChaListDefine.KeyType.ThumbAB);
                        csi.assetName = lib.GetInfo(ChaListDefine.KeyType.ThumbTex);
                        infoList.Add(csi);
                    }
                    return infoList;
                },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#MeshColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].meshColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[1].meshColor = (Color)v; chaCtrl.ChangeSettingHairMeshColor(1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#MeshW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].meshLayout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[1].meshLayout = updateLayout(chaCtrl.fileHair.parts[1].meshLayout, "x", (float)v); chaCtrl.ChangeSettingHairMeshLayout(1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#MeshH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].meshLayout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[1].meshLayout = updateLayout(chaCtrl.fileHair.parts[1].meshLayout, "y", (float)v); chaCtrl.ChangeSettingHairMeshLayout(1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#MeshX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].meshLayout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[1].meshLayout = updateLayout(chaCtrl.fileHair.parts[1].meshLayout, "z", (float)v); chaCtrl.ChangeSettingHairMeshLayout(1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#MeshY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[1].meshLayout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[1].meshLayout = updateLayout(chaCtrl.fileHair.parts[1].meshLayout, "w", (float)v); chaCtrl.ChangeSettingHairMeshLayout(1); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#FrontHair#Bundles",
                Type = CharaDetailDefine.CharaDetailDefineType.HAIR_BUNDLE,
                Get = (chaCtrl) => { return HairBundleDetailSet.BuildBundleDataDict(chaCtrl, 1); },
                Set = (chaCtrl, v) => { HairBundleDetailSet.RestoreBundleDataDict(chaCtrl, 1, (Dictionary<int, float[]>)v); },
            },
            // Hair#SideHair
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#SideHairType",
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
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#MeshType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].meshType; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[2].meshType = (int)v; chaCtrl.ChangeSettingHairMeshType(2); },
                SelectorList = (chaCtrl) =>
                {
                    Dictionary<int, ListInfoBase> lstDict = chaCtrl.lstCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.st_hairmeshptn);
                    List<CustomSelectInfo> infoList = new List<CustomSelectInfo>();
                    foreach (int i in lstDict.Keys)
                    {
                        CustomSelectInfo csi = new CustomSelectInfo();
                        ListInfoBase lib = lstDict[i];
                        csi.id = lib.Id;
                        csi.name = lib.Name;
                        csi.assetBundle = lib.GetInfo(ChaListDefine.KeyType.ThumbAB);
                        csi.assetName = lib.GetInfo(ChaListDefine.KeyType.ThumbTex);
                        infoList.Add(csi);
                    }
                    return infoList;
                },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#MeshColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].meshColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[2].meshColor = (Color)v; chaCtrl.ChangeSettingHairMeshColor(2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#MeshW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].meshLayout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[2].meshLayout = updateLayout(chaCtrl.fileHair.parts[2].meshLayout, "x", (float)v); chaCtrl.ChangeSettingHairMeshLayout(2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#MeshH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].meshLayout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[2].meshLayout = updateLayout(chaCtrl.fileHair.parts[2].meshLayout, "y", (float)v); chaCtrl.ChangeSettingHairMeshLayout(2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#MeshX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].meshLayout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[2].meshLayout = updateLayout(chaCtrl.fileHair.parts[2].meshLayout, "z", (float)v); chaCtrl.ChangeSettingHairMeshLayout(2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#MeshY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[2].meshLayout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[2].meshLayout = updateLayout(chaCtrl.fileHair.parts[2].meshLayout, "w", (float)v); chaCtrl.ChangeSettingHairMeshLayout(2); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#SideHair#Bundles",
                Type = CharaDetailDefine.CharaDetailDefineType.HAIR_BUNDLE,
                Get = (chaCtrl) => { return HairBundleDetailSet.BuildBundleDataDict(chaCtrl, 2); },
                Set = (chaCtrl, v) => { HairBundleDetailSet.RestoreBundleDataDict(chaCtrl, 2, (Dictionary<int, float[]>)v); },
            },
            // Hair#ExtensionHair
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#ExtensionHairType",
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
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#MeshType",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].meshType; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[3].meshType = (int)v; chaCtrl.ChangeSettingHairMeshType(3); },
                SelectorList = (chaCtrl) =>
                {
                    Dictionary<int, ListInfoBase> lstDict = chaCtrl.lstCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.st_hairmeshptn);
                    List<CustomSelectInfo> infoList = new List<CustomSelectInfo>();
                    foreach (int i in lstDict.Keys)
                    {
                        CustomSelectInfo csi = new CustomSelectInfo();
                        ListInfoBase lib = lstDict[i];
                        csi.id = lib.Id;
                        csi.name = lib.Name;
                        csi.assetBundle = lib.GetInfo(ChaListDefine.KeyType.ThumbAB);
                        csi.assetName = lib.GetInfo(ChaListDefine.KeyType.ThumbTex);
                        infoList.Add(csi);
                    }
                    return infoList;
                },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#MeshColor",
                Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].meshColor; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[3].meshColor = (Color)v; chaCtrl.ChangeSettingHairMeshColor(3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#MeshW",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].meshLayout.x; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[3].meshLayout = updateLayout(chaCtrl.fileHair.parts[3].meshLayout, "x", (float)v); chaCtrl.ChangeSettingHairMeshLayout(3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#MeshH",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].meshLayout.y; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[3].meshLayout = updateLayout(chaCtrl.fileHair.parts[3].meshLayout, "y", (float)v); chaCtrl.ChangeSettingHairMeshLayout(3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#MeshX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].meshLayout.z; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[3].meshLayout = updateLayout(chaCtrl.fileHair.parts[3].meshLayout, "z", (float)v); chaCtrl.ChangeSettingHairMeshLayout(3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#MeshY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[3].meshLayout.w; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[3].meshLayout = updateLayout(chaCtrl.fileHair.parts[3].meshLayout, "w", (float)v); chaCtrl.ChangeSettingHairMeshLayout(3); },
            },
            new CharaDetailDefine
            {
                Key = "Hair#ExtensionHair#Bundles",
                Type = CharaDetailDefine.CharaDetailDefineType.HAIR_BUNDLE,
                Get = (chaCtrl) => { return HairBundleDetailSet.BuildBundleDataDict(chaCtrl, 3); },
                Set = (chaCtrl, v) => { HairBundleDetailSet.RestoreBundleDataDict(chaCtrl, 3, (Dictionary<int, float[]>)v); },
            },
            #endregion
        };

        public static CharaDetailDefine[] ClothDetailBuilder(ChaControl charInfo, int index)
        {
            ChaListDefine.CategoryNo[] MALE_CLOTH_CATEGORYNO = new ChaListDefine.CategoryNo[] {
                ChaListDefine.CategoryNo.mo_top,
                ChaListDefine.CategoryNo.mo_bot,
                ChaListDefine.CategoryNo.mo_gloves,
                ChaListDefine.CategoryNo.mo_shoes
            };
            ChaListDefine.CategoryNo[] FEMALE_CLOTH_CATEGORYNO = new ChaListDefine.CategoryNo[] {
                ChaListDefine.CategoryNo.fo_top, 
                ChaListDefine.CategoryNo.fo_bot, 
                ChaListDefine.CategoryNo.fo_inner_t, 
                ChaListDefine.CategoryNo.fo_inner_b, 
                ChaListDefine.CategoryNo.fo_gloves, 
                ChaListDefine.CategoryNo.fo_panst, 
                ChaListDefine.CategoryNo.fo_socks, 
                ChaListDefine.CategoryNo.fo_shoes
            };
            List<CharaDetailDefine> clothDetails = new List<CharaDetailDefine>();
            string clothName = charInfo.sex == 1 ? CharaEditorController.FEMALE_CLOTHES_NAME[index] : CharaEditorController.MALE_CLOTHES_NAME[index];
            ChaListDefine.CategoryNo typeCategoryNo = charInfo.sex == 1 ? FEMALE_CLOTH_CATEGORYNO[index] : MALE_CLOTH_CATEGORYNO[index];
            CmpClothes cmpCloth = charInfo.cmpClothes[index];
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(charInfo);
            int[] threeStatusCloth = new int[] { 0, 1, 2 };
            int[] twoStatusCloth = new int[] { 0, 2 };
            string[] threeStatusClothStatusName = { "ON", "Half", "OFF" };
            string[] twoStatusClothStatusName = { "ON", "OFF" };
            bool isThreeStatus = charInfo.sex == 1 && (index != 4 && index != 6 && index != 7);

            // function to build details for color
            CharaDetailDefine[] clothColorInfoDetailBuilder(int clothIndex, int colorIndex)
            {
                void updateClothPatternLayout(ChaControl chaCtrl, int clothIndexL, int colorIndexL, string axis, float newValue)
                {
                    Vector4 oldLayout = chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].layout;
                    Vector4 newLayout;
                    if (axis == "x")
                        newLayout = new Vector4(newValue, oldLayout.y, oldLayout.z, oldLayout.w);
                    else if (axis == "y")
                        newLayout = new Vector4(oldLayout.x, newValue, oldLayout.z, oldLayout.w);
                    else if (axis == "z")
                        newLayout = new Vector4(oldLayout.x, oldLayout.y, newValue, oldLayout.w);
                    else if (axis == "w")
                        newLayout = new Vector4(oldLayout.x, oldLayout.y, oldLayout.z, newValue);
                    else
                        throw new Exception();
                    chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].layout = newLayout;
                    chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].colorInfo[colorIndex].layout = newLayout;
                    chaCtrl.ChangeCustomClothes(clothIndex, true, false, false, false);
                }

                string colorNo = " " + (colorIndex + 1).ToString();
                List<CharaDetailDefine> colorInfo = new List<CharaDetailDefine>();

                // color title seperator
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = "Clothes#" + clothName + "#Color" + colorNo + " Setting",
                    Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
                });

                // color
                CharaDetailDefine color = new CharaDetailDefine
                {
                    Key = "Clothes#" + clothName + "#Color" + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                    Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].baseColor; },
                    Set = (chaCtrl, v) => {
                        chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].baseColor = (Color)v;
                        chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].colorInfo[colorIndex].baseColor = (Color)v;
                        chaCtrl.ChangeCustomClothes(clothIndex, true, false, false, false);
                    },
                };
                colorInfo.Add(color);

                // gloss
                CharaDetailDefine gloss = new CharaDetailDefine
                {
                    Key = "Clothes#" + clothName + "#Gloss" + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                    Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].glossPower; },
                    Set = (chaCtrl, v) =>
                    {
                        chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].glossPower = (float)v;
                        chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].colorInfo[colorIndex].glossPower = (float)v;
                        chaCtrl.ChangeCustomClothes(clothIndex, true, false, false, false);
                    },
                };
                colorInfo.Add(gloss);

                // metallic
                CharaDetailDefine metallic = new CharaDetailDefine
                {
                    Key = "Clothes#" + clothName + "#Metallic" + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                    Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].metallicPower; },
                    Set = (chaCtrl, v) =>
                    {
                        chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].metallicPower = (float)v;
                        chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].colorInfo[colorIndex].metallicPower = (float)v;
                        chaCtrl.ChangeCustomClothes(clothIndex, true, false, false, false);
                    },
                };
                colorInfo.Add(metallic);

                // pattern
                CharaDetailDefine pattern = new CharaDetailDefine
                {
                    Key = "Clothes#" + clothName + "#Pattern" + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                    Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].pattern; },
                    Set = (chaCtrl, v) =>
                    {
                        chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].pattern = (int)v;
                        chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].colorInfo[colorIndex].pattern = (int)v;
                        chaCtrl.ChangeCustomClothes(clothIndex, false, colorIndex == 0, colorIndex == 1, colorIndex == 2);
                        // update cloth type
                        cec.UpdateDetailInfo_ClothType(clothName);
                    },
                    SelectorList = (chaCtrl) => { return CvsBase.CreateSelectList(ChaListDefine.CategoryNo.st_pattern); },
                };
                colorInfo.Add(pattern);

                // pattern detail
                if (charInfo.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].pattern != 0)
                {
                    // pattern color
                    CharaDetailDefine patternColor = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Pattern" + colorNo + " Color",
                        Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                        Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].patternColor; },
                        Set = (chaCtrl, v) => {
                            chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].patternColor = (Color)v;
                            chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].colorInfo[colorIndex].patternColor = (Color)v;
                            chaCtrl.ChangeCustomClothes(clothIndex, true, false, false, false);
                        },
                    };
                    colorInfo.Add(patternColor);

                    // pattern width
                    CharaDetailDefine patternWidth = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Pattern" + colorNo + " Width",
                        Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                        Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].layout.x; },
                        Set = (chaCtrl, v) => { updateClothPatternLayout(chaCtrl, clothIndex, colorIndex, "x", (float)v); },
                    };
                    colorInfo.Add(patternWidth);

                    // pattern height
                    CharaDetailDefine patternHeight = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Pattern" + colorNo + " Height",
                        Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                        Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].layout.y; },
                        Set = (chaCtrl, v) => { updateClothPatternLayout(chaCtrl, clothIndex, colorIndex, "y", (float)v); },
                    };
                    colorInfo.Add(patternHeight);

                    // pattern X
                    CharaDetailDefine patternX = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Pattern" + colorNo + " X",
                        Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                        Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].layout.z; },
                        Set = (chaCtrl, v) => { updateClothPatternLayout(chaCtrl, clothIndex, colorIndex, "z", (float)v); },
                    };
                    colorInfo.Add(patternX);

                    // pattern Y
                    CharaDetailDefine patternY = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Pattern" + colorNo + " Y",
                        Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                        Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].layout.w; },
                        Set = (chaCtrl, v) => { updateClothPatternLayout(chaCtrl, clothIndex, colorIndex, "w", (float)v); },
                    };
                    colorInfo.Add(patternY);

                    // pattern rotate
                    CharaDetailDefine patternRotate = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Pattern" + colorNo + " Rotate",
                        Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                        Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].rotation; },
                        Set = (chaCtrl, v) =>
                        {
                            chaCtrl.nowCoordinate.clothes.parts[clothIndex].colorInfo[colorIndex].rotation = (float)v;
                            chaCtrl.chaFile.coordinate.clothes.parts[clothIndex].colorInfo[colorIndex].rotation = (float)v;
                            chaCtrl.ChangeCustomClothes(clothIndex, true, false, false, false);
                        },
                    };
                    colorInfo.Add(patternRotate);
                }

                // restore color
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_CTHS + "#" + clothName + "#Restore color" + colorNo + " setting",
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.RestoreClothDefaultColor(clothName, clothIndex, colorIndex); },
                });

                return colorInfo.ToArray();
            }

            // type
            CharaDetailDefine clothType = new CharaDetailDefine
            {
                Key = "Clothes#" + clothName + "#" + clothName + " Type",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[index].id; },
                Set = (chaCtrl, v) =>
                {
                    chaCtrl.chaFile.coordinate.clothes.parts[index].id = (int)v;
                    chaCtrl.ChangeClothes(index, (int)v, false);
                    // update cloth type
                    cec.UpdateDetailInfo_ClothType(clothName);
                },
                SelectorList = (chaCtrl) => { return CvsBase.CreateSelectList(typeCategoryNo); },
            };
            clothDetails.Add(clothType);

            // other setting if valid
            if (cmpCloth != null)
            {
                // status
                clothDetails.Add(new CharaIntStatusDetailDefine
                {
                    Key = "Clothes#" + clothName + "#Cloth Status",
                    Get = (chaCtrl) => { return chaCtrl.fileStatus.clothesState[index]; },
                    Set = (chaCtrl, v) => { chaCtrl.SetClothesState(index, Convert.ToByte(v), true); },
                    IntStatus = isThreeStatus ? threeStatusCloth : twoStatusCloth,
                    IntStatusName = isThreeStatus ? threeStatusClothStatusName : twoStatusClothStatusName,
                });

                // restore color
                clothDetails.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_CTHS + "#" + clothName + "#Restore all color setting",
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.RestoreClothDefaultColor(clothName, index, -1); },
                });

                // color 1
                if (cmpCloth.useColorA01 || cmpCloth.useColorN01)
                {
                    clothDetails.AddRange(clothColorInfoDetailBuilder(index, 0));
                }

                // color 2
                if (cmpCloth.useColorA02 || cmpCloth.useColorN02)
                {
                    clothDetails.AddRange(clothColorInfoDetailBuilder(index, 1));
                }

                // color 3
                if (cmpCloth.useColorA03 || cmpCloth.useColorN03)
                {
                    clothDetails.AddRange(clothColorInfoDetailBuilder(index, 2));
                }

                // options title seperator
                bool hasOptionParts1 = cmpCloth.objOpt01 != null && cmpCloth.objOpt01.Length > 0;
                bool hasOptionParts2 = cmpCloth.objOpt02 != null && cmpCloth.objOpt02.Length > 0;
                if (cmpCloth.useBreak || hasOptionParts1 || hasOptionParts2)
                {
                    clothDetails.Add(new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Cloth Options",
                        Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
                    });
                }

                // break
                if (cmpCloth.useBreak)
                {
                    CharaDetailDefine breakRate = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Break Rate",
                        Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                        Get = (chaCtrl) => { return chaCtrl.nowCoordinate.clothes.parts[index].breakRate; },
                        Set = (chaCtrl, v) =>
                        {
                            chaCtrl.nowCoordinate.clothes.parts[index].breakRate = (float)v;
                            chaCtrl.chaFile.coordinate.clothes.parts[index].breakRate = (float)v;
                            chaCtrl.ChangeBreakClothes(index);
                        },
                    };
                    clothDetails.Add(breakRate);
                }

                // Option 1
                if (hasOptionParts1) {
                    CharaDetailDefine option1 = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Option Parts 1",
                        Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                        Get = (chaCtrl) => { return !chaCtrl.nowCoordinate.clothes.parts[index].hideOpt[0]; },
                        Set = (chaCtrl, v) =>
                        {
                            bool en = CharaDetailDefine.ParseBool(v);
                            chaCtrl.nowCoordinate.clothes.parts[index].hideOpt[0] = !en;
                            chaCtrl.chaFile.coordinate.clothes.parts[index].hideOpt[0] = !en;
                        },
                    };
                    clothDetails.Add(option1);
                }

                // Option 2
                if (hasOptionParts2)
                {
                    CharaDetailDefine option2 = new CharaDetailDefine
                    {
                        Key = "Clothes#" + clothName + "#Option Parts 2",
                        Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                        Get = (chaCtrl) => { return !chaCtrl.nowCoordinate.clothes.parts[index].hideOpt[1]; },
                        Set = (chaCtrl, v) =>
                        {
                            bool en = CharaDetailDefine.ParseBool(v);
                            chaCtrl.nowCoordinate.clothes.parts[index].hideOpt[1] = !en;
                            chaCtrl.chaFile.coordinate.clothes.parts[index].hideOpt[1] = !en;
                        },
                    };
                    clothDetails.Add(option2);
                }

                // Overlay
                if (cec.HasOverlayPlugin)
                {
                    clothDetails.AddRange(PluginOverlayDetailSet.BuildClothOverlayDefine(clothName, index));
                }
            }

            // Done
            return clothDetails.ToArray();
        }
    
        public static string[] ClothUpdateSequenceKeyBuilder(ChaControl charInfo, int index)
        {
            string clothName = charInfo.sex == 1 ? CharaEditorController.FEMALE_CLOTHES_NAME[index] : CharaEditorController.MALE_CLOTHES_NAME[index];
            List<string> keyList = new List<string>();

            string[] clothColorUpdateSequenceKeyBuilder(int colorIndex)
            {
                string colorNo = " " + (colorIndex + 1).ToString();

                return new string[]
                {
                    "Clothes#" + clothName + "#Color" + colorNo,
                    "Clothes#" + clothName + "#Gloss" + colorNo,
                    "Clothes#" + clothName + "#Metallic" + colorNo,
                    "Clothes#" + clothName + "#Pattern" + colorNo,
                    "Clothes#" + clothName + "#Pattern" + colorNo + " Color",
                    "Clothes#" + clothName + "#Pattern" + colorNo + " Width",
                    "Clothes#" + clothName + "#Pattern" + colorNo + " Height",
                    "Clothes#" + clothName + "#Pattern" + colorNo + " X",
                    "Clothes#" + clothName + "#Pattern" + colorNo + " Y",
                    "Clothes#" + clothName + "#Pattern" + colorNo + " Rotate",
                };
            }

            keyList.Add("Clothes#" + clothName + "#" + clothName + " Type");
            keyList.Add("Clothes#" + clothName + "#Cloth Status");
            keyList.AddRange(clothColorUpdateSequenceKeyBuilder(0));
            keyList.AddRange(clothColorUpdateSequenceKeyBuilder(1));
            keyList.AddRange(clothColorUpdateSequenceKeyBuilder(2));
            keyList.Add("Clothes#" + clothName + "#Break Rate");
            keyList.Add("Clothes#" + clothName + "#Option Parts 1");
            keyList.Add("Clothes#" + clothName + "#Option Parts 2");

            return keyList.ToArray();
        }
    
        public static CharaDetailDefine[] AccessoryDetailBuilder(ChaControl charInfo, string accKey)
        {
            List<CharaDetailDefine> accDetails = new List<CharaDetailDefine>();
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(charInfo);
            AccessoryInfo accInfo = cec.GetAccessoryInfoByKey(accKey);

            List<CharaDetailDefine> AccessoryHairColorDetailBuilder()
            {
                List<CharaDetailDefine> colorInfo = new List<CharaDetailDefine>();

                void updateAccHairColor(ChaControl chaCtrl, int colorIndex, Color newColor)
                {
                    accInfo.partsInfo.colorInfo[colorIndex].color = newColor;
                    if (accInfo.IsVanillaSlot)
                        accInfo.orgPartsInfo.colorInfo[colorIndex].color = newColor;
                    //chaCtrl.ChangeAccessoryColor(accInfo.slotNo);
                    chaCtrl.ChangeHairTypeAccessoryColor(accInfo.slotNo);
                }

                // color title seperator
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Acc Hair Color Setting",
                    Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
                });

                // BaseColor
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#BaseColor",
                    Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[0].color; },
                    Set = (chaCtrl, v) => { updateAccHairColor(chaCtrl, 0, (Color)v); },
                });

                // TopColor
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#TopColor",
                    Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[1].color; },
                    Set = (chaCtrl, v) => { updateAccHairColor(chaCtrl, 1, (Color)v); },
                });

                // UnderColor
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#UnderColor",
                    Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[2].color; },
                    Set = (chaCtrl, v) => { updateAccHairColor(chaCtrl, 2, (Color)v); },
                });

                // Specular
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Specular",
                    Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[3].color; },
                    Set = (chaCtrl, v) => { updateAccHairColor(chaCtrl, 3, (Color)v); },
                });

                // Metallic
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Metallic",
                    Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[0].metallicPower; },
                    Set = (chaCtrl, v) => 
                    {
                        accInfo.partsInfo.colorInfo[0].metallicPower = (float)v;
                        if (accInfo.IsVanillaSlot)
                            accInfo.orgPartsInfo.colorInfo[0].metallicPower = (float)v;
                        chaCtrl.ChangeHairTypeAccessoryColor(accInfo.slotNo);
                    },
                });

                // Smoothness
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Smoothness",
                    Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[0].smoothnessPower; },
                    Set = (chaCtrl, v) =>
                    {
                        accInfo.partsInfo.colorInfo[0].smoothnessPower = (float)v;
                        if (accInfo.IsVanillaSlot)
                            accInfo.orgPartsInfo.colorInfo[0].smoothnessPower = (float)v;
                        chaCtrl.ChangeHairTypeAccessoryColor(accInfo.slotNo);
                    },
                });

                // restore color
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Get back hair color",
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.AlignAccessoryColorWithHair(accKey, 0); },
                });
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Get front hair color",
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.AlignAccessoryColorWithHair(accKey, 1); },
                });
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Get side hair color",
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.AlignAccessoryColorWithHair(accKey, 2); },
                });
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Get extension hair color",
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.AlignAccessoryColorWithHair(accKey, 3); },
                });

                return colorInfo;
            }

            List<CharaDetailDefine> AccessoryColorDetailBuilder(int i)
            {
                List<CharaDetailDefine> colorInfo = new List<CharaDetailDefine>();

                string colorNo = (i + 1).ToString();

                // color title seperator
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Acc Color " + colorNo + " Setting",
                    Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
                });

                // Color
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Color " + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.COLOR,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[i].color; },
                    Set = (chaCtrl, v) => 
                    {
                        accInfo.partsInfo.colorInfo[i].color = (Color)v;
                        if (accInfo.IsVanillaSlot)
                            accInfo.orgPartsInfo.colorInfo[i].color = (Color)v;
                        chaCtrl.ChangeAccessoryColor(accInfo.slotNo);
                    },
                });

                // Gloss
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Gloss " + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[i].glossPower; },
                    Set = (chaCtrl, v) =>
                    {
                        accInfo.partsInfo.colorInfo[i].glossPower = (float)v;
                        if (accInfo.IsVanillaSlot)
                            accInfo.orgPartsInfo.colorInfo[i].glossPower = (float)v;
                        chaCtrl.ChangeAccessoryColor(accInfo.slotNo);
                    },
                });

                // Metallic
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Metallic " + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                    Get = (chaCtrl) => { return accInfo.partsInfo.colorInfo[i].metallicPower; },
                    Set = (chaCtrl, v) =>
                    {
                        accInfo.partsInfo.colorInfo[i].metallicPower = (float)v;
                        if (accInfo.IsVanillaSlot)
                            accInfo.orgPartsInfo.colorInfo[i].metallicPower = (float)v;
                        chaCtrl.ChangeAccessoryColor(accInfo.slotNo);
                    },
                });

                // restore color
                colorInfo.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Restore default color " + colorNo,
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.RestoreAccessoryDefaultColor(accKey, i); },
                });

                return colorInfo;
            }

            List<CharaDetailDefine> AccessoryMoveDetailBuilder(int moveIndex)
            {
                const float posDim1 = 0.005f;
                const float posDim2 = 0.05f;
                const float rotDim1 = 0.05f;
                const float rotDim2 = 0.5f;
                const float sclDim1 = 0.001f;
                const float sclDim2 = 0.01f;
                List<CharaDetailDefine> moveDetail = new List<CharaDetailDefine>();

                void updateMoveVector3(ChaControl chaCtrl, int trfIndex, string seg, float v)
                {
                    Vector3 oldV = accInfo.partsInfo.addMove[moveIndex, trfIndex];
                    Vector3 newV;
                    if (seg == "x")
                        newV = new Vector3(v, oldV.y, oldV.z);
                    else if (seg == "y")
                        newV = new Vector3(oldV.x, v, oldV.z);
                    else if (seg == "z")
                        newV = new Vector3(oldV.x, oldV.y, v);
                    else
                        throw new InvalidOperationException("Invalid segment: " + seg);

                    accInfo.partsInfo.addMove[moveIndex, trfIndex] = newV;
                    if (accInfo.IsVanillaSlot)
                        accInfo.orgPartsInfo.addMove[moveIndex, trfIndex] = newV;
                    chaCtrl.UpdateAccessoryMoveFromInfo(accInfo.slotNo);
                }

                string moveNo = (moveIndex + 1).ToString();

                // adjust title seperator
                moveDetail.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Acc Adjust " + moveNo + " Setting",
                    Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
                });

                // position x
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Position " + moveNo + " X",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 0].x; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 0, "x", (float)v); },
                    DefValue = 0,
                    DimStep1 = posDim1,
                    DimStep2 = posDim2,
                });

                // position y
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Position " + moveNo + " Y",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 0].y; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 0, "y", (float)v); },
                    DefValue = 0,
                    DimStep1 = posDim1,
                    DimStep2 = posDim2,
                });

                // position z
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Position " + moveNo + " Z",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 0].z; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 0, "z", (float)v); },
                    DefValue = 0,
                    DimStep1 = posDim1,
                    DimStep2 = posDim2,
                });

                // rotation x
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Rotation " + moveNo + " X",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 1].x; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 1, "x", (float)v); },
                    MinValue = 0,
                    MaxValue = 360,
                    DefValue = 0,
                    LoopValue = true,
                    DimStep1 = rotDim1,
                    DimStep2 = rotDim2,
                });

                // rotation y
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Rotation " + moveNo + " Y",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 1].y; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 1, "y", (float)v); },
                    MinValue = 0,
                    MaxValue = 360,
                    DefValue = 0,
                    LoopValue = true,
                    DimStep1 = rotDim1,
                    DimStep2 = rotDim2,
                });

                // rotation z
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Rotation " + moveNo + " Z",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 1].z; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 1, "z", (float)v); },
                    MinValue = 0,
                    MaxValue = 360,
                    DefValue = 0,
                    LoopValue = true,
                    DimStep1 = rotDim1,
                    DimStep2 = rotDim2,
                });

                // scale x
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Scale " + moveNo + " X",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 2].x; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 2, "x", (float)v); },
                    MinValue = sclDim1,
                    DefValue = 1,
                    DimStep1 = sclDim1,
                    DimStep2 = sclDim2,
                });

                // scale y
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Scale " + moveNo + " Y",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 2].y; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 2, "y", (float)v); },
                    MinValue = sclDim1,
                    DefValue = 1,
                    DimStep1 = sclDim1,
                    DimStep2 = sclDim2,
                });

                // scale z
                moveDetail.Add(new CharaValueDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Scale " + moveNo + " Z",
                    Get = (chaCtrl) => { return accInfo.partsInfo.addMove[moveIndex, 2].z; },
                    Set = (chaCtrl, v) => { updateMoveVector3(chaCtrl, 2, "z", (float)v); },
                    MinValue = sclDim1,
                    DefValue = 1,
                    DimStep1 = sclDim1,
                    DimStep2 = sclDim2,
                });

                return moveDetail;
            }

            // Category
            accDetails.Add(new CharaDetailDefine
            {
                Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Acc Category",
                Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                Get = (chaCtrl) => { return accInfo.category; },
                Set = (chaCtrl, v) =>
                {
                    accInfo.partsInfo.type = (int)v;
                    accInfo.partsInfo.parentKey = "";
                    //if (accInfo.IsVanillaSlot)
                    //{
                    //    accInfo.orgPartsInfo.type = accInfo.partsInfo.type;
                    //}
                    /*
                    for (int i = 0; i < 2; i++)
                    {
                        base.orgAcs.parts[base.SNo].addMove[i, 0] = (accInfo.partsInfo.addMove[i, 0] = Vector3.zero);
                        base.orgAcs.parts[base.SNo].addMove[i, 1] = (accInfo.partsInfo.addMove[i, 1] = Vector3.zero);
                        base.orgAcs.parts[base.SNo].addMove[i, 2] = (accInfo.partsInfo.addMove[i, 2] = Vector3.one);
                    }
                    */
                    chaCtrl.ChangeAccessory(accInfo.slotNo, accInfo.partsInfo.type, ChaAccessoryDefine.AccessoryDefaultIndex[(int)v - 350], "", true);
                    /*
                    this.SetDefaultColor();
                    base.chaCtrl.ChangeAccessoryColor(base.SNo);
                    */
                    //accInfo.partsInfo.noShake = false;
                    if (accInfo.IsVanillaSlot)
                    {
                        accInfo.orgPartsInfo.type = accInfo.partsInfo.type;
                        accInfo.orgPartsInfo.id = accInfo.partsInfo.id;
                        accInfo.orgPartsInfo.parentKey = accInfo.partsInfo.parentKey;
                        accInfo.orgPartsInfo.noShake = accInfo.partsInfo.noShake;

                    }

                    // update info
                    accInfo.UpdateAccessoryInfo(chaCtrl);
                    cec.UpdateDetailInfo_AccType(accKey);
                },
                SelectorList = (chaCtrl) => { return GetAccessoryCategorySelectList(); },
            });

            // if not empty
            if (!accInfo.IsEmptySlot)
            {
                // acc id
                accDetails.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Acc ID",
                    Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                    Get = (chaCtrl) => { return accInfo.partsInfo.id; },
                    Set = (chaCtrl, v) =>
                    {
                        //bool oldAccIsHair = accInfo.accCmp != null ? oldAccIsHair = accInfo.accCmp.typeHair : false;
                        string oldParentKey = accInfo.partsInfo.parentKey;

                        // change acc id
                        chaCtrl.ChangeAccessory(accInfo.slotNo, accInfo.partsInfo.type, (int)v, "", false);

                        // restore setting
                        if (!oldParentKey.Equals(accInfo.partsInfo.parentKey))
                        {
                            chaCtrl.ChangeAccessoryParent(accInfo.slotNo, oldParentKey);
                        }
                        //accInfo.partsInfo.noShake = false;  // reset no shake flag
                        
                        // org copy
                        if (accInfo.IsVanillaSlot)
                        {
                            accInfo.orgPartsInfo.id = accInfo.partsInfo.id;
                            accInfo.orgPartsInfo.parentKey = accInfo.partsInfo.parentKey;
                            accInfo.orgPartsInfo.noShake = accInfo.partsInfo.noShake;
                        }

                        /*
                        this.SetDefaultColor();
                        base.chaCtrl.ChangeAccessoryColor(base.SNo);
                        bool flag2 = false;
                        if (base.chaCtrl.cmpAccessory != null && null != base.chaCtrl.cmpAccessory[base.SNo])
                        {
                            flag2 = base.chaCtrl.cmpAccessory[base.SNo].typeHair;
                        }
                        if (!oldAccIsHair && flag2)
                        {
                            this.ChangeHairTypeAccessoryColor(0);
                        }
                        */

                        // update info
                        accInfo.UpdateAccessoryInfo(chaCtrl);
                        cec.UpdateDetailInfo_AccType(accKey);
                    },
                    SelectorList = (chaCtrl) => { return CvsBase.CreateSelectList((ChaListDefine.CategoryNo)accInfo.category); },
                });

                // parent
                accDetails.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Acc Parent",
                    Type = CharaDetailDefine.CharaDetailDefineType.SELECTOR,
                    Get = (chaCtrl) => { return ChaAccessoryDefine.GetAccessoryParentInt(accInfo.partsInfo.parentKey); },
                    Set = (chaCtrl, v) =>
                    {
                        string pKey = ((ChaAccessoryDefine.AccessoryParentKey)((int)v)).ToString();
                        chaCtrl.ChangeAccessoryParent(accInfo.slotNo, pKey);
                        if (accInfo.IsVanillaSlot)
                            accInfo.orgPartsInfo.parentKey = accInfo.partsInfo.parentKey;
                    },
                    SelectorList = (chaCtrl) => { return GetAccessoryParentSelectList(); },
                });

                // visible
                accDetails.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Visible",
                    Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                    Get = (chaCtrl) => { return PluginMoreAccessories.GetAccessoryVisible(chaCtrl, int.Parse(accKey)); },
                    Set = (chaCtrl, v) => { PluginMoreAccessories.SetAccessoryVisible(chaCtrl, int.Parse(accKey), CharaDetailDefine.ParseBool(v)); },
                });

                // restore color
                accDetails.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Restore all default color",
                    Type = CharaDetailDefine.CharaDetailDefineType.BUTTON,
                    Upd = (chaCtrl) => { cec.RestoreAccessoryDefaultColor(accKey, -1); },
                });

                // color
                if (accInfo.accCmp.typeHair)
                {
                    accDetails.AddRange(AccessoryHairColorDetailBuilder());
                }
                else
                {
                    if (accInfo.accCmp.useColor01)
                        accDetails.AddRange(AccessoryColorDetailBuilder(0));
                    if (accInfo.accCmp.useColor02)
                        accDetails.AddRange(AccessoryColorDetailBuilder(1));
                    if (accInfo.accCmp.useColor03)
                        accDetails.AddRange(AccessoryColorDetailBuilder(2));
                    if (accInfo.accCmp.rendAlpha != null && accInfo.accCmp.rendAlpha.Length > 0)
                        accDetails.AddRange(AccessoryColorDetailBuilder(3));
                }

                // move
                if (accInfo.accCmp.trfMove01 != null)
                    accDetails.AddRange(AccessoryMoveDetailBuilder(0));
                if (accInfo.accCmp.trfMove02 != null)
                    accDetails.AddRange(AccessoryMoveDetailBuilder(1));

                // no shake
                accDetails.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#Shake setting",
                    Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
                });
                accDetails.Add(new CharaDetailDefine
                {
                    Key = CharaEditorController.CT1_ACCS + "#" + accKey + "#No Shake",
                    Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                    Get = (chaCtrl) => { return accInfo.partsInfo.noShake; },
                    Set = (chaCtrl, v) =>
                    {
                        accInfo.partsInfo.noShake = CharaDetailDefine.ParseBool(v);
                        if (accInfo.IsVanillaSlot)
                            accInfo.orgPartsInfo.noShake = accInfo.partsInfo.noShake;
                    },
                });
            }

            // Done
            return accDetails.ToArray();
        }

        public static string[] AccessoryUpdateSequenceKeyBuilder(ChaControl charInfo, string accKey)
        {
            List<string> keyList = new List<string>();
            string keyBase = CharaEditorController.CT1_ACCS + "#" + accKey + "#";

            string[] AccHairColorUpdateSequenceKeyBuilder()
            {
                return new string[]
                {
                    keyBase + "BaseColor",
                    keyBase + "TopColor",
                    keyBase + "UnderColor",
                    keyBase + "Specular",
                    keyBase + "Metallic",
                    keyBase + "Smoothness",
                };
            }

            string[] AccColorUpdateSequenceKeyBuilder(int colorIndex)
            {
                string colorNo = (colorIndex + 1).ToString();

                return new string[]
                {
                    keyBase + "Color " + colorNo,
                    keyBase + "Gloss " + colorNo,
                    keyBase + "Metallic " + colorNo,
                };
            }

            string[] AccAdjustUpdateSequenceKeyBuilder(int moveIndex)
            {
                string moveNo = (moveIndex + 1).ToString();
                return new string[]
                {
                    keyBase + "Position " + moveNo + " X",
                    keyBase + "Position " + moveNo + " Y",
                    keyBase + "Position " + moveNo + " Z",
                    keyBase + "Rotation " + moveNo + " X",
                    keyBase + "Rotation " + moveNo + " Y",
                    keyBase + "Rotation " + moveNo + " Z",
                    keyBase + "Scale " + moveNo + " X",
                    keyBase + "Scale " + moveNo + " Y",
                    keyBase + "Scale " + moveNo + " Z",
                };
            }

            keyList.Add(keyBase + "Acc Category");
            keyList.Add(keyBase + "Acc ID");
            keyList.Add(keyBase + "Acc Parent");
            keyList.Add(keyBase + "Visible");
            keyList.AddRange(AccHairColorUpdateSequenceKeyBuilder());
            keyList.AddRange(AccColorUpdateSequenceKeyBuilder(0));
            keyList.AddRange(AccColorUpdateSequenceKeyBuilder(1));
            keyList.AddRange(AccColorUpdateSequenceKeyBuilder(2));
            keyList.AddRange(AccColorUpdateSequenceKeyBuilder(3));
            keyList.AddRange(AccAdjustUpdateSequenceKeyBuilder(0));
            keyList.AddRange(AccAdjustUpdateSequenceKeyBuilder(1));
            keyList.Add(keyBase + "No Shake");

            return keyList.ToArray();
        }

        private static List<CustomSelectInfo> accessoryCategorySelectList;
        public static List<CustomSelectInfo> GetAccessoryCategorySelectList()
        {
            if (accessoryCategorySelectList == null)
            {
                accessoryCategorySelectList = new List<CustomSelectInfo>();

                ChaListDefine.CategoryNo[] cateNos = new ChaListDefine.CategoryNo[]
                {
                    ChaListDefine.CategoryNo.ao_none,
                    ChaListDefine.CategoryNo.ao_head,
                    ChaListDefine.CategoryNo.ao_ear,
                    ChaListDefine.CategoryNo.ao_glasses,
                    ChaListDefine.CategoryNo.ao_face,
                    ChaListDefine.CategoryNo.ao_neck,
                    ChaListDefine.CategoryNo.ao_shoulder,
                    ChaListDefine.CategoryNo.ao_chest,
                    ChaListDefine.CategoryNo.ao_waist,
                    ChaListDefine.CategoryNo.ao_back,
                    ChaListDefine.CategoryNo.ao_arm,
                    ChaListDefine.CategoryNo.ao_hand,
                    ChaListDefine.CategoryNo.ao_leg,
                    ChaListDefine.CategoryNo.ao_kokan,
                };

                for (int i = 0; i < cateNos.Length; i ++)
                {
                    ChaListDefine.CategoryNo cNo = cateNos[i];
                    CustomSelectInfo csi = new CustomSelectInfo();
                    csi.id = (int)cNo;
                    csi.name = cNo.ToString().Substring(3) + " " + ChaAccessoryDefine.dictAccessoryType[i];
                    csi.assetBundle = null;
                    csi.assetName = null;
                    accessoryCategorySelectList.Add(csi);
                }
            }
            return accessoryCategorySelectList;
        }

        private static List<CustomSelectInfo> accessoryParentSelectList;
        public static List<CustomSelectInfo> GetAccessoryParentSelectList()
        {
            if (accessoryParentSelectList == null)
            {
                accessoryParentSelectList = new List<CustomSelectInfo>();
                for (int i = 1; i < ChaAccessoryDefine.AccessoryParentName.Length; i++)
                {
                    CustomSelectInfo csi = new CustomSelectInfo();
                    csi.id = i;
                    csi.name = ((ChaAccessoryDefine.AccessoryParentKey)i).ToString().Substring(2) + " " + ChaAccessoryDefine.AccessoryParentName[i];
                    csi.assetBundle = null;
                    csi.assetName = null;
                    accessoryParentSelectList.Add(csi);
                }
            }
            return accessoryParentSelectList;
        }

    }

    /*
     *  Detail set for hair bundle
     */
    class CharaHairBundleDetailDefine : CharaDetailDefine
    {
        public delegate object GetRevertValueFunc(float[] bundleSet);
        public GetRevertValueFunc GetRevertValue;
    }

    class HairBundleDetailSet
    {
        public static int PartsNo;
        public static int BundleKey;

        public static Dictionary<int, float[]> BuildBundleDataDict(ChaControl chaCtrl, int partsNo)
        {
            Dictionary<int, float[]> bundleDataDict = new Dictionary<int, float[]>();
            foreach (int i in chaCtrl.fileHair.parts[partsNo].dictBundle.Keys)
            {
                ChaFileHair.PartsInfo.BundleInfo binfo = chaCtrl.fileHair.parts[partsNo].dictBundle[i];
                float[] bset = new float[7];
                bset[0] = binfo.noShake ? 1 : 0;
                bset[1] = binfo.moveRate.x;
                bset[2] = binfo.moveRate.y;
                bset[3] = binfo.moveRate.z;
                bset[4] = binfo.rotRate.x;
                bset[5] = binfo.rotRate.y;
                bset[6] = binfo.rotRate.z;
                bundleDataDict[i] = bset;
            }
            return bundleDataDict;
        }

        public static void RestoreBundleDataDict(ChaControl chaCtrl, int partsNo, Dictionary<int, float[]> value)
        {
            if (value == null)
            {
                return;
            }
            foreach (int i in value.Keys)
            {
                if (chaCtrl.fileHair.parts[partsNo].dictBundle.ContainsKey(i))
                {
                    ChaFileHair.PartsInfo.BundleInfo binfo = chaCtrl.fileHair.parts[partsNo].dictBundle[i];
                    binfo.noShake = value[i][0] == 1;
                    binfo.moveRate = new Vector3(value[i][1], value[i][2], value[i][3]);
                    binfo.rotRate = new Vector3(value[i][4], value[i][5], value[i][6]);
                }
            }
            chaCtrl.ChangeSettingHairCorrectPosAll(partsNo);
            chaCtrl.ChangeSettingHairCorrectRotAll(partsNo);
        }

        public static void UpdateMoveRate(ChaControl chaCtrl, string seg, float v)
        {
            Vector3 oldV = chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].moveRate;
            Vector3 newV;
            if (seg.Equals("x"))
            {
                newV = new Vector3(v, oldV.y, oldV.z);
            }
            else if (seg.Equals("y"))
            {
                newV = new Vector3(oldV.x, v, oldV.z);
            }
            else
            {
                newV = new Vector3(oldV.x, oldV.y, v);
            }
            chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].moveRate = newV;
            chaCtrl.ChangeSettingHairCorrectPos(PartsNo, BundleKey);
        }

        public static void UpdateRotateRate(ChaControl chaCtrl, string seg, float v)
        {
            Vector3 oldV = chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].rotRate;
            Vector3 newV;
            if (seg.Equals("x"))
            {
                newV = new Vector3(v, oldV.y, oldV.z);
            }
            else if (seg.Equals("y"))
            {
                newV = new Vector3(oldV.x, v, oldV.z);
            }
            else
            {
                newV = new Vector3(oldV.x, oldV.y, v);
            }
            chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].rotRate = newV;
            chaCtrl.ChangeSettingHairCorrectRot(PartsNo, BundleKey);
        }

        public static CharaHairBundleDetailDefine[] Details =
        {
            new CharaHairBundleDetailDefine
            {
                Key = "Title",
                Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
            },
            new CharaHairBundleDetailDefine
            {
                Key = "NoShake",
                Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].noShake ? (float)1 : (float)0; },
                Set = (chaCtrl, v) => { chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].noShake = CharaDetailDefine.ParseBool(v); },
                GetRevertValue = (bundleSet) => { return bundleSet[0]; },
            },
            new CharaHairBundleDetailDefine
            {
                Key = "MoveX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].moveRate.x; },
                Set = (chaCtrl, v) => { UpdateMoveRate(chaCtrl, "x", (float)v); },
                GetRevertValue = (bundleSet) => { return bundleSet[1]; },
            },
            new CharaHairBundleDetailDefine
            {
                Key = "MoveY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].moveRate.y; },
                Set = (chaCtrl, v) => { UpdateMoveRate(chaCtrl, "y", (float)v); },
                GetRevertValue = (bundleSet) => { return bundleSet[2]; },
            },
            new CharaHairBundleDetailDefine
            {
                Key = "MoveZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].moveRate.z; },
                Set = (chaCtrl, v) => { UpdateMoveRate(chaCtrl, "z", (float)v); },
                GetRevertValue = (bundleSet) => { return bundleSet[3]; },
            },
            new CharaHairBundleDetailDefine
            {
                Key = "RotateX",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].rotRate.x; },
                Set = (chaCtrl, v) => { UpdateRotateRate(chaCtrl, "x", (float)v); },
                GetRevertValue = (bundleSet) => { return bundleSet[4]; },
            },
            new CharaHairBundleDetailDefine
            {
                Key = "RotateY",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].rotRate.y; },
                Set = (chaCtrl, v) => { UpdateRotateRate(chaCtrl, "y", (float)v); },
                GetRevertValue = (bundleSet) => { return bundleSet[5]; },
            },
            new CharaHairBundleDetailDefine
            {
                Key = "RotateZ",
                Type = CharaDetailDefine.CharaDetailDefineType.SLIDER,
                Get = (chaCtrl) => { return chaCtrl.fileHair.parts[PartsNo].dictBundle[BundleKey].rotRate.z; },
                Set = (chaCtrl, v) => { UpdateRotateRate(chaCtrl, "z", (float)v); },
                GetRevertValue = (bundleSet) => { return bundleSet[6]; },
            },
        };
    }

    /*
     * PushUp
     * 
    
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
                Set = (chaCtrl, v) => { SetPushupEnable(chaCtrl, CharaDetailDefine.ParseBool(v)); },
            },
        };
    }
    */

}
