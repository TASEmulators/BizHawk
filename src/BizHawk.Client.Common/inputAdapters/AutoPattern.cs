namespace BizHawk.Client.Common
{
	public class AutoPatternBool
	{
		public AutoPatternBool()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AutoPatternBool"/> class. 
		/// A simple on/off pattern.
		/// </summary>
		public AutoPatternBool(int on, int off, bool skipLag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skipLag;
			_index = offset;
			Pattern = new bool[on + off];
			Loop = loop;
			for (int i = 0; i < on; i++)
			{
				Pattern[i] = true;
			}
		}

		public AutoPatternBool(bool[] pattern, bool skipLag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skipLag;
			Pattern = pattern;
			_index = offset;
			Loop = loop;
		}

		private int _index;

		public bool SkipsLag { get; } = true;
		public bool[] Pattern { get; } = { true };
		public int Loop { get; }

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		public bool GetNextValue(bool isLag = false)
		{
			bool ret = Pattern[_index];
			if (!isLag || !SkipsLag)
			{
				_index++;
				if (_index == Pattern.Length)
				{
					_index = Loop;
				}
			}

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		public bool PeekNextValue()
		{
			return Pattern[_index];
		}

		public void Reset()
		{
			_index = 0;
		}
	}

	public class AutoPatternFloat
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AutoPatternFloat"/> class. 
		/// Defaults to 0.
		/// </summary>
		public AutoPatternFloat()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AutoPatternFloat"/> class. 
		/// Simple on/off pattern, using the given values as on/off.
		/// </summary>
		public AutoPatternFloat(float valueOn, int on, float valueOff, int off, bool skipLag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skipLag;
			_index = offset;
			Loop = loop;
			Pattern = new float[on + off];
			for (int i = 0; i < on; i++)
			{
				Pattern[i] = valueOn;
			}

			for (int i = on; i < Pattern.Length; i++)
			{
				Pattern[i] = valueOff;
			}
		}

		public AutoPatternFloat(float[] pattern, bool skipLag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skipLag;
			Pattern = pattern;
			_index = offset;
			Loop = loop;
		}

		private int _index;

		public bool SkipsLag { get; } = true;
		public float[] Pattern { get; } = { 0f };
		public int Loop { get; }

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		public float GetNextValue(bool isLag = false)
		{
			float ret = Pattern[_index];
			if (!isLag || !SkipsLag)
			{
				_index++;
				if (_index == Pattern.Length)
				{
					_index = Loop;
				}
			}

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		public float PeekNextValue()
		{
			return Pattern[_index];
		}

		public void Reset()
		{
			_index = 0;
		}
	}
}
