using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// ZXHawk: Core Class
    /// * Memory Domains *
    /// </summary>
    public partial class ZXSpectrum
    {
        internal IMemoryDomains memoryDomains;
        private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
        private bool _memoryDomainsInit = false;

        private void SetupMemoryDomains()
        {
            var domains = new List<MemoryDomain>
            {
                new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
                (addr) =>
                {
                    if (addr < 0 || addr >= 65536)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    return _machine.ReadBus((ushort)addr);
                },
                (addr, value) =>
                {
                    if (addr < 0 || addr >= 65536)
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    _machine.WriteBus((ushort)addr, value);
                }, 1)
            };

            SyncAllByteArrayDomains();

            memoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList());
            (ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(memoryDomains);

            _memoryDomainsInit = true;
        }

        private void SyncAllByteArrayDomains()
        {
            switch (_machineType)
            {
                case MachineType.ZXSpectrum16:
                    SyncByteArrayDomain("ROM - 48K BASIC", _machine.ROM0);
                    SyncByteArrayDomain("RAM - BANK 0 (Screen)", _machine.RAM0);
                    break;
                case MachineType.ZXSpectrum48:
                    SyncByteArrayDomain("ROM - 48K BASIC", _machine.ROM0);
                    SyncByteArrayDomain("RAM - BANK 0 (Screen)", _machine.RAM0);
                    SyncByteArrayDomain("RAM - BANK 1", _machine.RAM1);
                    SyncByteArrayDomain("RAM - BANK 2", _machine.RAM2);
                    break;

                case MachineType.ZXSpectrum128:
                case MachineType.ZXSpectrum128Plus2:
                    SyncByteArrayDomain("ROM - 128K Editor & Menu", _machine.ROM0);
                    SyncByteArrayDomain("ROM - 48K BASIC", _machine.ROM1);
                    SyncByteArrayDomain("RAM - BANK 5 (Screen)", _machine.RAM5);
                    SyncByteArrayDomain("RAM - BANK 2", _machine.RAM2);
                    SyncByteArrayDomain("RAM - BANK 0", _machine.RAM0);
                    SyncByteArrayDomain("RAM - BANK 1", _machine.RAM1);
                    SyncByteArrayDomain("RAM - BANK 3", _machine.RAM3);
                    SyncByteArrayDomain("RAM - BANK 4", _machine.RAM4);
                    SyncByteArrayDomain("RAM - BANK 6", _machine.RAM6);
                    SyncByteArrayDomain("RAM - BANK 7 (Shadow Screen)", _machine.RAM7);
                    break;

                case MachineType.ZXSpectrum128Plus2a:
                case MachineType.ZXSpectrum128Plus3:
                    SyncByteArrayDomain("ROM - 128K Editor & Menu", _machine.ROM0);
                    SyncByteArrayDomain("ROM - 128K Syntax Checker", _machine.ROM1);
                    SyncByteArrayDomain("ROM - +3DOS", _machine.ROM2);
                    SyncByteArrayDomain("ROM - 48K BASIC", _machine.ROM3);
                    SyncByteArrayDomain("RAM - BANK 5 (Screen)", _machine.RAM5);
                    SyncByteArrayDomain("RAM - BANK 2", _machine.RAM2);
                    SyncByteArrayDomain("RAM - BANK 0", _machine.RAM0);
                    SyncByteArrayDomain("RAM - BANK 1", _machine.RAM1);
                    SyncByteArrayDomain("RAM - BANK 3", _machine.RAM3);
                    SyncByteArrayDomain("RAM - BANK 4", _machine.RAM4);
                    SyncByteArrayDomain("RAM - BANK 6", _machine.RAM6);
                    SyncByteArrayDomain("RAM - BANK 7 (Shadow Screen)", _machine.RAM7);
                    break;
            }
            /*
            SyncByteArrayDomain("ROM0", _machine.ROM0);
            SyncByteArrayDomain("ROM1", _machine.ROM1);
            SyncByteArrayDomain("ROM2", _machine.ROM2);
            SyncByteArrayDomain("ROM3", _machine.ROM3);
            SyncByteArrayDomain("RAM0", _machine.RAM0);
            SyncByteArrayDomain("RAM1", _machine.RAM1);
            SyncByteArrayDomain("RAM2", _machine.RAM2);
            SyncByteArrayDomain("RAM3", _machine.RAM3);
            SyncByteArrayDomain("RAM4", _machine.RAM4);
            SyncByteArrayDomain("RAM5", _machine.RAM5);
            SyncByteArrayDomain("RAM6", _machine.RAM6);
            SyncByteArrayDomain("RAM7", _machine.RAM7);  
            */
        }

        private void SyncByteArrayDomain(string name, byte[] data)
        {
            
            if (_memoryDomainsInit || _byteArrayDomains.ContainsKey(name))
            {
                var m = _byteArrayDomains[name];
                m.Data = data;
            }
            else
            {
                var m = new MemoryDomainByteArray(name, MemoryDomain.Endian.Little, data, true, 1);
                _byteArrayDomains.Add(name, m);
            }
            
        }
    }
}
