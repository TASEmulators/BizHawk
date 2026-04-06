namespace BizHawk.Client.Common
{
	public struct AnalogBind
	{
		/// <summary>the physical stick that we're bound to</summary>
		public string Value;

		/// <summary>sensitivity and flip</summary>
		public float Mult;

		/// <summary>portion of axis to ignore</summary>
		public float Deadzone;

		public string ButtonBindPositive;

		public string ButtonBindNegative;

		public AnalogBind(string value, float mult, float deadzone, string positiveButton, string negativeButton)
		{
			Value = value;
			Mult = mult;
			Deadzone = deadzone;
			ButtonBindPositive = positiveButton;
			ButtonBindNegative = negativeButton;
		}
	}
}
