using System;
using System.IO;

namespace EMU7800.Core
{
    /// <summary>
    /// A context for serializing <see cref="MachineBase"/> objects.
    /// </summary>
    public class SerializationContext
    {
        #region Fields

        readonly BinaryWriter _binaryWriter;

        #endregion

        public void Write(byte value)
        {
            _binaryWriter.Write(value);
        }

        public void Write(ushort value)
        {
            _binaryWriter.Write(value);
        }

        public void Write(int value)
        {
            _binaryWriter.Write(value);
        }

        public void Write(uint value)
        {
            _binaryWriter.Write(value);
        }

        public void Write(long value)
        {
            _binaryWriter.Write(value);
        }

        public void Write(ulong value)
        {
            _binaryWriter.Write(value);
        }

        public void Write(bool value)
        {
            _binaryWriter.Write(value);
        }

        public void Write(double value)
        {
            _binaryWriter.Write(value);
        }

		/*
        public void Write(BufferElement bufferElement)
        {
            for (var i = 0; i < BufferElement.SIZE; i++)
                Write(bufferElement[i]);
        }
		*/

        public void Write(byte[] bytes)
        {
            _binaryWriter.Write(bytes.Length);
            if (bytes.Length > 0)
                _binaryWriter.Write(bytes);
        }

        public void Write(ushort[] ushorts)
        {
            var bytes = new byte[ushorts.Length << 1];
            Buffer.BlockCopy(ushorts, 0, bytes, 0, bytes.Length);
            Write(bytes);
        }

        public void Write(int[] ints)
        {
            var bytes = new byte[ints.Length << 2];
            Buffer.BlockCopy(ints, 0, bytes, 0, bytes.Length);
            Write(bytes);
        }

        public void Write(uint[] uints)
        {
            var bytes = new byte[uints.Length << 2];
            Buffer.BlockCopy(uints, 0, bytes, 0, bytes.Length);
            Write(bytes);
        }

        public void Write(bool[] booleans)
        {
            var bytes = new byte[booleans.Length];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(booleans[i] ? 0xff : 0x00);
            }
            Write(bytes);
        }

        public void Write(MachineBase m)
        {
            WriteTypeName(m);
            m.GetObjectData(this);
        }

        public void Write(AddressSpace mem)
        {
            mem.GetObjectData(this);
        }

        public void Write(M6502 cpu)
        {
            cpu.GetObjectData(this);
        }

        public void Write(PIA pia)
        {
            pia.GetObjectData(this);
        }

        public void Write(TIA tia)
        {
            tia.GetObjectData(this);
        }

        public void Write(TIASound tiaSound)
        {
            tiaSound.GetObjectData(this);
        }

        public void Write(Maria maria)
        {
            maria.GetObjectData(this);
        }

        public void Write(Cart cart)
        {
            WriteTypeName(cart);
            cart.GetObjectData(this);
        }

        public void Write(RAM6116 ram6116)
        {
            ram6116.GetObjectData(this);
        }

        public void Write(InputState inputState)
        {
            inputState.GetObjectData(this);
        }

        public void WriteVersion(int version)
        {
            Write(0x78000087);
            Write(version);
        }

        public void WriteOptional(byte[] bytes)
        {
            var hasBytes = (bytes != null);
            _binaryWriter.Write(hasBytes);
            if (!hasBytes)
                return;
            _binaryWriter.Write(bytes.Length);
            if (bytes.Length > 0)
                _binaryWriter.Write(bytes);
        }

        public void WriteOptional(HSC7800 hsc7800)
        {
            var exist = (hsc7800 != null);
            Write(exist);
            if (!exist)
                return;
            hsc7800.GetObjectData(this);
        }

        public void WriteOptional(Bios7800 bios7800)
        {
            var exist = (bios7800 != null);
            Write(exist);
            if (!exist)
                return;
            bios7800.GetObjectData(this);
        }

        public void WriteOptional(PokeySound pokeySound)
        {
            var exist = (pokeySound != null);
            Write(exist);
            if (!exist)
                return;
            pokeySound.GetObjectData(this);
        }

        #region Constructors

        private SerializationContext()
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="SerializationContext"/>.
        /// </summary>
        /// <param name="binaryWriter"/>
        internal SerializationContext(BinaryWriter binaryWriter)
        {
            if (binaryWriter == null)
                throw new ArgumentNullException("binaryWriter");
            _binaryWriter = binaryWriter;
        }

        #endregion

        #region Helpers

        void WriteTypeName(object o)
        {
            if (o == null)
                throw new Emu7800SerializationException("Type unexpectedly null.");
            var typeName = o.GetType().FullName;
            if (string.IsNullOrWhiteSpace(typeName))
                throw new Emu7800SerializationException("Unable to discover type name.");
            _binaryWriter.Write(typeName);
        }

        #endregion
    }
}
