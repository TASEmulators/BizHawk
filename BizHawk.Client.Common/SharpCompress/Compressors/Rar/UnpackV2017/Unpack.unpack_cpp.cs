#if !Rar2017_64bit
using nint = System.Int32;
using nuint = System.UInt32;
using size_t = System.UInt32;
#else
using nint = System.Int64;
using nuint = System.UInt64;
using size_t = System.UInt64;
#endif

using System;
using SharpCompress.Common;
using static SharpCompress.Compressors.Rar.UnpackV2017.UnpackGlobal;
using static SharpCompress.Compressors.Rar.UnpackV2017.PackDef;

namespace SharpCompress.Compressors.Rar.UnpackV2017
{
    internal sealed partial class Unpack : BitInput
    {

public Unpack(/* ComprDataIO *DataIO */)
//:Inp(true),VMCodeInp(true)
: base(true)
{
  _UnpackCtor();

  //UnpIO=DataIO;
  Window=null;
  Fragmented=false;
  Suspended=false;
  UnpAllBuf=false;
  UnpSomeRead=false;
#if RarV2017_RAR_SMP
  MaxUserThreads=1;
  UnpThreadPool=CreateThreadPool();
  ReadBufMT=null;
  UnpThreadData=null;
#endif
  MaxWinSize=0;
  MaxWinMask=0;

  // Perform initialization, which should be done only once for all files.
  // It prevents crash if first DoUnpack call is later made with wrong
  // (true) 'Solid' value.
  UnpInitData(false);
#if !RarV2017_SFX_MODULE
  // RAR 1.5 decompression initialization
  UnpInitData15(false);
  InitHuff();
#endif
}

// later: may need Dispose() if we support thread pool
//Unpack::~Unpack()
//{
//  InitFilters30(false);
//
//  if (Window!=null)
//    free(Window);
//#if RarV2017_RAR_SMP
//  DestroyThreadPool(UnpThreadPool);
//  delete[] ReadBufMT;
//  delete[] UnpThreadData;
//#endif
//}

      private void Init(size_t WinSize,bool Solid)
{
  // If 32-bit RAR unpacks an archive with 4 GB dictionary, the window size
  // will be 0 because of size_t overflow. Let's issue the memory error.
  if (WinSize==0)
    //ErrHandler.MemoryError();
    throw new InvalidFormatException("invalid window size (possibly due to a rar file with a 4GB being unpacked on a 32-bit platform)");

  // Minimum window size must be at least twice more than maximum possible
  // size of filter block, which is 0x10000 in RAR now. If window size is
  // smaller, we can have a block with never cleared flt->NextWindow flag
  // in UnpWriteBuf(). Minimum window size 0x20000 would be enough, but let's
  // use 0x40000 for extra safety and possible filter area size expansion.
  const size_t MinAllocSize=0x40000;
  if (WinSize<MinAllocSize)
    WinSize=MinAllocSize;

  if (WinSize<=MaxWinSize) // Use the already allocated window.
    return;
  if ((WinSize>>16)>0x10000) // Window size must not exceed 4 GB.
    return;

  // Archiving code guarantees that window size does not grow in the same
  // solid stream. So if we are here, we are either creating a new window
  // or increasing the size of non-solid window. So we could safely reject
  // current window data without copying them to a new window, though being
  // extra cautious, we still handle the solid window grow case below.
  bool Grow=Solid && (Window!=null || Fragmented);

  // We do not handle growth for existing fragmented window.
  if (Grow && Fragmented)
    //throw std::bad_alloc();
    throw new InvalidFormatException("Grow && Fragmented");

  byte[] NewWindow=Fragmented ? null : new byte[WinSize];

  if (NewWindow==null)
    if (Grow || WinSize<0x1000000)
    {
      // We do not support growth for new fragmented window.
      // Also exclude RAR4 and small dictionaries.
      //throw std::bad_alloc();
      throw new InvalidFormatException("Grow || WinSize<0x1000000");
    }
    else
    {
      if (Window!=null) // If allocated by preceding files.
      {
        //free(Window);
        Window=null;
      }
      FragWindow.Init(WinSize);
      Fragmented=true;
    }

  if (!Fragmented)
  {
    // Clean the window to generate the same output when unpacking corrupt
    // RAR files, which may access unused areas of sliding dictionary.
    // sharpcompress: don't need this, freshly allocated above
    //memset(NewWindow,0,WinSize);


    // If Window is not NULL, it means that window size has grown.
    // In solid streams we need to copy data to a new window in such case.
    // RAR archiving code does not allow it in solid streams now,
    // but let's implement it anyway just in case we'll change it sometimes.
    if (Grow)
      for (size_t I=1;I<=MaxWinSize;I++)
        NewWindow[(UnpPtr-I)&(WinSize-1)]=Window[(UnpPtr-I)&(MaxWinSize-1)];

    //if (Window!=null)
    //  free(Window);
    Window=NewWindow;
  }

  MaxWinSize=WinSize;
  MaxWinMask=MaxWinSize-1;
}

      private void DoUnpack(uint Method,bool Solid)
{
  // Methods <50 will crash in Fragmented mode when accessing NULL Window.
  // They cannot be called in such mode now, but we check it below anyway
  // just for extra safety.
  switch(Method)
  {
#if !RarV2017_SFX_MODULE
    case 15: // rar 1.5 compression
      if (!Fragmented)
        Unpack15(Solid);
      break;
    case 20: // rar 2.x compression
    case 26: // files larger than 2GB
      if (!Fragmented)
        Unpack20(Solid);
      break;
#endif
#if !RarV2017_RAR5ONLY
    case 29: // rar 3.x compression
      if (!Fragmented)
        throw new NotImplementedException();
      break;
#endif
    case 50: // RAR 5.0 compression algorithm.
#if RarV2017_RAR_SMP
      if (MaxUserThreads>1)
      {
//      We do not use the multithreaded unpack routine to repack RAR archives
//      in 'suspended' mode, because unlike the single threaded code it can
//      write more than one dictionary for same loop pass. So we would need
//      larger buffers of unknown size. Also we do not support multithreading
//      in fragmented window mode.
          if (!Fragmented)
          {
            Unpack5MT(Solid);
            break;
          }
      }
#endif
      Unpack5(Solid);
      break;
#if !Rar2017_NOSTRICT
    default: throw new InvalidFormatException("unknown compression method " + Method);
#endif
  }
}

      private void UnpInitData(bool Solid)
{
  if (!Solid)
  {
    Utility.Memset<uint>(OldDist, 0, OldDist.Length);
    OldDistPtr=0;
    LastDist=LastLength=0;
//    memset(Window,0,MaxWinSize);
    //memset(&BlockTables,0,sizeof(BlockTables));
    BlockTables = new UnpackBlockTables();
    // sharpcompress: no default ctor for struct
    BlockTables.Init();
    UnpPtr=WrPtr=0;
    WriteBorder=Math.Min(MaxWinSize,UNPACK_MAX_WRITE)&MaxWinMask;
  }
  // Filters never share several solid files, so we can safely reset them
  // even in solid archive.
  InitFilters();

  Inp.InitBitInput();
  WrittenFileSize=0;
  ReadTop=0;
  ReadBorder=0;

  //memset(&BlockHeader,0,sizeof(BlockHeader));
  BlockHeader = new UnpackBlockHeader();
  BlockHeader.BlockSize=-1;  // '-1' means not defined yet.
#if !RarV2017_SFX_MODULE
  UnpInitData20(Solid);
#endif
  //UnpInitData30(Solid);
  UnpInitData50(Solid);
}


// LengthTable contains the length in bits for every element of alphabet.
// Dec is the structure to decode Huffman code/
// Size is size of length table and DecodeNum field in Dec structure,
      private void MakeDecodeTables(byte[] LengthTable, int offset, DecodeTable Dec,uint Size)
{
  // Size of alphabet and DecodePos array.
  Dec.MaxNum=Size;

  // Calculate how many entries for every bit length in LengthTable we have.
  uint[] LengthCount = new uint[16];
  //memset(LengthCount,0,sizeof(LengthCount));
  for (size_t I=0;I<Size;I++)
    LengthCount[LengthTable[offset+I] & 0xf]++;

  // We must not calculate the number of zero length codes.
  LengthCount[0]=0;

  // Set the entire DecodeNum to zero.
  //memset(Dec->DecodeNum,0,Size*sizeof(*Dec->DecodeNum));
  Utility.FillFast<ushort>(Dec.DecodeNum, 0);

  // Initialize not really used entry for zero length code.
  Dec.DecodePos[0]=0;

  // Start code for bit length 1 is 0.
  Dec.DecodeLen[0]=0;

  // Right aligned upper limit code for current bit length.
  uint UpperLimit=0;

  for (int I=1;I<16;I++)
  {
    // Adjust the upper limit code.
    UpperLimit+=LengthCount[I];

    // Left aligned upper limit code.
    uint LeftAligned=UpperLimit<<(16-I);

    // Prepare the upper limit code for next bit length.
    UpperLimit*=2;

    // Store the left aligned upper limit code.
    Dec.DecodeLen[I]=(uint)LeftAligned;

    // Every item of this array contains the sum of all preceding items.
    // So it contains the start position in code list for every bit length. 
    Dec.DecodePos[I]=Dec.DecodePos[I-1]+LengthCount[I-1];
  }

  // Prepare the copy of DecodePos. We'll modify this copy below,
  // so we cannot use the original DecodePos.
  uint[] CopyDecodePos = new uint[Dec.DecodePos.Length];
  //memcpy(CopyDecodePos,Dec->DecodePos,sizeof(CopyDecodePos));
  Array.Copy(Dec.DecodePos, 0, CopyDecodePos, 0, CopyDecodePos.Length);

  // For every bit length in the bit length table and so for every item
  // of alphabet.
  for (uint I=0;I<Size;I++)
  {
    // Get the current bit length.
    byte _CurBitLength=(byte)(LengthTable[offset+I] & 0xf);

    if (_CurBitLength!=0)
    {
      // Last position in code list for current bit length.
      uint LastPos=CopyDecodePos[_CurBitLength];

      // Prepare the decode table, so this position in code list will be
      // decoded to current alphabet item number.
      Dec.DecodeNum[LastPos]=(ushort)I;

      // We'll use next position number for this bit length next time.
      // So we pass through the entire range of positions available
      // for every bit length.
      CopyDecodePos[_CurBitLength]++;
    }
  }

  // Define the number of bits to process in quick mode. We use more bits
  // for larger alphabets. More bits means that more codes will be processed
  // in quick mode, but also that more time will be spent to preparation
  // of tables for quick decode.
  switch (Size)
  {
    case NC:
    case NC20:
    case NC30:
      Dec.QuickBits=MAX_QUICK_DECODE_BITS;
      break;
    default:
      Dec.QuickBits=MAX_QUICK_DECODE_BITS-3;
      break;
  }

  // Size of tables for quick mode.
  uint QuickDataSize=1U<<(int)Dec.QuickBits;

  // Bit length for current code, start from 1 bit codes. It is important
  // to use 1 bit instead of 0 for minimum code length, so we are moving
  // forward even when processing a corrupt archive.
  //uint CurBitLength=1;
  byte CurBitLength=1;

  // For every right aligned bit string which supports the quick decoding.
  for (uint Code=0;Code<QuickDataSize;Code++)
  {
    // Left align the current code, so it will be in usual bit field format.
    uint BitField=Code<<(int)(16-Dec.QuickBits);

    // Prepare the table for quick decoding of bit lengths.
  
    // Find the upper limit for current bit field and adjust the bit length
    // accordingly if necessary.
    while (CurBitLength<Dec.DecodeLen.Length && BitField>=Dec.DecodeLen[CurBitLength])
      CurBitLength++;

    // Translation of right aligned bit string to bit length.
    Dec.QuickLen[Code]=CurBitLength;

    // Prepare the table for quick translation of position in code list
    // to position in alphabet.

    // Calculate the distance from the start code for current bit length.
    uint Dist=BitField-Dec.DecodeLen[CurBitLength-1];

    // Right align the distance.
    Dist>>=(16-CurBitLength);

    // Now we can calculate the position in the code list. It is the sum
    // of first position for current bit length and right aligned distance
    // between our bit field and start code for current bit length.
    uint Pos;
    if (CurBitLength<Dec.DecodePos.Length &&
        (Pos=Dec.DecodePos[CurBitLength]+Dist)<Size)
    {
      // Define the code to alphabet number translation.
      Dec.QuickNum[Code]=Dec.DecodeNum[Pos];
    }
    else
    {
      // Can be here for length table filled with zeroes only (empty).
      Dec.QuickNum[Code]=0;
    }
  }
}

    }
}
