using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed class CartridgePort
	{
		public Func<bool> ReadIRQ;
		public Func<bool> ReadNMI;

		Cart cart;
		bool connected;

		public CartridgePort()
		{
			// start up with no media connected
			Disconnect();
		}

		// ------------------------------------------

		public int PeekHiExp(int addr)
		{
		    return connected ? cart.PeekDF00(addr & 0x00FF) : 0xFF;
		}

	    public int PeekHiRom(int addr)
	    {
	        return connected ? cart.PeekA000(addr & 0x1FFF) : 0xFF;
	    }

	    public int PeekLoExp(int addr)
	    {
	        return connected ? cart.PeekDE00(addr & 0x00FF) : 0xFF;
	    }

	    public int PeekLoRom(int addr)
	    {
	        return connected ? cart.Peek8000(addr & 0x1FFF) : 0xFF;
	    }

	    public void PokeHiExp(int addr, int val) { if (connected) { cart.PokeDF00(addr & 0x00FF, val); } }
		public void PokeHiRom(int addr, int val) { if (connected) { cart.PokeA000(addr & 0x1FFF, val); } }
		public void PokeLoExp(int addr, int val) { if (connected) { cart.PokeDE00(addr & 0x00FF, val); } }
		public void PokeLoRom(int addr, int val) { if (connected) { cart.Poke8000(addr & 0x1FFF, val); } }

		public bool ReadExRom()
		{
		    return !connected || cart.ExRom;
		}

	    public bool ReadGame()
	    {
	        return !connected || cart.Game;
	    }

	    public int ReadHiExp(int addr)
	    {
	        return connected ? cart.ReadDF00((addr & 0x00FF)) : 0xFF;
	    }

	    public int ReadHiRom(int addr)
	    {
	        return connected ? cart.ReadA000((addr & 0x1FFF)) : 0xFF;
	    }

	    public int ReadLoExp(int addr)
	    {
	        return connected ? cart.ReadDE00((addr & 0x00FF)) : 0xFF;
	    }

	    public int ReadLoRom(int addr)
	    {
	        return connected ? cart.Read8000((addr & 0x1FFF)) : 0xFF;
	    }

	    public void WriteHiExp(int addr, int val) { if (connected) { cart.WriteDF00((addr & 0x00FF), val); } }
		public void WriteHiRom(int addr, int val) { if (connected) { cart.WriteA000((addr & 0x1FFF), val); } }
		public void WriteLoExp(int addr, int val) { if (connected) { cart.WriteDE00((addr & 0x00FF), val); } }
		public void WriteLoRom(int addr, int val) { if (connected) { cart.Write8000((addr & 0x1FFF), val); } }

		// ------------------------------------------

		public void Connect(Cart newCart)
		{
			cart = newCart;
			connected = true;
		}

		public void Disconnect()
		{
			cart = null;
			connected = false;
		}

		public void HardReset()
		{
			// note: this will not disconnect any attached media
		}

		public bool IsConnected
		{
			get
			{
				return connected;
			}
		}

		public bool ReadIRQBuffer()
		{
			return true;
		}

		public bool ReadNMIBuffer()
		{
			return true;
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}
	}
}
