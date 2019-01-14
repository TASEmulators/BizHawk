#if !Rar2017_64bit
using nint = System.Int32;
using nuint = System.UInt32;
using size_t = System.UInt32;
#else
using nint = System.Int64;
using nuint = System.UInt64;
using size_t = System.UInt64;
#endif
using int64 = System.Int64;

using static SharpCompress.Compressors.Rar.UnpackV2017.PackDef;

namespace SharpCompress.Compressors.Rar.UnpackV2017
{
    internal partial class Unpack
    {
      private void InsertOldDist(uint Distance)
{
  OldDist[3]=OldDist[2];
  OldDist[2]=OldDist[1];
  OldDist[1]=OldDist[0];
  OldDist[0]=Distance;
}

//#ifdef _MSC_VER
//#define FAST_MEMCPY
//#endif

      private void CopyString(uint Length,uint Distance)
{
  size_t SrcPtr=UnpPtr-Distance;

  if (SrcPtr<MaxWinSize-MAX_LZ_MATCH && UnpPtr<MaxWinSize-MAX_LZ_MATCH)
  {
    // If we are not close to end of window, we do not need to waste time
    // to "& MaxWinMask" pointer protection.

    // TODO: sharpcompress: non-optimized loop, we may be able to unroll and speed up
    var Window = this.Window;
    while (Length-- > 0)
    {
        Window[UnpPtr++] = Window[SrcPtr++];
    }

//    byte *Src=Window+SrcPtr;
//    byte *Dest=Window+UnpPtr;
//    UnpPtr+=Length;
//
//#if FAST_MEMCPY
//    if (Distance<Length) // Overlapping strings
//#endif
//      while (Length>=8)
//      {
//        Dest[0]=Src[0];
//        Dest[1]=Src[1];
//        Dest[2]=Src[2];
//        Dest[3]=Src[3];
//        Dest[4]=Src[4];
//        Dest[5]=Src[5];
//        Dest[6]=Src[6];
//        Dest[7]=Src[7];
//
//        Src+=8;
//        Dest+=8;
//        Length-=8;
//      }
//#if FAST_MEMCPY
//    else
//      while (Length>=8)
//      {
//        // In theory we still could overlap here.
//        // Supposing Distance == MaxWinSize - 1 we have memcpy(Src, Src + 1, 8).
//        // But for real RAR archives Distance <= MaxWinSize - MAX_LZ_MATCH
//        // always, so overlap here is impossible.
//
//        // This memcpy expanded inline by MSVC. We could also use uint64
//        // assignment, which seems to provide about the same speed.
//        memcpy(Dest,Src,8); 
//
//        Src+=8;
//        Dest+=8;
//        Length-=8;
//      }
//#endif
//
//    // Unroll the loop for 0 - 7 bytes left. Note that we use nested "if"s.
//    if (Length>0) { Dest[0]=Src[0];
//    if (Length>1) { Dest[1]=Src[1];
//    if (Length>2) { Dest[2]=Src[2];
//    if (Length>3) { Dest[3]=Src[3];
//    if (Length>4) { Dest[4]=Src[4];
//    if (Length>5) { Dest[5]=Src[5];
//    if (Length>6) { Dest[6]=Src[6]; } } } } } } } // Close all nested "if"s.
  }
  else
    while (Length-- > 0) // Slow copying with all possible precautions.
    {
      Window[UnpPtr]=Window[SrcPtr++ & MaxWinMask];
      // We need to have masked UnpPtr after quit from loop, so it must not
      // be replaced with 'Window[UnpPtr++ & MaxWinMask]'
      UnpPtr=(UnpPtr+1) & MaxWinMask;
    }
}

      private uint DecodeNumber(BitInput Inp,DecodeTable Dec)
{
  // Left aligned 15 bit length raw bit field.
  uint BitField=Inp.getbits() & 0xfffe;

  if (BitField<Dec.DecodeLen[Dec.QuickBits])
  {
    uint Code=BitField>>(int)(16-Dec.QuickBits);
    Inp.addbits(Dec.QuickLen[Code]);
    return Dec.QuickNum[Code];
  }

  // Detect the real bit length for current code.
  uint Bits=15;
  for (uint I=Dec.QuickBits+1;I<15;I++)
    if (BitField<Dec.DecodeLen[I])
    {
      Bits=I;
      break;
    }

  Inp.addbits(Bits);
  
  // Calculate the distance from the start code for current bit length.
  uint Dist=BitField-Dec.DecodeLen[Bits-1];

  // Start codes are left aligned, but we need the normal right aligned
  // number. So we shift the distance to the right.
  Dist>>=(int)(16-Bits);

  // Now we can calculate the position in the code list. It is the sum
  // of first position for current bit length and right aligned distance
  // between our bit field and start code for current bit length.
  uint Pos=Dec.DecodePos[Bits]+Dist;

  // Out of bounds safety check required for damaged archives.
  if (Pos>=Dec.MaxNum)
    Pos=0;

  // Convert the position in the code list to position in alphabet
  // and return it.
  return Dec.DecodeNum[Pos];
}

      private uint SlotToLength(BitInput Inp,uint Slot)
{
  uint LBits,Length=2;
  if (Slot<8)
  {
    LBits=0;
    Length+=Slot;
  }
  else
  {
    LBits=Slot/4-1;
    Length+=(4 | (Slot & 3)) << (int)LBits;
  }

  if (LBits>0)
  {
    Length+=Inp.getbits()>>(int)(16-LBits);
    Inp.addbits(LBits);
  }
  return Length;
}

    }
}
