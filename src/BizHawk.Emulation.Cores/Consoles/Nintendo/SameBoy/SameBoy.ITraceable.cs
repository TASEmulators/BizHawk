using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy
	{
		private ITraceable Tracer { get; }
		private readonly LibSameboy.TraceCallback _tracecb;

		private void MakeTrace(ushort pc)
		{
			int[] s = new int[10];
			LibSameboy.sameboy_getregs(SameboyState, s);

			Tracer.Put(new(
				disassembly: LR35902.Disassemble(
					pc,
					addr => LibSameboy.sameboy_cpuread(SameboyState, addr),
					_settings.UseRGBDSSyntax,
					out _).PadRight(36),
				registerInfo: string.Format(
					"A:{0:x2} F:{1:x2} B:{2:x2} C:{3:x2} D:{4:x2} E:{5:x2} H:{6:x2} L:{7:x2} SP:{8:x4} LY:{9:x2} Cy:{10}",
					s[1] & 0xFF,
					s[2] & 0xFF,
					s[3] & 0xFF,
					s[4] & 0xFF,
					s[5] & 0xFF,
					s[6] & 0xFF,
					s[7] & 0xFF,
					s[8] & 0xFF,
					s[9] & 0xFFFF,
					LibSameboy.sameboy_cpuread(SameboyState, 0xFF44),
					CycleCount
					)));
		}
	}
}
