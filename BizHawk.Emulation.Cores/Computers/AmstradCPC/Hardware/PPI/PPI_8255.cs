using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;
using System.Collections;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Emulates the PPI (8255) chip
    /// http://www.cpcwiki.eu/imgs/d/df/PPI_M5L8255AP-5.pdf
    /// http://www.cpcwiki.eu/index.php/8255
    /// </summary>
    public class PPI_8255 : IPortIODevice
    {
        #region Devices

        private CPCBase _machine;
        private CRCT_6845 CRTC => _machine.CRCT;
        private AmstradGateArray GateArray => _machine.GateArray;
        private IPSG PSG => _machine.AYDevice;
        private DatacorderDevice Tape => _machine.TapeDevice;
        private IKeyboard Keyboard => _machine.KeyboardDevice;

        #endregion

        #region Construction

        public PPI_8255(CPCBase machine)
        {
            _machine = machine;
            Reset();
        }

        #endregion

        #region Implementation

        /// <summary>
        /// BDIR Line connected to PSG
        /// </summary>
        public bool BDIR
        {
            get { return Regs[PORT_C].Bit(7); }
        }

        /// <summary>
        /// BC1 Line connected to PSG
        /// </summary>
        public bool BC1
        {
            get { return Regs[PORT_C].Bit(6); }
        }

        /* Port Constants */
        private const int PORT_A = 0;
        private const int PORT_B = 1;
        private const int PORT_C = 2;
        private const int PORT_CONTROL = 3;

        /// <summary>
        /// The i8255 internal data registers
        /// </summary>
        private byte[] Regs = new byte[4];

        /// <summary>
        /// Returns the currently latched port direction for Port A
        /// </summary>
        private PortDirection DirPortA
        {
            get { return Regs[PORT_CONTROL].Bit(4) ? PortDirection.Input : PortDirection.Output; }
        }

        /// <summary>
        /// Returns the currently latched port direction for Port B
        /// </summary>
        private PortDirection DirPortB
        {
            get { return Regs[PORT_CONTROL].Bit(1) ? PortDirection.Input : PortDirection.Output; }
        }

        /// <summary>
        /// Returns the currently latched port direction for Port C (lower half)
        /// </summary>
        private PortDirection DirPortCL
        {
            get { return Regs[PORT_CONTROL].Bit(0) ? PortDirection.Input : PortDirection.Output; }
        }

        /// <summary>
        /// Returns the currently latched port direction for Port C (upper half)
        /// </summary>
        private PortDirection DirPortCU
        {
            get { return Regs[PORT_CONTROL].Bit(3) ? PortDirection.Input : PortDirection.Output; }
        }

        #region OUT Methods

        /// <summary>
        /// Writes to Port A
        /// </summary>
        private void OUTPortA(int data)
        {
            // latch the data
            Regs[PORT_A] = (byte)data;

            if (DirPortA == PortDirection.Output)
            {
                // PSG write
                PSG.PortWrite(data);
            }
        }

        /// <summary>
        /// Writes to Port B
        /// </summary>
        private void OUTPortB(int data)
        {
            // PortB is read only
            // just latch the data
            Regs[PORT_B] = (byte)data;
        }

        /// <summary>
        /// Writes to Port C
        /// </summary>
        private void OUTPortC(int data)
        {
            // latch the data
            Regs[PORT_C] = (byte)data;

            if (DirPortCL == PortDirection.Output)
            {
                // lower Port C bits OUT
                // keyboard line update
                Keyboard.CurrentLine = Regs[PORT_C] & 0x0f;
            }

            if (DirPortCU == PortDirection.Output)
            {
                // upper Port C bits OUT
                // write to PSG using latched data
                PSG.SetFunction(data);
                PSG.PortWrite(Regs[PORT_A]);

                // cassete write data
                //not implemeted

                // cas motor control
                Tape.TapeMotor = Regs[PORT_C].Bit(4);
            }
        }

        /// <summary>
        /// Writes to the control register
        /// </summary>
        /// <param name="data"></param>
        private void OUTControl(int data)
        {
            if (data.Bit(7))
            {
                // update configuration
                Regs[PORT_CONTROL] = (byte)data;

                // Writing to PIO Control Register (with Bit7 set), automatically resets PIO Ports A,B,C to 00h each
                Regs[PORT_A] = 0;
                Regs[PORT_B] = 0;
                Regs[PORT_C] = 0;
            }
            else
            {
                // register is used to set/reset a single bit in Port C
                bool isSet = data.Bit(0);

                // get the bit in PortC that we wish to change
                var bit = (data >> 1) & 7;

                // modify this bit
                if (isSet)
                {
                    Regs[PORT_C] = (byte)(Regs[PORT_C] | (bit * bit));
                }
                else
                {
                    Regs[PORT_C] = (byte)(Regs[PORT_C] & ~(bit * bit));
                }

                // any other ouput business
                if (DirPortCL == PortDirection.Output)
                {
                    // update keyboard line
                    Keyboard.CurrentLine = Regs[PORT_C] & 0x0f;
                }

                if (DirPortCU == PortDirection.Output)
                {
                    // write to PSG using latched data
                    PSG.SetFunction(data);
                    PSG.PortWrite(Regs[PORT_A]);

                    // cassete write data
                    //not implemeted

                    // cas motor control
                    Tape.TapeMotor = Regs[PORT_C].Bit(4);
                }
            }
        }

        #endregion

        #region IN Methods

        /// <summary>
        /// Reads from Port A
        /// </summary>
        /// <returns></returns>
        private int INPortA()
        {
            if (DirPortA == PortDirection.Input)
            {
                // read from PSG
                return PSG.PortRead();
            }
            else
            {
                // Port A is set to output
                // return latched value
                return Regs[PORT_A];
            }
        }

        /// <summary>
        /// Reads from Port B
        /// </summary>
        /// <returns></returns>
        private int INPortB()
        {
            if (DirPortB == PortDirection.Input)
            {
                // build the PortB output
                // start with every bit reset
                BitArray rBits = new BitArray(8);

                // Bit0 - Vertical Sync ("1"=VSYNC active, "0"=VSYNC inactive)
                if (CRTC.VSYNC)
                    rBits[0] = true;

                // Bits1-3 - Distributor ID. Usually set to 4=Awa, 5=Schneider, or 7=Amstrad
                // force AMstrad
                rBits[1] = true;
                rBits[2] = true;
                rBits[3] = true;

                // Bit4 - Screen Refresh Rate ("1"=50Hz, "0"=60Hz)
                rBits[4] = true;

                // Bit5 - Expansion Port /EXP pin
                rBits[5] = false;

                // Bit6 - Parallel/Printer port ready signal, "1" = not ready, "0" = Ready
                rBits[6] = true;

                // Bit7 - Cassette data input
                rBits[7] = Tape.GetEarBit(_machine.CPU.TotalExecutedCycles);

                // return the byte
                byte[] bytes = new byte[1];
                rBits.CopyTo(bytes, 0);
                return bytes[0];
            }
            else
            {
                // return the latched value
                return Regs[PORT_B];
            }
        }

        /// <summary>
        /// Reads from Port C
        /// </summary>
        /// <returns></returns>
        private int INPortC()
        {
            // get the PortC value
            int val = Regs[PORT_C];

            if (DirPortCU == PortDirection.Input)
            {
                // upper port C bits
                // remove upper half
                val &= 0x0f;

                // isolate control bits
                var v = Regs[PORT_C] & 0xc0;

                if (v == 0xc0)
                {
                    // set reg is present. change to write reg
                    v = 0x80;
                }

                // cas wr is always set
                val |= v | 0x20;

                if (Tape.TapeMotor)
                {
                    val |= 0x10;
                }
            }

            if (DirPortCL == PortDirection.Input)
            {
                // lower port C bits
                val |= 0x0f;
            }

            return val;
        }


        #endregion

        #endregion        

        #region Reset

        public void Reset()
        {
            for (int i = 0; i < 3; i++)
            {
                Regs[i] = 0xff;
            }

            Regs[3] = 0xff;
        }

        #endregion

        #region IPortIODevice

        /*
            #F4XX	%xxxx0x00 xxxxxxxx	8255 PIO Port A (PSG Data)	                Read	Write
            #F5XX	%xxxx0x01 xxxxxxxx	8255 PIO Port B (Vsync,PrnBusy,Tape,etc.)	Read	-
            #F6XX	%xxxx0x10 xxxxxxxx	8255 PIO Port C (KeybRow,Tape,PSG Control)	-	    Write
            #F7XX	%xxxx0x11 xxxxxxxx	8255 PIO Control-Register	                -	    Write
         */

        /// <summary>
        /// Device responds to an IN instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ReadPort(ushort port, ref int result)
        {
            byte portUpper = (byte)(port >> 8);
            byte portLower = (byte)(port & 0xff);

            // The 8255 responds to bit 11 reset with A10 and A12-A15 set
            //if (portUpper.Bit(3))
            //return false;

            var PPIFunc = (port & 0x0300) >> 8; // portUpper & 3;

            switch (PPIFunc)
            {
                // Port A Read
                case 0:
                    
                    // PSG (Sound/Keyboard/Joystick)
                    result = INPortA();

                    break;

                // Port B Read
                case 1:

                    // Vsync/Jumpers/PrinterBusy/CasIn/Exp
                    result = INPortB();

                    break;

                // Port C Read (docs define this as write-only but we do need to do some processing)
                case 2:

                    // KeybRow/CasOut/PSG
                    result = INPortC();

                    break;
            }

            return true;
        }

        /// <summary>
        /// Device responds to an OUT instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WritePort(ushort port, int result)
        {
            byte portUpper = (byte)(port >> 8);
            byte portLower = (byte)(port & 0xff);

            // The 8255 responds to bit 11 reset with A10 and A12-A15 set
            if (portUpper.Bit(3))
                return false;

            var PPIFunc = portUpper & 3;

            switch (PPIFunc)
            {
                // Port A Write
                case 0:

                    // PSG (Sound/Keyboard/Joystick)
                    OUTPortA(result);

                    break;

                // Port B Write
                case 1:

                    // Vsync/Jumpers/PrinterBusy/CasIn/Exp
                    OUTPortB(result);

                    break;

                // Port C Write
                case 2:

                    // KeybRow/CasOut/PSG
                    OUTPortC(result);

                    break;

                // Control Register Write
                case 3:

                    // Control
                    OUTControl((byte)result);

                    break;
            }

            return true;
        }

        #endregion

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("PPI");
            ser.Sync("Regs", ref Regs, false);
            ser.EndSection();
        }

        #endregion        
    }

    public enum PortDirection
    {
        Input,
        Output
    }
}
