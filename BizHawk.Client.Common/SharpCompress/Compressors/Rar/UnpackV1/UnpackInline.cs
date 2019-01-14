
namespace SharpCompress.Compressors.Rar.UnpackV1
{
    internal partial class Unpack
    {
      private uint SlotToLength(uint Slot)
        {
          //uint LBits,Length=2;
          int LBits;
          uint Length=2;
          if (Slot<8)
          {
            LBits=0;
            Length+=Slot;
          }
          else
          {
            //LBits=Slot/4-1;
            LBits=(int)(Slot/4-1);
            Length+=(4 | (Slot & 3)) << LBits;
          }

          if (LBits>0)
          {
            Length+=getbits()>>(16-LBits);
            AddBits(LBits);
          }
          return Length;
        }
    }
}
