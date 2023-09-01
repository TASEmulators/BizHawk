using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic linked implementation of ISaveRam that can be used by any link core
	/// </summary>
	/// <seealso cref="ISaveRam" />
	public class LinkedSaveRam : ISaveRam
	{
		private readonly IEmulator[] _linkedCores;
		private readonly int _numCores;

		public LinkedSaveRam(IEmulator[] linkedCores, int numCores)
		{
			_linkedCores = linkedCores;
			_numCores = numCores;
		}

		public bool SaveRamModified => LinkedSaveRamModified();

		private bool LinkedSaveRamModified()
		{
			for (int i = 0; i < _numCores; i++)
			{
				if (_linkedCores[i].AsSaveRam().SaveRamModified)
				{
					return true;
				}
			}
			return false;
		}

		public byte[] CloneSaveRam()
		{
			List<byte[]> linkedBuffers = new();
			int len = 0;
			for (int i = 0; i < _numCores; i++)
			{
				linkedBuffers.Add(_linkedCores[i].AsSaveRam().CloneSaveRam()!);
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
				byte[] b = new byte[_linkedCores[i].AsSaveRam().CloneSaveRam()!.Length];
				Buffer.BlockCopy(data, pos, b, 0, b.Length);
				pos += b.Length;
				_linkedCores[i].AsSaveRam().StoreSaveRam(b);
			}
		}
	}
}
