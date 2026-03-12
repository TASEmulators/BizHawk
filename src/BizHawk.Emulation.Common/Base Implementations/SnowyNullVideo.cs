namespace BizHawk.Emulation.Common
{
	/// <seealso cref="NullVideo"/>
	public sealed class SnowyNullVideo : IVideoProvider
	{
		[CoreSettings]
		public sealed record class Settings(
			float Bias = -2.0f,
			int FramerateScalar = 10,
			float Intensity = 1.0f);

		public enum TriggerCriterion : byte
		{
			Never = 0,
			Always = 1,
			WeekOfChristmas = 2,
		}

		private const int DefaultHeight = 192;

		private const int DefaultWidth = 256;

		private readonly int[] _buf = new int[DefaultWidth * DefaultHeight];

		private int _rep = 0;

		private readonly Random _rng = new(Seed: 0x4); // chosen by fair dice roll. guaranteed to be random.

		public int BackgroundColor
			=> 0;

		public int BufferHeight
			=> DefaultHeight;

		public int BufferWidth
			=> DefaultWidth;

		public required Settings LiveSettings { get; set; }

		public int VirtualHeight
			=> DefaultHeight;

		public int VirtualWidth
			=> DefaultWidth;

		public int VsyncDenominator
			=> LiveSettings.FramerateScalar;

		public int VsyncNumerator
			=> 60;

		public int[] GetVideoBuffer()
		{
			if (_rep++ > LiveSettings.FramerateScalar) _rep = 0;
			if (_rep is not 0) return _buf;
			var noise = new byte[_buf.Length];
			_rng.NextBytes(noise);
			for (var i = 0; i < _buf.Length; i++)
			{
				var sampleF = noise[i] / 255.0f;
				sampleF = MathF.Pow(sampleF, MathF.Pow(10.0f, -LiveSettings.Bias)) * LiveSettings.Intensity;
				var sample = (byte) (sampleF * 255.0f);
				const int MASK = ~0xFFFFFF;
				_buf[i] = MASK | sample << 16 | sample << 8 | sample; // ARGB
			}
			return _buf;
		}
	}
}
