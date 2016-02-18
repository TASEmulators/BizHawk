using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : INESPPUViewable
	{
		// todo: don't just call the callbacks at the end of frame; use the scanline info
		private Action CB1;
		private Action CB2;

		public int[] GetPalette()
		{
			return VideoPalette;
		}

		private byte R2000 { get { return QN.qn_get_reg2000(Context); } }

		public bool BGBaseHigh
		{
			get { return (R2000 & 0x10) != 0; }
		}

		public bool SPBaseHigh
		{
			get { return (R2000 & 0x08) != 0; }
		}

		public bool SPTall
		{
			get { return (R2000 & 0x20) != 0; }
		}

		private byte[] ppubusbuf = new byte[0x3000];
		public byte[] GetPPUBus()
		{
			QN.qn_peek_ppubus(Context, ppubusbuf);
			return ppubusbuf;
		}

		private byte[] palrambuf = new byte[0x20];
		public byte[] GetPalRam()
		{
			Marshal.Copy(QN.qn_get_palmem(Context), palrambuf, 0, 0x20);
			return palrambuf;
		}

		byte[] oambuf = new byte[0x100];
		public byte[] GetOam()
		{
			Marshal.Copy(QN.qn_get_oammem(Context), oambuf, 0, 0x100);
			return oambuf;
		}

		public byte PeekPPU(int addr)
		{
			return QN.qn_peek_ppu(Context, addr);
		}

		// we don't use quicknes's MMC5 at all, so these three methods are just stubs
		public byte[] GetExTiles()
		{
			throw new InvalidOperationException();
		}

		public bool ExActive
		{
			get { return false; }
		}

		public byte[] GetExRam()
		{
			throw new InvalidOperationException();
		}

		public MemoryDomain GetCHRROM()
		{
			return _memoryDomains["CHR VROM"];
		}

		public void InstallCallback1(Action cb, int sl)
		{
			CB1 = cb;
		}

		public void InstallCallback2(Action cb, int sl)
		{
			CB2 = cb;
		}

		public void RemoveCallback1()
		{
			CB1 = null;
		}

		public void RemoveCallback2()
		{
			CB2 = null;
		}
	}
}
