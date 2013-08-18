using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public abstract partial class C64
    {
        public void InitializeConnections()
        {
            cia1.InputCNT = user.OutputCNT1;
            cia1.InputFlag = ReadCia1Flag;
            cia1.InputPortA = ReadCia1PortA;
            cia1.InputPortB = ReadCia1PortB;
            cia1.InputSP = user.OutputSP1;

            cia2.InputCNT = user.OutputCNT2;
            cia2.InputFlag = user.OutputFLAG2;
            cia2.InputPortA = ReadCia2PortA;
            cia2.InputPortB = user.OutputData;
            cia2.InputSP = user.OutputSP2;

            cpu.InputAEC = vic.OutputAEC;
            cpu.InputIRQ = ReadIRQ;
            cpu.InputNMI = ReadNMI;
            cpu.InputPort = ReadCPUPort;
            cpu.InputRDY = vic.OutputBA;
            cpu.ReadMemory = pla.ReadMemory;
            cpu.WriteMemory = pla.WriteMemory;

            //expansion.InputBA = vic.OutputBA;
            //expansion.InputData = ReadData;
            //expansion.InputHiExpansion = ReadHiExpansion;
            //expansion.InputHiRom = pla.OutputRomHi;
            //expansion.InputIRQ = ReadIRQ;
            //expansion.InputLoExpansion = ReadLoExpansion;
            //expansion.InputLoRom = pla.OutputRomLo;
            //expansion.InputNMI = ReadNMI;

            //pla.InputAEC = vic.OutputAEC;
            //pla.InputBA = vic.OutputBA;
            //pla.InputCharen = ReadCharen;
            //pla.InputExRom = expansion.OutputExRom;
            //pla.InputGame = expansion.OutputGame;
            //pla.InputHiRam = ReadHiRam;
            //pla.InputLoRam = ReadLoRam;
            //pla.InputVA = ReadVicAddress;

            //serial.InputATN = ReadSerialATN;
            //serial.InputClock = ReadSerialCLK;
            //serial.InputData = ReadSerialDTA;

            //user.InputCNT1 = cia1.OutputCNT;
            //user.InputCNT2 = cia2.OutputCNT;
            //user.InputData = cia2.OutputPortB;
            //user.InputPA2 = ReadUserPA2;
            //user.InputPC2 = cia2.OutputPC;
            //user.InputSP1 = cia1.OutputSP;
            //user.InputSP2 = cia2.OutputSP;
        }

        bool ReadCia1Cnt()
        {
            // this pin is not connected
            return true;
        }

        bool ReadCia1Flag()
        {
            return serial.SRQ && cassette.Data;
        }

        int ReadCia1PortA()
        {
            return joystickB.Data & keyboard.Column;
        }

        int ReadCia1PortB()
        {
            return joystickA.Data & keyboard.Row;
        }

        int ReadCia2PortA()
        {
            int result = 0xFF;
            if (!user.PA2)
                result &= 0xFB;
            if (!serial.Clock)
                result &= 0xBF;
            if (!serial.Data)
                result &= 0x7F;
            return result;
        }

        int ReadCPUPort()
        {
            return 0xFF;
        }

        bool ReadHiExpansion()
        {
            int addr = 0xFFFF;
            return (addr >= 0xDF00 && addr < 0xE000);
        }

        bool ReadIRQ()
        {
            return (
                cia1.IRQ &&
                vic.IRQ &&
                expansion.IRQ
                );
        }

        bool ReadLoExpansion()
        {
            int addr = 0xFFFF;
            return (addr >= 0xDE00 && addr < 0xDF00);
        }

        bool ReadLoRam()
        {
            return (cpu.Port & 0x1) != 0;
        }

        bool ReadNMI()
        {
            return (
                cia2.IRQ &&
                expansion.NMI
                );
        }

        bool ReadReset()
        {
            return (
                expansion.Reset
                );
        }

        bool ReadSerialATN()
        {
            return (cia2.PortA & 0x08) != 0;
        }

        bool ReadSerialCLK()
        {
            return (cia2.PortA & 0x10) != 0;
        }

        bool ReadSerialDTA()
        {
            return (cia2.PortA & 0x20) != 0;
        }

        bool ReadUserPA2()
        {
            return (cia2.PortA & 0x04) != 0;
        }

        int ReadVicAddress()
        {
            //return (vic.Address | ((cia2.PortA & 0x3) << 14));
            return 0xFFFF;
        }
    }
}
