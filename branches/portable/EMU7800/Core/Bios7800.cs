/*
 * BIOS7800.cs
 * 
 * The BIOS of the Atari 7800.
 * 
 * Copyright © 2004 Mike Murphy
 * 
 */
using System;

namespace EMU7800.Core
{
    public sealed class Bios7800 : IDevice
    {
        readonly byte[] ROM;
        readonly ushort Mask;

        public ushort Size { get { return (ushort)ROM.Length; } }

        public void Reset() { }

        public byte this[ushort addr]
        {
            get { return ROM[addr & Mask]; }
            set { }
        }

        public Bios7800(byte[] rom)
        {
            if (rom == null)
                throw new ArgumentNullException("rom");
            if (rom.Length != 4096 && rom.Length != 16384)
                throw new ArgumentException("ROM size not 4096 or 16384", "rom");

            ROM = rom;
            Mask = (ushort)ROM.Length;
            Mask--;
        }

        #region Serialization Members

        public Bios7800(DeserializationContext input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
            ROM = input.ReadExpectedBytes(4096, 16384);

            Mask = (ushort)ROM.Length;
            Mask--;
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(ROM);
        }

        #endregion
    }
}