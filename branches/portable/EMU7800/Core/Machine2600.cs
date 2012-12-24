/*
 * Machine2600.cs
 * 
 * The realization of a 2600 machine.
 * 
 * Copyright © 2003, 2004 Mike Murphy
 * 
 */
namespace EMU7800.Core
{
    public class Machine2600 : MachineBase
    {
        #region Fields

        protected TIA TIA { get; set; }

        #endregion

        public override void Reset()
        {
            base.Reset();
            TIA.Reset();
            PIA.Reset();
            CPU.Reset();
        }

        public override void ComputeNextFrame(FrameBuffer frameBuffer)
        {
            base.ComputeNextFrame(frameBuffer);
            TIA.StartFrame();
            CPU.RunClocks = (FrameBuffer.Scanlines + 3) * 76;
            while (CPU.RunClocks > 0 && !CPU.Jammed)
            {
                if (TIA.WSYNCDelayClocks > 0)
                {
                    CPU.Clock += (ulong)TIA.WSYNCDelayClocks / 3;
                    CPU.RunClocks -= TIA.WSYNCDelayClocks / 3;
                    TIA.WSYNCDelayClocks = 0;
                }
                if (TIA.EndOfFrame)
                {
                    break;
                }
                CPU.Execute();
            }
            TIA.EndFrame();
        }

        public Machine2600(Cart cart, ILogger logger, int slines, int startl, int fHZ, int sRate, int[] p)
             : base(logger, slines, startl, fHZ, sRate, p, 160)
        {
            Mem = new AddressSpace(this, 13, 6);  // 2600: 13bit, 64byte pages

            CPU = new M6502(this, 1);

            TIA = new TIA(this);
            for (ushort i = 0; i < 0x1000; i += 0x100)
            {
                Mem.Map(i, 0x0080, TIA);
            }

            PIA = new PIA(this);
            for (ushort i = 0x0080; i < 0x1000; i += 0x100)
            {
                Mem.Map(i, 0x0080, PIA);
            }

            Cart = cart;
            Mem.Map(0x1000, 0x1000, Cart);
        }

        #region Serialization Members

        public Machine2600(DeserializationContext input, int[] palette) : base(input, palette)
        {
            input.CheckVersion(1);

            Mem = input.ReadAddressSpace(this, 13, 6);  // 2600: 13bit, 64byte pages

            CPU = input.ReadM6502(this, 1);

            TIA = input.ReadTIA(this);
            for (ushort i = 0; i < 0x1000; i += 0x100)
            {
                Mem.Map(i, 0x0080, TIA);
            }

            PIA = input.ReadPIA(this);
            for (ushort i = 0x0080; i < 0x1000; i += 0x100)
            {
                Mem.Map(i, 0x0080, PIA);
            }

            Cart = input.ReadCart(this);
            Mem.Map(0x1000, 0x1000, Cart);
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(Mem);
            output.Write(CPU);
            output.Write(TIA);
            output.Write(PIA);
            output.Write(Cart);
        }

        #endregion
    }
}
