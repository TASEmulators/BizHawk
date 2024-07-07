using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ILinkable
	{
		private bool _linkConnected;

		public bool LinkConnected
		{
			get => _linkConnected;
			set { }
		}
	}
}
