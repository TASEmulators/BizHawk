using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public enum ShiftDirection
	{
		Right = 0,
		Left = 1
	}

	public class Helpers
	{
		public static int[,] MOVECyclesBW;
		public static int[,] MOVECyclesL;

		static Helpers()
		{
			MOVECyclesBW = new int[12,9] {
				{ 4, 4, 8, 8, 8, 12, 14, 12, 16 },
				{ 4, 4, 8, 8, 8, 12, 14, 12, 16 },
				{ 8, 8, 12, 12, 12, 16, 18, 16, 20 },
				{ 8, 8, 12, 12, 12, 16, 18, 16, 20 },
				{ 10, 10, 14, 14, 14, 18, 20, 18, 22 },
				{ 12, 12, 16, 16, 16, 20, 22, 20, 24 },
				{ 14, 14, 18, 18, 18, 22, 24, 22, 26 },
				{ 12, 12, 16, 16, 16, 20, 22, 20, 24 },
				{ 16, 16, 20, 20, 20, 24, 26, 24, 28 },
				{ 12, 12, 16, 16, 16, 20, 22, 20, 24 },
				{ 14, 14, 18, 18, 18, 22, 24, 22, 26 },
				{ 8, 8, 12, 12, 12, 16, 18, 16, 20 }
			};

			MOVECyclesL = new int[12,9] {
				{ 4, 4, 12, 12, 12, 16, 18, 16, 20 },
				{ 4, 4, 12, 12, 12, 16, 18, 16, 20 },
				{ 12, 12, 20, 20, 20, 24, 26, 24, 28 },
				{ 12, 12, 20, 20, 20, 24, 26, 24, 28 },
				{ 14, 14, 22, 22, 22, 26, 28, 26, 30 },
				{ 16, 16, 24, 24, 24, 28, 30, 28, 32 },
				{ 18, 18, 26, 26, 26, 30, 32, 30, 34 },
				{ 16, 16, 24, 24, 24, 28, 30, 28, 32 },
				{ 20, 20, 28, 28, 28, 32, 34, 32, 36 },
				{ 16, 16, 24, 24, 24, 28, 30, 28, 32 },
				{ 18, 18, 26, 26, 26, 30, 32, 30, 34 },
				{ 12, 12, 20, 20, 20, 24, 26, 24, 28 }
			};
		}

		#region Inject
		public static void Inject(ref int register, byte value)
		{
			register = (register & -0x100) | value;
		}

		public static void Inject(ref int register, sbyte value)
		{
			register = (register & -0x100) | (byte)value;
		}

		public static void Inject(ref int register, ushort value)
		{
			register = (register & -0x10000) | value;
		}

		public static void Inject(ref int register, short value)
		{
			register = (register & -0x10000) | (ushort)value;
		}
		#endregion Inject

		public static void Swap(ref int a, ref int b)
		{
			int c = a;
			a = b;
			b = c;
		}

		public static int EACalcTimeBW(int mode, int register)
		{
			switch (mode)
			{
				case 0: return 0;
				case 1: return 0;
				case 2: return 4;
				case 3: return 4;
				case 4: return 6;
				case 5: return 8;
				case 6: return 10;
				case 7:
					switch (register)
					{
						case 0: return 8;
						case 1: return 12;
						case 2: return 8;
						case 3: return 10;
						case 4: return 4;
					}
					break;
			}
			throw new ArgumentException();
		}

		public static int EACalcTimeL(int mode, int register)
		{
			switch (mode)
			{
				case 0: return 0;
				case 1: return 0;
				case 2: return 8;
				case 3: return 8;
				case 4: return 10;
				case 5: return 12;
				case 6: return 14;
				case 7:
					switch (register)
					{
						case 0: return 12;
						case 1: return 16;
						case 2: return 12;
						case 3: return 14;
						case 4: return 8;
					}
					break;
			}
			throw new ArgumentException();
		}
	}
}
