using BizHawk.Emulation.Common;
using System.IO;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : ICodeDataLogger
	{
		private ICodeDataLog _cdl;

		public void SetCDL(ICodeDataLog cdl)
		{
			_cdl = cdl;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			void AddIfExists(string name)
			{
				var found = _memoryDomains[name];
				if (found is not null) cdl[name] = new byte[found.Size];
			}

			cdl["System Bus"] = new byte[_memoryDomains["System Bus"]!.Size];

			AddIfExists("BIOS1");
			AddIfExists("BIOS2");
			AddIfExists("ROM");

			cdl.SubType = "ChannelF";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

		public enum CDLType
		{
			None,
			BIOS1, 
			BIOS2, 
			CARTROM
		}

		public struct CDLResult
		{
			public CDLType Type;
			public int Address;
		}

		private byte ReadMemory_CDL(ushort addr)
		{
			var mapping = ReadCDL(addr);
			var res = mapping.Type;
			var address = mapping.Address;

			byte data = ReadBus(addr);

			switch (res)
			{
				case CDLType.None:
				default:
					// shouldn't get here
					break;

				case CDLType.BIOS1:
					_cdl["BIOS1"][address] = data;
					break;

				case CDLType.BIOS2:
					_cdl["BIOS2"][address] = data;
					break;

				case CDLType.CARTROM:
					_cdl["ROM"][address] = data;
					break;
			}

			// update the system bus as well
			// because why not
			_cdl["System Bus"][addr] = data;

			return data;
		}
	}
}
