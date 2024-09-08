using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// ZXHawk: Core Class
	/// * ICodeDataLog *
	/// </summary>
	public partial class ZXSpectrum : ICodeDataLogger
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
				var found = memoryDomains[name];
				if (found is not null) cdl[name] = new byte[found.Size];
			}

			cdl["System Bus"] = new byte[memoryDomains["System Bus"]!.Size];

			AddIfExists("ROM - 128K Editor & Menu");
			AddIfExists("ROM - 128K Syntax Checker");
			AddIfExists("ROM - +3DOS");
			AddIfExists("ROM - 48K BASIC");

			// different RAM bank ordering for < 128k models
			if (_machineType == MachineType.ZXSpectrum16 || _machineType == MachineType.ZXSpectrum48)
			{
				AddIfExists("RAM - BANK 0 (Screen)");
				AddIfExists("RAM - BANK 1");
				AddIfExists("RAM - BANK 2");
			}
			else
			{
				AddIfExists("RAM - BANK 5 (Screen)");
				AddIfExists("RAM - BANK 2");
				AddIfExists("RAM - BANK 0");
				AddIfExists("RAM - BANK 1");
				AddIfExists("RAM - BANK 3");
				AddIfExists("RAM - BANK 4");
				AddIfExists("RAM - BANK 6");
				AddIfExists("RAM - BANK 7 (Shadow Screen)");
			}

			cdl.SubType = "ZXSpectrum";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

		public enum CDLType
		{
			None,
			ROM0, ROM1, ROM2, ROM3,
			RAM0, RAM1, RAM2, RAM3,
			RAM4, RAM5, RAM6, RAM7
		}

		public struct CDLResult
		{
			public CDLType Type;
			public int Address;
		}

		private byte ReadMemory_CDL(ushort addr)
		{
			var mapping = _machine.ReadCDL(addr);
			var res = mapping.Type;
			var address = mapping.Address;

			byte data = _machine.ReadMemory(addr);

			switch (res)
			{
				case CDLType.None:
				default:
					break;

				case CDLType.ROM0:
					switch (_machineType)
					{
						case MachineType.ZXSpectrum16:
						case MachineType.ZXSpectrum48:
							_cdl["ROM - 48K BASIC"][address] = data;
							break;
						default:
							_cdl["ROM - 128K Editor & Menu"][address] = data;
							break;
					}
					break;

				case CDLType.ROM1:
					switch (_machineType)
					{
						case MachineType.ZXSpectrum128:
						case MachineType.ZXSpectrum128Plus2:
							_cdl["ROM - 48K BASIC"][address] = data;
							break;
						case MachineType.ZXSpectrum128Plus2a:
						case MachineType.ZXSpectrum128Plus3:
							_cdl["ROM - 128K Syntax Checker"][address] = data;
							break;
					}
					break;

				case CDLType.ROM2:
					_cdl["ROM - +3DOS"][address] = data;
					break;

				case CDLType.ROM3:
					_cdl["ROM - 48K BASIC"][address] = data;
					break;

				case CDLType.RAM0:
					switch (_machineType)
					{
						case MachineType.ZXSpectrum16:
						case MachineType.ZXSpectrum48:
							_cdl["RAM - BANK 0 (Screen)"][address] = data;
							break;
						default:
							_cdl["RAM - BANK 0"][address] = data;
							break;
					}
					break;

				case CDLType.RAM1:
					_cdl["RAM - BANK 1"][address] = data;
					break;

				case CDLType.RAM2:
					_cdl["RAM - BANK 2"][address] = data;
					break;

				case CDLType.RAM3:
					_cdl["RAM - BANK 3"][address] = data;
					break;

				case CDLType.RAM4:
					_cdl["RAM - BANK 4"][address] = data;
					break;

				case CDLType.RAM5:
					_cdl["RAM - BANK 5 (Screen)"][address] = data;
					break;

				case CDLType.RAM6:
					_cdl["RAM - BANK 6"][address] = data;
					break;

				case CDLType.RAM7:
					_cdl["RAM - BANK 7 (Shadow Screen)"][address] = data;
					break;
			}

			// update the system bus as well
			// because why not
			_cdl["System Bus"][addr] = data;

			return data;
		}
	}
}
