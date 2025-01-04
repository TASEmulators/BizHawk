using System.Buffers;
using System.IO;

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
	public const int FluxBitsPerEntry = BytesPerEntry * 8;

	/// <summary>
	/// The number of flux transition bits stored for each track.
	/// </summary>
	private const int FluxBitsPerTrack = ClockRateHz / 5;
	
	/// <summary>
	/// The fixed size of the Bits array.
	/// </summary>
	private const int FluxEntriesPerTrack = FluxBitsPerTrack / FluxBitsPerEntry;

	private int[] _bits = new int[FluxEntriesPerTrack];
	private int[] _original = new int[FluxEntriesPerTrack];

	/// <summary>
	/// Current state of the disk, which may be changed from the original media. 
	/// </summary>
	public Span<int> Bits => _bits;

	/// <summary>
	/// Fixed state of the original media, from which deltas will be calculated.
	/// </summary>
	public ReadOnlySpan<int> Original => _original;

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
	/// Resets this track to the state of the original media.
	/// </summary>
	public void Reset()
	{
		_original.CopyTo(_bits.AsSpan());
	}

	/// <summary>
	/// Write an entry to <see cref="Bits"/>.
	/// </summary>
	/// <param name="index">
	/// Index of the entry to write.
	/// </param>
	/// <param name="bits">
	/// The new content of the entry.
	/// </param>
	/// <returns>
	/// True only if data in <see cref="Bits"/> has been altered.
	/// </returns>
	public bool Write(int index, int bits)
	{
		// We only need to update delta if the bits actually changed.

		if (_bits[index] == bits)
			return false;

		_bits[index] = bits;
		return true;
	}

	/// <summary>
	/// Check to see if the original bits and current bits are equivalent.
	/// </summary>
	/// <returns>
	/// True only if the content differs.
	/// </returns>
	public bool IsModified()
	{
		for (var i = 0; i < _original.Length; i++)
		{
			if (_original[i] != _bits[i])
				return true;
		}

		return false;
	}

	public void ReadFromGCR(int density, ReadOnlySpan<byte> bytes, int fluxBitOffset)
	{
		// There are four levels of track density correlated with the four different clock dividers
		// in the 1541 disk drive. Outer tracks have more surface area, so a technique is used to read
		// bits at a higher rate.

		var paddedLength = bytes.Length;
		switch (density)
		{
			case 3:
				paddedLength = Math.Max(bytes.Length, 7692);
				break;
			case 2:
				paddedLength = Math.Max(bytes.Length, 7142);
				break;
			case 1:
				paddedLength = Math.Max(bytes.Length, 6666);
				break;
			case 0:
				paddedLength = Math.Max(bytes.Length, 6250);
				break;
		}

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
		
		_bits.CopyTo(_original.AsSpan());
	}

	public void ReadFromSaveRam(Stream stream)
	{
		
	}

	public void WriteToSaveRam(Stream stream)
	{
		
	}
}