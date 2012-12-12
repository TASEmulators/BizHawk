using System;
using System.IO;
using System.Linq;

namespace EMU7800.Core
{
    /// <summary>
    /// A context for deserializing <see cref="MachineBase"/> objects.
    /// </summary>
    public class DeserializationContext
    {
        #region Fields

        readonly BinaryReader _binaryReader;

        #endregion

        public bool ReadBoolean()
        {
            return _binaryReader.ReadBoolean();
        }

        public byte ReadByte()
        {
            return _binaryReader.ReadByte();
        }

        public ushort ReadUInt16()
        {
            return _binaryReader.ReadUInt16();
        }

        public int ReadInt32()
        {
            return _binaryReader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return _binaryReader.ReadUInt32();
        }

        public long ReadInt64()
        {
            return _binaryReader.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            return _binaryReader.ReadUInt64();
        }

        public double ReadDouble()
        {
            return _binaryReader.ReadDouble();
        }

        public BufferElement ReadBufferElement()
        {
            var be = new BufferElement();
            for (var i = 0; i < BufferElement.SIZE; i++)
                be[i] = ReadByte();
            return be;
        }

        public byte[] ReadBytes()
        {
            var count = _binaryReader.ReadInt32();
            if (count <= 0)
                return new byte[0];
            if (count > 0x40000)
                throw new Emu7800SerializationException("Byte array length too large.");
            return _binaryReader.ReadBytes(count);
        }

        public byte[] ReadExpectedBytes(params int[] expectedSizes)
        {
            var count = _binaryReader.ReadInt32();
            if (!expectedSizes.Any(t => t == count))
                throw new Emu7800SerializationException("Byte array length incorrect.");
            return _binaryReader.ReadBytes(count);
        }

        public byte[] ReadOptionalBytes(params int[] expectedSizes)
        {
            var hasBytes = _binaryReader.ReadBoolean();
            return (hasBytes) ? ReadExpectedBytes(expectedSizes) : null;
        }

        public ushort[] ReadUnsignedShorts(params int[] expectedSizes)
        {
            var bytes = ReadExpectedBytes(expectedSizes.Select(t => t << 1).ToArray());
            var ushorts = new ushort[bytes.Length >> 1];
            Buffer.BlockCopy(bytes, 0, ushorts, 0, bytes.Length);
            return ushorts;
        }

        public int[] ReadIntegers(params int[] expectedSizes)
        {
            var bytes = ReadExpectedBytes(expectedSizes.Select(t => t << 2).ToArray());
            var integers = new int[bytes.Length >> 2];
            Buffer.BlockCopy(bytes, 0, integers, 0, bytes.Length);
            return integers;
        }

        public uint[] ReadUnsignedIntegers(params int[] expectedSizes)
        {
            var bytes = ReadExpectedBytes(expectedSizes.Select(t => t << 2).ToArray());
            var uints = new uint[bytes.Length >> 2];
            Buffer.BlockCopy(bytes, 0, uints, 0, bytes.Length);
            return uints;
        }

        public bool[] ReadBooleans(params int[] expectedSizes)
        {
            var bytes = ReadExpectedBytes(expectedSizes);
            var booleans = new bool[bytes.Length];
            for (var i = 0; i < bytes.Length; i++)
                booleans[i] = (bytes[i] != 0);
            return booleans;
        }

        public int CheckVersion(params int[] validVersions)
        {
            var magicNumber = _binaryReader.ReadInt32();
            if (magicNumber != 0x78000087)
                throw new Emu7800SerializationException("Magic number not found.");
            var version = _binaryReader.ReadInt32();
            if (!validVersions.Any(t => t == version))
                throw new Emu7800SerializationException("Invalid version number found.");
            return version;
        }

        public MachineBase ReadMachine()
        {
            var typeName = _binaryReader.ReadString();
            if (string.IsNullOrWhiteSpace(typeName))
                throw new Emu7800SerializationException("Invalid type name.");

            var type = Type.GetType(typeName);
            if (type == null)
                throw new Emu7800SerializationException("Unable to resolve type name: " + typeName);

            return (MachineBase)Activator.CreateInstance(type, new object[] { this });
        }

        public AddressSpace ReadAddressSpace(MachineBase m, int addrSpaceShift, int pageShift)
        {
            var addressSpace = new AddressSpace(this, m, addrSpaceShift, pageShift);
            return addressSpace;
        }

        public M6502 ReadM6502(MachineBase m, int runClocksMultiple)
        {
            var cpu = new M6502(this, m, runClocksMultiple);
            return cpu;
        }

        public Maria ReadMaria(Machine7800 m, int scanlines)
        {
            var maria = new Maria(this, m, scanlines);
            return maria;
        }

        public PIA ReadPIA(MachineBase m)
        {
            var pia = new PIA(this, m);
            return pia;
        }

        public TIA ReadTIA(MachineBase m)
        {
            var tia = new TIA(this, m);
            return tia;
        }

        public TIASound ReadTIASound(MachineBase m, int cpuClocksPerSample)
        {
            var tiaSound = new TIASound(this, m, cpuClocksPerSample);
            return tiaSound;
        }

        public RAM6116 ReadRAM6116()
        {
            var ram6116 = new RAM6116(this);
            return ram6116;
        }

        public InputState ReadInputState()
        {
            var inputState = new InputState(this);
            return inputState;
        }

        public HSC7800 ReadOptionalHSC7800()
        {
            var exist = ReadBoolean();
            return exist ? new HSC7800(this) : null;
        }

        public Bios7800 ReadOptionalBios7800()
        {
            var exist = ReadBoolean();
            return exist ? new Bios7800(this) : null;
        }

        public PokeySound ReadOptionalPokeySound(MachineBase m)
        {
            var exist = ReadBoolean();
            return exist ? new PokeySound(this, m) : null;
        }

        public Cart ReadCart(MachineBase m)
        {
            var typeName = _binaryReader.ReadString();
            if (string.IsNullOrWhiteSpace(typeName))
                throw new Emu7800SerializationException("Invalid type name.");

            var type = Type.GetType(typeName);
            if (type == null)
                throw new Emu7800SerializationException("Unable to resolve type name: " + typeName);

            return (Cart)Activator.CreateInstance(type, new object[] { this, m });
        }

        #region Constructors

        private DeserializationContext()
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="DeserializationContext"/>.
        /// </summary>
        /// <param name="binaryReader"/>
        internal DeserializationContext(BinaryReader binaryReader)
        {
            if (binaryReader == null)
                throw new ArgumentNullException("binaryReader");
            _binaryReader = binaryReader;
        }

        #endregion
    }
}
