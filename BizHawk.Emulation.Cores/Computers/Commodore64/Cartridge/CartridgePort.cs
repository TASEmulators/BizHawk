using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	public sealed class CartridgePort : IDriveLight
	{
	    public Func<int> ReadOpenBus; 

	    private CartridgeDevice _cartridgeDevice;
	    private bool _connected;

		public CartridgePort()
		{
			// start up with no media connected
			Disconnect();
		}

		// ------------------------------------------

		public int PeekHiExp(int addr)
		{
		    return _connected ? _cartridgeDevice.PeekDF00(addr & 0x00FF) : 0xFF;
		}

	    public int PeekHiRom(int addr)
	    {
	        return _connected ? _cartridgeDevice.PeekA000(addr & 0x1FFF) : 0xFF;
	    }

	    public int PeekLoExp(int addr)
	    {
	        return _connected ? _cartridgeDevice.PeekDE00(addr & 0x00FF) : 0xFF;
	    }

	    public int PeekLoRom(int addr)
	    {
	        return _connected ? _cartridgeDevice.Peek8000(addr & 0x1FFF) : 0xFF;
	    }

	    public void PokeHiExp(int addr, int val) { if (_connected) { _cartridgeDevice.PokeDF00(addr & 0x00FF, val); } }
		public void PokeHiRom(int addr, int val) { if (_connected) { _cartridgeDevice.PokeA000(addr & 0x1FFF, val); } }
		public void PokeLoExp(int addr, int val) { if (_connected) { _cartridgeDevice.PokeDE00(addr & 0x00FF, val); } }
		public void PokeLoRom(int addr, int val) { if (_connected) { _cartridgeDevice.Poke8000(addr & 0x1FFF, val); } }

		public bool ReadExRom()
		{
		    return !_connected || _cartridgeDevice.ExRom;
		}

	    public bool ReadGame()
	    {
	        return !_connected || _cartridgeDevice.Game;
	    }

	    public int ReadHiExp(int addr)
	    {
	        return _connected ? _cartridgeDevice.ReadDF00(addr & 0x00FF) : 0xFF;
	    }

	    public int ReadHiRom(int addr)
	    {
	        return _connected ? _cartridgeDevice.ReadA000(addr & 0x1FFF) : 0xFF;
	    }

	    public int ReadLoExp(int addr)
	    {
	        return _connected ? _cartridgeDevice.ReadDE00(addr & 0x00FF) : 0xFF;
	    }

	    public int ReadLoRom(int addr)
	    {
	        return _connected ? _cartridgeDevice.Read8000(addr & 0x1FFF) : 0xFF;
	    }

	    public void WriteHiExp(int addr, int val) { if (_connected) { _cartridgeDevice.WriteDF00(addr & 0x00FF, val); } }
		public void WriteHiRom(int addr, int val) { if (_connected) { _cartridgeDevice.WriteA000(addr & 0x1FFF, val); } }
		public void WriteLoExp(int addr, int val) { if (_connected) { _cartridgeDevice.WriteDE00(addr & 0x00FF, val); } }
		public void WriteLoRom(int addr, int val) { if (_connected) { _cartridgeDevice.Write8000(addr & 0x1FFF, val); } }

		// ------------------------------------------

		public void Connect(CartridgeDevice newCartridgeDevice)
		{
            _connected = true;
		    _cartridgeDevice = newCartridgeDevice;
		    newCartridgeDevice.ReadOpenBus = ReadOpenBus;
		}

		public void Disconnect()
		{
			_cartridgeDevice = null;
			_connected = false;
		}

	    public void ExecutePhase()
	    {
	        if (_connected)
                _cartridgeDevice.ExecutePhase();
	    }

		public void HardReset()
		{
			// note: this will not disconnect any attached media
		    if (_connected)
		    {
		        _cartridgeDevice.HardReset();
		    }
		}

		public bool IsConnected
		{
			get
			{
				return _connected;
			}
		}

		public bool ReadIrq()
		{
			return !_connected || _cartridgeDevice.IRQ;
		}

		public bool ReadNmi()
		{
            return !_connected || _cartridgeDevice.NMI;
        }

        public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}

	    public bool DriveLightEnabled { get { return _connected && _cartridgeDevice.DriveLightEnabled; } }
	    public bool DriveLightOn { get { return _connected && _cartridgeDevice.DriveLightOn; } }
	}
}
