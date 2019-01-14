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

using System;
using static SharpCompress.Compressors.Rar.UnpackV2017.PackDef;
using static SharpCompress.Compressors.Rar.UnpackV2017.UnpackGlobal;

namespace SharpCompress.Compressors.Rar.UnpackV2017
{
    internal partial class Unpack
    {
      private void Unpack5(bool Solid)
{
  FileExtracted=true;

  if (!Suspended)
  {
    UnpInitData(Solid);
    if (!UnpReadBuf())
      return;

    // Check TablesRead5 to be sure that we read tables at least once
    // regardless of current block header TablePresent flag.
    // So we can safefly use these tables below.
    if (!ReadBlockHeader(Inp,ref BlockHeader) || 
        !ReadTables(Inp,ref BlockHeader, ref BlockTables) || !TablesRead5)
      return;
  }

  while (true)
  {
    UnpPtr&=MaxWinMask;

    if (Inp.InAddr>=ReadBorder)
    {
      bool FileDone=false;

      // We use 'while', because for empty block containing only Huffman table,
      // we'll be on the block border once again just after reading the table.
      while (Inp.InAddr>BlockHeader.BlockStart+BlockHeader.BlockSize-1 || 
             Inp.InAddr==BlockHeader.BlockStart+BlockHeader.BlockSize-1 && 
             Inp.InBit>=BlockHeader.BlockBitSize)
      {
        if (BlockHeader.LastBlockInFile)
        {
          FileDone=true;
          break;
        }
        if (!ReadBlockHeader(Inp,ref BlockHeader) || !ReadTables(Inp, ref BlockHeader, ref BlockTables))
          return;
      }
      if (FileDone || !UnpReadBuf())
        break;
    }

    if (((WriteBorder-UnpPtr) & MaxWinMask)<MAX_LZ_MATCH+3 && WriteBorder!=UnpPtr)
    {
      UnpWriteBuf();
      if (WrittenFileSize>DestUnpSize)
        return;
      if (Suspended)
      {
        FileExtracted=false;
        return;
      }
    }

    uint MainSlot=DecodeNumber(Inp,BlockTables.LD);
    if (MainSlot<256)
    {
      if (Fragmented)
        FragWindow[UnpPtr++]=(byte)MainSlot;
      else
        Window[UnpPtr++]=(byte)MainSlot;
      continue;
    }
    if (MainSlot>=262)
    {
      uint Length=SlotToLength(Inp,MainSlot-262);

      uint DBits,Distance=1,DistSlot=DecodeNumber(Inp,BlockTables.DD);
      if (DistSlot<4)
      {
        DBits=0;
        Distance+=DistSlot;
      }
      else
      {
        DBits=DistSlot/2 - 1;
        Distance+=(2 | (DistSlot & 1)) << (int)DBits;
      }

      if (DBits>0)
      {
        if (DBits>=4)
        {
          if (DBits>4)
          {
            Distance+=((Inp.getbits32()>>(int)(36-DBits))<<4);
            Inp.addbits(DBits-4);
          }
          uint LowDist=DecodeNumber(Inp,BlockTables.LDD);
          Distance+=LowDist;
        }
        else
        {
          Distance+=Inp.getbits32()>>(int)(32-DBits);
          Inp.addbits(DBits);
        }
      }

      if (Distance>0x100)
      {
        Length++;
        if (Distance>0x2000)
        {
          Length++;
          if (Distance>0x40000)
            Length++;
        }
      }

      InsertOldDist(Distance);
      LastLength=Length;
      if (Fragmented)
        FragWindow.CopyString(Length,Distance,ref UnpPtr,MaxWinMask);
      else
        CopyString(Length,Distance);
      continue;
    }
    if (MainSlot==256)
    {
      UnpackFilter Filter = new UnpackFilter();
      if (!ReadFilter(Inp,Filter) || !AddFilter(Filter))
        break;
      continue;
    }
    if (MainSlot==257)
    {
      if (LastLength!=0)
        if (Fragmented)
          FragWindow.CopyString(LastLength,OldDist[0],ref UnpPtr,MaxWinMask);
        else
          CopyString(LastLength,OldDist[0]);
      continue;
    }
    if (MainSlot<262)
    {
      uint DistNum=MainSlot-258;
      uint Distance=OldDist[DistNum];
      for (uint I=DistNum;I>0;I--)
        OldDist[I]=OldDist[I-1];
      OldDist[0]=Distance;

      uint LengthSlot=DecodeNumber(Inp,BlockTables.RD);
      uint Length=SlotToLength(Inp,LengthSlot);
      LastLength=Length;
      if (Fragmented)
        FragWindow.CopyString(Length,Distance,ref UnpPtr,MaxWinMask);
      else
        CopyString(Length,Distance);
      continue;
    }
  }
  UnpWriteBuf();
}

      private uint ReadFilterData(BitInput Inp)
{
  uint ByteCount=(Inp.fgetbits()>>14)+1;
  Inp.addbits(2);

  uint Data=0;
  for (uint I=0;I<ByteCount;I++)
  {
    Data+=(Inp.fgetbits()>>8)<<(int)(I*8);
    Inp.addbits(8);
  }
  return Data;
}

      private bool ReadFilter(BitInput Inp,UnpackFilter Filter)
{
  if (!Inp.ExternalBuffer && Inp.InAddr>ReadTop-16)
    if (!UnpReadBuf())
      return false;

  Filter.BlockStart=ReadFilterData(Inp);
  Filter.BlockLength=ReadFilterData(Inp);
  if (Filter.BlockLength>MAX_FILTER_BLOCK_SIZE)
    Filter.BlockLength=0;

  Filter.Type=(byte)(Inp.fgetbits()>>13);
  Inp.faddbits(3);

  if (Filter.Type==FILTER_DELTA)
  {
    Filter.Channels=(byte)((Inp.fgetbits()>>11)+1);
    Inp.faddbits(5);
  }

  return true;
}

      private bool AddFilter(UnpackFilter Filter)
{
  if (Filters.Count>=MAX_UNPACK_FILTERS)
  {
    UnpWriteBuf(); // Write data, apply and flush filters.
    if (Filters.Count>=MAX_UNPACK_FILTERS)
      InitFilters(); // Still too many filters, prevent excessive memory use.
  }

  // If distance to filter start is that large that due to circular dictionary
  // mode now it points to old not written yet data, then we set 'NextWindow'
  // flag and process this filter only after processing that older data.
  Filter.NextWindow=WrPtr!=UnpPtr && ((WrPtr-UnpPtr)&MaxWinMask)<=Filter.BlockStart;

  Filter.BlockStart=(uint)((Filter.BlockStart+UnpPtr)&MaxWinMask);
  Filters.Add(Filter);
  return true;
}

      private bool UnpReadBuf()
{
  int DataSize=ReadTop-Inp.InAddr; // Data left to process.
  if (DataSize<0)
    return false;
  BlockHeader.BlockSize-=Inp.InAddr-BlockHeader.BlockStart;
  if (Inp.InAddr>MAX_SIZE/2)
  {
    // If we already processed more than half of buffer, let's move
    // remaining data into beginning to free more space for new data
    // and ensure that calling function does not cross the buffer border
    // even if we did not read anything here. Also it ensures that read size
    // is not less than CRYPT_BLOCK_SIZE, so we can align it without risk
    // to make it zero.
    if (DataSize>0)
      //x memmove(Inp.InBuf,Inp.InBuf+Inp.InAddr,DataSize);
      Buffer.BlockCopy(Inp.InBuf, Inp.InAddr, Inp.InBuf, 0, DataSize);
    Inp.InAddr=0;
    ReadTop=DataSize;
  }
  else
    DataSize=ReadTop;
  int ReadCode=0;
  if (MAX_SIZE!=DataSize)
    ReadCode=UnpIO_UnpRead(Inp.InBuf,DataSize,MAX_SIZE-DataSize);
  if (ReadCode>0) // Can be also -1.
    ReadTop+=ReadCode;
  ReadBorder=ReadTop-30;
  BlockHeader.BlockStart=Inp.InAddr;
  if (BlockHeader.BlockSize!=-1) // '-1' means not defined yet.
  {
    // We may need to quit from main extraction loop and read new block header
    // and trees earlier than data in input buffer ends.
    ReadBorder=Math.Min(ReadBorder,BlockHeader.BlockStart+BlockHeader.BlockSize-1);
  }
  return ReadCode!=-1;
}

      private void UnpWriteBuf()
{
  size_t WrittenBorder=WrPtr;
  size_t FullWriteSize=(UnpPtr-WrittenBorder)&MaxWinMask;
  size_t WriteSizeLeft=FullWriteSize;
  bool NotAllFiltersProcessed=false;
  //for (size_t I=0;I<Filters.Count;I++)
  // sharpcompress: size_t -> int
  for (int I=0;I<Filters.Count;I++)
  {
    // Here we apply filters to data which we need to write.
    // We always copy data to another memory block before processing.
    // We cannot process them just in place in Window buffer, because
    // these data can be used for future string matches, so we must
    // preserve them in original form.

    UnpackFilter flt=Filters[I];
    if (flt.Type==FILTER_NONE)
      continue;
    if (flt.NextWindow)
    {
      // Here we skip filters which have block start in current data range
      // due to address wrap around in circular dictionary, but actually
      // belong to next dictionary block. If such filter start position
      // is included to current write range, then we reset 'NextWindow' flag.
      // In fact we can reset it even without such check, because current
      // implementation seems to guarantee 'NextWindow' flag reset after
      // buffer writing for all existing filters. But let's keep this check
      // just in case. Compressor guarantees that distance between
      // filter block start and filter storing position cannot exceed
      // the dictionary size. So if we covered the filter block start with
      // our write here, we can safely assume that filter is applicable
      // to next block on no further wrap arounds is possible.
      if (((flt.BlockStart-WrPtr)&MaxWinMask)<=FullWriteSize)
        flt.NextWindow=false;
      continue;
    }
    uint BlockStart=flt.BlockStart;
    uint BlockLength=flt.BlockLength;
    if (((BlockStart-WrittenBorder)&MaxWinMask)<WriteSizeLeft)
    {
      if (WrittenBorder!=BlockStart)
      {
        UnpWriteArea(WrittenBorder,BlockStart);
        WrittenBorder=BlockStart;
        WriteSizeLeft=(UnpPtr-WrittenBorder)&MaxWinMask;
      }
      if (BlockLength<=WriteSizeLeft)
      {
        if (BlockLength>0) // We set it to 0 also for invalid filters.
        {
          uint BlockEnd=(BlockStart+BlockLength)&MaxWinMask;

          //x FilterSrcMemory.Alloc(BlockLength);
          FilterSrcMemory = EnsureCapacity(FilterSrcMemory, checked((int)BlockLength));
          byte[] Mem= FilterSrcMemory;
          if (BlockStart<BlockEnd || BlockEnd==0)
          {
            if (Fragmented)
              FragWindow.CopyData(Mem,0,BlockStart,BlockLength);
            else
              //x memcpy(Mem,Window+BlockStart,BlockLength);
              Utility.Copy(Window, BlockStart, Mem, 0, BlockLength);
          }
          else
          {
            size_t FirstPartLength=(size_t)(MaxWinSize-BlockStart);
            if (Fragmented)
            {
              FragWindow.CopyData(Mem,0,BlockStart,FirstPartLength);
              FragWindow.CopyData(Mem,FirstPartLength,0,BlockEnd);
            }
            else
            {
              //x memcpy(Mem,Window+BlockStart,FirstPartLength);
              Utility.Copy(Window, BlockStart, Mem, 0, FirstPartLength);
              //x memcpy(Mem+FirstPartLength,Window,BlockEnd);
              Utility.Copy(Window, 0, Mem, FirstPartLength,BlockEnd);
            }
          }

          byte[] OutMem=ApplyFilter(Mem,BlockLength,flt);

          Filters[I].Type=FILTER_NONE;

          if (OutMem!=null)
            UnpIO_UnpWrite(OutMem,0,BlockLength);

          UnpSomeRead=true;
          WrittenFileSize+=BlockLength;
          WrittenBorder=BlockEnd;
          WriteSizeLeft=(UnpPtr-WrittenBorder)&MaxWinMask;
        }
      }
      else
      {
        // Current filter intersects the window write border, so we adjust
        // the window border to process this filter next time, not now.
        WrPtr=WrittenBorder;

        // Since Filter start position can only increase, we quit processing
        // all following filters for this data block and reset 'NextWindow'
        // flag for them.
        //for (size_t J=I;J<Filters.Count;J++)
        // sharpcompress: size_t -> int
        for (int J=I;J<Filters.Count;J++)
        {
          UnpackFilter _flt=Filters[J];
          if (_flt.Type!=FILTER_NONE)
            _flt.NextWindow=false;
        }

        // Do not write data left after current filter now.
        NotAllFiltersProcessed=true;
        break;
      }
    }
  }

  // Remove processed filters from queue.
  // sharpcompress: size_t -> int
  int EmptyCount=0;
  // sharpcompress: size_t -> int
  for (int I=0;I<Filters.Count;I++)
  {
    if (EmptyCount>0)
      Filters[I-EmptyCount]=Filters[I];
    if (Filters[I].Type==FILTER_NONE)
      EmptyCount++;
  }
  if (EmptyCount>0)
    //Filters.Alloc(Filters.Count-EmptyCount);
    Filters.RemoveRange(Filters.Count-EmptyCount, EmptyCount);

  if (!NotAllFiltersProcessed) // Only if all filters are processed.
  {
    // Write data left after last filter.
    UnpWriteArea(WrittenBorder,UnpPtr);
    WrPtr=UnpPtr;
  }

  // We prefer to write data in blocks not exceeding UNPACK_MAX_WRITE
  // instead of potentially huge MaxWinSize blocks. It also allows us
  // to keep the size of Filters array reasonable.
  WriteBorder=(UnpPtr+Math.Min(MaxWinSize,UNPACK_MAX_WRITE))&MaxWinMask;

  // Choose the nearest among WriteBorder and WrPtr actual written border.
  // If border is equal to UnpPtr, it means that we have MaxWinSize data ahead.
  if (WriteBorder==UnpPtr || 
      WrPtr!=UnpPtr && ((WrPtr-UnpPtr)&MaxWinMask)<((WriteBorder-UnpPtr)&MaxWinMask))
    WriteBorder=WrPtr;
}

      private byte[] ApplyFilter(byte[] __d,uint DataSize,UnpackFilter Flt)
{
  int Data = 0;
  byte[] SrcData=__d;
  switch(Flt.Type)
  {
    case FILTER_E8:
    case FILTER_E8E9:
      {
        uint FileOffset=(uint)WrittenFileSize;

        const uint FileSize=0x1000000;
        byte CmpByte2=Flt.Type==FILTER_E8E9 ? (byte)0xe9 : (byte)0xe8;
        // DataSize is unsigned, so we use "CurPos+4" and not "DataSize-4"
        // to avoid overflow for DataSize<4.
        for (uint CurPos=0;CurPos+4<DataSize;)
        {
          //x byte CurByte=*(Data++);
          byte CurByte=__d[Data++];
          CurPos++;
          if (CurByte==0xe8 || CurByte==CmpByte2)
          {
            uint Offset=(CurPos+FileOffset)%FileSize;
            uint Addr=RawGet4(__d,Data);

            // We check 0x80000000 bit instead of '< 0' comparison
            // not assuming int32 presence or uint size and endianness.
            if ((Addr & 0x80000000)!=0)              // Addr<0
            {
              if (((Addr+Offset) & 0x80000000)==0)   // Addr+Offset>=0
                RawPut4(Addr+FileSize,__d,Data);
            }
            else
              if (((Addr-FileSize) & 0x80000000)!=0) // Addr<FileSize
                RawPut4(Addr-Offset,__d,Data);

            Data+=4;
            CurPos+=4;
          }
        }
      }
      return SrcData;
    case FILTER_ARM:
      {
        uint FileOffset=(uint)WrittenFileSize;
        // DataSize is unsigned, so we use "CurPos+3" and not "DataSize-3"
        // to avoid overflow for DataSize<3.
        for (uint CurPos=0;CurPos+3<DataSize;CurPos+=4)
        {
          var D=Data+CurPos;
          if (__d[D+3]==0xeb) // BL command with '1110' (Always) condition.
          {
            uint Offset=__d[D]+(uint)(__d[D+1])*0x100+(uint)(__d[D+2])*0x10000;
            Offset-=(FileOffset+CurPos)/4;
            __d[D]=(byte)Offset;
            __d[D+1]=(byte)(Offset>>8);
            __d[D+2]=(byte)(Offset>>16);
          }
        }
      }
      return SrcData;
    case FILTER_DELTA:
      {
        // Unlike RAR3, we do not need to reject excessive channel
        // values here, since RAR5 uses only 5 bits to store channel.
        uint Channels=Flt.Channels,SrcPos=0;

        //x FilterDstMemory.Alloc(DataSize);
        FilterDstMemory = EnsureCapacity(FilterDstMemory, checked((int)DataSize));

        byte[] DstData=FilterDstMemory;

        // Bytes from same channels are grouped to continual data blocks,
        // so we need to place them back to their interleaving positions.
        for (uint CurChannel=0;CurChannel<Channels;CurChannel++)
        {
          byte PrevByte=0;
          for (uint DestPos=CurChannel;DestPos<DataSize;DestPos+=Channels)
            DstData[DestPos]=(PrevByte-=__d[Data+SrcPos++]);
        }
        return DstData;
      }

  }
  return null;
}

      private void UnpWriteArea(size_t StartPtr,size_t EndPtr)
{
  if (EndPtr!=StartPtr)
    UnpSomeRead=true;
  if (EndPtr<StartPtr)
    UnpAllBuf=true;

  if (Fragmented)
  {
    size_t SizeToWrite=(EndPtr-StartPtr) & MaxWinMask;
    while (SizeToWrite>0)
    {
      size_t BlockSize=FragWindow.GetBlockSize(StartPtr,SizeToWrite);
      //UnpWriteData(&FragWindow[StartPtr],BlockSize);
      FragWindow.GetBuffer(StartPtr, out var __buffer, out var __offset);
      UnpWriteData(__buffer, __offset, BlockSize);
      SizeToWrite-=BlockSize;
      StartPtr=(StartPtr+BlockSize) & MaxWinMask;
    }
  }
  else
    if (EndPtr<StartPtr)
    {
      UnpWriteData(Window,StartPtr,MaxWinSize-StartPtr);
      UnpWriteData(Window,0,EndPtr);
    }
    else
      UnpWriteData(Window,StartPtr,EndPtr-StartPtr);
}

      private void UnpWriteData(byte[] Data, size_t offset, size_t Size)
{
  if (WrittenFileSize>=DestUnpSize)
    return;
  size_t WriteSize=Size;
  int64 LeftToWrite=DestUnpSize-WrittenFileSize;
  if ((int64)WriteSize>LeftToWrite)
    WriteSize=(size_t)LeftToWrite;
  UnpIO_UnpWrite(Data, offset, WriteSize);
  WrittenFileSize+=Size;
}

      private void UnpInitData50(bool Solid)
{
  if (!Solid)
    TablesRead5=false;
}

      private bool ReadBlockHeader(BitInput Inp,ref UnpackBlockHeader Header)
{
  Header.HeaderSize=0;

  if (!Inp.ExternalBuffer && Inp.InAddr>ReadTop-7)
    if (!UnpReadBuf())
      return false;
  Inp.faddbits((uint)((8-Inp.InBit)&7));
  
  byte BlockFlags=(byte)(Inp.fgetbits()>>8);
  Inp.faddbits(8);
  uint ByteCount=(uint)(((BlockFlags>>3)&3)+1); // Block size byte count.
  
  if (ByteCount==4)
    return false;

  Header.HeaderSize=(int)(2+ByteCount);

  Header.BlockBitSize=(BlockFlags&7)+1;

  byte SavedCheckSum=(byte)(Inp.fgetbits()>>8);
  Inp.faddbits(8);

  int BlockSize=0;
  for (uint I=0;I<ByteCount;I++)
  {
    BlockSize+=(int)((Inp.fgetbits()>>8)<<(int)(I*8));
    Inp.addbits(8);
  }

  Header.BlockSize=BlockSize;
  byte CheckSum=(byte)(0x5a^BlockFlags^BlockSize^(BlockSize>>8)^(BlockSize>>16));
  if (CheckSum!=SavedCheckSum)
    return false;

  Header.BlockStart=Inp.InAddr;
  ReadBorder=Math.Min(ReadBorder,Header.BlockStart+Header.BlockSize-1);

  Header.LastBlockInFile=(BlockFlags & 0x40)!=0;
  Header.TablePresent=(BlockFlags & 0x80)!=0;
  return true;
}

      private bool ReadTables(BitInput Inp,ref UnpackBlockHeader Header, ref UnpackBlockTables Tables)
{
  if (!Header.TablePresent)
    return true;

  if (!Inp.ExternalBuffer && Inp.InAddr>ReadTop-25)
    if (!UnpReadBuf())
      return false;

  byte[] BitLength = new byte[BC];
  for (uint I=0;I<BC;I++)
  {
    uint Length=(byte)(Inp.fgetbits() >> 12);
    Inp.faddbits(4);
    if (Length==15)
    {
      uint ZeroCount=(byte)(Inp.fgetbits() >> 12);
      Inp.faddbits(4);
      if (ZeroCount==0)
        BitLength[I]=15;
      else
      {
        ZeroCount+=2;
        while (ZeroCount-- > 0 && I<BitLength.Length)
          BitLength[I++]=0;
        I--;
      }
    }
    else
      BitLength[I]=(byte)Length;
  }

  MakeDecodeTables(BitLength,0,Tables.BD,BC);

  byte[] Table = new byte[HUFF_TABLE_SIZE];
  const uint TableSize=HUFF_TABLE_SIZE;
  for (uint I=0;I<TableSize;)
  {
    if (!Inp.ExternalBuffer && Inp.InAddr>ReadTop-5)
      if (!UnpReadBuf())
        return false;
    uint Number=DecodeNumber(Inp,Tables.BD);
    if (Number<16)
    {
      Table[I]=(byte)Number;
      I++;
    }
    else
      if (Number<18)
      {
        uint N;
        if (Number==16)
        {
          N=(Inp.fgetbits() >> 13)+3;
          Inp.faddbits(3);
        }
        else
        {
          N=(Inp.fgetbits() >> 9)+11;
          Inp.faddbits(7);
        }
        if (I==0)
        {
          // We cannot have "repeat previous" code at the first position.
          // Multiple such codes would shift Inp position without changing I,
          // which can lead to reading beyond of Inp boundary in mutithreading
          // mode, where Inp.ExternalBuffer disables bounds check and we just
          // reserve a lot of buffer space to not need such check normally.
          return false;
        }
        else
          while (N-- > 0 && I<TableSize)
          {
            Table[I]=Table[I-1];
            I++;
          }
      }
      else
      {
        uint N;
        if (Number==18)
        {
          N=(Inp.fgetbits() >> 13)+3;
          Inp.faddbits(3);
        }
        else
        {
          N=(Inp.fgetbits() >> 9)+11;
          Inp.faddbits(7);
        }
        while (N-- > 0 && I<TableSize)
          Table[I++]=0;
      }
  }
  TablesRead5=true;
  if (!Inp.ExternalBuffer && Inp.InAddr>ReadTop)
    return false;
  MakeDecodeTables(Table, 0, Tables.LD,NC);
  MakeDecodeTables(Table, (int)NC,Tables.DD,DC);
  MakeDecodeTables(Table, (int)(NC+DC),Tables.LDD,LDC);
  MakeDecodeTables(Table, (int)(NC+DC+LDC),Tables.RD,RC);
  return true;
}

      private void InitFilters()
{
  //Filters.SoftReset();
  Filters.Clear();
}

    }
}
