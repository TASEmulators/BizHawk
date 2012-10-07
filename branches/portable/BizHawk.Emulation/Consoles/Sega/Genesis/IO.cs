namespace BizHawk.Emulation.Consoles.Sega
{
    partial class Genesis
    {
        bool SegaCD = false;

        class IOPort
        {
            public byte Data;
            public byte Control;
            public byte TxData;
            public byte RxData;
            public byte SCtrl;
            // TODO- a reference to connected device? That gets into the issue of configuring different types of controllers. :|

            public bool TH { get { return (Data & 0x40) != 0; } }
        }

        IOPort[] IOPorts = new IOPort[] 
        { 
            new IOPort { Data = 0x7F, TxData = 0xFF, RxData = 0xFF, SCtrl = 0xFF }, 
            new IOPort { Data = 0x7F, TxData = 0xFF, RxData = 0xFF, SCtrl = 0xFF }, 
            new IOPort { Data = 0x7F, TxData = 0xFF, RxData = 0xFF, SCtrl = 0xFF } 
        };

        byte ReadIO(int offset)
        {
            offset >>= 1;
            offset &= 0x0F;
            if (offset > 1)
                Log.Note("CPU", "^^^ IO Read {0}: 00", offset);
            switch (offset)
            {
                case 0: // version
                    byte value = (byte) (SegaCD ? 0x00 : 0x20);
                    switch((char)RomData[0x01F0])
                    {
                        case 'J': value |= 0x00; break;
                        case 'U': value |= 0x80; break;
                        case 'E': value |= 0xC0; break;
                        case 'A': value |= 0xC0; break;
                        case '4': value |= 0x80; break;
                        default:  value |= 0x80; break;
                    }
                    //value |= 1; // US
                    Log.Note("CPU", "^^^ IO Read 0: {0:X2}", value);
                    return value;
                case 1: // Port A 
					lagged = false;
                    ReadController(ref IOPorts[0].Data);
                    Log.Note("CPU", "^^^ IO Read 1: {0:X2}", IOPorts[0].Data);
                    return IOPorts[0].Data;
                case 2: return 0xFF;
                case 3: return 0xFF;

                case 0x04: return IOPorts[0].Control;
                case 0x05: return IOPorts[1].Control;
                case 0x06: return IOPorts[2].Control;

                case 0x07: return IOPorts[0].TxData;
                case 0x08: return IOPorts[0].RxData;
                case 0x09: return IOPorts[0].SCtrl;

                case 0x0A: return IOPorts[1].TxData;
                case 0x0B: return IOPorts[1].RxData;
                case 0x0C: return IOPorts[1].SCtrl;

                case 0x0D: return IOPorts[2].TxData;
                case 0x0E: return IOPorts[2].RxData;
                case 0x0F: return IOPorts[2].SCtrl;
            }
            Log.Note("CPU", "^^^ IO Read {0}: {1:X2}", offset, 0xFF);
            return 0xFF;
        }

        void WriteIO(int offset, int value)
        {
            offset >>= 1;
            offset &= 0x0F;

            switch (offset)
            {
                case 0x00: break;

                case 0x01: IOPorts[0].Data    = (byte) value; break;
                case 0x02: IOPorts[1].Data    = (byte) value; break;
                case 0x03: IOPorts[2].Data    = (byte) value; break;

                case 0x04: IOPorts[0].Control = (byte) value; break;
                case 0x05: IOPorts[1].Control = (byte) value; break;
                case 0x06: IOPorts[2].Control = (byte) value; break;

                case 0x07: IOPorts[0].TxData  = (byte) value; break;
                case 0x08: IOPorts[0].RxData  = (byte) value; break;
                case 0x09: IOPorts[0].SCtrl   = (byte) value; break;

                case 0x0A: IOPorts[1].TxData  = (byte) value; break;
                case 0x0B: IOPorts[1].RxData  = (byte) value; break;
                case 0x0C: IOPorts[1].SCtrl   = (byte) value; break;

                case 0x0D: IOPorts[2].TxData  = (byte) value; break;
                case 0x0E: IOPorts[2].RxData  = (byte) value; break;
                case 0x0F: IOPorts[2].SCtrl   = (byte) value; break;
            }
        }

        void ReadController(ref byte data)
        {
			if (CoreInputComm.InputCallback != null) CoreInputComm.InputCallback();
			data &= 0xC0;
            if ((data & 0x40) != 0) // TH high
            {
                if (Controller["P1 Up"]    == false) data |= 0x01;
                if (Controller["P1 Down"]  == false) data |= 0x02;
                if (Controller["P1 Left"]  == false) data |= 0x04;
                if (Controller["P1 Right"] == false) data |= 0x08;
                if (Controller["P1 B"]     == false) data |= 0x10;
                if (Controller["P1 C"]     == false) data |= 0x20;
            } else { // TH low
                if (Controller["P1 Up"]    == false) data |= 0x01;
                if (Controller["P1 Down"]  == false) data |= 0x02;
                if (Controller["P1 A"]     == false) data |= 0x10;
                if (Controller["P1 Start"] == false) data |= 0x20;
            }
            
        }
    }
}