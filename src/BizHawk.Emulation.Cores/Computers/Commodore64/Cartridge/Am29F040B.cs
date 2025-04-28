using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;

/// <summary>
/// AMD flash chip used for EasyFlash emulation.
/// </summary>
public class Am29F040B
{
	// Source:
	// https://www.mouser.com/datasheet/2/196/spansion_inc_am29f040b_eol_21445e8-3004346.pdf
	//
	// Flash erase suspend/resume are not implemented.

	public const int ImageSize = 1 << 19;
	public const int ImageMask = ImageSize - 1;
	private const int SectorSize = 1 << 16;
	private const int SectorMask = SectorSize - 1;
	private const int RegisterMask = (1 << 11) - 1;

	private const byte ToggleBit2 = 1 << 2;
	private const byte ErrorBit = 1 << 5;
	private const byte ToggleBit = 1 << 6;
	private const byte PollingBit = 1 << 7;
	private const byte EraseBit = 1 << 3;

	private const int WriteLatency = 7;
	private const int EraseSectorLatency = 1000000;
	private const int EraseChipLatency = 8000000;
	private const int EraseValue = 0xFF;

	private const byte ManufacturerCode = 0x01;
	private const byte DeviceCode = 0xA4;
	private const byte WriteProtect = 0x00; // can be set to 1 to tell software it is write-protected

	private enum Sequence
	{
		None,
		Start,
		Complete,
		Command
	}

	private enum Mode
	{
		Read,
		Erase,
		AutoSelect,
		Write
	}

	private enum Register
	{
		Command0 = 0x0555,
		Command1 = 0x02AA
	}

	private enum Signal
	{
		Command0 = 0xAA,
		Command1 = 0x55,
		Erase = 0x80,
		AutoSelect = 0x90,
		Program = 0xA0,
		ChipErase = 0x10,
		SectorErase = 0x30,
		Reset = 0xF0
	}

	private int _busyTimeRemaining;
	private int _status;
	private byte[] _data = new byte[ImageSize];
	private Mode _mode;
	private Sequence _sequence;
	private bool _returnStatus;
	private int _startAddress;
	private int _endAddress;
	private bool _errorPending;
	private bool _dataDirty;

	public MemoryDomain CreateMemoryDomain(string name) =>
		new MemoryDomainByteArray(
			name: name,
			endian: MemoryDomain.Endian.Little,
			data: _data,
			writable: true,
			wordSize: 1
		);

	public void Clock()
	{
		if (_busyTimeRemaining <= 0)
			return;

		_busyTimeRemaining--;

		if (_busyTimeRemaining != 0)
			return;

		_status ^= PollingBit;

		if (_errorPending)
		{
			_errorPending = false;
			_status |= ErrorBit;
		}
	}

	/// <summary>
	/// Synchronize state.
	/// </summary>
	/// <param name="ser">
	/// State serializer.
	/// </param>
	/// <param name="withData">
	/// True only if the raw data should be synchronized. If false,
	/// the caller is responsible for synchronizing deltas.
	/// </param>
	public void SyncState(Serializer ser, bool withData)
	{
		ser.Sync("BusyTimeRemaining", ref _busyTimeRemaining);
		ser.Sync("Status", ref _status);
		ser.SyncEnum("Mode", ref _mode);
		ser.SyncEnum("Sequence", ref _sequence);
		ser.Sync("ReturnStatus", ref _returnStatus);
		ser.Sync("StartAddress", ref _startAddress);
		ser.Sync("EndAddress", ref _endAddress);
		ser.Sync("ErrorPending", ref _errorPending);
		ser.Sync("DataDirty", ref _dataDirty);

		if (withData)
			ser.Sync("Data", ref _data, false);
	}

	public void Reset()
	{
		_busyTimeRemaining = 0;
		_status = 0;
		_mode = Mode.Read;
		_sequence = Sequence.None;
		_errorPending = false;
		_startAddress = 0;
		_endAddress = ImageMask;
	}

	public Span<byte> Data =>
		_data.AsSpan();

	public int Peek(int addr) =>
		_data[addr & ImageMask] & 0xFF;

	public int Poke(int addr, int val)
	{
		var newData = val & 0xFF;
		_dataDirty |= _data[addr & ImageMask] != newData;
		return _data[addr & ImageMask] = unchecked((byte)newData);
	}

	// From the datasheet:
	// Address bits A18-A11 = X = Don’t Care for all address
	// commands except for Program Address (PA), Sector Address (SA), Read
	// Address (RA), and AutoSelect sector protect verify.

	public int Read(int addr)
	{
		int data;

		if (_busyTimeRemaining > 0)
		{
			if (addr >= _startAddress && addr <= _endAddress)
				_status ^= ToggleBit2;

			_status ^= ToggleBit;
			return _status;
		}

		// Some commands allow one read of status before going back to read mode.
		// Areas being written or erased will always return status during modification.
		if (_returnStatus && addr >= _startAddress && addr <= _endAddress)
		{
			_returnStatus = false;
			return _status;
		}

		// Read manufacturer registers or memory.
		switch (_mode)
		{
			case Mode.AutoSelect:
			{
				switch (addr & 0xFF)
				{
					case 0x00:
						data = ManufacturerCode;
						break;
					case 0x01:
						data = DeviceCode;
						break;
					case 0x02:
						data = WriteProtect;
						break;
					default:
						data = 0xFF;
						break;
				}
				break;
			}
			default:
			{
				data = _data[addr & ImageMask];
				break;
			}
		}

		return data;
	}

	public void Write(int addr, int data)
	{
		switch (_mode, _sequence, (Register)(addr & RegisterMask), (Signal)data)
		{
			case (Mode.Write, _, _, _):
			{
				_mode = Mode.Read;
				_sequence = Sequence.None;

				if (_busyTimeRemaining > 0)
					break;

				var originalData = _data[addr & ImageMask];
				var newData = originalData & data & 0xFF;
				_dataDirty |= newData != originalData;
				_errorPending = data != newData;
				_data[addr & ImageMask] = unchecked((byte)newData);
				_busyTimeRemaining = WriteLatency; // 7-30us
				_status = (data & 0x80) ^ PollingBit;
				_returnStatus = true;
				_startAddress = _endAddress = addr;
				break;
			}
			case (_, _, Register.Command0, Signal.Command0):
			{
				_sequence = Sequence.Start;
				break;
			}
			case (_, Sequence.Start, Register.Command1, Signal.Command1):
			{
				_sequence = Sequence.Complete;
				break;
			}
			case (_, Sequence.Complete, Register.Command0, Signal.Erase):
			{
				_mode = Mode.Erase;
				_sequence = Sequence.None;
				break;
			}
			case (Mode.Erase, Sequence.Complete, Register.Command0, Signal.ChipErase):
			{
				_mode = Mode.Read;
				_sequence = Sequence.None;

				if (_busyTimeRemaining > 0)
					break;

				_busyTimeRemaining = EraseChipLatency; // 8-64sec
				_data.AsSpan().Fill(EraseValue);
				_dataDirty = true;
				_returnStatus = true;
				_status = EraseBit; // bit 7 = complete
				break;
			}
			case (Mode.Erase, Sequence.Complete, _, Signal.SectorErase):
			{
				_mode = Mode.Read;
				_sequence = Sequence.None;

				if (_busyTimeRemaining > 0)
					break;

				_busyTimeRemaining = EraseSectorLatency; // ~1sec
				_data.AsSpan(addr & ~SectorMask, SectorSize).Fill(0xFF);
				_dataDirty = true;
				_returnStatus = true;
				_status = EraseBit; // bit 7 = complete
				break;
			}
			case (Mode.Read, Sequence.Complete, Register.Command0, Signal.AutoSelect):
			{
				_mode = Mode.AutoSelect;
				_sequence = Sequence.None;
				break;
			}
			case (Mode.Read, Sequence.Complete, Register.Command0, Signal.Program):
			{
				_mode = Mode.Write;
				break;
			}
			case (_, _, _, Signal.Reset):
			{
				_mode = Mode.Read;
				_sequence = Sequence.None;
				break;
			}
		}
	}

	public bool IsDataDirty => _dataDirty;

	public bool CheckDataDirty()
	{
		var result = _dataDirty;
		_dataDirty = false;
		return result;
	}
}