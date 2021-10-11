namespace BizHawk.Client.Common
{
	public class LoadRomArgs
	{
		public readonly bool? Deterministic;

		public readonly IOpenAdvanced OpenAdvanced;

		public LoadRomArgs(IOpenAdvanced ioa, bool? deterministic = null)
		{
			Deterministic = deterministic;
			OpenAdvanced = ioa;
		}
	}
}
