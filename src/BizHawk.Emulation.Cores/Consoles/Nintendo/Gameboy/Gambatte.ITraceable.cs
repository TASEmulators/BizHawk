using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy
	{
		private ITraceable Tracer { get; }
		private LibGambatte.TraceCallback _tracecb;

		private void MakeTrace(IntPtr _s)
		{
			var s = new int[14];
			Marshal.Copy(_s, s, 0, 14);
			var PC = (ushort)s[1];

			Tracer.Put(new(
				disassembly: LR35902.Disassemble(
					PC,
					addr =>
					{
						if (addr == PC)
						{
							//opcode
							return (byte)((s[12] >> 16) & 0xFF);
						}

						if (addr == ((PC + 1) & 0xFFFF))
						{
							//high operand
							return (byte)((s[12] >> 8) & 0xFF);
						}

						//low operand
						return (byte)(s[12] & 0xFF);
					},
					_settings.RgbdsSyntax,
					out _).PadRight(36),
#pragma warning disable CA2241
				registerInfo: string.Format(
					"A:{3:x2} F:{8:x2} B:{4:x2} C:{5:x2} D:{6:x2} E:{7:x2} H:{9:x2} L:{10:x2} LY:{13:x2} SP:{2:x2} {11} Cy:{0}",
					(ulong)s[0] + _cycleCount,
					s[1] & 0xffff, // unused (this is the PC)
					s[2] & 0xffff,
					s[3] & 0xff,
					s[4] & 0xff,
					s[5] & 0xff,
					s[6] & 0xff,
					s[7] & 0xff,
					s[8] & 0xff,
					s[9] & 0xff,
					s[10] & 0xff,
					s[11] != 0 ? "prefetched" : "",
					s[12] & 0xffffff, // unused (this is the opcode)
					s[13] & 0xff)));
#pragma warning restore CA2241
		}
	}
}
