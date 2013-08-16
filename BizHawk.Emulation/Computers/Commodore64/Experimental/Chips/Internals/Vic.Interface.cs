using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public abstract partial class Vic
    {
        public Func<int> InputAddress;
        public Func<int> InputData;
        public Func<bool> InputRead;

        public Vic()
        {
            backgroundColor = new int[4];
            spriteMultiColor = new int[2];
            sprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
                sprites[i] = new Sprite();
        }

        virtual public int Address
        {
            get
            {
                return bufferADDR;
            }
        }

        virtual public bool AEC
        {
            get
            {
                return bufferAEC;
            }
        }

        virtual public bool BA
        {
            get
            {
                return bufferBA;
            }
        }

        virtual public bool CAS
        {
            get
            {
                return bufferCAS;
            }
        }

        virtual public int Data
        {
            get
            {
                return bufferDATA;
            }
        }

        virtual public bool IRQ
        {
            get
            {
                return bufferIRQ;
            }
        }

        public int OutputAddress()
        {
            return Address;
        }

        public bool OutputAEC()
        {
            return AEC;
        }

        public bool OutputBA()
        {
            return BA;
        }

        public bool OutputCAS()
        {
            return CAS;
        }

        public int OutputData()
        {
            return Data;
        }

        public bool OutputIRQ()
        {
            return IRQ;
        }

        public bool OutputPHI0()
        {
            return PHI0;
        }

        public bool OutputPixelClock()
        {
            return PixelClock;
        }

        public bool OutputRAS()
        {
            return RAS;
        }

        virtual public bool PHI0
        {
            get
            {
                return bufferPHI0;
            }
        }

        virtual public bool PixelClock
        {
            get
            {
                return true;
            }
        }

        virtual public bool RAS
        {
            get
            {
                return bufferRAS;
            }
        }

        virtual public void Precache() { }
        virtual public void SyncState(Serializer ser) { }
    }
}
