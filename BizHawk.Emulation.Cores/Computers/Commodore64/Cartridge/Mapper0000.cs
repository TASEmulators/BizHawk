using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
    public abstract partial class CartridgeDevice
    {
        private sealed class Mapper0000 : CartridgeDevice
        {
            [SaveState.DoNotSave]
            private readonly int[] _romA;
            [SaveState.SaveWithName("RomMaskA")]
            private readonly int _romAMask;
            [SaveState.DoNotSave]
            private readonly int[] _romB;
            [SaveState.SaveWithName("RomMaskB")]
            private readonly int _romBMask;

            // standard cartridge mapper (Commodore)
            // note that this format also covers Ultimax carts

            public Mapper0000(IList<int> newAddresses, IList<int[]> newData, bool game, bool exrom)
            {
                pinGame = game;
                pinExRom = exrom;

                validCartridge = true;

                // default to empty banks
                _romA = new int[1];
                _romB = new int[1];
                _romA[0] = 0xFF;
                _romB[0] = 0xFF;

                for (var i = 0; i < newAddresses.Count; i++)
                {
                    if (newAddresses[i] == 0x8000)
                    {
                        switch (newData[i].Length)
                        {
                            case 0x1000:
                                _romAMask = 0x0FFF;
                                _romA = newData[i];
                                break;
                            case 0x2000:
                                _romAMask = 0x1FFF;
                                _romA = newData[i];
                                break;
                            case 0x4000:
                                _romAMask = 0x1FFF;
                                _romBMask = 0x1FFF;
                                // split the rom into two banks
                                _romA = new int[0x2000];
                                _romB = new int[0x2000];
                                Array.Copy(newData[i], 0x0000, _romA, 0x0000, 0x2000);
                                Array.Copy(newData[i], 0x2000, _romB, 0x0000, 0x2000);
                                break;
                            default:
                                validCartridge = false;
                                return;
                        }
                    }
                    else if (newAddresses[i] == 0xA000 || newAddresses[i] == 0xE000)
                    {
                        switch (newData[i].Length)
                        {
                            case 0x1000:
                                _romBMask = 0x0FFF;
                                break;
                            case 0x2000:
                                _romBMask = 0x1FFF;
                                break;
                            default:
                                validCartridge = false;
                                return;
                        }
                        _romB = newData[i];
                    }
                }
            }

            public override int Peek8000(int addr)
            {
                return _romA[addr & _romAMask];
            }

            public override int PeekA000(int addr)
            {
                return _romB[addr & _romBMask];
            }

            public override int Read8000(int addr)
            {
                return _romA[addr & _romAMask];
            }

            public override int ReadA000(int addr)
            {
                return _romB[addr & _romBMask];
            }
        }
    }
}
