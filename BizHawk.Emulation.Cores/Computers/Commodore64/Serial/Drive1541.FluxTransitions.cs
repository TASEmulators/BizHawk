using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
    public sealed partial class Drive1541
    {
        private const long LEHMER_RNG_PRIME = 48271;

        private int _diskDensityCounter; // density .. 16
        private int _diskSupplementaryCounter; // 0 .. 16
        private bool _diskFluxReversalDetected;
        private int _diskBitsLeft;
        private int _diskByteOffset;
        private int _diskBits;
        private int _diskCycle;
        private int _diskDensity;
        private bool _previousCa1;
        private int _countsBeforeRandomTransition;
        private int _rngCurrent;

        // Lehmer RNG
        private void AdvanceRng()
        {
            if (_rngCurrent == 0)
                _rngCurrent = 1;
            _rngCurrent = (int)(_rngCurrent * LEHMER_RNG_PRIME % int.MaxValue);
        }

        private void ExecuteFlux()
        {
            for (_diskCycle = 0; _diskCycle < 16; _diskCycle++)
            {
                // rotate disk
                if (_motorEnabled)
                {
                    if (_diskBitsLeft <= 0)
                    {
                        _diskByteOffset++;
                        if (_diskByteOffset == Disk.FLUX_ENTRIES_PER_TRACK)
                        {
                            _diskByteOffset = 0;
                        }
                        _diskBits = _trackImageData[_diskByteOffset];
                        _diskBitsLeft = Disk.FLUX_BITS_PER_ENTRY;
                    }
                    if ((_diskBits & 1) != 0)
                    {
                        _countsBeforeRandomTransition = 0;
                        _diskFluxReversalDetected = true;
                    }
                    _diskBits >>= 1;
                    _diskBitsLeft--;
                }

                // random flux transition readings for unformatted data
                if (_countsBeforeRandomTransition > 0)
                {
                    _countsBeforeRandomTransition--;
                    if (_countsBeforeRandomTransition == 0)
                    {
                        _diskFluxReversalDetected = true;
                        AdvanceRng();
                        // This constant is what VICE uses. TODO: Determine accuracy.
                        _countsBeforeRandomTransition = (_rngCurrent % 367) + 33;
                    }
                }

                // flux transition circuitry
                if (_diskFluxReversalDetected)
                {
                    _diskDensityCounter = _diskDensity;
                    _diskSupplementaryCounter = 0;
                    _diskFluxReversalDetected = false;
                    if (_countsBeforeRandomTransition == 0)
                    {
                        AdvanceRng();
                        // This constant is what VICE uses. TODO: Determine accuracy.
                        _countsBeforeRandomTransition = (_rngCurrent & 0x1F) + 289;
                    }
                }

                // counter circuitry
                if (_diskDensityCounter >= 16)
                {
                    _diskDensityCounter = _diskDensity;
                    _diskSupplementaryCounter++;
                    if ((_diskSupplementaryCounter & 0x3) == 0x2)
                    {
                        _byteReady = false;
                        _bitsRemainingInLatchedByte--;
                        if (_bitsRemainingInLatchedByte <= 0)
                        {
                            _bitsRemainingInLatchedByte = 8;

                            // SOE (sync output enabled)
                            _byteReady = Via1.Ca2;
                        }

                        _bitHistory = (_bitHistory << 1) | ((_diskSupplementaryCounter & 0xC) == 0x0 ? 1 : 0);
                        _sync = false;
                        if (Via1.Cb2 && (_bitHistory & 0x3FF) == 0x3FF)
                        {
                            _sync = true;
                            _bitsRemainingInLatchedByte = 8;
                            _byteReady = false;
                        }

                        // negative transition activates SO pin on CPU
                        _previousCa1 = Via1.Ca1;
                        Via1.Ca1 = !_byteReady;
                        if (_previousCa1 && !Via1.Ca1)
                        {
                            // cycle 6 is roughly 400ns
                            _overflowFlagDelaySr |= _diskCycle > 6 ? 4 : 2;
                        }
                    }
                }

                if (_diskSupplementaryCounter >= 16)
                {
                    _diskSupplementaryCounter = 0;
                }

                _diskDensityCounter++;
            }
        }
    }
}
