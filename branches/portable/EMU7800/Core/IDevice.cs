/*
 * IDevice.cs 
 *
 * Defines interface for devices accessable via the AddressSpace class.
 *
 * Copyright © 2003, 2011 Mike Murphy
 *
 */
namespace EMU7800.Core
{
    public interface IDevice
    {
        void Reset();
        byte this[ushort addr] { get; set; }
    }
}
