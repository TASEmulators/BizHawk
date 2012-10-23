/*
 * HSC7800.cs
 * 
 * The 7800 High Score cartridge--courtesy of Matthias <matthias@atari8bit.de>.
 * 
 */
using System;

namespace EMU7800.Core
{
    public sealed class HSC7800 : IDevice
    {
        readonly byte[] ROM;
        readonly ushort Mask;

        public static ushort Size { get; private set; }

        #region IDevice Members

        public void Reset()
        {
        }

        public byte this[ushort addr]
        {
            get { return ROM[addr & Mask]; }
            set { }
        }

        #endregion

        public RAM6116 SRAM  { get; private set; }

        #region Constructors

        private HSC7800()
        {
        }

        public HSC7800(byte[] hscRom, byte[] ram)
        {
            if (hscRom == null)
                throw new ArgumentNullException("hscRom");
            if (ram == null)
                throw new ArgumentNullException("ram");
            if (hscRom.Length != 4096)
                throw new ArgumentException("ROM size not 4096", "hscRom");

            ROM = hscRom;
            SRAM = new RAM6116(ram);

            Size = Mask = (ushort)ROM.Length;
            Mask--;
        }

        #endregion

        #region Serialization Members

        public HSC7800(DeserializationContext input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
            ROM = input.ReadExpectedBytes(4096);
            SRAM = input.ReadRAM6116();
 
            Size = Mask = (ushort)ROM.Length;
            Mask--;
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(SRAM);
        }

        #endregion
    }
}