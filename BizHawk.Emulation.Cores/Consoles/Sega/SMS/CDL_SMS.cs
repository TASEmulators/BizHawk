using System;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	partial class SMS
	{
		enum CDLog_AddrType
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
		};

		struct CDLog_MapResults
		{
			public CDLog_AddrType Type;
			public int Address;
		}

		delegate CDLog_MapResults MapMemoryDelegate(ushort addr, bool write);

		MapMemoryDelegate MapMemory;

		void RunCDL(ushort address, CDLog_Flags flags)
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
		public byte FetchMemory_CDL(ushort address, bool first)
		{
			RunCDL(address, first ? CDLog_Flags.ExecFirst : CDLog_Flags.ExecOperand);
			return ReadMemory(address);
		}

		/// <summary>
		/// A wrapper for ReadMemory which inserts CDL logic
		/// </summary>
		public byte ReadMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.Data);
			return ReadMemory(address);
		}

		void ICodeDataLogger.SetCDL(CodeDataLog cdl)
		{
			CDL = cdl;
			if (cdl == null)
			{
				Cpu.ReadMemory = ReadMemory;
				Cpu.WriteMemory = WriteMemory;
				Cpu.FetchMemory = FetchMemory_StubThunk;
			}
			else
			{
				Cpu.ReadMemory = ReadMemory_CDL;
				Cpu.WriteMemory = WriteMemory;
				Cpu.FetchMemory = FetchMemory_CDL;
			}
		}

		void ICodeDataLogger.NewCDL(CodeDataLog cdl)
		{
			cdl["ROM"] = new byte[memoryDomains["ROM"].Size];
			cdl["Main RAM"] = new byte[memoryDomains["Main RAM"].Size];

		if (memoryDomains.Has("Save RAM"))
			cdl["Save RAM"] = new byte[memoryDomains["Save RAM"].Size];

		if (memoryDomains.Has("Cart (Volatile) RAM"))
			cdl["Cart (Volatile) RAM"] = new byte[memoryDomains["Cart (Volatile) RAM"].Size];

			cdl.SubType = "SMS";
			cdl.SubVer = 0;
		}

		//not supported
		void ICodeDataLogger.DisassembleCDL(Stream s, CodeDataLog cdl) { }

		CodeDataLog CDL;
	}
}