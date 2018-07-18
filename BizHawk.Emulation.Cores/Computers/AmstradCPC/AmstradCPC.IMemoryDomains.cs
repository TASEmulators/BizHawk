using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * Memory Domains *
    /// </summary>
    public partial class AmstradCPC
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
            SyncByteArrayDomain("ROMLower", _machine.ROMLower);
            SyncByteArrayDomain("ROM0", _machine.ROM0);
            SyncByteArrayDomain("ROM7", _machine.ROM7);
            SyncByteArrayDomain("RAM0", _machine.RAM0);
            SyncByteArrayDomain("RAM1", _machine.RAM1);
            SyncByteArrayDomain("RAM2", _machine.RAM2);
            SyncByteArrayDomain("RAM3", _machine.RAM3);
            SyncByteArrayDomain("RAM4", _machine.RAM4);
            SyncByteArrayDomain("RAM5", _machine.RAM5);
            SyncByteArrayDomain("RAM6", _machine.RAM6);
            SyncByteArrayDomain("RAM7", _machine.RAM7);
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
