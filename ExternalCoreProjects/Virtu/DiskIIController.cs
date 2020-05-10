using System.Collections.Generic;

namespace Jellyfish.Virtu
{
	public interface IDiskIIController : IPeripheralCard
	{
		bool DriveLight { get; set; }

		// ReSharper disable once UnusedMemberInSuper.Global
		DiskIIDrive Drive1 { get; }

		// ReSharper disable once UnusedMember.Global
		void Sync(IComponentSerializer ser);
	}

	// ReSharper disable once UnusedMember.Global
	public sealed class DiskIIController : IDiskIIController
	{
		private const int Phase1On = 1 << 1;
		private readonly IVideo _video;
		private readonly byte[] _romRegionC1C7;

		private bool _driveLight;
		private int _latch;
		private int _phaseStates;
		private bool _motorOn;
		private int _driveNumber;
		private bool _loadMode;
		private bool _writeMode;
		private bool _driveSpin;

		public DiskIIController(IVideo video, byte[] diskIIRom)
		{
			_video = video;
			_romRegionC1C7 = diskIIRom;
			Drive1 = new DiskIIDrive(this);
			Drive2 = new DiskIIDrive(this);
			_phaseStates = 0;
			SetMotorOn(false);
			SetDriveNumber(0);
			_loadMode = false;
			_writeMode = false;
		}

		public DiskIIDrive Drive1 { get; }
		public DiskIIDrive Drive2 { get; }

		public bool DriveLight
		{
			get => _driveLight;
			set => _driveLight = value;
		}

		public void Sync(IComponentSerializer ser)
		{
			ser.Sync(nameof(_driveLight), ref _driveLight);

			ser.Sync(nameof(_latch), ref _latch);
			ser.Sync(nameof(_phaseStates), ref _phaseStates);
			ser.Sync(nameof(_motorOn), ref _motorOn);
			ser.Sync(nameof(_driveNumber), ref _driveNumber);
			ser.Sync(nameof(_loadMode), ref _loadMode);
			ser.Sync(nameof(_writeMode), ref _writeMode);
			ser.Sync(nameof(_driveSpin), ref _driveSpin);

			Drive1.Sync(ser);
			Drive2.Sync(ser);
		}
		public IList<DiskIIDrive> Drives => new List<DiskIIDrive> { Drive1, Drive2 };

		public void WriteIoRegionC8CF(int address, int data) => _video.ReadFloatingBus();

		public int ReadIoRegionC0C0(int address)
		{
			switch (address & 0xF)
			{
				case 0x0:
				case 0x1:
				case 0x2:
				case 0x3:
				case 0x4:
				case 0x5:
				case 0x6:
				case 0x7:
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

						WriteLatch();
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

			return _video.ReadFloatingBus();
		}

		public int ReadIoRegionC1C7(int address)
		{
			return _romRegionC1C7[address & 0xFF];
		}

		public int ReadIoRegionC8CF(int address) => _video.ReadFloatingBus();

		public void WriteIoRegionC0C0(int address, int data)
		{
			switch (address & 0xF)
			{
				case 0x0:
				case 0x1:
				case 0x2:
				case 0x3:
				case 0x4:
				case 0x5:
				case 0x6:
				case 0x7:
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

		public void WriteIoRegionC1C7(int address, int data) { }

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
	}
}
