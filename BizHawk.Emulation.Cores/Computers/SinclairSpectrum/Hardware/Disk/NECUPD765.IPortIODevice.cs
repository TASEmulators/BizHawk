using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// IPortIODevice
    /// </summary>
    #region Attribution
    /*
        Implementation based on the information contained here:
        http://www.cpcwiki.eu/index.php/765_FDC
        and here:
        http://www.cpcwiki.eu/imgs/f/f3/UPD765_Datasheet_OCRed.pdf
    */
    #endregion
    public partial class NECUPD765 : IPortIODevice
    {

        public string outputfile = @"D:\Dropbox\Dropbox\_Programming\TASVideos\BizHawk\output\zxhawkio-" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        public string outputString = "STATUS,WRITE,READ\r\n";
        public bool writeDebug = false;

        /// <summary>
        /// Device responds to an IN instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool ReadPort(ushort port, ref int data)
        {
            BitArray bits = new BitArray(new byte[] { (byte)data });

            if (port == 0x3ffd)
            {
                // Z80 is trying to read from the data register
                data = ReadDataRegister();
                if (writeDebug)
                    outputString += ",," + data + "\r\n";
                return true;
            }

            if (port == 0x2ffd)
            {
                // read main status register
                // this can happen at any time
                data = ReadMainStatus();
                if (writeDebug)
                    outputString += data + ",,\r\n";
                return true;
            }

            return false;
        }

        /// <summary>
        /// Device responds to an OUT instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool WritePort(ushort port, int data)
        {
            BitArray bits = new BitArray(new byte[] { (byte)data });

            if (port == 0x3ffd)
            {
                // Z80 is attempting to write to the data register
                WriteDataRegister((byte)data);
                if (writeDebug)
                {
                    outputString += "," + data + ",\r\n";
                    System.IO.File.WriteAllText(outputfile, outputString);
                }
                    
                return true;
            }

            if (port == 0x1ffd)
            {
                // set disk motor on/off
                FDD_FLAG_MOTOR = bits[3];
                return true;
            }
            return false;
        }
    }
}
