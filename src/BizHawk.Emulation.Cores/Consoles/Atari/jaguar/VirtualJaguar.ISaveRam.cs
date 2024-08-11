using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public partial class VirtualJaguar : ISaveRam
	{
		private readonly int _saveRamSize;

		public new bool SaveRamModified => _saveRamSize > 0 && _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			if (_saveRamSize == 0)
			{
				return null;
			}

			byte[] ret = new byte[_saveRamSize];
			_core.GetSaveRam(ret);
			return ret;
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (_saveRamSize > 0)
			{
				if (data.Length != _saveRamSize)
				{
					throw new ArgumentException(message: "buffer wrong size", paramName: nameof(data));
				}

				_core.PutSaveRam(data);
			}
		}
	}
}
