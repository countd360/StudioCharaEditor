using AIChara;

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

}
