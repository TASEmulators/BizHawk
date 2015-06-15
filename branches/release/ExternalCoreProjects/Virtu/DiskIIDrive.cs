using System;
using System.IO;
using Jellyfish.Library;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public sealed class DiskIIDrive : MachineComponent
    {
		public DiskIIDrive() { }
        public DiskIIDrive(Machine machine) :
            base(machine)
        {
            DriveArmStepDelta[0] = new int[] { 0,  0,  1,  1,  0,  0,  1,  1, -1, -1,  0,  0, -1, -1,  0,  0 }; // phase 0
            DriveArmStepDelta[1] = new int[] { 0, -1,  0, -1,  1,  0,  1,  0,  0, -1,  0, -1,  1,  0,  1,  0 }; // phase 1
            DriveArmStepDelta[2] = new int[] { 0,  0, -1, -1,  0,  0, -1, -1,  1,  1,  0,  0,  1,  1,  0,  0 }; // phase 2
            DriveArmStepDelta[3] = new int[] { 0,  1,  0,  1, -1,  0, -1,  0,  0,  1,  0,  1, -1,  0, -1,  0 }; // phase 3
        }

        public void InsertDisk(string name, byte[] data, bool isWriteProtected)
        {
            DebugService.WriteMessage("Inserting disk '{0}'", name);
            FlushTrack();
            _disk = Disk525.CreateDisk(name, data, isWriteProtected);
            _trackLoaded = false;
        }

        public void RemoveDisk()
        {
            if (_disk != null)
            {
                DebugService.WriteMessage("Removing disk '{0}'", _disk.Name);
                _trackLoaded = false;
                _trackChanged = false;
                _trackNumber = 0;
                _trackOffset = 0;
                _disk = null;
            }
        }

        public void ApplyPhaseChange(int phaseState)
        {
            // step the drive head according to stepper magnet changes
            int delta = DriveArmStepDelta[_trackNumber & 0x3][phaseState];
            if (delta != 0)
            {
                int newTrackNumber = MathHelpers.Clamp(_trackNumber + delta, 0, TrackNumberMax);
                if (newTrackNumber != _trackNumber)
                {
                    FlushTrack();
                    _trackNumber = newTrackNumber;
                    _trackOffset = 0;
                    _trackLoaded = false;
                }
            }
        }

        public int Read()
        {
            if (LoadTrack())
            {
                int data = _trackData[_trackOffset++];
                if (_trackOffset >= Disk525.TrackSize)
                {
                    _trackOffset = 0;
                }

				Machine.DriveLight = true;
                return data;
            }

			return 0x80;
			// TODO: WTF was this
            //return _random.Next(0x01, 0xFF);
        }

        public void Write(int data)
        {
            if (LoadTrack())
            {
                _trackChanged = true;
                _trackData[_trackOffset++] = (byte)data;
                if (_trackOffset >= Disk525.TrackSize)
                {
                    _trackOffset = 0;
                }

				Machine.DriveLight = true;
            }
        }

        private bool LoadTrack()
        {
            if (!_trackLoaded && (_disk != null))
            {
                _disk.ReadTrack(_trackNumber, 0, _trackData);
                _trackLoaded = true;
            }

            return _trackLoaded;
        }

        public void FlushTrack()
        {
            if (_trackChanged)
            {
                _disk.WriteTrack(_trackNumber, 0, _trackData);
                _trackChanged = false;
            }
        }

		[Newtonsoft.Json.JsonIgnore]
        public bool IsWriteProtected { get { return _disk.IsWriteProtected; } }

        private const int TrackNumberMax = 0x44;

        private const int PhaseCount = 4;

        private int[][] DriveArmStepDelta = new int[PhaseCount][];

        private bool _trackLoaded;
        private bool _trackChanged;
        private int _trackNumber;
        private int _trackOffset;
        private byte[] _trackData = new byte[Disk525.TrackSize];
        private Disk525 _disk;
    }
}
