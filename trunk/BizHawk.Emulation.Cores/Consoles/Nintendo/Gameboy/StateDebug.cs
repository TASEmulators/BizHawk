using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/*
	 * the new gambatte savestater includes functionality that
	 * could be used to make structured text mode savestates. this module just does a bit of
	 * sanity checking using that capability
	 */

	public class StateDebug
	{
		Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();

		string Path = "/";

		List<string> SaveHistory = new List<string>();
		int LoadHistoryPos = 0;

		public void Save(IntPtr data, int length, string name)
		{
			byte[] d = new byte[length];
			Marshal.Copy(data, d, 0, length);
			string s = Path + name;
			SaveHistory.Add(s);
			if (Data.ContainsKey(s))
				throw new Exception("Already stored");
			Data[s] = d;
		}

		public void Load(IntPtr data, int length, string name)
		{
			string s = Path + name;
			byte[] d = Data[s];
			if (SaveHistory[LoadHistoryPos++] != s)
				throw new Exception("Loading out of order!");
			Marshal.Copy(d, 0, data, length);
		}

		public void EnterSection(string name)
		{
			Path = Path + name + '/';
		}

		public void ExitSection(string name)
		{
			int i = Path.Substring(0, Path.Length - 1).LastIndexOf('/');
			if (i < 0)
				throw new Exception("Couldn't unwind stack!");
			string newPath = Path.Substring(0, i + 1);
			string unwind = Path.Substring(0, Path.Length - 1).Substring(i + 1);
			if (unwind != name)
				throw new Exception("Left wrong section!");
			Path = newPath;
		}
	}
}
