using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public partial class VirtualJaguar : ISaveRam
	{
		private readonly int _saveRamSize;

		public new bool SaveRamModified => _saveRamSize > 0 && _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam(bool clearDirty)
		{
			if (_saveRamSize == 0)
			{
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			}

			byte[] ret = new byte[_saveRamSize];
			_core.GetSaveRam(ret, clearDirty);
			return ret;
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (_saveRamSize == 0)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			else
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
