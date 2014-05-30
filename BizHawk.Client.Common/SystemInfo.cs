using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class SystemInfo
	{
		public SystemInfo() { }

		public string DisplayName { get; set; }
		public int ByteSize { get; set; } // For Ram tools, whether it is a 8/16/32 bit system
		public MemoryDomain.Endian Endian { get; set; } //Big endian/little endian, etc

		public static SystemInfo Null
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "",
					ByteSize = 1,
					Endian = MemoryDomain.Endian.Little
				};
			}
		}

		public static SystemInfo Nes
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Nintendo",
					ByteSize = 1,
					Endian = MemoryDomain.Endian.Big
				};
			}
		}
	}
}
