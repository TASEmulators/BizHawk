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

//
//TODO - make a background thread to validate the EDC. be sure to terminate thread when the Blob disposes

//TODO - binary search the index.

//TODO - stress test the random access system:
//  pick random chunk lengths, increment counter by length, put records in list, until bin file is exhausted
//  jumble records
//  read all the records through ECM and not-ECM and make sure the contents match
 
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
				//List<IndexEntry> temp = new List<IndexEntry>();

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

					//Console.WriteLine(logOffset);
				}

				//TODO - endian bug
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
			//unlikely that we'll be hitting this from multiple threads, so low chance of contention.
			byte[] Read_SectorBuf = new byte[2352];

			int LastReadIndex = 0;

			public int Read(long byte_pos, byte[] buffer, int offset, int _count)
			{
				//Console.WriteLine("{0:X8}", byte_pos);
				//if (byte_pos + _count >= 0xb47d161)
				if (byte_pos == 0xb47c830)
				{
					int zzz = 9;
				}
				long remain = _count;
				int completed = 0;

				//we take advantage of the fact that we pretty much always read one sector at a time.
				//this would be really inefficient if we only read one byte at a time.
				//on the other hand, just in case, we could keep a cache of the most recently decoded sector. that would be easy and would solve that problem (if we had it)
				while (remain > 0)
				{
					//find the IndexEntry that corresponds to this byte position
					//int listIndex = Index.BinarySearch(idx => idx.LogicalOffset, byte_pos);
					//TODO - binary search. no builtin binary search is good enough to return something sensible for a non-match.
					//check BinarySearch extension method in Util.cs and finish it up (too complex to add in to this mess right now)
				RETRY:
					int listIndex = LastReadIndex;
					for (; ; )
					{
						IndexEntry curie = Index[listIndex];
						if (curie.LogicalOffset > byte_pos)
						{
							if (Index[listIndex - 1].LogicalOffset > byte_pos)
							{
								LastReadIndex = 0;
								goto RETRY;
							}
							break;
						}
						listIndex++;
						if (listIndex == Index.Count)
						{
							break;
						}
					}
					listIndex--;
						
					//if it wasnt found, then we didn't actually read anything
					if (listIndex == -1 || listIndex == Index.Count)
					{
						//fix O() for this operation to not be exponential
						if (LastReadIndex == 0)
							return 0;
						LastReadIndex = 0;
						goto RETRY;
					}
					LastReadIndex = listIndex;

					IndexEntry ie = Index[listIndex];

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

						//lock (Read_SectorBuf) //todo

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

						//sector is decoded to 2352 bytes. Handling doesnt depend on type from here

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