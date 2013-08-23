/*
 * RAM6116.cs
 *
 * Implements a 6116 RAM device found in the 7800.
 *
 * Copyright © 2004 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public sealed class RAM6116 : IDevice
    {
        readonly byte[] RAM;

        #region IDevice Members

        public void Reset() {}

        public byte this[ushort addr]
        {
            get { return RAM[addr & 0x07ff]; }
            set { RAM[addr & 0x07ff] = value; }
        }

        #endregion

        #region Constructors

        public RAM6116()
        {
            RAM = new byte[0x800];
        }

        public RAM6116(byte[] ram)
        {
            RAM = ram;
        }

        #endregion

        #region Serialization Members

        public RAM6116(DeserializationContext input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
            RAM = input.ReadExpectedBytes(0x800);
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(RAM);
        }

        #endregion
    }
}