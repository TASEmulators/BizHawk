using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public sealed partial class VectrexHawk : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			CDL = cdl;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["RAM"] = new byte[MemoryDomains["Main RAM"]!.Size];
			cdl["ROM"] = new byte[MemoryDomains["ROM"]!.Size];

			cdl.SubType = "VEC";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

		private enum CDLog_AddrType
		{
			None,
			RAM,
			ROM,			
		}

		[Flags]
		private enum CDLog_Flags
		{
			ExecFirst = 0x01,
			ExecOperand = 0x02,
			Data = 0x04
		}

#pragma warning disable CS0649
		private struct CDLog_MapResults
		{
			public CDLog_AddrType Type;
			public int Address;
		}

		private delegate CDLog_MapResults MapMemoryDelegate(ushort addr, bool write);
		private MapMemoryDelegate MapMemory;
#pragma warning restore CS0649
		private ICodeDataLog CDL;

		private void RunCDL(ushort address, CDLog_Flags flags)
		{
			if (MapMemory != null)
			{
				CDLog_MapResults results = MapMemory(address, false);
				switch (results.Type)
				{
					case CDLog_AddrType.None: break;
					case CDLog_AddrType.RAM: CDL["RAM"][results.Address] |= (byte)flags; break;
					case CDLog_AddrType.ROM: CDL["ROM"][results.Address] |= (byte)flags; break;
				}
			}
		}

		/// <summary>
		/// A wrapper for FetchMemory which inserts CDL logic
		/// </summary>
		private byte FetchMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.ExecFirst);
			return PeekMemory(address);
		}

		/// <summary>
		/// A wrapper for ReadMemory which inserts CDL logic
		/// </summary>
		private byte ReadMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.Data);
			return ReadMemory(address);
		}
	}
}