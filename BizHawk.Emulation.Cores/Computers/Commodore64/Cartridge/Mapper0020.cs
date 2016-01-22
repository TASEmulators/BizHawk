using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
    // EasyFlash cartridge
    // No official games came on one of these but there
    // are a few dumps from GameBase64 that use this mapper

    // There are 64 banks total, DE00 is bank select.
    // Selecing a bank will select both Lo and Hi ROM.
    // DE02 will switch exrom/game bits: bit 0=game, 
    // bit 1=exrom, bit 2=for our cases, always set true.
    // These two registers are write only.

    // This cartridge always starts up in Ultimax mode,
    // with Game set high and ExRom set low.

    // There is also 256 bytes RAM at DF00-DFFF.

    // We emulate having the AM29F040 chip.

    public abstract partial class CartridgeDevice
    {
        private sealed class Mapper0020 : CartridgeDevice
        {
            private readonly int[][] _banksA = new int[64][]; //8000
            private readonly int[][] _banksB = new int[64][]; //A000
            private int _bankNumber;
            private bool _boardLed;
            private int[] _currentBankA;
            private int[] _currentBankB;
            private bool _jumper = false;
            private int _stateBits;
            private readonly int[] _ram = new int[256];
            private bool _commandLatch55;
            private bool _commandLatchAa;
            private int _internalRomState;

            public Mapper0020(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
            {
                var count = newAddresses.Count;

                // build dummy bank
                var dummyBank = new int[0x2000];
                for (var i = 0; i < 0x2000; i++)
                    dummyBank[i] = 0xFF; // todo: determine if this is correct

                // force ultimax mode (the cart SHOULD set this
                // otherwise on load, according to the docs)
                pinGame = false;
                pinExRom = true;

                // for safety, initialize all banks to dummy
                for (var i = 0; i < 64; i++)
                    _banksA[i] = dummyBank;
                for (var i = 0; i < 64; i++)
                    _banksB[i] = dummyBank;

                // load in all banks
                for (var i = 0; i < count; i++)
                {
                    switch (newAddresses[i])
                    {
                        case 0x8000:
                            _banksA[newBanks[i]] = newData[i];
                            break;
                        case 0xA000:
                        case 0xE000:
                            _banksB[newBanks[i]] = newData[i];
                            break;
                    }
                }

                // default to bank 0
                BankSet(0);

                // internal operation settings
                _commandLatch55 = false;
                _commandLatchAa = false;
                _internalRomState = 0;
            }

            private void BankSet(int index)
            {
                _bankNumber = index & 0x3F;
                UpdateState();
            }

            public override int Peek8000(int addr)
            {
                return _currentBankA[addr];
            }

            public override int PeekA000(int addr)
            {
                return _currentBankB[addr];
            }

            public override int PeekDE00(int addr)
            {
                // normally you can't read these regs
                // but Peek is provided here for debug reasons
                // and may not stay around
                addr &= 0x02;
                return addr == 0x00 ? _bankNumber : _stateBits;
            }

            public override int PeekDF00(int addr)
            {
                return _ram[addr];
            }

            public override void PokeDE00(int addr, int val)
            {
                addr &= 0x02;
                if (addr == 0x00)
                    BankSet(val);
                else
                    StateSet(val);
            }

            public override void PokeDF00(int addr, int val)
            {
                _ram[addr] = val & 0xFF;
            }

            public override int Read8000(int addr)
            {
                return ReadInternal(addr);
            }

            public override int ReadA000(int addr)
            {
                return ReadInternal(addr | 0x2000);
            }

            public override int ReadDF00(int addr)
            {
                return _ram[addr];
            }

            private int ReadInternal(int addr)
            {
                switch (_internalRomState)
                {
                    case 0x80:
                        break;
                    case 0x90:
                        switch (addr & 0x1FFF)
                        {
                            case 0x0000:
                                return 0x01;
                            case 0x0001:
                                return 0xA4;
                            case 0x0002:
                                return 0x00;
                        }
                        break;
                    case 0xA0:
                        break;
                    case 0xF0:
                        break;
                }

                return (addr & 0x3FFF) < 0x2000
                    ? _currentBankA[addr & 0x1FFF]
                    : _currentBankB[addr & 0x1FFF];
            }

            private void StateSet(int val)
            {
                _stateBits = val &= 0x87;
                if ((val & 0x04) != 0)
                    pinGame = ((val & 0x01) == 0);
                else
                    pinGame = _jumper;
                pinExRom = ((val & 0x02) == 0);
                _boardLed = ((val & 0x80) != 0);
                _internalRomState = 0;
                UpdateState();
            }

            private void UpdateState()
            {
                _currentBankA = _banksA[_bankNumber];
                _currentBankB = _banksB[_bankNumber];
            }

            public override void Write8000(int addr, int val)
            {
                WriteInternal(addr, val);
            }

            public override void WriteA000(int addr, int val)
            {
                WriteInternal(addr | 0x2000, val);
            }

            private void WriteInternal(int addr, int val)
            {
                if (pinGame || !pinExRom)
                {
                    return;
                }

                System.Diagnostics.Debug.WriteLine("EasyFlash Write: $" + C64Util.ToHex(addr | 0x8000, 4) + " = " + C64Util.ToHex(val, 2));
                if (val == 0xF0) // any address, resets flash
                {
                    _internalRomState = 0;
                    _commandLatch55 = false;
                    _commandLatchAa = false;
                }
                else if (_internalRomState != 0x00 && _internalRomState != 0xF0)
                {
                    switch (_internalRomState)
                    {
                        case 0xA0:
                            if ((addr & 0x2000) == 0)
                            {
                                addr &= 0x1FFF;
                                _banksA[_bankNumber][addr] = val & 0xFF;
                                _currentBankA[addr] = val & 0xFF;
                            }
                            else
                            {
                                addr &= 0x1FFF;
                                _banksB[_bankNumber][addr] = val & 0xFF;
                                _currentBankB[addr] = val & 0xFF;
                            }
                            break;
                    }
                }
                else if (addr == 0x0555) // $8555
                {
                    if (!_commandLatchAa)
                    {
                        if (val == 0xAA)
                        {
                            _commandLatch55 = true;
                        }
                    }
                    else
                    {
                        // process EZF command
                        _internalRomState = val;
                    }
                }
                else if (addr == 0x02AA) // $82AA
                {
                    if (_commandLatch55 && val == 0x55)
                    {
                        _commandLatchAa = true;
                    }
                    else
                    {
                        _commandLatch55 = false;
                    }
                }
                else
                {
                    _commandLatch55 = false;
                    _commandLatchAa = false;
                }
            }

            public override void WriteDE00(int addr, int val)
            {
                addr &= 0x02;
                if (addr == 0x00)
                    BankSet(val);
                else
                    StateSet(val);
            }

            public override void WriteDF00(int addr, int val)
            {
                _ram[addr] = val & 0xFF;
            }

            public override void SyncState(Serializer ser)
            {
                base.SyncState(ser);
                if (ser.IsReader)
                    UpdateState();
            }
        }
    }
}
