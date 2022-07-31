using AIChara;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KoiSkinOverlayX;
using KoiClothesOverlayX;
using UnityEngine;
using KKAPI.Utilities;
using System.Reflection;
using BepInEx.Logging;
using System.IO;

namespace StudioCharaEditor
{
    class SkinOverlayDetailDefine : CharaDetailDefine
    {
        static public readonly string TEXTYPE_OVER = "Over";
        static public readonly string TEXTYPE_UNDER = "Under";

        public string texName;

        public SkinOverlayDetailDefine(string category1, string texType)
        {
            base.Key = category1 + "#Overlay#" + category1 + " " + texType;
            base.Type = CharaDetailDefineType.SKIN_OVERLAY;
            base.Catelog = CharaDetailDefineCatelog.OVERLAY;
            base.Get = GetSkinOverlayTex;
            base.Set = SetSkinOverlayTex;
            texName = category1 + texType;
        }

        private KoiSkinOverlayController getSkinOverlayCtrl(ChaControl chaCtrl)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            if (cec == null || !cec.HasOverlayPlugin)
            {
                return null;
            }
            return cec.SkinOverlayContrller as KoiSkinOverlayController;
        }

        public object GetSkinOverlayTex(ChaControl chaCtrl)
        {
            // return type = Texture2D or null
            KoiSkinOverlayController skinOverlayCtrl = getSkinOverlayCtrl(chaCtrl);
            TexType texType = (TexType)Enum.Parse(typeof(TexType), texName);
            return skinOverlayCtrl?.OverlayStorage.GetTexture(texType);
        }

        public void SetSkinOverlayTex(ChaControl chaCtrl, object tex)
        {
            KoiSkinOverlayController skinOverlayCtrl = getSkinOverlayCtrl(chaCtrl);
            if (skinOverlayCtrl == null) return;

            try
            {
                TexType texType = (TexType)Enum.Parse(typeof(TexType), texName);
                byte[] texBytes = tex == null ? null : ImageConversion.EncodeToPNG((Texture2D)tex);
                skinOverlayCtrl.SetOverlayTex(texBytes, texType);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                string msg = "Import texture failed: " + e.Message;
                StudioCharaEditor.Logger.Log(LogLevel.Message | LogLevel.Warning, msg);
            }
        }

        public void LoadNewOverlayTexture(ChaControl chaCtrl)
        {
            KoiSkinOverlayController skinOverlayCtrl = getSkinOverlayCtrl(chaCtrl);
            if (skinOverlayCtrl == null) return;

            void OnFileAccept(string[] filenames)
            {
                if (filenames == null || filenames.Length == 0)
                    return;
                CharaEditorUI.ToDoQueue.Enqueue(() => LoadNewOverlayTextureFile(chaCtrl, filenames[0]));
            }

            // open file dialog to load
            OpenFileDialog.Show(strings => OnFileAccept(strings), "Open overlay image", KoiSkinOverlayGui.GetDefaultLoadDir(), KoiSkinOverlayGui.FileFilter, KoiSkinOverlayGui.FileExt);
        }

        public void LoadNewOverlayTextureFile(ChaControl chaCtrl, string filename)
        {
            //KoiSkinOverlayController skinOverlayCtrl = getSkinOverlayCtrl(chaCtrl);
            //if (skinOverlayCtrl == null) return;

            try
            {
                Texture2D tex = new Texture2D(2, 2);
                byte[] bytesToLoad = File.ReadAllBytes(filename);
                tex.LoadImage(bytesToLoad);
                SetSkinOverlayTex(chaCtrl, tex);
            }
            catch (Exception ex)
            {
                StudioCharaEditor.Logger.LogError("Fail to load overlay from " + filename + ": " + ex.Message);
            }
        }

        public void DumpSkinOverlayTexture(ChaControl chaCtrl)
        {
            Texture2D tex = (Texture2D)GetSkinOverlayTex(chaCtrl);
            if (tex == null) return;
            var texCopy = tex.ToTexture2D();
            KoiSkinOverlayGui.WriteAndOpenPng(texCopy.EncodeToPNG(), texName);
            UnityEngine.Object.Destroy(texCopy);
        }
    }

    class ClothOverlayDetailDefine : CharaDetailDefine
    {
        public int clothIndex;
        public bool modified;

        public ClothOverlayDetailDefine(string category2, int index)
        {
            base.Key = CharaEditorController.CT1_CTHS + "#" + category2 + "#Overlay";
            base.Type = CharaDetailDefineType.CLOTH_OVERLAY;
            base.Catelog = CharaDetailDefineCatelog.OVERLAY;
            base.Get = GetClothOverlayTex;
            base.Set = SetClothOverlayTex;
            clothIndex = index;
            modified = false;
        }

        private KoiClothesOverlayController getClothOverlayCtrl(ChaControl chaCtrl)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            if (cec == null || !cec.HasOverlayPlugin)
            {
                return null;
            }
            return cec.ClothOverlayContrller as KoiClothesOverlayController;
        }

        private string getClothId(ChaControl chaCtrl)
        {
            int[] maleIndex = new int[] { 0, 1, 4, 7 };
            if (chaCtrl.sex == 0)
            {
                return KoiClothesOverlayMgr.MainClothesNames[maleIndex[clothIndex]];
            }
            else
            {
                return KoiClothesOverlayMgr.MainClothesNames[clothIndex];
            }
        }

        public object GetClothOverlayTexData(ChaControl chaCtrl)
        {
            KoiClothesOverlayController overlayControll = getClothOverlayCtrl(chaCtrl);
            if (overlayControll == null) return null;

            ClothesTexData texData = overlayControll.GetOverlayTex(getClothId(chaCtrl), false);
            return texData;
        }

        public object GetClothOverlayTex(ChaControl chaCtrl)
        {
            // return type = Texture2D or null
            //return (GetClothOverlayTexData(chaCtrl) as ClothesTexData)?.Texture;
            Texture2D srcTex = (GetClothOverlayTexData(chaCtrl) as ClothesTexData)?.Texture;
            if (srcTex != null)
            {
                Texture2D copyTexture = new Texture2D(srcTex.width, srcTex.height);
                copyTexture.SetPixels(srcTex.GetPixels());
                copyTexture.Apply();
                return copyTexture;
            }
            else
            {
                return null;
            }
        }

        public void SetClothOverlayTex(ChaControl chaCtrl, object tex)
        {
            KoiClothesOverlayController overlayControll = getClothOverlayCtrl(chaCtrl);
            if (overlayControll == null) return;

            var texData = overlayControll.GetOverlayTex(getClothId(chaCtrl), tex != null);
            if (texData == null) return;

            if (tex == null)
            {
                texData.Texture = null;
            }
            else
            {
                Texture2D srcTex = tex as Texture2D;
                Texture2D copyTexture = new Texture2D(srcTex.width, srcTex.height);
                copyTexture.SetPixels(srcTex.GetPixels());
                copyTexture.Apply();
                texData.Texture = copyTexture;
            }
            //texData.Texture = (Texture2D)tex;
            overlayControll.RefreshTexture(getClothId(chaCtrl));
            modified = true;
        }

        public void RefreshClothesTexture(ChaControl chaCtrl)
        {
            KoiClothesOverlayController overlayControll = getClothOverlayCtrl(chaCtrl);
            if (overlayControll == null) return;

            overlayControll.RefreshTexture(getClothId(chaCtrl));
        }

        public void LoadNewOverlayTexture(ChaControl chaCtrl)
        {
            KoiClothesOverlayController overlayControll = getClothOverlayCtrl(chaCtrl);
            if (overlayControll == null) return;

            void OnFileAccept(string[] filenames)
            {
                if (filenames == null || filenames.Length == 0)
                    return;
                CharaEditorUI.ToDoQueue.Enqueue(() => LoadNewOverlayTextureFile(chaCtrl, filenames[0]));
            }

            OpenFileDialog.Show(
                strings => OnFileAccept(strings),
                "Open overlay image",
                KoiSkinOverlayGui.GetDefaultLoadDir(),
                KoiSkinOverlayGui.FileFilter,
                KoiSkinOverlayGui.FileExt
            );
        }

        public void LoadNewOverlayTextureFile(ChaControl chaCtrl, string filename)
        {
            KoiClothesOverlayController overlayControll = getClothOverlayCtrl(chaCtrl);
            if (overlayControll == null) return;

            try
            {
                Texture2D tex = new Texture2D(2, 2);
                byte[] bytesToLoad = File.ReadAllBytes(filename);
                tex.LoadImage(bytesToLoad);
                SetClothOverlayTex(chaCtrl, tex);
            }
            catch (Exception ex)
            {
                StudioCharaEditor.Logger.LogError("Fail to load overlay from " + filename + ": " + ex.Message);
            }
        }

        public void DumpClothOverlayTexture(ChaControl chaCtrl)
        {
            KoiClothesOverlayController overlayControll = getClothOverlayCtrl(chaCtrl);
            if (overlayControll == null) return;

            try
            {
                var tex = overlayControll.GetOverlayTex(getClothId(chaCtrl), false)?.TextureBytes;
                if (tex == null)
                {
                    StudioCharaEditor.Logger.LogMessage("Nothing to export");
                    return;
                }

                KoiSkinOverlayGui.WriteAndOpenPng(tex, getClothId(chaCtrl));
            }
            catch (Exception ex)
            {
                StudioCharaEditor.Logger.LogMessage("Failed to export texture - " + ex.Message);
            }
        }

        public void DumpClothOrignalTexture(ChaControl chaCtrl)
        {
            KoiClothesOverlayController overlayControll = getClothOverlayCtrl(chaCtrl);
            if (overlayControll == null) return;

            overlayControll.DumpBaseTexture(getClothId(chaCtrl), b => KoiSkinOverlayGui.WriteAndOpenPng(b, getClothId(chaCtrl) + "_Original"));
        }
    }

    class PluginOverlayDetailSet
    {
        static public CharaDetailDefine[] BuildSkinOverlayDefine(string category1)
        {
            SkinOverlayDetailDefine over = new SkinOverlayDetailDefine(category1, SkinOverlayDetailDefine.TEXTYPE_OVER);
            SkinOverlayDetailDefine under = new SkinOverlayDetailDefine(category1, SkinOverlayDetailDefine.TEXTYPE_UNDER);
            return new CharaDetailDefine[] { over, under };
        }

        static public CharaDetailDefine[] BuildClothOverlayDefine(string category2, int index)
        {
            CharaDetailDefine sep = new CharaDetailDefine
            {
                Key = CharaEditorController.CT1_CTHS + "#" + category2 + "#Overlay Setting",
                Type = CharaDetailDefine.CharaDetailDefineType.SEPERATOR,
            };

            ClothOverlayDetailDefine ovl = new ClothOverlayDetailDefine(category2, index);

            CharaDetailDefine hbt = new CharaDetailDefine
            {
                Key = CharaEditorController.CT1_CTHS + "#" + category2 + "#Overlay hide base textrue",
                Type = CharaDetailDefine.CharaDetailDefineType.TOGGLE,
                Get = (chaCtrl) => 
                {
                    ClothesTexData texData = (ClothesTexData)ovl.GetClothOverlayTexData(chaCtrl);
                    if (texData != null)
                        return texData.Override;
                    else
                        return false;
                },
                Set = (chaCtrl, v) =>
                {
                    bool en = CharaDetailDefine.ParseBool(v);
                    ClothesTexData texData = (ClothesTexData)ovl.GetClothOverlayTexData(chaCtrl);
                    if (texData != null && texData.Override != en)
                    {
                        texData.Override = en;
                        ovl.RefreshClothesTexture(chaCtrl);
                    }
                },
            };

            return new CharaDetailDefine[] { sep, ovl, hbt };
        }

        static public string[] ClothOverlayUpdateSequenceKey(string category2)
        {
            return new string[] {
                 CharaEditorController.CT1_CTHS + "#" + category2 + "#Overlay",
                 CharaEditorController.CT1_CTHS + "#" + category2 + "#Overlay hide base textrue",
            };
        }
    }
}
