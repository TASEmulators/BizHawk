using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Jellyfish.Library;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public sealed class DiskIIController : PeripheralCard
    {
        public DiskIIController(Machine machine) : 
            base(machine)
        {
            Drive1 = new DiskIIDrive(machine);
            Drive2 = new DiskIIDrive(machine);

            Drives = new Collection<DiskIIDrive> { Drive1, Drive2 };

            BootDrive = Drive1;
        }

        public override void Initialize()
        {
            StorageService.LoadResource("Roms/DiskII.rom", stream => stream.ReadBlock(_romRegionC1C7));
        }

        public override void Reset()
        {
            _phaseStates = 0;
            SetMotorOn(false);
            SetDriveNumber(0);
            _loadMode = false;
            _writeMode = false;
        }

        public override void LoadState(BinaryReader reader, Version version)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            _latch = reader.ReadInt32();
            _phaseStates = reader.ReadInt32();
            _motorOn = reader.ReadBoolean();
            _driveNumber = reader.ReadInt32();
            _loadMode = reader.ReadBoolean();
            _writeMode = reader.ReadBoolean();
            _driveSpin = reader.ReadBoolean();
            foreach (var drive in Drives)
            {
                DebugService.WriteMessage("Loading machine '{0}'", drive.GetType().Name);
                drive.LoadState(reader, version);
                //DebugService.WriteMessage("Loaded machine '{0}'", drive.GetType().Name);
            }
        }

        public override void SaveState(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.Write(_latch);
            writer.Write(_phaseStates);
            writer.Write(_motorOn);
            writer.Write(_driveNumber);
            writer.Write(_loadMode);
            writer.Write(_writeMode);
            writer.Write(_driveSpin);
            foreach (var drive in Drives)
            {
                DebugService.WriteMessage("Saving machine '{0}'", drive.GetType().Name);
                drive.SaveState(writer);
                //DebugService.WriteMessage("Saved machine '{0}'", drive.GetType().Name);
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public override int ReadIoRegionC0C0(int address)
        {
            switch (address & 0xF)
            {
                case 0x0: case 0x1: case 0x2: case 0x3: case 0x4: case 0x5: case 0x6: case 0x7:
                    SetPhase(address);
                    break;

                case 0x8:
                    SetMotorOn(false);
                    break;

                case 0x9:
                    SetMotorOn(true);
                    break;

                case 0xA:
                    SetDriveNumber(0);
                    break;

                case 0xB:
                    SetDriveNumber(1);
                    break;

                case 0xC:
                    _loadMode = false;
                    if (_motorOn)
                    {
                        if (!_writeMode)
                        {
                            return _latch = Drives[_driveNumber].Read();
                        }
                        else
                        {
                            WriteLatch();
                        }
                    }
                    break;

                case 0xD:
                    _loadMode = true;
                    if (_motorOn && !_writeMode)
                    {
                        // write protect is forced if phase 1 is on [F9.7]
                        _latch &= 0x7F;
                        if (Drives[_driveNumber].IsWriteProtected || 
                            (_phaseStates & Phase1On) != 0)
                        {
                            _latch |= 0x80;
                        }
                    }
                    break;

                case 0xE:
                    _writeMode = false;
                    break;

                case 0xF:
                    _writeMode = true;
                    break;
            }

            if ((address & 1) == 0)
            {
                // only even addresses return the latch
                if (_motorOn)
                {
                    return _latch;
                }

                // simple hack to fool DOS SAMESLOT drive spin check (usually at $BD34)
                _driveSpin = !_driveSpin;
                return _driveSpin ? 0x7E : 0x7F;
            }

            return ReadFloatingBus();
        }

        public override int ReadIoRegionC1C7(int address)
        {
            return _romRegionC1C7[address & 0xFF];
        }

        public override void WriteIoRegionC0C0(int address, int data)
        {
            switch (address & 0xF)
            {
                case 0x0: case 0x1: case 0x2: case 0x3: case 0x4: case 0x5: case 0x6: case 0x7:
                    SetPhase(address);
                    break;

                case 0x8:
                    SetMotorOn(false);
                    break;

                case 0x9:
                    SetMotorOn(true);
                    break;

                case 0xA: 
                    SetDriveNumber(0);
                    break;

                case 0xB:
                    SetDriveNumber(1);
                    break;

                case 0xC:
                    _loadMode = false;
                    if (_writeMode)
                    {
                        WriteLatch();
                    }
                    break;

                case 0xD:
                    _loadMode = true;
                    break;

                case 0xE:
                    _writeMode = false;
                    break;

                case 0xF:
                    _writeMode = true;
                    break;
            }

            if (_motorOn && _writeMode)
            {
                if (_loadMode)
                {
                    // any address writes latch for sequencer LD; OE1/2 irrelevant ['323 datasheet]
                    _latch = data;
                }
            }
        }

        private void WriteLatch()
        {
            // write protect is forced if phase 1 is on [F9.7]
            if ((_phaseStates & Phase1On) == 0)
            {
                Drives[_driveNumber].Write(_latch);
            }
        }

        private void Flush()
        {
            Drives[_driveNumber].FlushTrack();
        }

        private void SetDriveNumber(int driveNumber)
        {
            if (_driveNumber != driveNumber)
            {
                Flush();
                _driveNumber = driveNumber;
            }
        }

        private void SetMotorOn(bool state)
        {
            if (_motorOn && !state)
            {
                Flush();
            }
            _motorOn = state;
        }

        private void SetPhase(int address)
        {
            int phase = (address >> 1) & 0x3;
            int state = address & 1;
            _phaseStates &= ~(1 << phase);
            _phaseStates |= (state << phase);

            if (_motorOn)
            {
                Drives[_driveNumber].ApplyPhaseChange(_phaseStates);
            }
        }

        public DiskIIDrive Drive1 { get; private set; }
        public DiskIIDrive Drive2 { get; private set; }

        public Collection<DiskIIDrive> Drives { get; private set; }

        public DiskIIDrive BootDrive { get; private set; }

        private const int Phase0On = 1 << 0;
        private const int Phase1On = 1 << 1;
        private const int Phase2On = 1 << 2;
        private const int Phase3On = 1 << 3;

        private int _latch;
        private int _phaseStates;
        private bool _motorOn;
        private int _driveNumber;
        private bool _loadMode;
        private bool _writeMode;
        private bool _driveSpin;

        private byte[] _romRegionC1C7 = new byte[0x0100];
    }
}
