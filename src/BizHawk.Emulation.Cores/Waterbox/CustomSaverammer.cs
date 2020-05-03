using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Waterbox
{
	internal class CustomSaverammer : ISaveRam
	{
		private readonly ICustomSaveram _s;
		private readonly int _size;


		public CustomSaverammer(ICustomSaveram s)
		{
			_s = s;
			_size = s.GetSaveramSize();
		}

		public bool SaveRamModified => _size > 0;

		public byte[] CloneSaveRam()
		{
			var ret = new byte[_size];
			_s.GetSaveram(ret, ret.Length);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Length != _size)
				throw new InvalidOperationException("Wrong size saveram");
			_s.PutSaveram(data, data.Length);
		}
	}
}
