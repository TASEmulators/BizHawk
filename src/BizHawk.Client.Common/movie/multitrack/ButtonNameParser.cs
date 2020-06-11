namespace BizHawk.Client.Common
{
	internal class ButtonNameParser
	{
		public static ButtonNameParser Parse(string button)
		{
			// See if we're being asked for a button that we know how to rewire
			var parts = button.Split(' ');

			if (parts.Length < 2)
			{
				return null;
			}

			if (parts[0][0] != 'P')
			{
				return null;
			}

			if (!int.TryParse(parts[0].Substring(1), out var player))
			{
				return null;
			}

			return new ButtonNameParser
			{
				PlayerNum = player,
				ButtonPart = button.Substring(parts[0].Length + 1)
			};
		}

		public int PlayerNum { get; set; }
		public string ButtonPart { get; private set; }

		public override string ToString() => $"P{PlayerNum} {ButtonPart}";
	}
}
