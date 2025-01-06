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
	private readonly Func<int> _getCurrentDiskNumber;

	public void InitSaveRam(int diskCount)
	{
		_usedDiskTracks = new bool[diskCount][];
		_diskDeltas = new byte[diskCount][][];
		for (var i = 0; i < diskCount; i++)
		{
			_usedDiskTracks[i] = new bool[84];
			_diskDeltas[i] = new byte[84][];
			for (var j = 0; j < 84; j++)
			{
				_diskDeltas[i][j] = Array.Empty<byte>();
			}
		}
	}

	public bool SaveRamModified { get; private set; } = false;

	public byte[] CloneSaveRam()
	{
		SaveDeltas(); // update the current deltas

		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms);
		bw.Write(_usedDiskTracks.Length);
		for (var diskNumber = 0; diskNumber < _usedDiskTracks.Length; diskNumber++)
		{
			bw.WriteByteBuffer(_usedDiskTracks[diskNumber]
				.ToUByteBuffer());
			for (var trackNumber = 0; trackNumber < 84; trackNumber++)
			{
				bw.WriteByteBuffer(_diskDeltas[diskNumber][trackNumber]);
			}
		}

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

		ResetDeltas();

		for (var i = 0; i < _usedDiskTracks.Length; i++)
		{
			_usedDiskTracks[i] = br.ReadByteBuffer(returnNull: false)!.ToBoolBuffer();
			for (var j = 0; j < 84; j++)
			{
				_diskDeltas[i][j] = br.ReadByteBuffer(returnNull: true);
			}
		}

		LoadDeltas(); // load up new deltas
		_usedDiskTracks[_getCurrentDiskNumber()][_trackNumber] = true; // make sure this gets set to true now
	}

	public void SaveDeltas()
	{
		_disk?.DeltaUpdate((tracknum, track) =>
		{
			SaveDelta(tracknum, track.Delta);
		});
	}

	public void LoadDeltas()
	{
		_disk?.DeltaUpdate((tracknum, track) =>
		{
			LoadDelta(tracknum, track.Delta);
		});
	}

	private void ResetDeltas()
	{
		_disk?.DeltaUpdate(static (_, track) =>
		{
			track.Reset();
		});
	}

	private void SaveDelta(int trackNumber, byte[] delta)
	{
		SaveRamModified = true;
		_diskDeltas[_getCurrentDiskNumber()][trackNumber] = delta;
	}

	private void LoadDelta(int trackNumber, byte[] delta)
	{
		_diskDeltas[_getCurrentDiskNumber()][trackNumber] = delta;
		_disk.GetTrack(trackNumber).ApplyDelta(delta);
	}

	private void ResetDelta(int trackNumber)
	{
		SaveRamModified = true;
		_diskDeltas[_getCurrentDiskNumber()][trackNumber] = Array.Empty<byte>();
		_disk.GetTrack(trackNumber).Reset();
	}
}
