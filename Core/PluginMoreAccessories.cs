using AIChara;
using CharaCustom;
using HarmonyLib;
using System;
using UnityEngine;

namespace StudioCharaEditor
{
	public class AccessoryInfo
    {
		public int slotNo;
		public int category;
		public string AccKey;
		public string AccName;
		public ListInfoBase accInfo;
		public CmpAccessory accCmp;
		public ChaFileAccessory.PartsInfo partsInfo;
		public ChaFileAccessory.PartsInfo orgPartsInfo;

		public bool IsEmptySlot
        {
			get
            {
				return accInfo == null;
			}
        }

		public bool IsVanillaSlot
        {
			get
            {
				return slotNo < PluginMoreAccessories.VANILLA_ACC_NUM;
			}
        }

		public AccessoryInfo(ChaControl chaCtrl, int index)
        {
			slotNo = index;
			AccKey = slotNo.ToString();

			UpdateAccessoryInfo(chaCtrl);
		}

		public void UpdateAccessoryInfo(ChaControl chaCtrl)
        {
			// basic info
			accInfo = PluginMoreAccessories.GetAccessoryInfo(chaCtrl, slotNo);
			accCmp = PluginMoreAccessories.GetAccessoryCmp(chaCtrl, slotNo);
			partsInfo = PluginMoreAccessories.GetPartsInfo(chaCtrl, slotNo);
			category = IsEmptySlot ? (int)ChaListDefine.CategoryNo.ao_none : accInfo.Category;
			orgPartsInfo = IsVanillaSlot ? chaCtrl.chaFile.coordinate.accessory.parts[slotNo] : null;

			// name of acc
			AccName = string.Format("{0:D2}: ", (slotNo+1)) + (IsEmptySlot ? "---" : accInfo.Name);
		}
	}

    public static class PluginMoreAccessories
    {
		public static readonly int VANILLA_ACC_NUM = 20;

		public static bool HasMoreAccessories = false;
		public static Traverse MoreAccessoryTraverse = null;

		private static Func<ChaFile, object> _getAdditionData;

		private static Func<ChaControl, int, ChaFileAccessory.PartsInfo> _getPartsInfo;

		private static Func<ChaControl, int, ListInfoBase> _getAccessoryInfo;

		private static Func<ChaControl, int, CmpAccessory> _getChaAccessoryCmp;

		private static Action _addOneSlot;
		private static Action _addTenSlots;

		// static methods

		public static bool DetectMoreAccessories()
		{
			try
			{
				var mc = Type.GetType("MoreAccessoriesAI.MoreAccessories, MoreAccessories", false);
				return mc != null;
			}
			catch (Exception data)
			{
				StudioCharaEditor.Logger.LogWarning("Failed to detect MoreAccessories!");
				StudioCharaEditor.Logger.LogDebug(data);
				return false;
			}
		}

		public static void Initialize()
        {
			HasMoreAccessories = false;
			if (!DetectMoreAccessories())
            {
				return;
            }
			try
            {
				// manager object traverse
				Traverse maCls = Traverse.CreateWithType("MoreAccessoriesAI.MoreAccessories, MoreAccessories");
				Traverse maMgrField = maCls.Field("_self");
				if (!maMgrField.FieldExists())
				{
					throw new InvalidOperationException("_self not found in MoreAccessories");
                }
				MoreAccessoryTraverse = Traverse.Create(maMgrField.GetValue());

				// GetAdditionalDataByCharacter method
				Traverse mGADBC = MoreAccessoryTraverse.Method("GetAdditionalDataByCharacter", new Type[] { typeof(ChaFile) }, null);
				if (mGADBC.MethodExists())
				{
					_getAdditionData = ((ChaFile chaFile) => mGADBC.GetValue<object>(new object[] { chaFile }));
				}
				else
                {
					throw new InvalidOperationException("Can not get GetAdditionalDataByCharacter from MoreAccessories");
                }

				// AddOneSlot method
				Traverse mAOS = MoreAccessoryTraverse.Method("AddOneSlot", new Type[] { }, null);
				if (mAOS.MethodExists())
				{
					_addOneSlot = (() => mAOS.GetValue());
				}
				else
				{
					throw new InvalidOperationException("Can not get AddOneSlot from MoreAccessories");
				}

				// AddTenSlots method
				Traverse mATS = MoreAccessoryTraverse.Method("AddTenSlots", new Type[] { }, null);
				if (mAOS.MethodExists())
				{
					_addTenSlots = (() => mATS.GetValue());
				}
				else
				{
					throw new InvalidOperationException("Can not get AddTenSlots from MoreAccessories");
				}

				// patch traverse
				Traverse patch_traverse = Traverse.CreateWithType("MoreAccessoriesAI.Patches.ChaControl_Patches, MoreAccessories");

				// GetCmpAccessory function
				Traverse mGca = patch_traverse.Method("GetCmpAccessory", new Type[]
				{
						typeof(ChaControl),
						typeof(int)
				}, null);
				if (!mGca.MethodExists())
				{
					throw new InvalidOperationException("Failed to find MoreAccessoriesAI.Patches.ChaControl_Patches.GetCmpAccessory");
				}
				_getChaAccessoryCmp = ((ChaControl control, int componentIndex) => mGca.GetValue<CmpAccessory>(new object[]
				{
						control,
						componentIndex
				}));

				// GetPartsInfo function
				Traverse mGpi = patch_traverse.Method("GetPartsInfo", new Type[]
				{
						typeof(ChaControl),
						typeof(int)
				}, null);
				if (!mGpi.MethodExists())
				{
					throw new InvalidOperationException("Failed to find MoreAccessoriesAI.Patches.ChaControl_Patches.GetPartsInfo");
				}
				_getPartsInfo = ((ChaControl control, int i) => mGpi.GetValue<ChaFileAccessory.PartsInfo>(new object[]
				{
						control,
						i
				}));

				// GetInfoAccessory function
				Traverse mGai = patch_traverse.Method("GetInfoAccessory", new Type[]
				{
						typeof(ChaControl),
						typeof(int)
				}, null);
				if (!mGai.MethodExists())
				{
					throw new InvalidOperationException("Failed to find MoreAccessoriesAI.Patches.ChaControl_Patches.GetInfoAccessory");
				}
				_getAccessoryInfo = ((ChaControl control, int i) => mGai.GetValue<ListInfoBase>(new object[]
				{
						control,
						i
				}));


				// Done
				HasMoreAccessories = true;
			}
			catch (Exception e)
            {
				Console.WriteLine(e.Message);
				StudioCharaEditor.Logger.LogWarning("Failed to set up MoreAccessories integration!");
				StudioCharaEditor.Logger.LogDebug(e);
			}
		}

		public static int GetAccessoryCount(ChaControl chaCtrl)
        {
			if (!HasMoreAccessories || _getAdditionData == null)
            {
				return VANILLA_ACC_NUM;
            }
			else
            {
				var additionData = _getAdditionData(chaCtrl.chaFile);
				if (additionData == null)
				{
					throw new Exception("Can not get additional data");
				}

				Traverse addDataTra = Traverse.Create(additionData);
				Traverse objsTra = addDataTra.Field("objects");
				if (!objsTra.FieldExists())
                {
					throw new Exception("Can not get objects from additional data");
                }

				Traverse objsObjTra = Traverse.Create(objsTra.GetValue());
				Traverse countTra = objsObjTra.Property("Count");
				if (!countTra.PropertyExists())
                {
					throw new Exception("Can not get count for additionaldata.objects");
                }

				return VANILLA_ACC_NUM + (int)countTra.GetValue();
			}
        }

		public static bool GetAccessoryVisible(ChaControl chaCtrl, int accIndex)
        {
			// vanilla parts
			if (accIndex < VANILLA_ACC_NUM)
            {
				return chaCtrl.fileStatus.showAccessory[accIndex];
			}

			// addition parts
			if (!HasMoreAccessories || _getAdditionData == null)
			{
				throw new Exception("MoreAccessories plugin not valid for additional accessory.");
			}
			else
			{
				var additionData = _getAdditionData(chaCtrl.chaFile);
				if (additionData == null)
				{
					throw new Exception("Can not get additional data");
				}

				Traverse addDataTra = Traverse.Create(additionData);
				Traverse objsTra = addDataTra.Field("objects");
				if (!objsTra.FieldExists())
				{
					throw new Exception("Can not get objects from additional data");
				}

				Traverse objsObjTra = Traverse.Create(objsTra.GetValue());
				Traverse elmAtTra = objsObjTra.Method("get_Item", new Type[] { typeof(int) }, null);
				if (!elmAtTra.MethodExists())
				{
					Console.WriteLine("List methods for additionaldata.objects");
					foreach (string m in objsObjTra.Methods())
					{
						Console.WriteLine(m);
					}
					Console.WriteLine("List field for additionaldata.objects");
					foreach (string m in objsObjTra.Fields())
					{
						Console.WriteLine(m);
					}
					Console.WriteLine("List properties for additionaldata.objects");
					foreach (string m in objsObjTra.Properties())
					{
						Console.WriteLine(m);
					}
					throw new Exception("Can not get get_Item for additionaldata.objects");
				}

				Traverse accObjTra = Traverse.Create(elmAtTra.GetValue(new object[] { accIndex - VANILLA_ACC_NUM }));
				Traverse showTra = accObjTra.Field("show");
				if (!showTra.FieldExists())
                {
					throw new Exception("Can not get show for additionaldata.objects[index]");
				}

				return (bool)showTra.GetValue();
			}
		}

		public static void SetAccessoryVisible(ChaControl chaCtrl, int accIndex, bool visible)
		{
			// vanilla parts
			if (accIndex < VANILLA_ACC_NUM)
			{
				chaCtrl.fileStatus.showAccessory[accIndex] = visible;
				return;
			}

			// addition parts
			if (!HasMoreAccessories || _getAdditionData == null)
			{
				throw new Exception("MoreAccessories plugin not valid for additional accessory.");
			}
			else
			{
				var additionData = _getAdditionData(chaCtrl.chaFile);
				if (additionData == null)
				{
					throw new Exception("Can not get additional data");
				}

				Traverse addDataTra = Traverse.Create(additionData);
				Traverse objsTra = addDataTra.Field("objects");
				if (!objsTra.FieldExists())
				{
					throw new Exception("Can not get objects from additional data");
				}

				Traverse objsObjTra = Traverse.Create(objsTra.GetValue());
				Traverse elmAtTra = objsObjTra.Method("get_Item", new Type[] { typeof(int) }, null);
				if (!elmAtTra.MethodExists())
				{
					Console.WriteLine("List methods for additionaldata.objects");
					foreach (string m in objsObjTra.Methods())
					{
						Console.WriteLine(m);
					}
					Console.WriteLine("List field for additionaldata.objects");
					foreach (string m in objsObjTra.Fields())
					{
						Console.WriteLine(m);
					}
					Console.WriteLine("List properties for additionaldata.objects");
					foreach (string m in objsObjTra.Properties())
					{
						Console.WriteLine(m);
					}
					throw new Exception("Can not get get_Item for additionaldata.objects");
				}

				Traverse accObjTra = Traverse.Create(elmAtTra.GetValue(new object[] { accIndex - VANILLA_ACC_NUM }));
				Traverse showTra = accObjTra.Field("show");
				if (!showTra.FieldExists())
				{
					throw new Exception("Can not get show for additionaldata.objects[index]");
				}

				showTra.SetValue(visible);
			}
		}

		public static ListInfoBase GetAccessoryInfo(ChaControl character, int index)
        {
			if (_getAccessoryInfo != null)
            {
				return _getAccessoryInfo(character, index);
            }
			else if (index < VANILLA_ACC_NUM)
            {
				return character.infoAccessory[index];
			}
			else
            {
				throw new InvalidOperationException("Accessory index out of range and MoreAccessories not found!");
            }
        }

		public static CmpAccessory GetAccessoryCmp(ChaControl character, int index)
		{
			if (_getChaAccessoryCmp != null)
			{
				return _getChaAccessoryCmp(character, index);
			}
			else if (index < VANILLA_ACC_NUM)
            {
				return character.cmpAccessory[index];
			}
			else
            {
				throw new InvalidOperationException("Accessory index out of range and MoreAccessories not found!");
			}
		}

		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl character, int index)
		{
			if (_getPartsInfo != null)
            {
				return _getPartsInfo(character, index);
			}
			else if (index < VANILLA_ACC_NUM)
			{
				return character.nowCoordinate.accessory.parts[index];
			}
			else
            {
				throw new InvalidOperationException("Accessory index out of range and MoreAccessories not found!");
			}
		}

		public static bool GetAccessoryDefaultColor(ref Color color, ref float gloss, ref float metallic, ChaControl character, int slotNo, int no)
		{
			if (slotNo < VANILLA_ACC_NUM)
			{
				return character.GetAccessoryDefaultColor(ref color, ref gloss, ref metallic, slotNo, no);
			}
			CmpAccessory accessory = GetAccessoryCmp(character, slotNo);
			if (null == accessory)
			{
				return false;
			}
			if (no == 0 && accessory.useColor01)
			{
				color = accessory.defColor01;
				gloss = accessory.defGlossPower01;
				metallic = accessory.defMetallicPower01;
				return true;
			}
			if (1 == no && accessory.useColor02)
			{
				color = accessory.defColor02;
				gloss = accessory.defGlossPower02;
				metallic = accessory.defMetallicPower02;
				return true;
			}
			if (2 == no && accessory.useColor03)
			{
				color = accessory.defColor03;
				gloss = accessory.defGlossPower03;
				metallic = accessory.defMetallicPower03;
				return true;
			}
			if (3 == no && accessory.rendAlpha != null && accessory.rendAlpha.Length != 0)
			{
				color = accessory.defColor04;
				gloss = accessory.defGlossPower04;
				metallic = accessory.defMetallicPower04;
				return true;
			}
			return false;
		}

		public static void AddOneAccessorySlot(ChaControl chaCtrl)
        {
			if (!HasMoreAccessories || _addOneSlot == null)
            {
				return;
            }

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
					StudioCharaEditor.Logger.LogError("Fail to add accessory slot.");
					return;
                }
			}

			// add
			try
            {
				Singleton<CustomBase>.Instance.chaCtrl = chaCtrl;
				_addOneSlot();
			}
			catch (Exception ex)
			{
				Console.WriteLine("This is an expected exception for calling AddOneSlot in studio: " + ex.Message);
			}

			// remove CustomBase??
		}

		public static void AddTenAccessorySlots(ChaControl chaCtrl)
		{
			if (!HasMoreAccessories || _addTenSlots == null)
			{
				return;
			}

			// check and init CustomBase
			if (Singleton<CustomBase>.Instance == null)
			{
				try
				{
					CustomBase dummyCustomBase = CharaEditorMgr.Instance.gameObject.AddComponent<CustomBase>();
				}
				catch (NullReferenceException ex)
				{
					Console.WriteLine("This is an expected exception for creating a CustomBase in studio: " + ex.Message);
				}

				// re-check
				if (Singleton<CustomBase>.Instance == null)
				{
					StudioCharaEditor.Logger.LogError("Fail to add accessory slot.");
					return;
				}
			}

			// add
			try
			{
				Singleton<CustomBase>.Instance.chaCtrl = chaCtrl;
				_addTenSlots();
			}
			catch (Exception ex)
			{
				Console.WriteLine("This is an expected exception for calling AddTenSlots in studio: " + ex.Message);
			}

			// remove CustomBase??
		}
	}

}
