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

namespace SharpCompress.Compressors.Rar.UnpackV2017
{
  internal partial class FragmentedWindow
    {

public FragmentedWindow()
{
  //memset(Mem,0,sizeof(Mem));
  //memset(MemSize,0,sizeof(MemSize));
}


//FragmentedWindow::~FragmentedWindow()
//{
//  Reset();
//}

      private void Reset()
{
  for (uint I=0;I<Mem.Length;I++)
    if (Mem[I]!=null)
    {
      //free(Mem[I]);
      Mem[I]=null;
    }
}


public void Init(size_t WinSize)
{
  Reset();

  uint BlockNum=0;
  size_t TotalSize=0; // Already allocated.
  while (TotalSize<WinSize && BlockNum<Mem.Length)
  {
    size_t Size=WinSize-TotalSize; // Size needed to allocate.

    // Minimum still acceptable block size. Next allocations cannot be larger
    // than current, so we do not need blocks if they are smaller than
    // "size left / attempts left". Also we do not waste time to blocks
    // smaller than some arbitrary constant.
    size_t MinSize=Math.Max(Size/(size_t)(Mem.Length-BlockNum), 0x400000);

    byte[] NewMem=null;
    while (Size>=MinSize)
    {
      NewMem=new byte[Size];
      if (NewMem!=null)
        break;
      Size-=Size/32;
    }
    if (NewMem==null)
      //throw std::bad_alloc();
      throw new InvalidOperationException();
    
    // Clean the window to generate the same output when unpacking corrupt
    // RAR files, which may access to unused areas of sliding dictionary.
    // sharpcompress: don't need this, freshly allocated above
    //Utility.Memset(NewMem,0,Size);

    Mem[BlockNum]=NewMem;
    TotalSize+=Size;
    MemSize[BlockNum]=TotalSize;
    BlockNum++;
  }
  if (TotalSize<WinSize) // Not found enough free blocks.
    //throw std::bad_alloc();
    throw new InvalidOperationException();
}


public byte this[size_t Item] {
get {
  if (Item<MemSize[0])
    return Mem[0][Item];
  for (uint I=1;I<MemSize.Length;I++)
    if (Item<MemSize[I])
      return Mem[I][Item-MemSize[I-1]];
  return Mem[0][0]; // Must never happen;
}
set {
  if (Item<MemSize[0]) {
    Mem[0][Item] = value;
    return;
  }                
  for (uint I=1;I<MemSize.Length;I++)
    if (Item<MemSize[I]) {
      Mem[I][Item-MemSize[I-1]] = value;
      return;
    }                        
  Mem[0][0] = value; // Must never happen;
}
}

// sharpcompress: added the following code
public void GetBuffer(size_t Item, out byte[] buf, out uint offset) {
  if (Item<MemSize[0]) {
    //return Mem[0][Item];
    buf = Mem[0]; offset = Item; return;
  }
  for (uint I=1;I<MemSize.Length;I++) {
    if (Item<MemSize[I]) {
      //return Mem[I][Item-MemSize[I-1]];
      buf = Mem[I]; offset = Item-MemSize[I-1]; return;
    }
  }
  //return Mem[0][0]; // Must never happen;
  buf = Mem[0]; offset = 0; return; // Must never happen;
}

public void CopyString(uint Length,uint Distance,ref size_t UnpPtr,size_t MaxWinMask)
{
  size_t SrcPtr=UnpPtr-Distance;
  while (Length-- > 0)
  {
    this[UnpPtr]=this[SrcPtr++ & MaxWinMask];
    // We need to have masked UnpPtr after quit from loop, so it must not
    // be replaced with '(*this)[UnpPtr++ & MaxWinMask]'
    UnpPtr=(UnpPtr+1) & MaxWinMask;
  }
}


public void CopyData(byte[] Dest, size_t destOffset, size_t WinPos,size_t Size)
{
  for (size_t I=0;I<Size;I++)
    Dest[destOffset+I]=this[WinPos+I];
}


public size_t GetBlockSize(size_t StartPos,size_t RequiredSize)
{
  for (uint I=0;I<MemSize.Length;I++)
    if (StartPos<MemSize[I])
      return Math.Min(MemSize[I]-StartPos,RequiredSize);
  return 0; // Must never be here.
}

    }
}
