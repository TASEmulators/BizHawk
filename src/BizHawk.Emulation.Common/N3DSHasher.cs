using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Performs hashing against 3DS roms
	/// 3DS roms can't just have full file hashing done
	/// As 3DS roms may be >= 2GiB, too large for a .NET array
	/// As such, we need to perform a quick hash to identify them
	/// For this purpose, we re-use RetroAchievement's hashing formula
	/// Reference code: https://github.com/RetroAchievements/rcheevos/blob/8d8ef920e253f1286464771e81ce4cf7f4358eee/src/rhash/hash.c#L1573-L2184
	/// </summary>
	public class N3DSHasher(byte[]? aesKeys, byte[]? seedDb)
	{
		// https://github.com/CasualPokePlayer/encore/blob/2b20082581906fe973e26ed36bef695aa1f64527/src/core/hw/aes/key.cpp#L23-L30
		private static readonly BigInteger GENERATOR_CONSTANT = BigInteger.Parse("1FF9E9AAC5FE0408024591DC5D52768A", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger U128_MAX = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture);

		private static byte[] Derive3DSNormalKey(BigInteger keyX, BigInteger keyY)
		{
			static BigInteger LeftRot128(BigInteger v, int rot)
			{
				var l = (v << rot) & U128_MAX;
				var r = v >> (128 - rot);
				return l | r;
			}

			static BigInteger Add128(BigInteger v1, BigInteger v2)
				=> (v1 + v2) & U128_MAX;

			var normalKey = LeftRot128(Add128(LeftRot128(keyX, 2) ^ keyY, GENERATOR_CONSTANT), 87);
			var normalKeyBytes = normalKey.ToByteArray();
			if (normalKeyBytes.Length > 17)
			{
				// this shoudn't ever happen
				throw new InvalidOperationException();
			}

			// get rid of a final trailing 0
			// but also make sure we have 0 padding to 16 bytes
			Array.Resize(ref normalKeyBytes, 16);

			// .ToByteArray() is always in little endian order, but we want big endian order
			Array.Reverse(normalKeyBytes);
			return normalKeyBytes;
		}

		private (BigInteger Key1, BigInteger Key2) FindAesKeys(string key1Prefix, string key2Prefix)
		{
			if (aesKeys == null)
			{
				throw new InvalidOperationException("AES keys are not present");
			}

			using var keys = new StreamReader(new MemoryStream(aesKeys, writable: false), Encoding.UTF8);
			string? key1Str = null, key2Str = null;
			while ((key1Str is null || key2Str is null) && keys.ReadLine() is { } line)
			{
				if (line.Length == 0 || line.StartsWith('#'))
				{
					continue;
				}

				var eqpos = line.IndexOf('=');
				if (eqpos == -1 || eqpos != line.LastIndexOf('='))
				{
					throw new InvalidOperationException("Malformed key list");
				}

				if (key1Str is null)
				{
					if (line.StartsWithOrdinal(key1Prefix))
					{
						key1Str = line[(eqpos + 1)..];
						if (key1Str.Length != 32)
						{
							throw new InvalidOperationException("Invalid key length");
						}
					}
				}

				if (key2Str is null)
				{
					if (line.StartsWithOrdinal(key2Prefix))
					{
						key2Str = line[(eqpos + 1)..];
						if (key2Str.Length != 32)
						{
							throw new InvalidOperationException("Invalid key length");
						}
					}
				}
			}

			if (key1Str is null || key2Str is null)
			{
				throw new InvalidOperationException("Couldn't find requested keys");
			}

			var key1 = BigInteger.Parse($"0{key1Str}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var key2 = BigInteger.Parse($"0{key2Str}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			return (key1, key2);
		}

		private void GetNCCHNormalKeys(ReadOnlySpan<byte> primaryKeyYRaw, byte secondaryKeyXSlot, ReadOnlySpan<byte> programIdBytes,
			Span<byte> primaryKey, Span<byte> secondaryKey)
		{
			var (primaryKeyX, secondaryKeyX) = FindAesKeys("slot0x2CKeyX=", $"slot0x{secondaryKeyXSlot:X2}KeyX=");

			var primaryKeyYBytes = new byte[17];
			primaryKeyYRaw.CopyTo(primaryKeyYBytes.AsSpan(1));
			Array.Reverse(primaryKeyYBytes); // convert big endian to little endian
			var primaryKeyY = new BigInteger(primaryKeyYBytes);

			Derive3DSNormalKey(primaryKeyX, primaryKeyY).AsSpan().CopyTo(primaryKey);

			if (programIdBytes.IsEmpty)
			{
				Derive3DSNormalKey(secondaryKeyX, primaryKeyY).AsSpan().CopyTo(secondaryKey);
				return;
			}

			if (seedDb == null)
			{
				throw new InvalidOperationException("Seed DB is not present");
			}

			var programId = BinaryPrimitives.ReadUInt64LittleEndian(programIdBytes);
			using var seeddb = new BinaryReader(new MemoryStream(seedDb, false));
			var count = seeddb.ReadUInt32();
			seeddb.BaseStream.Seek(12, SeekOrigin.Current); // apparently some padding bytes before actual seeds
			for (long i = 0; i < count; i++)
			{
				var titleId = seeddb.ReadUInt64();
				if (titleId != programId)
				{
					seeddb.BaseStream.Seek(24, SeekOrigin.Current);
					continue;
				}

				var sha256Input = new byte[32];
				primaryKeyYRaw.CopyTo(sha256Input);
				if (seeddb.BaseStream.Read(sha256Input, offset: 16, count: 16) != 16)
				{
					throw new Exception("Failed to read seed in seeddb");
				}

				var sha256Digest = SHA256Checksum.Compute(sha256Input);

				var secondaryKeyYBytes = new byte[17];
				Buffer.BlockCopy(sha256Digest, 0, secondaryKeyYBytes, 1, 16);
				Array.Reverse(secondaryKeyYBytes); // convert big endian to little endian
				var secondaryKeyY = new BigInteger(secondaryKeyYBytes);
				Derive3DSNormalKey(secondaryKeyX, secondaryKeyY).AsSpan().CopyTo(secondaryKey);
				return;
			}

			throw new Exception("Could not find seed in seeddb");
		}

		private void HashNCCH(FileStream romFile, IncrementalHash md5Inc, byte[] header, Aes? ciaAes = null)
		{
			long exeFsOffset = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x1A0, 4));
			long exeFsSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x1A4, 4));

			// Offset and size are in "media units" (1 media unit = 0x200 bytes)
			exeFsOffset *= 0x200;
			exeFsSize *= 0x200;

			// This region is technically optional, but it should always be present for executable content (i.e. games)
			if (exeFsOffset == 0 || exeFsSize == 0)
			{
				throw new Exception("ExeFS was not available");
			}

			// NCCH flag 7 is a bitfield of various crypto related flags
			var fixedKeyFlag = header[0x188 + 7].Bit(0);
			var noCryptoFlag = header[0x188 + 7].Bit(2);
			var seedCryptoFlag = header[0x188 + 7].Bit(5);

			var primaryKey = new byte[128 / 8];
			var secondaryKey = new byte[128 / 8];
			var iv = new byte[128 / 8];

			if (!noCryptoFlag)
			{
				if (fixedKeyFlag)
				{
					// Fixed crypto key means all 0s for both keys
					primaryKey.AsSpan().Clear();
					secondaryKey.AsSpan().Clear();
				}
				else
				{
					// Primary key y is just the first 16 bytes of the header
					var primaryKeyY = header.AsSpan(0, 16);

					// NCCH flag 3 indicates which secondary key x slot is used
					var cryptoMethod = header[0x188 + 3];
					byte secondaryKeyXSlot = cryptoMethod switch
					{
						0x00 => 0x2C,
						0x01 => 0x25,
						0x0A => 0x18,
						0x0B => 0x1B,
						_ => throw new InvalidOperationException($"Invalid crypto method {cryptoMethod:X2}")
					};

					// We only need the program id if we're doing seed crypto
					var programId = seedCryptoFlag ? header.AsSpan(0x118, 8) : [ ];

					GetNCCHNormalKeys(primaryKeyY, secondaryKeyXSlot, programId, primaryKey, secondaryKey);
				}
			}

			var ncchVersion = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(0x112, 2));
			switch (ncchVersion)
			{
				case 0:
				case 2:
					for (var i = 0; i < 8; i++)
					{
						// First 8 bytes is the partition id in reverse byte order
						iv[7 - i] = header[0x108 + i];
					}

					// Magic number for ExeFS
					iv[8] = 2;

					// Rest of the bytes are 0
					iv.AsSpan(9).Clear();
					break;
				case 1:
					// First 8 bytes is the partition id in normal byte order 
					header.AsSpan(0x108, 8).CopyTo(iv);

					// Next 4 bytes are 0
					iv.AsSpan(8, 4).Clear();

					// Last 4 bytes is the ExeFS byte offset in big endian 
					BinaryPrimitives.WriteUInt32BigEndian(iv.AsSpan(12, 4), (uint)exeFsOffset);
					break;
				default:
					throw new Exception($"Invalid NCCH version {ncchVersion:X4}");
			}

			// Clear out crypto flags to ensure we get the same hash for decrypted and encrypted ROMs
			header.AsSpan(0x114, 4).Clear();
			header[0x188 + 3] = 0;
			header[0x188 + 7] &= ~(0x20 | 0x04 | 0x01) & 0xFF;

			md5Inc.AppendData(header);

			// note: stream offset must be +0x200 from the beginning of the NCCH (i.e. after the NCCH header)
			exeFsOffset -= 0x200;

			if (ciaAes != null)
			{
				// CBC decryption works by setting the IV to the encrypted previous block.
				// Normally this means we would need to decrypt the data between the header and the ExeFS so the CIA AES state is correct.
				// However, we can abuse how CBC decryption works and just set the IV to last block we would otherwise decrypt.
				// We don't care about the data betweeen the header and ExeFS, so this works fine.

				var ciaIv = new byte[ciaAes.BlockSize / 8];
				romFile.Seek(exeFsOffset - ciaIv.Length, SeekOrigin.Current);
				if (romFile.Read(ciaIv, 0, ciaIv.Length) != ciaIv.Length)
				{
					throw new Exception("Failed to read NCCH data");
				}

				ciaAes.IV = ciaIv;
			}
			else
			{
				// No encryption present, just skip over the in-between data
				romFile.Seek(exeFsOffset, SeekOrigin.Current);
			}

			// constrict hash buffer size to 64MiBs (like RetroAchievements does)
			var exeFsBufferSize = (int)Math.Min(exeFsSize, 64 * 1024 * 1024);
			var exeFsBuffer = new byte[exeFsBufferSize];
			if (romFile.Read(exeFsBuffer, 0, exeFsBufferSize) != exeFsBufferSize)
			{
				throw new Exception("Failed to read ExeFS data");
			}

			if (ciaAes != null)
			{
				using var decryptor = ciaAes.CreateDecryptor();
				Debug.Assert(decryptor.CanTransformMultipleBlocks, "AES decryptor can transform multiple blocks");
				decryptor.TransformBlock(exeFsBuffer, 0, exeFsBuffer.Length, exeFsBuffer, 0);
			}

			if (!noCryptoFlag)
			{
				using var aes = Aes.Create();
				// 3DS NCCH encryption uses AES-CTR
				// However, this is not directly implemented in .NET
				// We'll just implement it ourselves with ECB
				aes.Mode = CipherMode.ECB;
				aes.Padding = PaddingMode.None;
				aes.BlockSize = 128;
				aes.KeySize = 128;

				aes.Key = primaryKey;
				aes.IV = new byte[iv.Length];

				AesCtrTransform(aes, iv, exeFsBuffer.AsSpan(0, 0x200));

				for (var i = 0; i < 8; i++)
				{
					var exeFsSectionSize = BinaryPrimitives.ReadUInt32LittleEndian(exeFsBuffer.AsSpan(i * 16 + 12, 4));

					// 0 size indicates an unused section
					if (exeFsSectionSize == 0)
					{
						continue;
					}

					var exeFsSectionName = Encoding.ASCII.GetString(exeFsBuffer.AsSpan(i * 16, 8));
					var exeFsSectionOffset = BinaryPrimitives.ReadUInt32LittleEndian(exeFsBuffer.AsSpan(i * 16 + 8, 4));

					// Offsets must be aligned by a media unit
					if ((exeFsSectionOffset & 0x1FF) != 0)
					{
						throw new Exception("ExeFS section offset is misaligned");
					}

					// Offset is relative to the end of the header
					exeFsSectionOffset += 0x200;

					// Check against malformed sections
					if (exeFsSectionOffset + (((ulong)exeFsSectionSize + 0x1FF) & ~0x1FFUL) > (ulong)exeFsSize)
					{
						throw new Exception("ExeFS section would overflow");
					}

					if (exeFsSectionName[..4] == "icon" || exeFsSectionName[..6] == "banner")
					{
						// Align size up by a media unit
						exeFsSectionSize = (uint)((exeFsSectionSize + 0x1FF) & ~0x1FFUL);
						aes.Key = primaryKey;
					}
					else
					{
						// We don't align size up here, as the padding bytes will use the primary key rather than the secondary key
						aes.Key = secondaryKey;
					}

					// In theory, the section offset + size could be greater than the buffer size
					// In practice, this likely never occurs, but just in case it does, ignore the section or constrict the size
					if (exeFsSectionOffset + exeFsSectionSize > exeFsBufferSize)
					{
						if (exeFsSectionOffset >= exeFsBufferSize)
						{
							continue;
						}

						exeFsSectionSize = (uint)(exeFsBufferSize - exeFsSectionOffset);
					}

					AesCtrTransform(aes, iv, exeFsBuffer.AsSpan((int)exeFsSectionOffset, (int)(exeFsSectionSize & ~0xFU)));

					if ((exeFsSectionSize & 0x1FF) != 0)
					{
						// Handle padding bytes, these always use the primary key
						exeFsSectionOffset += exeFsSectionSize;
						exeFsSectionSize = 0x200 - (exeFsSectionSize & 0x1FF);

						// Align our decryption start to an AES block boundary
						if ((exeFsSectionSize & 0xF) != 0)
						{
							var ivCopy = new byte[iv.Length];
							iv.AsSpan().CopyTo(ivCopy);
							exeFsSectionOffset &= ~0xFU;

							// First decrypt these last bytes using the secondary key
							AesCtrTransform(aes, iv, exeFsBuffer.AsSpan((int)exeFsSectionOffset, (int)(0x10 - (exeFsSectionSize & 0xF))));

							// Now re-encrypt these bytes using the primary key
							aes.Key = primaryKey;
							ivCopy.AsSpan().CopyTo(iv);
							AesCtrTransform(aes, iv, exeFsBuffer.AsSpan((int)exeFsSectionOffset, (int)(0x10 - (exeFsSectionSize & 0xF))));

							// All of the padding can now be decrypted using the primary key
							ivCopy.AsSpan().CopyTo(iv);
							exeFsSectionSize += 0x10 - (exeFsSectionSize & 0xF);
						}

						aes.Key = primaryKey;
						AesCtrTransform(aes, iv, exeFsBuffer.AsSpan((int)exeFsSectionOffset, (int)exeFsSectionSize));
					}
				}
			}

			md5Inc.AppendData(exeFsBuffer);
		}

		private byte[] GetCIANormalKey(byte commonKeyIndex)
		{
			var (keyX, keyY) = FindAesKeys("slot0x3DKeyX=", $"common{commonKeyIndex}=");
			return Derive3DSNormalKey(keyX, keyY);
		}

		private static uint CIASignatureSize(byte[] header)
		{
			var signatureType = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0, 4));
			return signatureType switch
			{
				0x010000 or 0x010003 => 0x200 + 0x3C,
				0x010001 or 0x010004 => 0x100 + 0x3C,
				0x010002 or 0x010005 => 0x3C + 0x40,
				_ => throw new InvalidOperationException($"Invalid signature type {signatureType:X8}"),
			};
		}

		private const uint CIA_HEADER_SIZE = 0x2020;

		// note that the header passed here is just the first 0x200 bytes, not a full CIA_HEADER_SIZE
		private void HashCIA(FileStream romFile, IncrementalHash md5Inc, byte[] header)
		{
			var certSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x08, 4));
			var tikSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x0C, 4));
			var tmdSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x10, 4));

			const long CIA_ALIGNMENT_MASK = 64 - 1; // sizes are aligned to 64 bytes
			const long CERT_OFFSET = (CIA_HEADER_SIZE + CIA_ALIGNMENT_MASK) & ~CIA_ALIGNMENT_MASK;
			var tikOffset = (CERT_OFFSET + certSize + CIA_ALIGNMENT_MASK) & ~CIA_ALIGNMENT_MASK;
			var tmdOffset = (tikOffset + tikSize + CIA_ALIGNMENT_MASK) & ~CIA_ALIGNMENT_MASK;
			var contentOffset = (tmdOffset + tmdSize + CIA_ALIGNMENT_MASK) & ~CIA_ALIGNMENT_MASK;

			// Check if this CIA is encrypted, if it isn't, we can hash it right away

			romFile.Seek(tmdOffset, SeekOrigin.Begin);
			if (romFile.Read(header, 0, 4) != 4)
			{
				throw new Exception("Failed to read TMD signature type");
			}

			var signatureSize = CIASignatureSize(header);

			romFile.Seek(signatureSize + 0x9E, SeekOrigin.Current);
			if (romFile.Read(header, 0, 2) != 2)
			{
				throw new Exception("Failed to read TMD content count");
			}

			var contentCount = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0, 2));

			romFile.Seek(0x9C4 - 0x9E - 2, SeekOrigin.Current);
			int contentCountIndex;
			for (contentCountIndex = 0; contentCountIndex < contentCount; contentCountIndex++)
			{
				if (romFile.Read(header, 0, 0x30) != 0x30)
				{
					throw new Exception("Failed to read TMD content chunk");
				}

				// Content index 0 is the main content (i.e. the 3DS executable)
				var contentIndex = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4, 2));
				if (contentIndex == 0)
				{
					break;
				}

				contentOffset += BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0xC, 4));
			}

			if (contentCountIndex == contentCount)
			{
				throw new Exception("Failed to find main content chunk in TMD");
			}

			var cryptoFlag = header[7].Bit(0);
			string ncchHeaderTag;
			if (!cryptoFlag)
			{
				// Not encrypted, we can hash the NCCH immediately
				romFile.Seek(contentOffset, SeekOrigin.Begin);
				if (romFile.Read(header, 0, 0x200) != 0x200)
				{
					throw new Exception("Failed to read NCCH header");
				}

				ncchHeaderTag = Encoding.ASCII.GetString(header.AsSpan(0x100, 4));
				if (ncchHeaderTag != "NCCH")
				{
					throw new Exception($"NCCH header was not at offset {contentOffset:X}");
				}

				HashNCCH(romFile, md5Inc, header);
				return;
			}

			// Acquire the encrypted title key, title id, and common key index from the ticket
			// These will be needed to decrypt the title key, and that will be needed to decrypt the CIA

			romFile.Seek(tikOffset, SeekOrigin.Begin);
			if (romFile.Read(header, 0, 4) != 4)
			{
				throw new Exception("Failed to read ticket signature type");
			}

			signatureSize = CIASignatureSize(header);

			romFile.Seek(signatureSize, SeekOrigin.Current);
			if (romFile.Read(header, 0, 0xB2) != 0xB2)
			{
				throw new Exception("Failed to read ticket data");
			}

			var commonKeyIndex = header[0xB1];
			if (commonKeyIndex > 5)
			{
				throw new Exception($"Invalid common key index {commonKeyIndex:X2}");
			}

			var normalKey = GetCIANormalKey(commonKeyIndex);
			var titleId = header.AsSpan(0x9C, sizeof(ulong));
			var iv = new byte[128 / 8];
			titleId.CopyTo(iv);

			using var aes = Aes.Create();
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.None;
			aes.BlockSize = 128;
			aes.KeySize = 128;
			aes.Key = normalKey;
			aes.IV = iv;

			// Finally, decrypt the title key
			var titleKey = header.AsSpan(0x7F, 128 / 8).ToArray();
			using (var decryptor = aes.CreateDecryptor())
			{
				decryptor.TransformBlock(titleKey, 0, titleKey.Length, titleKey, 0);
			}

			// Now we can hash the NCCH

			romFile.Seek(contentOffset, SeekOrigin.Begin);
			if (romFile.Read(header, 0, 0x200) != 0x200)
			{
				throw new Exception("Failed to read NCCH header");
			}

			// Content index is iv (which is always 0 for main content)
			iv.AsSpan().Clear();
			aes.Key = titleKey;
			aes.IV = iv;

			using (var decryptor = aes.CreateDecryptor())
			{
				Debug.Assert(decryptor.CanTransformMultipleBlocks, "AES decryptor can transform multiple blocks");
				decryptor.TransformBlock(header, 0, header.Length, header, 0);
			}

			ncchHeaderTag = Encoding.ASCII.GetString(header.AsSpan(0x100, 4));
			if (ncchHeaderTag != "NCCH")
			{
				throw new Exception($"NCCH header was not at offset {contentOffset:X}");
			}

			HashNCCH(romFile, md5Inc, header, aes);
		}

		private static void Hash3DSX(FileStream romFile, IncrementalHash md5Inc, byte[] header)
		{
			var headerSize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(4, 2));
			var relocHeaderSize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(6, 2));
			var codeSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x10, 4));

			// 3 relocation headers are in-between the 3DSX header and code segment
			var codeOffset = headerSize + relocHeaderSize * 3;

			// constrict hash buffer size to 64MiB (like RetroAchievements does)
			var codeBufferSize = (int)Math.Min(codeSize, 64 * 1024 * 1024);
			var codeBuffer = new byte[codeBufferSize];

			romFile.Seek(codeOffset, SeekOrigin.Begin);
			if (romFile.Read(codeBuffer, 0, codeBufferSize) != codeBufferSize)
			{
				throw new Exception("Failed to read 3DSX code segment");
			}

			md5Inc.AppendData(codeBuffer);
		}

		public string? HashROM(string romPath)
		{
			try
			{
				using var romFile = File.OpenRead(romPath);
				using var md5Inc = IncrementalHash.CreateHash(HashAlgorithmName.MD5);

				// NCCH and NCSD headers are both 0x200 bytes
				var header = new byte[0x200];
				if (romFile.Read(header, 0, header.Length) != header.Length)
				{
					throw new Exception("Failed to read ROM header");
				}

				var ncsdHeaderTag = Encoding.ASCII.GetString(header.AsSpan(0x100, 4));
				if (ncsdHeaderTag == "NCSD")
				{
					// A NCSD container contains 1-8 NCCH partitions
					// The first partition (index 0) is reserved for executable content
					long headerOffset = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x120, 4));
					// Offset is in "media units" (1 media unit = 0x200 bytes)
					headerOffset *= 0x200;

					// We include the NCSD header in the hash, as that will ensure different versions of a game result in a different hash
					// This is due to some revisions / languages only ever changing other NCCH paritions (e.g. the game manual)
					md5Inc.AppendData(header);

					romFile.Seek(headerOffset, SeekOrigin.Begin);
					if (romFile.Read(header, 0, header.Length) != header.Length)
					{
						throw new Exception("Failed to read NCCH header");
					}

					var ncsdNcchHeaderTag = Encoding.ASCII.GetString(header.AsSpan(0x100, 4));
					if (ncsdNcchHeaderTag != "NCCH")
					{
						throw new Exception($"NCCH header was not at offset {headerOffset:X}");
					}
				}

				var ncchHeaderTag = Encoding.ASCII.GetString(header.AsSpan(0x100, 4));
				if (ncchHeaderTag == "NCCH")
				{
					HashNCCH(romFile, md5Inc, header);
					return FinalizeHash(md5Inc);
				}

				// Couldn't identify either an NCSD or NCCH

				// Try to identify this as a CIA
				if (BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4)) == CIA_HEADER_SIZE)
				{
					HashCIA(romFile, md5Inc, header);
					return FinalizeHash(md5Inc);
				}

				// This might be a homebrew game, try to detect that
				var _3dsxTag = Encoding.ASCII.GetString(header.AsSpan(0, 4));
				if (_3dsxTag == "3DSX")
				{
					Hash3DSX(romFile, md5Inc, header);
					return FinalizeHash(md5Inc);
				}

				// Check for a raw ELF marker (AXF/ELF files)
				var elfTag = Encoding.ASCII.GetString(header.AsSpan(1, 3));
				if (header[0] == 0x7F && elfTag == "ELF")
				{
					romFile.Seek(0, SeekOrigin.Begin);
					// constrict hash buffer size to 64MiB (like RetroAchievements does)
					var elfSize = (int)Math.Min(romFile.Length, 64 * 1024 * 1024);
					var elfData = new byte[elfSize];
					if (romFile.Read(elfData, 0, elfSize) != elfSize)
					{
						throw new Exception("Failed to read AXF/ELF file");
					}

					md5Inc.AppendData(elfData);
					return FinalizeHash(md5Inc);
				}

				throw new Exception("Could not identify 3DS ROM type");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return null;
			}
		}

		private static void AesCtrTransform(Aes aes, byte[] iv, Span<byte> inputOutput)
		{
			// ECB encryptor is used for both CTR encryption and decryption
			using var encryptor = aes.CreateEncryptor();
			var blockSize = aes.BlockSize / 8;
			var outputBlockBuffer = new byte[blockSize];

			// mostly copied from tiny-AES-c (public domain)
			for (int i = 0, bi = blockSize; i < inputOutput.Length; ++i, ++bi)
			{
				if (bi == blockSize)
				{
					encryptor.TransformBlock(iv, 0, iv.Length, outputBlockBuffer, 0);
					for (bi = blockSize - 1; bi >= 0; --bi)
					{
						if (iv[bi] == 0xFF)
						{
							iv[bi] = 0;
							continue;
						}

						++iv[bi];
						break;
					}

					bi = 0;
				}

				inputOutput[i] ^= outputBlockBuffer[bi];
			}
		}

		private static string FinalizeHash(IncrementalHash md5Inc)
		{
			var hashBytes = md5Inc.GetHashAndReset();
			return hashBytes.BytesToHexString();
		}
	}
}
