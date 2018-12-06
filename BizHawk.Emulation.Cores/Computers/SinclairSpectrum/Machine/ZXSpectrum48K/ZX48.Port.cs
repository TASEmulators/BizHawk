
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// 48K Port
    /// </summary>
    public partial class ZX48 : SpectrumBase
    {
        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override byte ReadPort(ushort port)
        {
            int result = 0xFF;

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x0001) == 0;
            byte lowByte = (byte)(port & 0xff);
            
            // Kempston joystick input takes priority over keyboard input
            // if this is detected just return the kempston byte
            if (lowByte == 0x1f)
            {
                if (LocateUniqueJoystick(JoystickType.Kempston) != null)
                    return (byte)((KempstonJoystick)LocateUniqueJoystick(JoystickType.Kempston) as KempstonJoystick).JoyLine;

                // not a lag frame
                InputRead = true;
            }
            // Even ports always address the ULA
            else if (lowBitReset)
            {
                // Even I/O address so get input from keyboard
                KeyboardDevice.ReadPort(port, ref result);

                // not a lagframe
                InputRead = true;           

                // process tape INs
                TapeDevice.ReadPort(port, ref result);
            }
            else
            {
                // devices other than the ULA will respond here
                // (e.g. the AY sound chip in a 128k spectrum

                // AY register activate - no AY chip in a 48k spectrum

                // Kemptson Mouse (not implemented yet)


                // If this is an unused port the floating memory bus should be returned
                ULADevice.ReadFloatingBus((int)CurrentFrameCycle, ref result, port);                
            }

            return (byte)result;
        }

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public override void WritePort(ushort port, byte value)
        {
            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            if ((port & 0x0001) != 0)
                return;

            LastFe = value;

            // store the last OUT byte
            LastULAOutByte = value;

            /*
                Bit   7   6   5   4   3   2   1   0
                    +-------------------------------+
                    |   |   |   | E | M |   Border  |
                    +-------------------------------+
            */

            // Border - LSB 3 bits hold the border colour
            if (ULADevice.BorderColor != (value & BORDER_BIT))
            {
                //ULADevice.RenderScreen((int)CurrentFrameCycle);
                ULADevice.BorderColor = value & BORDER_BIT;
            }

            // Buzzer
            BuzzerDevice.ProcessPulseValue((value & EAR_BIT) != 0);

            // Tape
            TapeDevice.WritePort(port, value);

            // Tape mic processing (not implemented yet)
            //TapeDevice.ProcessMicBit((value & MIC_BIT) != 0);

        }

    }
}
