#if !Rar2017_64bit
using nint = System.Int32;
using nuint = System.UInt32;
using size_t = System.UInt32;
#else
using nint = System.Int64;
using nuint = System.UInt64;
using size_t = System.UInt64;
#endif

namespace SharpCompress.Compressors.Rar.UnpackV2017
{
    internal partial class BitInput
    {

public BitInput(bool AllocBuffer)
{
    ExternalBuffer=false;
    if (AllocBuffer)
    {
    // getbits32 attempts to read data from InAddr, ... InAddr+3 positions.
    // So let's allocate 3 additional bytes for situation, when we need to
    // read only 1 byte from the last position of buffer and avoid a crash
    // from access to next 3 bytes, which contents we do not need.
    size_t BufSize=MAX_SIZE+3;
    InBuf=new byte[BufSize];

    // Ensure that we get predictable results when accessing bytes in area
    // not filled with read data.
    //memset(InBuf,0,BufSize);
    }
    else
    InBuf=null;
}


//BitInput::~BitInput()
//{
//    if (!ExternalBuffer)
//    delete[] InBuf;
//}
//

public            
void faddbits(uint Bits)
{
    // Function wrapped version of inline addbits to save code size.
    addbits(Bits);
}

public
uint fgetbits()
{
    // Function wrapped version of inline getbits to save code size.
    return getbits();
}

        private void SetExternalBuffer(byte []Buf)
{
    //if (InBuf!=NULL && !ExternalBuffer)
    //  delete[] InBuf;
    InBuf=Buf;
    ExternalBuffer=true;
}

    }
}
