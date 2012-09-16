using System;
using System.Globalization;

namespace BizHawk.Emulation.Consoles.Sega
{
    partial class Genesis
    {
        // State
        bool EepromEnabled;

        int EepromSize;
        int EepromAddrMask;
        int SdaInAddr, SdaInBit;
        int SdaOutAddr, SdaOutBit;
        int SclAddr, SclBit;

        int SdaInCurrValue, SdaOutCurrValue, SclCurrValue;
        int SdaInPrevValue, SdaOutPrevValue, SclPrevValue;

        // Code

        void InitializeEeprom(GameInfo game)
        {
            if (game["EEPROM"] == false) 
                return;
             
            EepromEnabled = true;
            EepromAddrMask = game.GetHexValue("EEPROM_ADDR_MASK");
            EepromSize = EepromAddrMask + 1;

            var t = game.OptionValue("SDA_IN").Split(':');
            SdaInAddr = int.Parse(t[0], NumberStyles.HexNumber);
            SdaInBit = int.Parse(t[1]);

            t = game.OptionValue("SDA_OUT").Split(':');
            SdaOutAddr = int.Parse(t[0], NumberStyles.HexNumber);
            SdaOutBit = int.Parse(t[1]);

            t = game.OptionValue("SCL").Split(':');
            SclAddr = int.Parse(t[0], NumberStyles.HexNumber);
            SclBit = int.Parse(t[1]);

            SaveRAM = new byte[EepromSize];

            Console.WriteLine("EEPROM enabled. Size: ${0:X} SDA_IN: ${1:X}:{2} SDA_OUT: ${3:X}:{4}, SCL: ${5:X}:{6}", 
                EepromSize, SdaInAddr, SdaInBit, SdaOutAddr, SdaOutBit, SclAddr, SclBit);
        }

        void WriteByteEeprom(int address, byte value)
        {
            if (address == SdaInAddr)
            {
                SdaInPrevValue = SdaInCurrValue;
                SdaInCurrValue = (value >> SdaInBit) & 1;
                Console.WriteLine("SDA_IN: {0}", SdaInCurrValue);
            }
            if (address == SclAddr)
            {
                SclPrevValue = SclCurrValue;
                SclCurrValue = (value >> SclBit) & 1;
                Console.WriteLine("SCL: {0}", SclCurrValue);
            }

            // todo: logic!

        }


        byte ReadByteEeeprom()
        {
            return 0; // meh
        }
    }
}
