using System;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed partial class NES : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			CDL = cdl;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["RAM"] = new byte[_memoryDomains["RAM"].Size];

			if (_memoryDomains.Has("Save RAM"))
			{
				cdl["Save RAM"] = new byte[_memoryDomains["Save RAM"].Size];
			}

			if (_memoryDomains.Has("Battery RAM"))
			{
				cdl["Battery RAM"] = new byte[_memoryDomains["Battery RAM"].Size];
			}

			if (_memoryDomains.Has("Battery RAM"))
			{
				cdl["Battery RAM"] = new byte[_memoryDomains["Battery RAM"].Size];
			}

			cdl.SubType = "NES";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
		{

		}

		public enum CDLog_AddrType
		{
			None,
			ROM,
			MainRAM,
			SaveRAM,
		}

		[Flags]
		public enum CDLog_Flags
		{
			ExecFirst = 0x01,
			ExecOperand = 0x02,
			Data = 0x04
		}

#pragma warning disable CS0649
		public struct CDLog_MapResults
		{
			public CDLog_AddrType Type;
			public int Address;
		}

		private delegate CDLog_MapResults MpMemoryDelegate(ushort addr, bool write);
#pragma warning restore CS0649
		private ICodeDataLog CDL;

		private void RunCDL(ushort address, CDLog_Flags flags)
		{
			
			CDLog_MapResults results = Board.MapMemory(address, false);
			switch (results.Type)
			{
				case CDLog_AddrType.None: break;
				case CDLog_AddrType.MainRAM: CDL["RAM"][results.Address] |= (byte)flags; break;
				case CDLog_AddrType.SaveRAM: CDL["Save RAM"][results.Address] |= (byte)flags; break;
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