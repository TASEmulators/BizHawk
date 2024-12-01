using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			CDL = cdl;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			void AddIfExists(string name)
			{
				var found = MemoryDomains[name];
				if (found is not null) cdl[name] = new byte[found.Size];
			}
			cdl["ROM"] = new byte[MemoryDomains["ROM"]!.Size];
			cdl["Main RAM"] = new byte[MemoryDomains["Main RAM"]!.Size];
			AddIfExists("Save RAM");
			AddIfExists("Cart (Volatile) RAM");
			cdl.SubType = "SMS";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

		public enum CDLog_AddrType
		{
			None,
			ROM, 
			MainRAM, 
			SaveRAM,
			CartRAM, //"Cart (Volatile) RAM" aka ExtRam
		}

		[Flags]
		public enum CDLog_Flags
		{
			ExecFirst = 0x01,
			ExecOperand = 0x02,
			Data = 0x04
		}

		public struct CDLog_MapResults
		{
			public CDLog_AddrType Type;
			public int Address;
		}

		public delegate CDLog_MapResults MapMemoryDelegate(ushort addr, bool write);
		public MapMemoryDelegate MapMemory;
		public ICodeDataLog CDL;

		public void RunCDL(ushort address, CDLog_Flags flags)
		{
			if (MapMemory != null)
			{
				CDLog_MapResults results = MapMemory(address, false);
				switch (results.Type)
				{
					case CDLog_AddrType.None: break;
					case CDLog_AddrType.ROM: CDL["ROM"][results.Address] |= (byte)flags; break;
					case CDLog_AddrType.MainRAM: CDL["Main RAM"][results.Address] |= (byte)flags; break;
					case CDLog_AddrType.SaveRAM: CDL["Save RAM"][results.Address] |= (byte)flags; break;
					case CDLog_AddrType.CartRAM: CDL["Cart (Volatile) RAM"][results.Address] |= (byte)flags; break;
				}
			}
		}

		/// <summary>
		/// A wrapper for FetchMemory which inserts CDL logic
		/// </summary>
		public byte FetchMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.ExecFirst);
			return FetchMemory(address);
		}

		/// <summary>
		/// A wrapper for ReadMemory which inserts CDL logic
		/// </summary>
		public byte ReadMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.Data);
			return ReadMemory(address);
		}
	}
}
