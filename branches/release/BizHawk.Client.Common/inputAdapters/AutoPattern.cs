using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class AutoPatternBool
	{
		public bool SkipsLag = true;
		private bool[] _pattern;
		private int _index;

		/// <summary>
		/// Autohold.
		/// </summary>
		public AutoPatternBool()
		{
			SkipsLag = true;
			_index = 0;
			_pattern = new bool[] { true };
		}
		/// <summary>
		/// Simple on/off pattern.
		/// </summary>
		/// <param name="on"></param>
		/// <param name="off"></param>
		public AutoPatternBool(int on, int off, bool skip_lag = true, int offset = 0)
		{
			SkipsLag = skip_lag;
			_index = offset;
			_pattern = new bool[on + off];
			for (int i = 0; i < on; i++)
				_pattern[i] = true;
		}
		public AutoPatternBool(bool[] pattern, bool skip_lag = true, int offset = 0)
		{
			SkipsLag = skip_lag;
			_pattern = pattern;
			_index = offset;
		}

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		/// <returns></returns>
		public bool GetNextValue()
		{
			bool ret = _pattern[_index];
			_index++;
			_index = _index % _pattern.Length;

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		/// <returns></returns>
		public bool PeekNextValue()
		{ return _pattern[_index]; }
	}

	public class AutoPatternFloat
	{
		public bool SkipsLag = true;
		private float[] _pattern;
		private int _index;

		/// <summary>
		/// Defaults to 0.
		/// </summary>
		public AutoPatternFloat()
		{
			SkipsLag = true;
			_pattern = new float[] { 0f };
			_index = 0;
		}
		/// <summary>
		/// Sinple on/off pattern, using the given values as on/off.
		/// </summary>
		public AutoPatternFloat(float valueOn, int on, float valueOff, int off, bool skip_lag = true, int offset = 0)
		{
			SkipsLag = skip_lag;
			_index = offset;
			_pattern = new float[on + off];
			for (int i = 0; i < on; i++)
				_pattern[i] = valueOn;
			for (int i = on; i < _pattern.Length; i++)
				_pattern[i] = valueOff;
		}
		public AutoPatternFloat(float[] pattern, bool skip_lag = true, int offset = 0)
		{
			SkipsLag = skip_lag;
			_pattern = pattern;
			_index = offset;
		}

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		/// <returns></returns>
		public float GetNextValue()
		{
			float ret = _pattern[_index];
			_index++;
			_index = _index % _pattern.Length;

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		/// <returns></returns>
		public float PeekNextValue()
		{ return _pattern[_index]; }
	}
}
