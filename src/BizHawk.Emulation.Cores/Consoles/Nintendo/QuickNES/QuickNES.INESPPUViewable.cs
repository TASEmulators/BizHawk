using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : INESPPUViewable
	{
		// todo: don't just call the callbacks at the end of frame; use the scanline info
		private Action _callBack1;
		private Action _callBack2;

		public int[] GetPalette() => _videoPalette;

		private byte R2000 => QN.qn_get_reg2000(Context);

		public bool BGBaseHigh => (R2000 & 0x10) != 0;

		public bool SPBaseHigh => (R2000 & 0x08) != 0;

		public bool SPTall => (R2000 & 0x20) != 0;

		private readonly byte[] ppubusbuf = new byte[0x3000];
		public byte[] GetPPUBus()
		{
			QN.qn_peek_ppubus(Context, ppubusbuf);
			return ppubusbuf;
		}

		private readonly byte[] palrambuf = new byte[0x20];
		public byte[] GetPalRam()
		{
			Marshal.Copy(QN.qn_get_palmem(Context), palrambuf, 0, 0x20);
			return palrambuf;
		}

		private readonly byte[] oambuf = new byte[0x100];
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

		public bool ExActive => false;

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
			_callBack1 = cb;
		}

		public void InstallCallback2(Action cb, int sl)
		{
			_callBack2 = cb;
		}

		public void RemoveCallback1()
		{
			_callBack1 = null;
		}

		public void RemoveCallback2()
		{
			_callBack2 = null;
		}
	}
}
