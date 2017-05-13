using System;
using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	internal static class SaveState
	{
		public class DoNotSave : Attribute
		{
		}

		public class SaveWithName : Attribute
		{
			public string Name { get; set; }

			public SaveWithName(string name)
			{
				Name = name;
			}
		}

		private static int[] GetDelta(IList<int> source, IList<int> data)
		{
			var length = Math.Min(source.Count, data.Count);
			var delta = new int[length];
			for (var i = 0; i < length; i++)
			{
				delta[i] = source[i] ^ data[i];
			}
			return delta;
		}

		public static void SyncDelta(string name, Serializer ser, int[] source, ref int[] data)
		{
			int[] delta = null;
			if (ser.IsWriter && data != null)
			{
				delta = GetDelta(source, data);
			}
			ser.Sync(name, ref delta, false);
			if (ser.IsReader && delta != null)
			{
				data = GetDelta(source, delta);
			}
		}
	}
}
