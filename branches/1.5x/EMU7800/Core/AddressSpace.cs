/*
 * AddressSpace.cs
 *
 * The class representing the memory map or address space of a machine.
 *
 * Copyright © 2003, 2011 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public sealed class AddressSpace
    {
        public MachineBase M { get; private set; }

        readonly int AddrSpaceShift;
        readonly int AddrSpaceSize;
        readonly int AddrSpaceMask;

        readonly int PageShift;
        readonly int PageSize;

        readonly IDevice[] MemoryMap;
        IDevice Snooper;

        public byte DataBusState { get; private set; }

        public override string ToString()
        {
            return "AddressSpace";
        }

        public byte this[ushort addr]
        {
            get
            {
                if (Snooper != null)
                {
                    // here DataBusState is just facilitating a dummy read to the snooper device
                    // the read operation may have important side effects within the device
                    DataBusState = Snooper[addr];
                }
                var pageno = (addr & AddrSpaceMask) >> PageShift;
                var dev = MemoryMap[pageno];
                DataBusState = dev[addr];
                return DataBusState;
            }
            set
            {
                DataBusState = value;
                if (Snooper != null)
                {
                    Snooper[addr] = DataBusState;
                }
                var pageno = (addr & AddrSpaceMask) >> PageShift;
                var dev = MemoryMap[pageno];
                dev[addr] = DataBusState;
            }
        }

        public void Map(ushort basea, ushort size, IDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            for (int addr = basea; addr < basea + size; addr += PageSize)
            {
                var pageno = (addr & AddrSpaceMask) >> PageShift;
                MemoryMap[pageno] = device;
            }

            LogDebug("{0}: Mapped {1} to ${2:x4}:${3:x4}", this, device, basea, basea + size - 1);
        }

        public void Map(ushort basea, ushort size, Cart cart)
        {
            if (cart == null)
                throw new ArgumentNullException("cart");

            cart.Attach(M);
            var device = (IDevice)cart;
            if (cart.RequestSnooping)
            {
                Snooper = device;
            }
            Map(basea, size, device);
        }

        #region Constructors

        private AddressSpace()
        {
        }

        public AddressSpace(MachineBase m, int addrSpaceShift, int pageShift)
        {
            if (m == null)
                throw new ArgumentNullException("m");

            M = m;

            AddrSpaceShift = addrSpaceShift;
            AddrSpaceSize  = 1 << AddrSpaceShift;
            AddrSpaceMask = AddrSpaceSize - 1;

            PageShift = pageShift;
            PageSize = 1 << PageShift;

            MemoryMap = new IDevice[1 << addrSpaceShift >> PageShift];
            IDevice nullDev = new NullDevice(M);
            for (var pageno=0; pageno < MemoryMap.Length; pageno++)
            {
                MemoryMap[pageno] = nullDev;
            }
        }

        #endregion

        #region Serialization Members

        public AddressSpace(DeserializationContext input, MachineBase m, int addrSpaceShift, int pageShift) : this(m, addrSpaceShift, pageShift)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
            DataBusState = input.ReadByte();
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(DataBusState);
        }

        #endregion

        #region Helpers

        [System.Diagnostics.Conditional("DEBUG")]
        void LogDebug(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        #endregion
    }
}