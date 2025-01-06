using System.Buffers;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media;

/// <summary>
/// Represents the magnetic flux transitions for one rotation of floppy disk media. Each bit represents
/// the transition of the signal level from 1 to 0, or from 0 to 1.
/// </summary>
public sealed class DiskTrack
{
	/// <summary>
	/// The master clock rate for synchronization.
	/// </summary>
	private const int ClockRateHz = 16000000;

	/// <summary>
	/// Number of bytes per element in the Bits array.
	/// </summary>
	private const int BytesPerEntry = sizeof(int);

	/// <summary>
	/// Number of bits contained in a single value of the Bits array.
	/// </summary>
	private const int FluxBitsPerEntry = BytesPerEntry * 8;

	/// <summary>
	/// The number of flux transition bits stored for each track.
	/// </summary>
	private const int FluxBitsPerTrack = ClockRateHz / 5;

	/// <summary>
	/// The fixed size of the Bits array.
	/// </summary>
	private const int FluxEntriesPerTrack = FluxBitsPerTrack / FluxBitsPerEntry;

	/// <summary>
	/// The number of bytes contained in the cached delta, for use with save states.
	/// </summary>
	private const int DeltaBytesPerTrack = FluxEntriesPerTrack * BytesPerEntry + 4;

	private int[] _bits = new int[FluxEntriesPerTrack];
	private int[] _original = new int[FluxEntriesPerTrack];
	private byte[] _delta = new byte[DeltaBytesPerTrack];
	private bool _dirty = true;
	private bool _modified = false;

	/// <summary>
	/// Current state of the disk, which may be changed from the original media.
	/// </summary>
	public ReadOnlySpan<int> Bits => _bits;

	/// <summary>
	/// Fixed state of the original media, from which deltas will be calculated.
	/// </summary>
	public ReadOnlySpan<int> Original => _original;

	/// <summary>
	/// The compressed difference between
	/// </summary>
	public byte[] Delta => _delta;

	/// <summary>
	/// If true, the delta needs to be recalculated.
	/// </summary>
	public bool IsDirty => _dirty;

	/// <summary>
	/// If true, the track data has been modified.
	/// </summary>
	public bool IsModified => _modified;

	/// <summary>
	/// Create a clone of the DiskTrack.
	/// </summary>
	/// <returns>
	/// A new DiskTrack with an identical copy of <see cref="Bits"/>.
	/// </returns>
	public DiskTrack Clone()
	{
		var clone = new DiskTrack();
		Bits.CopyTo(clone._bits.AsSpan());
		clone._original = _original;
		return clone;
	}

	/// <summary>
	/// Prepare the <see cref="IsModified"/> property.
	/// </summary>
	/// <returns>
	/// The new value of <see cref="IsModified"/>.
	/// </returns>
	private bool CheckModified()
		=> _modified = !_original.AsSpan().SequenceEqual(_bits);

	/// <summary>
	/// Apply a compressed delta over the original media.
	/// </summary>
	/// <param name="delta">
	/// Compressed delta data.
	/// </param>
	public void ApplyDelta(ReadOnlySpan<byte> delta)
	{
		DeltaSerializer.ApplyDelta<int>(_original, _bits, delta);
		_delta = delta.ToArray();
		_dirty = false;
		CheckModified();
	}

	/// <summary>
	/// Updates the delta for this track.
	/// </summary>
	/// <returns>
	/// True if the delta has updated, false otherwise.
	/// </returns>
	public bool UpdateDelta()
	{
		if (!_dirty) return false;

		_delta = DeltaSerializer.GetDelta<int>(_original, _bits).ToArray();
		_dirty = false;
		return true;
	}

	/// <summary>
	/// Resets this track to the state of the original media.
	/// </summary>
	public void Reset()
	{
		_original.CopyTo(_bits.AsSpan());
		_delta = Array.Empty<byte>();
		_dirty = false;
	}

	/// <summary>
	/// Synchronize state.
	/// </summary>
	/// <param name="ser">
	/// Serializer with which to synchronize.
	/// </param>
	public void SyncState(Serializer ser, string deltaId)
	{
		ser.Sync(deltaId, ref _delta, useNull: true);
	}

	public void Write(int index, int bits)
	{
		// We only need to update delta if the bits actually changed.

		if (_bits[index] == bits) return;

		_bits[index] = bits;
		_dirty = true;
	}

	public void ReadFromGCR(int density, ReadOnlySpan<byte> bytes, int fluxBitOffset)
	{
		// There are four levels of track density correlated with the four different clock dividers
		// in the 1541 disk drive. Outer tracks have more surface area, so a technique is used to read
		// bits at a higher rate.

		var paddedLength = density switch
		{
			3 => Math.Max(bytes.Length, 7692),
			2 => Math.Max(bytes.Length, 7142),
			1 => Math.Max(bytes.Length, 6666),
			0 => Math.Max(bytes.Length, 6250),
			_ => bytes.Length
		};

		// One extra byte is added at the end to break up tracks so that if the data is perfectly
		// aligned in an unfortunate way, loaders don't seize up trying to find data. Some copy protections
		// will read the same track repeatedly to account for variations in drive mechanics, and this should get
		// the more temperamental ones to load eventually.

		paddedLength++;

		// It is possible that there are more or fewer bits than the specification due to any number
		// of reasons (e.g. copy protection, tiny variations in motor speed) so we pad out with the "default"
		// bit pattern.

		using var paddedBytesMem = MemoryPool<byte>.Shared.Rent(paddedLength);
		var paddedBytes = paddedBytesMem.Memory.Span.Slice(0, paddedLength);
		bytes.CopyTo(paddedBytes);
		paddedBytes.Slice(bytes.Length).Fill(0xAA);

		var lengthBits = paddedLength * 8 - 7;
		var remainingBits = lengthBits;

		const long bitsNum = FluxEntriesPerTrack * FluxBitsPerEntry;
		long bitsDen = lengthBits;

		for (var i = 0; i < paddedLength; i++)
		{
			var byteData = paddedBytes[i];
			for (var j = 0; j < 8; j++)
			{
				var offset = fluxBitOffset + ((i * 8 + j) * bitsNum / bitsDen);
				var byteOffset = (int)(offset / FluxBitsPerEntry);
				var bitOffset = (int)(offset % FluxBitsPerEntry);
				_bits[byteOffset] |= (byteData >> 7) << bitOffset;
				byteData <<= 1;
				remainingBits--;
				if (remainingBits <= 0)
				{
					break;
				}
			}

			if (remainingBits <= 0)
			{
				break;
			}
		}
	}
}
