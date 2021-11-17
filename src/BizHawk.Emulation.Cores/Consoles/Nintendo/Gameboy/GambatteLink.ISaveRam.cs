using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ISaveRam
	{
		public bool SaveRamModified => LinkedSaveRamModified();

		private bool LinkedSaveRamModified()
		{
			for (int i = 0; i < _numCores; i++)
			{
				if (_linkedCores[i].SaveRamModified)
				{
					return true;
				}
			}
			return false;
		}

		public byte[] CloneSaveRam()
		{
			var linkedBuffers = new List<byte[]>();
			int len = 0;
			for (int i = 0; i < _numCores; i++)
			{
				linkedBuffers.Add(_linkedCores[i].CloneSaveRam()!);
				len += linkedBuffers[i].Length;
			}
			byte[] ret = new byte[len];
			int pos = 0;
			for (int i = 0; i < _numCores; i++)
			{
				Buffer.BlockCopy(linkedBuffers[i], 0, ret, pos, linkedBuffers[i].Length);
				pos += linkedBuffers[i].Length;
			}
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			int pos = 0;
			for (int i = 0; i < _numCores; i++)
			{
				var b = new byte[_linkedCores[i].CloneSaveRam()!.Length];
				Buffer.BlockCopy(data, pos, b, 0, b.Length);
				pos += b.Length;
				_linkedCores[i].StoreSaveRam(b);
			}
		}
	}
}
