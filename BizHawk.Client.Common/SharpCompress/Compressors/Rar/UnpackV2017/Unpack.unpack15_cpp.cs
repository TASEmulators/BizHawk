using static SharpCompress.Compressors.Rar.UnpackV2017.Unpack.Unpack15Local;

namespace SharpCompress.Compressors.Rar.UnpackV2017
{
    internal partial class Unpack
    {
      private const int STARTL1  =2;

      private static readonly uint[] DecL1={0x8000,0xa000,0xc000,0xd000,0xe000,0xea00,
                             0xee00,0xf000,0xf200,0xf200,0xffff};

      private static readonly uint[] PosL1={0,0,0,2,3,5,7,11,16,20,24,32,32};

      private const int STARTL2  =3;

      private static readonly uint[] DecL2={0xa000,0xc000,0xd000,0xe000,0xea00,0xee00,
                             0xf000,0xf200,0xf240,0xffff};

      private static readonly uint[] PosL2={0,0,0,0,5,7,9,13,18,22,26,34,36};

      private const int STARTHF0  =4;

      private static readonly uint[] DecHf0={0x8000,0xc000,0xe000,0xf200,0xf200,0xf200,
                              0xf200,0xf200,0xffff};

      private static readonly uint[] PosHf0={0,0,0,0,0,8,16,24,33,33,33,33,33};

      private const int STARTHF1  =5;

      private static readonly uint[] DecHf1={0x2000,0xc000,0xe000,0xf000,0xf200,0xf200,
                              0xf7e0,0xffff};

      private static readonly uint[] PosHf1={0,0,0,0,0,0,4,44,60,76,80,80,127};

      private const int STARTHF2  =5;

      private static readonly uint[] DecHf2={0x1000,0x2400,0x8000,0xc000,0xfa00,0xffff,
                              0xffff,0xffff};

      private static readonly uint[] PosHf2={0,0,0,0,0,0,2,7,53,117,233,0,0};

      private const int STARTHF3  =6;

      private static readonly uint[] DecHf3={0x800,0x2400,0xee00,0xfe80,0xffff,0xffff,
                              0xffff};

      private static readonly uint[] PosHf3={0,0,0,0,0,0,0,2,16,218,251,0,0};

      private const int STARTHF4  =8;
      private static readonly uint[] DecHf4={0xff00,0xffff,0xffff,0xffff,0xffff,0xffff};
      private static readonly uint[] PosHf4={0,0,0,0,0,0,0,0,0,255,0,0,0};

      private void Unpack15(bool Solid)
{
  UnpInitData(Solid);
  UnpInitData15(Solid);
  UnpReadBuf();
  if (!Solid)
  {
    InitHuff();
    UnpPtr=0;
  }
  else
    UnpPtr=WrPtr;
  --DestUnpSize;
  if (DestUnpSize>=0)
  {
    GetFlagsBuf();
    FlagsCnt=8;
  }

  while (DestUnpSize>=0)
  {
    UnpPtr&=MaxWinMask;

    if (Inp.InAddr>ReadTop-30 && !UnpReadBuf())
      break;
    if (((WrPtr-UnpPtr) & MaxWinMask)<270 && WrPtr!=UnpPtr)
      UnpWriteBuf20();
    if (StMode != 0)
    {
      HuffDecode();
      continue;
    }

    if (--FlagsCnt < 0)
    {
      GetFlagsBuf();
      FlagsCnt=7;
    }

    if ((FlagBuf & 0x80) != 0)
    {
      FlagBuf<<=1;
      if (Nlzb > Nhfb)
        LongLZ();
      else
        HuffDecode();
    }
    else
    {
      FlagBuf<<=1;
      if (--FlagsCnt < 0)
      {
        GetFlagsBuf();
        FlagsCnt=7;
      }
      if ((FlagBuf & 0x80) != 0)
      {
        FlagBuf<<=1;
        if (Nlzb > Nhfb)
          HuffDecode();
        else
          LongLZ();
      }
      else
      {
        FlagBuf<<=1;
        ShortLZ();
      }
    }
  }
  UnpWriteBuf20();
}


//#define GetShortLen1(pos) ((pos)==1 ? Buf60+3:ShortLen1[pos])
      private uint GetShortLen1(uint pos) { return ((pos)==1 ? (uint)(Buf60+3):ShortLen1[pos]); }
//#define GetShortLen2(pos) ((pos)==3 ? Buf60+3:ShortLen2[pos])
      private uint GetShortLen2(uint pos) { return ((pos)==3 ? (uint)(Buf60+3):ShortLen2[pos]); }

internal static class Unpack15Local {
  public static readonly uint[] ShortLen1={1,3,4,4,5,6,7,8,8,4,4,5,6,6,4,0};
  public static readonly  uint[] ShortXor1={0,0xa0,0xd0,0xe0,0xf0,0xf8,0xfc,0xfe,
                                   0xff,0xc0,0x80,0x90,0x98,0x9c,0xb0};
  public static readonly  uint[] ShortLen2={2,3,3,3,4,4,5,6,6,4,4,5,6,6,4,0};
  public static readonly  uint[] ShortXor2={0,0x40,0x60,0xa0,0xd0,0xe0,0xf0,0xf8,
                                   0xfc,0xc0,0x80,0x90,0x98,0x9c,0xb0};
}

      private void ShortLZ()
{
  uint Length,SaveLength;
  uint LastDistance;
  uint Distance;
  int DistancePlace;
  NumHuf=0;

  uint BitField=Inp.fgetbits();
  if (LCount==2)
  {
    Inp.faddbits(1);
    if (BitField >= 0x8000)
    {
      CopyString15((uint)LastDist,LastLength);
      return;
    }
    BitField <<= 1;
    LCount=0;
  }

  BitField>>=8;

//  not thread safe, replaced by GetShortLen1 and GetShortLen2 macro
//  ShortLen1[1]=ShortLen2[3]=Buf60+3;

  if (AvrLn1<37)
  {
    for (Length=0;;Length++)
      if (((BitField^ShortXor1[Length]) & (~(0xff>>(int)GetShortLen1(Length))))==0)
        break;
    Inp.faddbits(GetShortLen1(Length));
  }
  else
  {
    for (Length=0;;Length++)
      if (((BitField^ShortXor2[Length]) & (~(0xff>>(int)GetShortLen2(Length))))==0)
        break;
    Inp.faddbits(GetShortLen2(Length));
  }

  if (Length >= 9)
  {
    if (Length == 9)
    {
      LCount++;
      CopyString15((uint)LastDist,LastLength);
      return;
    }
    if (Length == 14)
    {
      LCount=0;
      Length=DecodeNum(Inp.fgetbits(),STARTL2,DecL2,PosL2)+5;
      Distance=(Inp.fgetbits()>>1) | 0x8000;
      Inp.faddbits(15);
      LastLength=Length;
      LastDist=Distance;
      CopyString15(Distance,Length);
      return;
    }

    LCount=0;
    SaveLength=Length;
    Distance=OldDist[(OldDistPtr-(Length-9)) & 3];
    Length=DecodeNum(Inp.fgetbits(),STARTL1,DecL1,PosL1)+2;
    if (Length==0x101 && SaveLength==10)
    {
      Buf60 ^= 1;
      return;
    }
    if (Distance > 256)
      Length++;
    if (Distance >= MaxDist3)
      Length++;

    OldDist[OldDistPtr++]=Distance;
    OldDistPtr = OldDistPtr & 3;
    LastLength=Length;
    LastDist=Distance;
    CopyString15(Distance,Length);
    return;
  }

  LCount=0;
  AvrLn1 += Length;
  AvrLn1 -= AvrLn1 >> 4;

  DistancePlace=(int)(DecodeNum(Inp.fgetbits(),STARTHF2,DecHf2,PosHf2) & 0xff);
  Distance=ChSetA[DistancePlace];
  if (--DistancePlace != -1)
  {
    LastDistance=ChSetA[DistancePlace];
    ChSetA[DistancePlace+1]=(ushort)LastDistance;
    ChSetA[DistancePlace]=(ushort)Distance;
  }
  Length+=2;
  OldDist[OldDistPtr++] = ++Distance;
  OldDistPtr = OldDistPtr & 3;
  LastLength=Length;
  LastDist=Distance;
  CopyString15(Distance,Length);
}

      private void LongLZ()
{
  uint Length;
  uint Distance;
  uint DistancePlace,NewDistancePlace;
  uint OldAvr2,OldAvr3;

  NumHuf=0;
  Nlzb+=16;
  if (Nlzb > 0xff)
  {
    Nlzb=0x90;
    Nhfb >>= 1;
  }
  OldAvr2=AvrLn2;

  uint BitField=Inp.fgetbits();
  if (AvrLn2 >= 122)
    Length=DecodeNum(BitField,STARTL2,DecL2,PosL2);
  else
    if (AvrLn2 >= 64)
      Length=DecodeNum(BitField,STARTL1,DecL1,PosL1);
    else
      if (BitField < 0x100)
      {
        Length=BitField;
        Inp.faddbits(16);
      }
      else
      {
        for (Length=0;((BitField<<(int)Length)&0x8000)==0;Length++)
          ;
        Inp.faddbits(Length+1);
      }

  AvrLn2 += Length;
  AvrLn2 -= AvrLn2 >> 5;

  BitField=Inp.fgetbits();
  if (AvrPlcB > 0x28ff)
    DistancePlace=DecodeNum(BitField,STARTHF2,DecHf2,PosHf2);
  else
    if (AvrPlcB > 0x6ff)
      DistancePlace=DecodeNum(BitField,STARTHF1,DecHf1,PosHf1);
    else
      DistancePlace=DecodeNum(BitField,STARTHF0,DecHf0,PosHf0);

  AvrPlcB += DistancePlace;
  AvrPlcB -= AvrPlcB >> 8;
  while (true)
  {
    Distance = ChSetB[DistancePlace & 0xff];
    NewDistancePlace = NToPlB[Distance++ & 0xff]++;
    if ((Distance & 0xff) != 0)
      CorrHuff(ChSetB,NToPlB);
    else
      break;
  }

  ChSetB[DistancePlace & 0xff]=ChSetB[NewDistancePlace];
  ChSetB[NewDistancePlace]=(ushort)Distance;

  Distance=((Distance & 0xff00) | (Inp.fgetbits() >> 8)) >> 1;
  Inp.faddbits(7);

  OldAvr3=AvrLn3;
  if (Length!=1 && Length!=4)
    if (Length==0 && Distance <= MaxDist3)
    {
      AvrLn3++;
      AvrLn3 -= AvrLn3 >> 8;
    }
    else
      if (AvrLn3 > 0)
        AvrLn3--;
  Length+=3;
  if (Distance >= MaxDist3)
    Length++;
  if (Distance <= 256)
    Length+=8;
  if (OldAvr3 > 0xb0 || AvrPlc >= 0x2a00 && OldAvr2 < 0x40)
    MaxDist3=0x7f00;
  else
    MaxDist3=0x2001;
  OldDist[OldDistPtr++]=Distance;
  OldDistPtr = OldDistPtr & 3;
  LastLength=Length;
  LastDist=Distance;
  CopyString15(Distance,Length);
}

      private void HuffDecode()
{
  uint CurByte,NewBytePlace;
  uint Length;
  uint Distance;
  int BytePlace;

  uint BitField=Inp.fgetbits();

  if (AvrPlc > 0x75ff)
    BytePlace=(int)DecodeNum(BitField,STARTHF4,DecHf4,PosHf4);
  else
    if (AvrPlc > 0x5dff)
      BytePlace=(int)DecodeNum(BitField,STARTHF3,DecHf3,PosHf3);
    else
      if (AvrPlc > 0x35ff)
        BytePlace=(int)DecodeNum(BitField,STARTHF2,DecHf2,PosHf2);
      else
        if (AvrPlc > 0x0dff)
          BytePlace=(int)DecodeNum(BitField,STARTHF1,DecHf1,PosHf1);
        else
          BytePlace=(int)DecodeNum(BitField,STARTHF0,DecHf0,PosHf0);
  BytePlace&=0xff;
  if (StMode != 0)
  {
    if (BytePlace==0 && BitField > 0xfff)
      BytePlace=0x100;
    if (--BytePlace==-1)
    {
      BitField=Inp.fgetbits();
      Inp.faddbits(1);
      if ((BitField & 0x8000) != 0)
      {
        NumHuf=StMode=0;
        return;
      }
      else
      {
        Length = (BitField & 0x4000) != 0 ? 4U : 3;
        Inp.faddbits(1);
        Distance=DecodeNum(Inp.fgetbits(),STARTHF2,DecHf2,PosHf2);
        Distance = (Distance << 5) | (Inp.fgetbits() >> 11);
        Inp.faddbits(5);
        CopyString15(Distance,Length);
        return;
      }
    }
  }
  else
    if (NumHuf++ >= 16 && FlagsCnt==0)
      StMode=1;
  AvrPlc += (uint)BytePlace;
  AvrPlc -= AvrPlc >> 8;
  Nhfb+=16;
  if (Nhfb > 0xff)
  {
    Nhfb=0x90;
    Nlzb >>= 1;
  }

  Window[UnpPtr++]=(byte)(ChSet[BytePlace]>>8);
  --DestUnpSize;

  while (true)
  {
    CurByte=ChSet[BytePlace];
    NewBytePlace=NToPl[CurByte++ & 0xff]++;
    if ((CurByte & 0xff) > 0xa1)
      CorrHuff(ChSet,NToPl);
    else
      break;
  }

  ChSet[BytePlace]=ChSet[NewBytePlace];
  ChSet[NewBytePlace]=(ushort)CurByte;
}

      private void GetFlagsBuf()
{
  uint Flags,NewFlagsPlace;
  uint FlagsPlace=DecodeNum(Inp.fgetbits(),STARTHF2,DecHf2,PosHf2);

  // Our Huffman table stores 257 items and needs all them in other parts
  // of code such as when StMode is on, so the first item is control item.
  // While normally we do not use the last item to code the flags byte here,
  // we need to check for value 256 when unpacking in case we unpack
  // a corrupt archive.
  if (FlagsPlace>=ChSetC.Length)
    return;

  while (true)
  {
    Flags=ChSetC[FlagsPlace];
    FlagBuf=Flags>>8;
    NewFlagsPlace=NToPlC[Flags++ & 0xff]++;
    if ((Flags & 0xff) != 0)
      break;
    CorrHuff(ChSetC,NToPlC);
  }

  ChSetC[FlagsPlace]=ChSetC[NewFlagsPlace];
  ChSetC[NewFlagsPlace]=(ushort)Flags;
}

      private void UnpInitData15(bool Solid)
{
  if (!Solid)
  {
    AvrPlcB=AvrLn1=AvrLn2=AvrLn3=0;
    NumHuf=Buf60=0;
    AvrPlc=0x3500;
    MaxDist3=0x2001;
    Nhfb=Nlzb=0x80;
  }
  FlagsCnt=0;
  FlagBuf=0;
  StMode=0;
  LCount=0;
  ReadTop=0;
}

      private void InitHuff()
{
  for (uint I=0;I<256;I++)
  {
    ChSet[I]=ChSetB[I]=(ushort)(I<<8);
    ChSetA[I]=(ushort)I;
    ChSetC[I]=(ushort)(((~I+1) & 0xff)<<8);
  }
  Utility.Memset(NToPl,0,NToPl.Length);
  Utility.Memset(NToPlB,0,NToPlB.Length);
  Utility.Memset(NToPlC,0,NToPlC.Length);
  CorrHuff(ChSetB,NToPlB);
}

      private void CorrHuff(ushort[] CharSet,byte[] NumToPlace)
{
  int I,J;
  for (I=7;I>=0;I--)
    for (J=0;J<32;J++)
      CharSet[J]=(ushort)((CharSet[J] & ~0xff) | I);
  Utility.Memset(NumToPlace,0,NToPl.Length);
  for (I=6;I>=0;I--)
    NumToPlace[I]=(byte)((7-I)*32);
}

      private void CopyString15(uint Distance,uint Length)
{
  DestUnpSize-=Length;
  while (Length-- != 0)
  {
    Window[UnpPtr]=Window[(UnpPtr-Distance) & MaxWinMask];
    UnpPtr=(UnpPtr+1) & MaxWinMask;
  }
}

      private uint DecodeNum(uint Num,uint StartPos,uint[] DecTab,uint[] PosTab)
{
  int I;
  for (Num&=0xfff0,I=0;DecTab[I]<=Num;I++)
    StartPos++;
  Inp.faddbits(StartPos);
  return(((Num-(I != 0 ? DecTab[I-1]:0))>>(int)(16-StartPos))+PosTab[StartPos]);
}

    }
}
