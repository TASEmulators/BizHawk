namespace BizHawk.WinForms.BuilderDSL
{
	public readonly struct ControlBuilderContext
	{
		public readonly bool AutoPosOnly;

		public readonly bool AutoSizeOnly;

		public readonly bool IsLTR;

		public bool IsRTL => !IsLTR;

		public ControlBuilderContext(bool isLTR, bool autoPosOnly = false, bool autoSizeOnly = false)
		{
			AutoPosOnly = autoPosOnly;
			AutoSizeOnly = autoSizeOnly;
			IsLTR = isLTR;
		}
	}
}
