using System;

namespace BizHawk.Common
{
	public class MutableIntRange
	{
		private int _min;
		private int _max;

		/// <exception cref="ArgumentException">(from setter) <paramref name="value"/> > <see cref="Max"/></exception>
		public int Min
		{
			get => _min;
			set
			{
				if (_max < value) throw new ArgumentException();
				_min = value;
			}
		}

		/// <exception cref="ArgumentException">(from setter) <paramref name="value"/> &lt; <see cref="Min"/></exception>
		public int Max
		{
			get => _max;
			set
			{
				if (value < _min) throw new ArgumentException();
				_max = value;
			}
		}

		public MutableIntRange(int min, int max)
		{
			_min = min;
			Max = max; // set property instead of field to validate and possibly throw an ArgumentException
		}

		public int Constrain(int i) => i < _min ? _min : i > _max ? _max : i;

		/// <returns>true if i is in the inclusive range <see cref="Min"/>..<see cref="Max"/>, false otherwise</returns>
		public bool Covers(int i) => _min <= i && i <= _max;

		public uint GetCount() => (uint) ((long) _max - _min + 1);

		/// <returns>true if i is in the exclusive range <see cref="Min"/>..<see cref="Max"/>, false otherwise</returns>
		/// <remarks>
		/// You probably want <see cref="Covers"/>
		/// </remarks>
		public bool StrictContains(int i) => _min < i && i < _max;
	}
}