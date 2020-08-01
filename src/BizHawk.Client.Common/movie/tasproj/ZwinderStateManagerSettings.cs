namespace BizHawk.Client.Common
{
	// TODO: interface this?
	public class ZwinderStateManagerSettings
	{
		public ZwinderStateManagerSettings() { }

		public ZwinderStateManagerSettings(ZwinderStateManagerSettings settings)
		{
			 // TODO
		}

		/// <summary>
		/// Buffer settings when navigating near now
		/// </summary>
		public IRewindSettings Current { get; set; } = new RewindConfig
		{
			UseCompression = false,
			BufferSize = 64,
			TargetFrameLength = 1000,
		};
		/// <summary>
		/// Buffer settings when navigating directly before the Current buffer
		/// </summary>
		/// <value></value>
		public IRewindSettings Recent { get; set; } = new RewindConfig
		{
			UseCompression = false,
			BufferSize = 64,
			TargetFrameLength = 10000,
		};
		/// <summary>
		/// How often to maintain states when outside of Current and Recent intervals
		/// </summary>
		/// <value></value>
		public int AncientStateInterval { get; set; } = 5000;

		/// <summary>
		/// TODO: NUKE THIS, it doesn't belong here, maybe?
		/// </summary>
		/// <value></value>
		public bool SaveStateHistory { get; set; } = true;
	}
}
