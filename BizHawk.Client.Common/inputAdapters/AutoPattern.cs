namespace BizHawk.Client.Common
{
	public class AutoPatternBool
	{
		public readonly bool SkipsLag = true;
		public readonly bool[] Pattern;
		public readonly int Loop = 0;
		private int _index = 0;

		/// <summary>
		/// Autohold.
		/// </summary>
		public AutoPatternBool()
		{
			Pattern = new bool[] { true };
		}
		/// <summary>
		/// Simple on/off pattern.
		/// </summary>
		/// <param name="on"></param>
		/// <param name="off"></param>
		public AutoPatternBool(int on, int off, bool skip_lag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skip_lag;
			_index = offset;
			Pattern = new bool[on + off];
			Loop = loop;
			for (int i = 0; i < on; i++)
				Pattern[i] = true;
		}
		public AutoPatternBool(bool[] pattern, bool skip_lag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skip_lag;
			Pattern = pattern;
			_index = offset;
			Loop = loop;
		}

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		/// <returns></returns>
		public bool GetNextValue(bool isLag = false)
		{
			bool ret = Pattern[_index];
			if (!isLag || !SkipsLag)
			{
				_index++;
				if (_index == Pattern.Length)
					_index = Loop;
			}

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		/// <returns></returns>
		public bool PeekNextValue()
		{ return Pattern[_index]; }

		public void Reset()
		{ _index = 0; }
	}

	public class AutoPatternFloat
	{
		public readonly bool SkipsLag = true;
		public readonly float[] Pattern;
		public readonly int Loop = 0;
		private int _index;

		/// <summary>
		/// Defaults to 0.
		/// </summary>
		public AutoPatternFloat()
		{
			Pattern = new float[] { 0f };
		}
		/// <summary>
		/// Sinple on/off pattern, using the given values as on/off.
		/// </summary>
		public AutoPatternFloat(float valueOn, int on, float valueOff, int off, bool skip_lag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skip_lag;
			_index = offset;
			Loop = loop;
			Pattern = new float[on + off];
			for (int i = 0; i < on; i++)
				Pattern[i] = valueOn;
			for (int i = on; i < Pattern.Length; i++)
				Pattern[i] = valueOff;
		}
		public AutoPatternFloat(float[] pattern, bool skip_lag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skip_lag;
			Pattern = pattern;
			_index = offset;
			Loop = loop;
		}

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		/// <returns></returns>
		public float GetNextValue(bool isLag = false)
		{
			float ret = Pattern[_index];
			if (!isLag || !SkipsLag)
			{
				_index++;
				if (_index == Pattern.Length)
					_index = Loop;
			}

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		/// <returns></returns>
		public float PeekNextValue()
		{ return Pattern[_index]; }

		public void Reset()
		{ _index = 0; }
	}
}
