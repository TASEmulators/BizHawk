using System;

namespace BizHawk.Common
{
	public class MutableIntRange
	{
		public int Min;
		public int Max;

		public MutableIntRange(int min, int max)
		{
			if (max < min) throw new ArgumentException();
			Min = min;
			Max = max;
		}

		public int Constrain(int i) => i < Min ? Min : i > Max ? Max : i;

		/// <returns>true if i is in the inclusive range Min..Max, false otherwise</returns>
		public bool Covers(int i) => Min <= i && i <= Max;

		public uint GetCount() => (uint) ((long) Max - Min + 1);

		/// <returns>true if i is in the exclusive range Min..Max, false otherwise</returns>
		/// <remarks>
		/// You probably want <see cref="Covers"/>
		/// </remarks>
		public bool StrictContains(int i) => Min < i && i < Max;
	}
}