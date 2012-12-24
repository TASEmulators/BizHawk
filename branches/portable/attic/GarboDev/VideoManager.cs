namespace GarboDev
{
    public class VideoManager
    {
        public delegate void OnPresent(uint[] data);

        private Memory memory = null;
        private IRenderer renderer = null;
        private OnPresent presenter;
        private GbaManager gbaManager;
        private int curLine;

        public Memory Memory
        {
            set{ this.memory = value; }
        }

        public IRenderer Renderer
        {
            set
            {
                this.renderer = value;
                this.renderer.Memory = this.memory;
            }
        }

        public OnPresent Presenter
        {
            set { this.presenter = value; }
        }

        public VideoManager(GbaManager gbaManager)
        {
            this.gbaManager = gbaManager;
        }

        public void Reset()
        {
            this.curLine = 0;

            this.renderer.Memory = memory;
            this.renderer.Reset();
        }

        private void EnterVBlank(Arm7Processor processor)
        {
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat |= 1;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            // Render the frame
            this.gbaManager.FramesRendered++;
            this.presenter(this.renderer.ShowFrame());

            if ((dispstat & (1 << 3)) != 0)
            {
                // Fire the vblank irq
                processor.RequestIrq(0);
            }

            // Check for DMA triggers
            this.memory.VBlankDma();
        }

        private void LeaveVBlank(Arm7Processor processor)
        {
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat &= 0xFFFE;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            processor.UpdateKeyState();

            // Update the rot/scale values
            this.memory.Bgx[0] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG2X_L);
            this.memory.Bgx[1] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG3X_L);
            this.memory.Bgy[0] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG2Y_L);
            this.memory.Bgy[1] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG3Y_L);
        }

        public void EnterHBlank(Arm7Processor processor)
        {
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat |= 1 << 1;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            // Advance the bgx registers
            for (int bg = 0; bg <= 1; bg++)
            {
                short dmx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PB + (uint)bg * 0x10);
                short dmy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PD + (uint)bg * 0x10);
                this.memory.Bgx[bg] += dmx;
                this.memory.Bgy[bg] += dmy;
            }

            if (this.curLine < 160)
            {
                this.memory.HBlankDma();

                // Trigger hblank irq
                if ((dispstat & (1 << 4)) != 0)
                {
                    processor.RequestIrq(1);
                }
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="processor"></param>
		/// <returns>true if end of frame</returns>
        public bool LeaveHBlank(Arm7Processor processor)
        {
			bool ret = false;
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat &= 0xFFF9;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            // Move to the next line
            this.curLine++;

            if (this.curLine >= 228)
            {
                // Start again at the beginning
                this.curLine = 0;
            }

            // Update registers
            Memory.WriteU16(this.memory.IORam, Memory.VCOUNT, (ushort)this.curLine);

            // Check for vblank
            if (this.curLine == 160)
            {
                this.EnterVBlank(processor);
				ret = true;
            }
            else if (this.curLine == 0)
            {
                this.LeaveVBlank(processor);
            }

            // Check y-line trigger
            if (((dispstat >> 8) & 0xff) == this.curLine)
            {
                dispstat = (ushort)(Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT) | (1 << 2));
                Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

                if ((dispstat & (1 << 5)) != 0)
                {
                    processor.RequestIrq(2);
                }
            }
			return ret;
        }

        public void RenderLine()
        {
            if (this.curLine < 160)
            {
                this.renderer.RenderLine(this.curLine);
            }
        }
    }
}