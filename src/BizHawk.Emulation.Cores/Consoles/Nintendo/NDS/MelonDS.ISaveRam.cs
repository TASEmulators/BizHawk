using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS : ISaveRam
	{
		private readonly int DSiWareSaveLength;

		public new bool SaveRamModified => IsDSiWare || _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			if (IsDSiWare)
			{
				if (DSiWareSaveLength == 0)
				{
					return null;
				}

				var ret = new byte[DSiWareSaveLength];
				_core.ExportDSiWareSavs(DSiTitleId.Lower, ret);
				return ret;
			}

			var length = _core.GetSaveRamLength();

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetSaveRam(ret);
				return ret;
			}

			return null;
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (IsDSiWare)
			{
				if (data.Length == DSiWareSaveLength)
				{
					_core.ImportDSiWareSavs(DSiTitleId.Lower, data);
				}
			}
			else if (data.Length > 0)
			{
				_core.PutSaveRam(data, (uint)data.Length);
			}
		}
	}
}