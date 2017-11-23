using System;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			CDL = cdl;
			if (cdl == null)
			{
				Cpu.ReadMemory = ReadMemory;
				Cpu.WriteMemory = WriteMemory;
				Cpu.FetchMemory = FetchMemory;
			}
			else
			{
				Cpu.ReadMemory = ReadMemory_CDL;
				Cpu.WriteMemory = WriteMemory;
				Cpu.FetchMemory = FetchMemory_CDL;
			}
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["ROM"] = new byte[MemoryDomains["ROM"].Size];
			cdl["Main RAM"] = new byte[MemoryDomains["Main RAM"].Size];

			if (MemoryDomains.Has("Save RAM"))
			{
				cdl["Save RAM"] = new byte[MemoryDomains["Save RAM"].Size];
			}

			if (MemoryDomains.Has("Cart (Volatile) RAM"))
			{
				cdl["Cart (Volatile) RAM"] = new byte[MemoryDomains["Cart (Volatile) RAM"].Size];
			}

			cdl.SubType = "SMS";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
		{

		}

		private enum CDLog_AddrType
		{
			None,
			ROM, 
			MainRAM, 
			SaveRAM,
			CartRAM, //"Cart (Volatile) RAM" aka ExtRam
		}

		[Flags]
		private enum CDLog_Flags
		{
			ExecFirst = 0x01,
			ExecOperand = 0x02,
			Data = 0x04
		};

		private struct CDLog_MapResults
		{
			public CDLog_AddrType Type;
			public int Address;
		}

		private delegate CDLog_MapResults MapMemoryDelegate(ushort addr, bool write);
		private MapMemoryDelegate MapMemory;
		private ICodeDataLog CDL;

		private void RunCDL(ushort address, CDLog_Flags flags)
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
		private byte FetchMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.ExecFirst);
			return ReadMemory(address);
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