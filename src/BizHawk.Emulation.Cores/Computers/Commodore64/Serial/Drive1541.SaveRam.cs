using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial;

public sealed partial class Drive1541 : ISaveRam
{
	// ISaveRam implementation

	// this is some extra state used to keep savestate size down, as most tracks don't get used
	// we keep it here for all disks as we need to remember it when swapping disks around
	// _usedDiskTracks.Length also doubles as a way to remember the disk count
	private bool[][] _usedDiskTracks;
	private bool[][] _dirtyDiskTracks;
	private readonly Func<int> _getCurrentDiskNumber;
	private int _diskCount;

	public void InitSaveRam(int diskCount)
	{
		_diskCount = diskCount;
		_usedDiskTracks = new bool[diskCount][];
		_dirtyDiskTracks = new bool[diskCount][];
		_diskDeltas = new byte[diskCount][][];

		for (var diskNumber = 0; diskNumber < diskCount; diskNumber++)
		{
			_usedDiskTracks[diskNumber] = new bool[84];
			_diskDeltas[diskNumber] = new byte[84][];
			_dirtyDiskTracks[diskNumber] = new bool[84];

			for (var trackNumber = 0; trackNumber < 84; trackNumber++)
			{
				_diskDeltas[diskNumber][trackNumber] = Array.Empty<byte>();
			}
		}
		
		SaveRamModified = false;
	}

	public bool SaveRamModified { get; private set; } = false;

	public byte[] CloneSaveRam()
	{
		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms);
		bw.Write(_usedDiskTracks.Length);
		
		for (var diskNumber = 0; diskNumber < _usedDiskTracks.Length; diskNumber++)
		{
			bw.WriteByteBuffer(_usedDiskTracks[diskNumber].ToUByteBuffer());

			for (var trackNumber = 0; trackNumber < 84; trackNumber++)
			{
				bw.WriteByteBuffer(_diskDeltas[diskNumber][trackNumber]);
			}
		}

		SaveRamModified = false;
		return ms.ToArray();
	}

	public void StoreSaveRam(byte[] data)
	{
		using var ms = new MemoryStream(data, false);
		using var br = new BinaryReader(ms);

		var ndisks = br.ReadInt32();
		if (ndisks != _usedDiskTracks.Length)
		{
			throw new InvalidOperationException("Disk count mismatch!");
		}

		for (var i = 0; i < _usedDiskTracks.Length; i++)
		{
			_usedDiskTracks[i] = br.ReadByteBuffer(returnNull: false)!.ToBoolBuffer();
			for (var j = 0; j < 84; j++)
			{
				_diskDeltas[i][j] = br.ReadByteBuffer(returnNull: true);
			}
		}
		
		LoadDeltas();
		SaveRamModified = false;
	}

	/// <summary>
	/// Clear all cached deltas.
	/// </summary>
	public void ResetDeltas()
	{
		for (var i = 0; i < _diskCount; i++)
		{
			for (var j = 0; j < 84; j++)
			{
				_diskDeltas[i][j] = Array.Empty<byte>();
				_usedDiskTracks[i][j] = false;
			}
		}
	}

	/// <summary>
	/// Calculate and cache the deltas for each track on the current disk.
	/// </summary>
	public void SaveDeltas()
	{
		if (_disk == null)
			return;

		var diskNumber = _getCurrentDiskNumber();
		var deltas = _diskDeltas[diskNumber];
		
		for (var trackNumber = 0; trackNumber < 84; trackNumber++)
		{
			var track = _disk.Tracks[trackNumber];
			var isModified = track.IsModified();

			_usedDiskTracks[diskNumber][trackNumber] = isModified;

			if (_dirtyDiskTracks[diskNumber][trackNumber])
			{
				SaveRamModified = true;

				deltas[trackNumber] = isModified
					? DeltaSerializer.GetDelta(track.Original, track.Bits).ToArray()
					: Array.Empty<byte>();

				_dirtyDiskTracks[diskNumber][trackNumber] = false;
			}
		}
	}

	/// <summary>
	/// Apply new deltas for each track on the current disk.
	/// </summary>
	public void LoadDeltas()
	{
		if (_disk == null)
			return;

		var diskNumber = _getCurrentDiskNumber();
		var deltas = _diskDeltas[diskNumber];

		for (var trackNumber = 0; trackNumber < 84; trackNumber++)
		{
			var track = _disk.Tracks[trackNumber];
			var delta = deltas[trackNumber];

			if (delta == null || delta.Length == 0)
			{
				track.Reset();
			}
			else
			{
				DeltaSerializer.ApplyDelta(track.Original, track.Bits, delta);
				SaveRamModified = true;
			}
			
			_usedDiskTracks[diskNumber][trackNumber] = track.IsModified();
			_dirtyDiskTracks[diskNumber][trackNumber] = false;
		}
		
	}
}