#nullable enable

using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public sealed class LinuxColorTable : ProfessionalColorTable
	{
		public override Color MenuStripGradientBegin { get; } = Color.WhiteSmoke;

		public override Color ToolStripDropDownBackground { get; } = Color.WhiteSmoke;

		public override Color ToolStripGradientEnd { get; } = Color.WhiteSmoke;
	}
}
