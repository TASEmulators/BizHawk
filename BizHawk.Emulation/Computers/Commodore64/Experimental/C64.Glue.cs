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
            basicRom.InputAddress = ReadAddress;
            basicRom.InputData = ReadData;

            characterRom.InputAddress = ReadAddress;
            characterRom.InputData = ReadData;

            cia1.InputAddress = ReadAddress;
            cia1.InputClock = vic.OutputPHI0;
            cia1.InputCNT = user.OutputCNT1;
            cia1.InputData = ReadData;
            cia1.InputFlag = ReadCia1Flag;
            cia1.InputPortA = ReadCia1PortA;
            cia1.InputPortB = ReadCia1PortB;
            cia1.InputRead = cpu.OutputRead;
            cia1.InputReset = ReadReset;
            cia1.InputSP = user.OutputSP1;

            cia2.InputAddress = ReadAddress;
            cia2.InputClock = vic.OutputPHI0;
            cia2.InputCNT = user.OutputCNT2;
            cia2.InputData = ReadData;
            cia2.InputFlag = user.OutputFLAG2;
            cia2.InputPortA = ReadCia2PortA;
            cia2.InputPortB = user.OutputData;
            cia2.InputRead = cpu.OutputRead;
            cia2.InputReset = ReadReset;
            cia2.InputSP = user.OutputSP2;

            colorRam.InputAddress = ReadAddress;
            colorRam.InputData = ReadData;
            colorRam.InputRead = cpu.OutputRead;

            cpu.InputAddress = ReadAddress;
            cpu.InputAEC = vic.OutputAEC;
            cpu.InputClock = vic.OutputPHI0;
            cpu.InputData = ReadData;
            cpu.InputIRQ = ReadIRQ;
            cpu.InputNMI = ReadNMI;
            cpu.InputPort = ReadCPUPort;
            cpu.InputRDY = vic.OutputBA;
            cpu.InputReset = ReadReset;

            expansion.InputAddress = ReadAddress;
            expansion.InputBA = vic.OutputBA;
            expansion.InputData = ReadData;
            expansion.InputDotClock = vic.OutputPixelClock;
            expansion.InputHiExpansion = ReadHiExpansion;
            expansion.InputHiRom = pla.OutputRomHi;
            expansion.InputIRQ = ReadIRQ;
            expansion.InputLoExpansion = ReadLoExpansion;
            expansion.InputLoRom = pla.OutputRomLo;
            expansion.InputNMI = ReadNMI;
            expansion.InputRead = cpu.OutputRead;
            expansion.InputReset = ReadReset;

            kernalRom.InputAddress = ReadAddress;
            kernalRom.InputData = ReadData;

            memory.InputAddress = ReadAddress;
            memory.InputData = ReadData;
            memory.InputRead = cpu.OutputRead;

            pla.InputAddress = ReadAddress;
            pla.InputAEC = vic.OutputAEC;
            pla.InputBA = vic.OutputBA;
            pla.InputCAS = vic.OutputCAS;
            pla.InputCharen = ReadCharen;
            pla.InputExRom = expansion.OutputExRom;
            pla.InputGame = expansion.OutputGame;
            pla.InputHiRam = ReadHiRam;
            pla.InputLoRam = ReadLoRam;
            pla.InputRead = cpu.OutputRead;
            pla.InputVA = ReadVicAddress;

            serial.InputATN = ReadSerialATN;
            serial.InputClock = ReadSerialCLK;
            serial.InputData = ReadSerialDTA;
            serial.InputReset = ReadReset;

            sid.InputAddress = ReadAddress;
            sid.InputData = ReadData;
            sid.InputRead = cpu.OutputRead;

            user.InputCNT1 = cia1.OutputCNT;
            user.InputCNT2 = cia2.OutputCNT;
            user.InputData = cia2.OutputPortB;
            user.InputPA2 = ReadUserPA2;
            user.InputPC2 = cia2.OutputPC;
            user.InputReset = ReadReset;
            user.InputSP1 = cia1.OutputSP;
            user.InputSP2 = cia2.OutputSP;

            vic.InputAddress = ReadAddress;
            vic.InputData = ReadData;
            vic.InputRead = cpu.OutputRead;
        }

        int ReadAddress()
        {
            int addr = 0xFFFF;
            addr &= cpu.Address;
            addr &= expansion.Address;
            addr &= vic.Address;
            return addr;
        }

        bool ReadCharen()
        {
            return (cpu.Port & 0x4) != 0;
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
            return (joystickB.Data | 0xE0) & keyboard.Column;
        }

        int ReadCia1PortB()
        {
            return (joystickA.Data | 0xE0) & keyboard.Row;
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

        int ReadData()
        {
            int data = 0xFF;
            data &= expansion.Data;
            if (pla.Basic)
                data &= basicRom.Data;
            if (pla.CharRom)
                data &= characterRom.Data;
            if (pla.GraphicsRead)
                data &= colorRam.Data;
            if (pla.IO)
            {
                data &= cia1.Data;
                data &= cia2.Data;
                data &= sid.Data;
                data &= vic.Data;
            }
            if (vic.BA)
                data &= cpu.Data;
            if (pla.Kernal)
                data &= kernalRom.Data;
            if (pla.CASRam)
                data &= memory.Data;
            return data;
        }

        bool ReadHiExpansion()
        {
            int addr = ReadAddress();
            return (addr >= 0xDF00 && addr < 0xE000);
        }

        bool ReadHiRam()
        {
            return (cpu.Port & 0x2) != 0;
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
            int addr = ReadAddress();
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
            return (vic.Address | ((cia2.PortA & 0x3) << 14));
        }
    }
}
