//Copyright (c) 2012 BizHawk team

//Permission is hereby granted, free of charge, to any person obtaining a copy of 
//this software and associated documentation files (the "Software"), to deal in
//the Software without restriction, including without limitation the rights to
//use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//of the Software, and to permit persons to whom the Software is furnished to do
//so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all 
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

//ECM File Format reading support

//TODO - make a background thread to validate the EDC. be sure to terminate thread when the Blob disposes
//remember: may need another stream for that. the IBlob architecture doesnt demand multithreading support

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.DiscSystem
{
	partial class Disc
	{
		class Blob_ECM : IBlob
		{
			FileStream stream;
			
			public void Dispose()
			{
				if(stream != null)
					stream.Dispose();
				stream = null;
			}

			class IndexEntry
			{
				public int Type;
				public uint Number;
				public long ECMOffset;
				public long LogicalOffset;
			}
			
			/// <summary>
			/// an index of blocks within the ECM file, for random-access.
			/// itll be sorted by logical ordering, so you can binary search for the address you want
			/// </summary>
			List<IndexEntry> Index = new List<IndexEntry>();

			/// <summary>
			/// the ECMfile-provided EDC integrity checksum. not being used right now
			/// </summary>
			int EDC;

			public long Length;

			public void Parse(string path)
			{
				stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				
				//skip header
				stream.Seek(4, SeekOrigin.Current);

				long logOffset = 0;
				for (; ; )
				{
					//read block count. this format is really stupid. maybe its good for detecting non-ecm files or something.
					int b = stream.ReadByte();
					if (b == -1) MisformedException();
					int bytes = 1;
					int T = b & 3;
					long N = (b >> 2) & 0x1F;
					int nbits = 5;
					while (b.Bit(7))
					{
						if (bytes == 5) MisformedException(); //if we're gonna need a 6th byte, this file is broken
						b = stream.ReadByte();
						bytes++;
						if (b == -1) MisformedException();
						N |= (long)(b & 0x7F) << nbits;
						nbits += 7;
					}

					//end of blocks section
					if (N == 0xFFFFFFFF)
						break;

					//the 0x80000000 business is confusing, but this is almost positively an error
					if (N >= 0x100000000)
						MisformedException();

					uint todo = (uint)N + 1;

					IndexEntry ie = new IndexEntry();
					ie.Number = todo;
					ie.ECMOffset = stream.Position;
					ie.LogicalOffset = logOffset;
					ie.Type = T;
					Index.Add(ie);

					if (T == 0)
					{
						stream.Seek(todo, SeekOrigin.Current);
						logOffset += todo;
					}
					else if (T == 1)
					{
						stream.Seek(todo * (2048 + 3), SeekOrigin.Current);
						logOffset += todo * 2352;
					}
					else if (T == 2)
					{
						stream.Seek(todo * 2052, SeekOrigin.Current);
						logOffset += todo * 2336;
					}
					else if (T == 3)
					{
						stream.Seek(todo * 2328, SeekOrigin.Current);
						logOffset += todo * 2336;
					}
					else MisformedException();
				}

				//TODO - endian bug. need endian-independent binary reader with good license
				var br = new BinaryReader(stream);
				EDC = br.ReadInt32();

				Length = logOffset;
			}

			void MisformedException()
			{
				throw new InvalidOperationException("Mis-formed ECM file");
			}

			public static bool IsECM(string path)
			{
				using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					int e = fs.ReadByte();
					int c = fs.ReadByte();
					int m = fs.ReadByte();
					int o = fs.ReadByte();
					if (e != 'E' || c != 'C' || m != 'M' || o != 0)
						return false;
				}

				return true;
			}

			/// <summary>
			/// finds the IndexEntry for the specified logical offset
			/// </summary>
			int FindInIndex(long offset, int LastReadIndex)
			{
				//try to avoid searching the index. check the last index we we used.
				for(int i=0;i<2;i++) //try 2 times
				{
					IndexEntry last = Index[LastReadIndex];
					if (LastReadIndex == Index.Count - 1)
					{
						//byte_pos would have to be after the last entry
						if (offset >= last.LogicalOffset)
						{
							return LastReadIndex;
						}
					}
					else
					{
						IndexEntry next = Index[LastReadIndex + 1];
						if (offset >= last.LogicalOffset && offset < next.LogicalOffset)
						{
							return LastReadIndex;
						}

						//well, maybe we just advanced one sector. just try again one sector ahead
						LastReadIndex++;
					}
				}

				//Console.WriteLine("binary searched"); //use this to check for mistaken LastReadIndex logic resulting in binary searches during sequential access
				int listIndex = Index.LowerBoundBinarySearch(idx => idx.LogicalOffset, offset);
				System.Diagnostics.Debug.Assert(listIndex < Index.Count);
				//Console.WriteLine("byte_pos {0:X8} using index #{1} at offset {2:X8}", offset, listIndex, Index[listIndex].LogicalOffset);

				return listIndex;
			}

			void Reconstruct(byte[] secbuf, int type)
			{
				//sync
				secbuf[0] = 0;
				for (int i = 1; i <= 10; i++)
					secbuf[i] = 0xFF;
				secbuf[11] = 0x00;

				//misc stuff
				switch (type)
				{
					case 1:
						//mode 1
						secbuf[15] = 0x01;
						//reserved
						for (int i = 0x814; i <= 0x81B; i++)
							secbuf[i] = 0x00;
						break;
					
					case 2:
					case 3:
						//mode 2
						secbuf[15] = 0x02;
						//flags - apparently CD XA specifies two copies of these 4bytes of flags. ECM didnt store the first copy; so we clone the second copy which was stored down to the spot for the first copy.
						secbuf[0x10] = secbuf[0x14];
						secbuf[0x11] = secbuf[0x15];
						secbuf[0x12] = secbuf[0x16];
						secbuf[0x13] = secbuf[0x17];
						break;
				}

				//edc
				switch (type)
				{
					case 1: ECM.PokeUint(secbuf, 0x810, ECM.EDC_Calc(secbuf, 0, 0x810)); break;
					case 2: ECM.PokeUint(secbuf, 0x818, ECM.EDC_Calc(secbuf, 16, 0x808)); break;
					case 3: ECM.PokeUint(secbuf, 0x92C, ECM.EDC_Calc(secbuf, 16, 0x91C)); break;
				}

				//ecc
				switch (type)
				{
					case 1: ECM.ECC_Populate(secbuf, 0, secbuf, 0, false); break;
					case 2: ECM.ECC_Populate(secbuf, 0, secbuf, 0, true); break;
				}

			}

			//we dont want to keep churning through this many big byte arrays while reading stuff, so we save a sector cache.
			byte[] Read_SectorBuf = new byte[2352];
			int Read_LastIndex = 0;

			public int Read(long byte_pos, byte[] buffer, int offset, int _count)
			{
				long remain = _count;
				int completed = 0;

				//we take advantage of the fact that we pretty much always read one sector at a time.
				//this would be really inefficient if we only read one byte at a time.
				//on the other hand, just in case, we could keep a cache of the most recently decoded sector. that would be easy and would solve that problem (if we had it)

				while (remain > 0)
				{
					int listIndex = FindInIndex(byte_pos, Read_LastIndex);

					IndexEntry ie = Index[listIndex];
					Read_LastIndex = listIndex;

					if (ie.Type == 0)
					{
						//type 0 is special: its just a raw blob. so all we need to do is read straight out of the stream
						long blockOffset = byte_pos - ie.LogicalOffset;
						long bytesRemainInBlock = ie.Number - blockOffset;

						long todo = remain;
						if (bytesRemainInBlock < todo)
							todo = bytesRemainInBlock;

						stream.Position = ie.ECMOffset + blockOffset;
						while (todo > 0)
						{
							int toRead;
							if (todo > int.MaxValue)
								toRead = int.MaxValue;
							else toRead = (int)todo;

							int done = stream.Read(buffer, offset, toRead);
							if (done != toRead)
								return completed;

							completed += done;
							remain -= done;
							todo -= done;
							offset += done;
							byte_pos += done;
						}

						//done reading the raw block; go back to check for another block
						continue;
					} //if(type 0)
					else
					{
						//these are sector-based types. they have similar handling.

						long blockOffset = byte_pos - ie.LogicalOffset;

						//figure out which sector within the block we're in
						int outSecSize;
						int inSecSize;
						int outSecOffset;
						if (ie.Type == 1) { outSecSize = 2352; inSecSize = 2048; outSecOffset = 0; }
						else if (ie.Type == 2) { outSecSize = 2336; inSecSize = 2052; outSecOffset = 16; }
						else if (ie.Type == 3) { outSecSize = 2336; inSecSize = 2328; outSecOffset = 16; }
						else throw new InvalidOperationException();

						long secNumberInBlock = blockOffset / outSecSize;
						long secOffsetInEcm = secNumberInBlock * outSecSize;
						long bytesAskedIntoSector = blockOffset % outSecSize;
						long bytesRemainInSector = outSecSize - bytesAskedIntoSector;

						long todo = remain;
						if (bytesRemainInSector < todo)
							todo = bytesRemainInSector;

						//move stream to beginning of this sector in ecm
						stream.Position = ie.ECMOffset + inSecSize * secNumberInBlock;

						//read and decode the sector
						switch (ie.Type)
						{
							case 1:
								//TODO - read first 3 bytes
								if (stream.Read(Read_SectorBuf, 16, 2048) != 2048)
									return completed;
								Reconstruct(Read_SectorBuf, 1);
								break;
							case 2:
								if (stream.Read(Read_SectorBuf, 20, 2052) != 2052)
									return completed;
								Reconstruct(Read_SectorBuf, 2);
								break;
							case 3:
								if (stream.Read(Read_SectorBuf, 20, 2328) != 2328)
									return completed;
								Reconstruct(Read_SectorBuf, 3);
								break;
						}

						//sector is decoded to 2352 bytes. Handling doesnt depend much on type from here

						Array.Copy(Read_SectorBuf, (int)bytesAskedIntoSector + outSecOffset, buffer, offset, todo);
						int done = (int)todo;

						offset += done;
						completed += done;
						remain -= done;
						byte_pos += done;

					} //not type 0

				} // while(Remain)

				return completed;
			}
		}
	}
}

//-------------------------------------------------------------------------------------------

//The ecm file begins with 4 bytes: ECM\0

//then, repeat forever processing these blocks:
//  Read the block header bytes. The block header is terminated after processing a byte without 0x80 set.
//  The block header contains these bits packed in the bottom 7 LSB of successive bytes:
//    xNNNNNNN NNNNNNNN NNNNNNNN NNNNNNNN TTT
//      N: a Number
//      T: the type of the sector
//  If you encounter a Number of 0xFFFFFFFF then the blocks section is finished.
//  If you need a 6th byte for the block header, then the block header is erroneous
//  Increment Number, since storing 0 wouldve been useless.

//  Now, process the block.
//    Type 0:
//      Read Number bytes from the ECM file and write to the output stream.
//        This block isn't necessarily a multiple of any particular sector size.
//      accumulate all those bytes through the EDC

//    Type 1: For Number of sectors:
//      Read sector bytes 12,13,14
//      Read 2048 sector bytes @16
//      Reconstruct sector as type 1
//      accumulate 2352 sector bytes @0 through the EDC
//      write 2352 sector byte @0 to the output stream

//    Type 2: For Number of sectors:
//      Read 2052 sector bytes @20
//      Reconstruct sector as type 2
//      accumulate 2336 sector bytes @16 through the EDC
//      write 2336 sector bytes @16 to the output stream

//    Type 3: For Number of sectors:
//      Read 2328 sector bytes @20
//      Reconstruct sector as type 3
//      accumulate 2336 sector bytes @16 through the EDC
//      write 2336 sector bytes @16 to the output stream

//After encountering our end marker and exiting the block processing section:
//read a 32bit little endian value, which should be the output of the EDC (just a little check to make sure the file is valid)
//That's the end of the file
