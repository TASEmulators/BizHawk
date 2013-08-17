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

        public int Address
        {
            get
            {
                return cachedADDR;
            }
        }

        public bool AEC
        {
            get
            {
                return cachedAEC;
            }
        }

        public bool BA
        {
            get
            {
                return cachedBA;
            }
        }

        public bool CAS
        {
            get
            {
                return cachedCAS;
            }
        }

        public int Data
        {
            get
            {
                return cachedDATA;
            }
        }

        public bool IRQ
        {
            get
            {
                return cachedIRQ;
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

        public bool OutputRAS()
        {
            return RAS;
        }

        public bool RAS
        {
            get
            {
                return cachedRAS;
            }
        }

        public void Precache() 
        {
            cachedAEC = (pixelTimer >= 4);
            cachedBA = ba;
            cachedCAS = cas;
            cachedDATA = data;
            cachedIRQ = irq;
            
        }

        public void SyncState(Serializer ser) { }
    }
}
