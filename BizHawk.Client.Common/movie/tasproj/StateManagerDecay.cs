/****************************************************************************************
	
	algo by r57shell & feos

	_zeros is the key to GREENZONE DECAY PATTERN

	in a 16 element example, we evaluate these bitwise numbers to count zeros on the right.
	first element is always assumed to be 16, which has all 4 bits set to 0.
	each right zero means that we lower the priority of a state that goes at that index.
	priority changes depending on current frame and amount of states.
	states with biggest priority get erased first.
	with a 4-bit battern and no initial gap between states, total frame coverage is about 5 times state count.

	_zeros values are essentialy the values of rshiftby here:
	bitwise view     frame    rshiftby priority
	  00010000         0         4         1
	  00000001         1         0        15
	  00000010         2         1         7
	  00000011         3         0        13
	  00000100         4         2         3
	  00000101         5         0        11
	  00000110         6         1         5
	  00000111         7         0         9
	  00001000         8         3         1
	  00001001         9         0         7
	  00001010        10         1         3
	  00001011        11         0         5
	  00001100        12         2         1
	  00001101        13         0         3
	  00001110        14         1         1
	  00001111        15         0         1

*****************************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	internal class StateManagerDecay
	{
		private TasStateManager _tsm;
		private List<int> _zeros; // amount of least significant zeros in bitwise view (also max step)
		private int _bits; // size of _zeros = 2 raised to the power of _bits
		private int _mask; // for remainder calculation using bitwise instead of division
		private int _base; // repeat count (like fceux's capacity). only used by aligned spread
		private int _capacity; // total amount of savestates

		private enum DecayDirection
		{
			Forward,    // from 0 to current frame
			Backward,   // from last frame to current frame
			Bothway     // both in one call
		}

		private enum PriorityFormula
		{
			Spread,
			AlignedSpread
		}

		public StateManagerDecay(TasStateManager tsm)
		{
			_tsm = tsm;
			_zeros = new List<int>();
		}

		public void Trigger(int decayStates)
		{
			if (_tsm.StateCount <= 1 || decayStates <= 1)
				return;

			DecayDirection direction = DecayDirection.Forward;
			PriorityFormula formula = PriorityFormula.AlignedSpread;

			int baseStateIndex = _tsm.GetStateIndexByFrame(Global.Emulator.Frame);
			int baseStateFrame = _tsm.GetStateFrameByIndex(baseStateIndex);

			// priority is key, frame is value
			SortedList<int, int> ForwardFamePriorities = new SortedList<int, int>();
			SortedList<int, int> BackwardFamePriorities = new SortedList<int, int>();

			// add default values to compare to
			ForwardFamePriorities.Add(0, 0);
			BackwardFamePriorities.Add(0, 0);

			if (direction == DecayDirection.Forward || direction == DecayDirection.Bothway)
			{
				for (int currentStateIndex = 1; currentStateIndex < baseStateIndex; currentStateIndex++)
				{
					int currentFrame = _tsm.GetStateFrameByIndex(currentStateIndex);
					int zeroCount = _zeros[currentFrame & _mask];
					int priority = ((baseStateFrame - currentFrame) >> zeroCount);

					if (formula == PriorityFormula.AlignedSpread)
					{
						priority -= ((_base * ((1 << zeroCount) * 2 - 1)) >> zeroCount);
					}

					if (priority > ForwardFamePriorities.Last().Key)
					{
						ForwardFamePriorities.Add(priority, currentFrame);
					}
				}
			}

			if (direction == DecayDirection.Backward || direction == DecayDirection.Bothway)
			{
				for (int currentStateIndex = _tsm.StateCount - 1; currentStateIndex > baseStateIndex; currentStateIndex--)
				{
					int currentFrame = _tsm.GetStateFrameByIndex(currentStateIndex);
					int zeroCount = _zeros[currentFrame & _mask];
					int priority = ((currentFrame - baseStateFrame) >> zeroCount);

					if (formula == PriorityFormula.AlignedSpread)
					{
						priority -= ((_base * ((1 << zeroCount) * 2 - 1)) >> zeroCount);
					}

					if (priority > ForwardFamePriorities.Last().Key)
					{
						BackwardFamePriorities.Add(priority, currentFrame);
					}
				}
			}

			for (; decayStates > 0;)
			{
				if (ForwardFamePriorities.Count > 1)
				{
					_tsm.RemoveState(ForwardFamePriorities.Last().Value);
					decayStates--;
				}
				else if (BackwardFamePriorities.Count > 1)
				{
					_tsm.RemoveState(BackwardFamePriorities.Last().Value);
					decayStates--;
				}
				else
				{
					// todo: this should never happen!!!
					_tsm.RemoveState(_tsm.GetStateFrameByIndex(1));
					decayStates--;
				}
			}
		}

		public void UpdateSettings(int capacity, int bits)
		{
			_bits = bits;
			_capacity = capacity;
			_mask = (1 << _bits) - 1;
			_base = (_capacity + _bits / 2) / (_bits + 1);
			_zeros.Add(_bits);

			for (int i = 1; i < (1 << _bits); i++)
			{
				_zeros.Add(0);

				for (int j = i; j > 0; j >>= 1)
				{
					if ((j & 1) > 0)
						break;

					_zeros[i]++;
				}
			}
		}
	}
}
