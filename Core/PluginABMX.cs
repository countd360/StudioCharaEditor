using AIChara;
using KKABMX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioCharaEditor
{
    class CharaABMXDetailDefine1 : CharaDetailDefine
    {
        [Flags]
        public enum ABMXSliderTarget
        {
            Scale = 0x0100,
            Offset = 0x0200,
            Tilt = 0x0400,
            Length = 0x0800,

            X = 0x0001,
            Y = 0x0002,
            Z = 0x0004,
        };

        public static readonly ABMXSliderTarget[] SLIDERRARGETS_SXYZ_L =
        {
            ABMXSliderTarget.Scale | ABMXSliderTarget.X,
            ABMXSliderTarget.Scale | ABMXSliderTarget.Y,
            ABMXSliderTarget.Scale | ABMXSliderTarget.Z,
            ABMXSliderTarget.Length,
        };
        public static readonly ABMXSliderTarget[] SLIDERRARGETS_SXZ =
        {
            ABMXSliderTarget.Scale | ABMXSliderTarget.X,
            ABMXSliderTarget.Scale | ABMXSliderTarget.Z,
        };

        public string BoneName;
        //public new CharaDetailDefineType Type = CharaDetailDefineType.ABMXSET1;
        public string[] SubSlidersNames = { "Scale X", "Scale Y", "Scale Z", };
        public ABMXSliderTarget[] SubSliderTargets = { 
            ABMXSliderTarget.Scale | ABMXSliderTarget.X, 
            ABMXSliderTarget.Scale | ABMXSliderTarget.Y,
            ABMXSliderTarget.Scale | ABMXSliderTarget.Z,
        };

        public CharaABMXDetailDefine1()
        {
            base.Type = CharaDetailDefineType.ABMXSET1;
            base.Catelog = CharaDetailDefineCatelog.ABMX;
            base.Get = (chaCtrl) => { return this.ABMXGetSingle(chaCtrl); };
            base.Set = (chaCtrl, v) => { this.ABMXSetSingle(chaCtrl, (float[])v); };
        }

        public float[] ABMXGetSingle(ChaControl chaCtrl)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            BoneController bc = cec.BoneController as BoneController;
            return GetABMXDataSet(bc, BoneName, SubSliderTargets);
        }

        public void ABMXSetSingle(ChaControl chaCtrl, float[] value)
        {
            /*
            Console.WriteLine("ABMXSetSingle to bone:" + BoneName);
            for (int i = 0; i < SubSlidersNames.Length; i ++)
            {
                Console.WriteLine(" {0} : {1}", SubSlidersNames[i], value[i]);
            }
            */
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            BoneController bc = cec.BoneController as BoneController;
            SetABMXDataSet(bc, BoneName, SubSliderTargets, value);
        }

        public static float[] GetABMXDataSet(BoneController boneCtrl, string boneName, ABMXSliderTarget[] dataTarget)
        {
            BoneModifier bm = boneCtrl.GetModifier(boneName);
            BoneModifierData bdata = bm != null ? bm.GetModifier(boneCtrl.CurrentCoordinate.Value) : null;

            float[] dataArray = new float[dataTarget.Length];
            for (int i = 0; i < dataTarget.Length; i ++)
            {
                switch (dataTarget[i])
                {
                    case ABMXSliderTarget.Scale | ABMXSliderTarget.X:
                        dataArray[i] = bdata != null ? bdata.ScaleModifier.x : 1.0f;
                        break;
                    case ABMXSliderTarget.Scale | ABMXSliderTarget.Y:
                        dataArray[i] = bdata != null ? bdata.ScaleModifier.y : 1.0f;
                        break;
                    case ABMXSliderTarget.Scale | ABMXSliderTarget.Z:
                        dataArray[i] = bdata != null ? bdata.ScaleModifier.z : 1.0f;
                        break;
                    case ABMXSliderTarget.Length:
                        dataArray[i] = bdata != null ? bdata.LengthModifier : 1.0f;
                        break;
                    default:
                        throw new ArgumentException("Unknown type of ABMXSliderTarget for bone: " + boneName);
                }
            }
            return dataArray;
        }

        public static void SetABMXDataSet(BoneController boneCtrl, string boneName, ABMXSliderTarget[] dataTarget, float[] value)
        {
            BoneModifier bm = boneCtrl.GetModifier(boneName);
            if (bm == null)
            {
                // check input
                bool newModifier = false;
                foreach (float v in value)
                {
                    if (v != 1)
                    {
                        newModifier = true;
                        break;
                    }
                }
                if (newModifier)
                {
                    // build on demand
                    bm = new BoneModifier(boneName);
                    boneCtrl.AddModifier(bm);
                    if (bm.BoneTransform == null)
                        throw new Exception("Transform not found for bone: " + boneName);
                }
                else
                {
                    return;
                }
            }
            BoneModifierData bdata = bm.GetModifier(boneCtrl.CurrentCoordinate.Value);
            for (int i = 0; i < dataTarget.Length; i ++)
            {
                switch (dataTarget[i])
                {
                    case ABMXSliderTarget.Scale | ABMXSliderTarget.X:
                        bdata.ScaleModifier.x = value[i];
                        break;
                    case ABMXSliderTarget.Scale | ABMXSliderTarget.Y:
                        bdata.ScaleModifier.y = value[i];
                        break;
                    case ABMXSliderTarget.Scale | ABMXSliderTarget.Z:
                        bdata.ScaleModifier.z = value[i];
                        break;
                    case ABMXSliderTarget.Length:
                        bdata.LengthModifier = value[i];
                        break;
                    default:
                        throw new ArgumentException("Unknown type of ABMXSliderTarget for bone: " + boneName);
                }
            }
        }
    }

    class CharaABMXDetailDefine2 : CharaABMXDetailDefine1
    {
        //public new CharaDetailDefineType Type = CharaDetailDefineType.ABMXSET2;
        public string[] targetNames = { "Both", "Left", "Right" };
        public int curTargetIndex = 0;

        public CharaABMXDetailDefine2()
        {
            base.Type = CharaDetailDefineType.ABMXSET2;
            base.Get = (chaCtrl) => { return this.ABMXGetSymmetric(chaCtrl); };
            base.Set = (chaCtrl, v) => { this.ABMXSetSymmetric(chaCtrl, (float[][])v); };
        }

        public float[][] ABMXGetSymmetric(ChaControl chaCtrl)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            BoneController bc = cec.BoneController as BoneController;
            float[] ldata = GetABMXDataSet(bc, BoneName + "_L", SubSliderTargets);
            float[] rdata = GetABMXDataSet(bc, BoneName + "_R", SubSliderTargets);
            return new float[][] { ldata, rdata };
        }

        public void ABMXSetSymmetric(ChaControl chaCtrl, float[][] value)
        {
            /*
            Console.WriteLine("ABMXSetSymmetric to bone:" + BoneName);
            for (int i = 0; i < SubSlidersNames.Length; i++)
            {
                Console.WriteLine("{0} {1} : {2}", i == 0 ? "left " : "     ", SubSlidersNames[i], value[0][i]);
            }
            for (int i = 0; i < SubSlidersNames.Length; i++)
            {
                Console.WriteLine("{0} {1} : {2}", i == 0 ? "right" : "     ", SubSlidersNames[i], value[1][i]);
            }
            */
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            BoneController bc = cec.BoneController as BoneController;
            if (curTargetIndex == 0 || curTargetIndex == 1)
            {
                SetABMXDataSet(bc, BoneName + "_L", SubSliderTargets, value[0]);
            }
            if (curTargetIndex == 0 || curTargetIndex == 2)
            {
                SetABMXDataSet(bc, BoneName + "_R", SubSliderTargets, value[1]);
            }
        }
    }

    class CharaABMXDetailDefine3 : CharaABMXDetailDefine2
    {
        public string[] fingerNames = { "All", "1", "2", "3", "4", "5" };
        public int curFingerIndex = 0;
        public string[] segmentNames = { "Base", "Center", "Tip" };
        public int curSegmentIndex = 0;
        public readonly string[] fingerBoneNameBase = { "cf_J_Hand_Thumb", "cf_J_Hand_Index", "cf_J_Hand_Middle", "cf_J_Hand_Ring", "cf_J_Hand_Little" };
        public readonly string[] fingerSegmentBase = { "01", "02", "03" };

        public CharaABMXDetailDefine3()
        {
            base.Type = CharaDetailDefineType.ABMXSET3;
            base.Get = (chaCtrl) => { return this.ABMXGetFinger(chaCtrl); };
            base.Set = (chaCtrl, v) => { this.ABMXSetFinger(chaCtrl, (float[][][][])v); };
        }

        public float[][][][] ABMXGetFinger(ChaControl chaCtrl)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            BoneController bc = cec.BoneController as BoneController;

            float[][] ldata1 = new float[][] { 
                GetABMXDataSet(bc, fingerBoneNameBase[0] + fingerSegmentBase[0] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[0] + fingerSegmentBase[1] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[0] + fingerSegmentBase[2] + "_L", SubSliderTargets),
            };
            float[][] ldata2 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[1] + fingerSegmentBase[0] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[1] + fingerSegmentBase[1] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[1] + fingerSegmentBase[2] + "_L", SubSliderTargets),
            };
            float[][] ldata3 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[2] + fingerSegmentBase[0] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[2] + fingerSegmentBase[1] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[2] + fingerSegmentBase[2] + "_L", SubSliderTargets),
            };
            float[][] ldata4 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[3] + fingerSegmentBase[0] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[3] + fingerSegmentBase[1] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[3] + fingerSegmentBase[2] + "_L", SubSliderTargets),
            };
            float[][] ldata5 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[4] + fingerSegmentBase[0] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[4] + fingerSegmentBase[1] + "_L", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[4] + fingerSegmentBase[2] + "_L", SubSliderTargets),
            };
            float[][][] ldata = new float[][][] { ldata1, ldata2, ldata3, ldata4, ldata5 };

            float[][] rdata1 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[0] + fingerSegmentBase[0] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[0] + fingerSegmentBase[1] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[0] + fingerSegmentBase[2] + "_R", SubSliderTargets),
            };
            float[][] rdata2 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[1] + fingerSegmentBase[0] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[1] + fingerSegmentBase[1] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[1] + fingerSegmentBase[2] + "_R", SubSliderTargets),
            };
            float[][] rdata3 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[2] + fingerSegmentBase[0] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[2] + fingerSegmentBase[1] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[2] + fingerSegmentBase[2] + "_R", SubSliderTargets),
            };
            float[][] rdata4 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[3] + fingerSegmentBase[0] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[3] + fingerSegmentBase[1] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[3] + fingerSegmentBase[2] + "_R", SubSliderTargets),
            };
            float[][] rdata5 = new float[][] {
                GetABMXDataSet(bc, fingerBoneNameBase[4] + fingerSegmentBase[0] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[4] + fingerSegmentBase[1] + "_R", SubSliderTargets),
                GetABMXDataSet(bc, fingerBoneNameBase[4] + fingerSegmentBase[2] + "_R", SubSliderTargets),
            };
            float[][][] rdata = new float[][][] { rdata1, rdata2, rdata3, rdata4, rdata5 };

            return new float[][][][] { ldata, rdata };
        }

        public void ABMXSetFinger(ChaControl chaCtrl, float[][][][] value)
        {
            CharaEditorController cec = CharaEditorMgr.Instance.GetEditorController(chaCtrl);
            BoneController bc = cec.BoneController as BoneController;

            for (int lr = 0; lr < 2; lr ++)
            {
                if (curTargetIndex != 0 && curTargetIndex - 1 != lr)
                {
                    continue;
                }
                string tgtHand = lr == 0 ? "_L" : "_R";

                for (int f = 0; f < 5; f ++)
                {
                    if (curFingerIndex != 0 && curFingerIndex - 1 != f)
                    {
                        continue;
                    }

                    string tgtbone = fingerBoneNameBase[f] + fingerSegmentBase[curSegmentIndex] + tgtHand;
                    SetABMXDataSet(bc, tgtbone, SubSliderTargets, value[lr][f][curSegmentIndex]);
                }
            }
        }
    }

    class AMBXSettingDetailSet
    {
        public static CharaDetailDefine[] Details =
        {
            #region BODY
            // Body#ShapeWhole
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeWhole#ABMX Body",
                BoneName = "cf_N_height",
            },
            // Body#ShapeBreast
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Breast 1",
                BoneName = "cf_J_Mune00_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Breast 2",
                BoneName = "cf_J_Mune01_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Breast 3",
                BoneName = "cf_J_Mune02_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Breast Tip",
                BoneName = "cf_J_Mune03_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Areola",
                BoneName = "cf_J_Mune04_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Protrusion", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Nipple",
                BoneName = "cf_J_Mune_Nip01_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Nipple Tip",
                BoneName = "cf_J_Mune_Nip02_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeBreast#ABMX Breast Collision",
                BoneName = "cf_hit_Mune02_s",
            },
            // Body#ShapeUpper
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeUpper#ABMX Neck",
                BoneName = "cf_J_Neck_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeUpper#ABMX Shoulder",
                BoneName = "cf_J_Shoulder02_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Shape", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeUpper#ABMX Upper Torso",
                BoneName = "cf_J_Spine03_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeUpper#ABMX Middle Torso",
                BoneName = "cf_J_Spine02_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeUpper#ABMX Lower Torso",
                BoneName = "cf_J_Spine01_s",
            },
            // Body#ShapeLower
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLower#ABMX Ass",
                BoneName = "cf_J_Siri_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Position", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLower#ABMX Ass Collision",
                BoneName = "cf_hit_Siri_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeLower#ABMX Pelvis",
                BoneName = "cf_J_Kosi01_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeLower#ABMX Hips",
                BoneName = "cf_J_Kosi02_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeLower#ABMX Pelvis & Legs",
                BoneName = "cf_J_Kosi01",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeLower#ABMX Hips & Legs",
                BoneName = "cf_J_Kosi02",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeLower#ABMX Genital Area",
                BoneName = "cf_J_Kokan",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Body#ShapeLower#ABMX Anus",
                BoneName = "cf_J_Ana",
            },
            // Body#ShapeArm
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Whole Arm",
                BoneName = "cf_J_ArmUp00",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Offset", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Upper Arm Deltoids",
                BoneName = "cf_J_ArmUp01_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Upper Arm Triceps",
                BoneName = "cf_J_ArmUp02_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Upper Arm Lower",
                BoneName = "cf_J_ArmUp03_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Elbow",
                BoneName = "cf_J_ArmElboura_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Elbow Cap",
                BoneName = "cf_J_ArmElbo_low_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Forearm Upper",
                BoneName = "cf_J_ArmLow01_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Forearm Lower",
                BoneName = "cf_J_ArmLow02_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Wrist",
                BoneName = "cf_J_Hand_Wrist_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeArm#ABMX Hand",
                BoneName = "cf_J_Hand_s",
            },
            new CharaABMXDetailDefine3
            {
                Key = "Body#ShapeArm#ABMX Finger",
            },
            // Body#ShapeLeg
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Whole Leg",
                BoneName = "cf_J_LegUp00",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Offset", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Outer Upper Thigh",
                BoneName = "cf_J_LegUpDam_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Outer Upper Thigh",
                BoneName = "cf_J_LegUpDam_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Upper Thigh",
                BoneName = "cf_J_LegUp01_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Center Thigh",
                BoneName = "cf_J_LegUp02_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Lower Thigh",
                BoneName = "cf_J_LegUp03_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Kneecap",
                BoneName = "cf_J_LegKnee_low_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Kneecap Back",
                BoneName = "cf_J_LegKnee_back_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Upper Calve",
                BoneName = "cf_J_LegLow01_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Center Calve",
                BoneName = "cf_J_LegLow02_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Lower Calve",
                BoneName = "cf_J_LegLow03_s",
                SubSlidersNames = new string[] { "Scale X", "Scale Z", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXZ,
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Foot 1",
                BoneName = "cf_J_Foot01",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Foot 2",
                BoneName = "cf_J_Foot02",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Body#ShapeLeg#ABMX Foot Toes",
                BoneName = "cf_J_Toes01",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Offset", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            #endregion
            #region FACE
            // Face#ShapeWhole
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeWhole#ABMX Head",
                BoneName = "cf_J_FaceBase",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Position", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeWhole#ABMX Head + Neck",
                BoneName = "cf_J_Head_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeWhole#ABMX Lower Head Cheek",
                BoneName = "cf_J_FaceLow_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeWhole#ABMX Upper Head",
                BoneName = "cf_J_FaceUp_ty",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeWhole#ABMX Upper Front Head",
                BoneName = "cf_J_FaceUp_tz",
            },
            // Face#ShapeChin
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeChin#ABMX Jaw",
                BoneName = "cf_J_Chin_rs",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Offset", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeChin#ABMX Chin",
                BoneName = "cf_J_ChinLow",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeChin#ABMX Chin Tip",
                BoneName = "cf_J_ChinTip_s",
            },
            // Face#ShapeCheek
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeCheek#ABMX Upper Cheek",
                BoneName = "cf_J_CheekUp",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeCheek#ABMX Lower Cheek",
                BoneName = "cf_J_CheekLow",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Offset", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            // Face#ShapeEyebrow
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeEyebrow#ABMX Eyebrow",
                BoneName = "cf_J_Mayu",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeEyebrow#ABMX Inner Eyebrow",
                BoneName = "cf_J_MayuMid_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeEyebrow#ABMX Outer Eyebrow",
                BoneName = "cf_J_MayuTip_s",
            },
            // Face#ShapeEyes
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeEyes#ABMX Eye",
                BoneName = "cf_J_Eye_s",
            },
            // Face#ShapeNose
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeNose#ABMX Nose + Bridge",
                BoneName = "cf_J_NoseBase_trs",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeNose#ABMX Nose",
                BoneName = "cf_J_NoseBase_s",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeNose#ABMX Nose Tip",
                BoneName = "cf_J_Nose_tip",
                SubSlidersNames = new string[] { "Scale X", "Scale Y", "Scale Z", "Offset", },
                SubSliderTargets = CharaABMXDetailDefine1.SLIDERRARGETS_SXYZ_L,
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeNose#ABMX Nose Bridge",
                BoneName = "cf_J_NoseBridge_t",
            },
            // Face#ShapeMouth
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeMouth#ABMX Mouth",
                BoneName = "cf_J_MouthBase_tr",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeMouth#ABMX Lip",
                BoneName = "cf_J_MouthMove",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeMouth#ABMX Mouth Side",
                BoneName = "cf_J_Mouth",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeMouth#ABMX Upper Lip",
                BoneName = "cf_J_Mouthup",
            },
            new CharaABMXDetailDefine1
            {
                Key = "Face#ShapeMouth#ABMX Lower Lip",
                BoneName = "cf_J_MouthLow",
            },
            // Face#ShapeEar
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeEar#ABMX Ear",
                BoneName = "cf_J_EarBase_s",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeEar#ABMX Upper Ear",
                BoneName = "cf_J_EarUp",
            },
            new CharaABMXDetailDefine2
            {
                Key = "Face#ShapeEar#ABMX Lower Ear",
                BoneName = "cf_J_EarLow",
            },
            #endregion
        };
    }
}
