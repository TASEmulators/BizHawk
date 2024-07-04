using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public sealed partial class GGHawkLink : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			CDL = cdl;
			if (cdl == null)
			{
				L.Cpu.ReadMemory = L.ReadMemory;
				L.Cpu.WriteMemory = L.WriteMemory;
				L.Cpu.FetchMemory = L.FetchMemory;
			}
			else
			{
				L.Cpu.ReadMemory = ReadMemory_CDL;
				L.Cpu.WriteMemory = L.WriteMemory;
				L.Cpu.FetchMemory = FetchMemory_CDL;
			}
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			void AddIfExists(string name)
			{
				var found = _memoryDomains[name];
				if (found is not null) cdl[name] = new byte[found.Size];
			}
			cdl["ROM"] = new byte[_memoryDomains["ROM"]!.Size];
			cdl["Main RAM"] = new byte[_memoryDomains["Main RAM"]!.Size];
			AddIfExists("Save RAM");
			AddIfExists("Cart (Volatile) RAM");
			cdl.SubType = "SMS";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

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
		}

		private ICodeDataLog CDL;

		private void RunCDL(ushort address, CDLog_Flags flags)
		{
			if (L.MapMemory != null)
			{
				SMS.CDLog_MapResults results = L.MapMemory(address, false);
				switch (results.Type)
				{
					case SMS.CDLog_AddrType.None: break;
					case SMS.CDLog_AddrType.ROM: CDL["ROM"][results.Address] |= (byte)flags; break;
					case SMS.CDLog_AddrType.MainRAM: CDL["Main RAM"][results.Address] |= (byte)flags; break;
					case SMS.CDLog_AddrType.SaveRAM: CDL["Save RAM"][results.Address] |= (byte)flags; break;
					case SMS.CDLog_AddrType.CartRAM: CDL["Cart (Volatile) RAM"][results.Address] |= (byte)flags; break;
				}
			}
		}

		/// <summary>
		/// A wrapper for FetchMemory which inserts CDL logic
		/// </summary>
		private byte FetchMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.ExecFirst);
			return L.ReadMemory(address);
		}

		/// <summary>
		/// A wrapper for ReadMemory which inserts CDL logic
		/// </summary>
		private byte ReadMemory_CDL(ushort address)
		{
			RunCDL(address, CDLog_Flags.Data);
			return L.ReadMemory(address);
		}
	}
}