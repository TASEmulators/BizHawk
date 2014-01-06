using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NES
{
	public interface IHasNESPPUDebug
	{
		INESPPUDebug GetDebugger();
	}

	public interface INESPPUDebug
	{
		/// <summary>
		/// looks up an internal NES pixel value to an rgb int (applying the core's current palette and assuming no deemph)
		/// </summary>
		int LookupColor(int pixel);

		// reg 2000 stuff

		Bit vram_incr32 { get; } //(0: increment by 1, going across; 1: increment by 32, going down)
		Bit obj_pattern_hi { get; } //Sprite pattern table address for 8x8 sprites (0: $0000; 1: $1000)
		Bit bg_pattern_hi { get; } //Background pattern table address (0: $0000; 1: $1000)
		Bit obj_size_16 { get; } //Sprite size (0: 8x8 sprites; 1: 8x16 sprites)
		Bit ppu_layer { get; } //PPU layer select (should always be 0 in the NES; some Nintendo arcade boards presumably had two PPUs)
		Bit vblank_nmi_gen { get; } //Vertical blank NMI generation (0: off; 1: on)

		byte Reg2000Value { get; }

		byte OAM(int addr);
		byte PALRAM(int addr);
		byte PPUBUS(int addr);

		void SetNTViewCallback(int scanline, Action target);
		void SetPPUViewCallback(int scanline, Action target);
	}

	// implementation for NESHAWK
	public class NESHawkPPUDebug : INESPPUDebug
	{
		private BizHawk.Emulation.Cores.Nintendo.NES.NES _nes;
		private BizHawk.Emulation.Cores.Nintendo.NES.NES.PPU _ppu;

		public NESHawkPPUDebug(BizHawk.Emulation.Cores.Nintendo.NES.NES nes)
		{
			_nes = nes;
			_ppu = nes.ppu;
		}

		public int LookupColor(int pixel)
		{
			return _nes.LookupColor(pixel);
		}

		public Bit vram_incr32
		{
			get { return _ppu.reg_2000.vram_incr32; }
		}

		public Bit obj_pattern_hi
		{
			get { return _ppu.reg_2000.obj_pattern_hi; }
		}

		public Bit bg_pattern_hi
		{
			get { return _ppu.reg_2000.bg_pattern_hi; }
		}

		public Bit obj_size_16
		{
			get { return _ppu.reg_2000.obj_size_16; }
		}

		public Bit ppu_layer
		{
			get { return _ppu.reg_2000.ppu_layer; }
		}

		public Bit vblank_nmi_gen
		{
			get { return _ppu.reg_2000.vblank_nmi_gen; }
		}

		public byte Reg2000Value
		{
			get { return _ppu.reg_2000.Value; }
		}

		public byte OAM(int addr)
		{
			return _ppu.OAM[addr];
		}

		public byte PALRAM(int addr)
		{
			return _ppu.PALRAM[addr];
		}

		public byte PPUBUS(int addr)
		{
			return _ppu.ppubus_peek(addr);
		}

		public void SetNTViewCallback(int scanline, Action target)
		{
			if (target != null)
				_ppu.NTViewCallback = new Cores.Nintendo.NES.NES.PPU.DebugCallback
				{
					Scanline = scanline,
					Callback = target
				};
			else
				_ppu.NTViewCallback = null;
		}

		public void SetPPUViewCallback(int scanline, Action target)
		{
			if (target != null)
				_ppu.PPUViewCallback = new Cores.Nintendo.NES.NES.PPU.DebugCallback
				{
					Scanline = scanline,
					Callback = target
				};
			else
				_ppu.PPUViewCallback = null;
		}
	}
}
