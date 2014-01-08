using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class NesMnemonicGenerator : IMnemonicPorts
	{
		public NesMnemonicGenerator(bool fds =  false, bool isFourscore = false)
		{
			_isFds = fds;
			_isFourscore = isFourscore;
		}

		public bool FourScoreEnabled
		{
			get { return _isFourscore; } 
			set { _isFourscore = value; } 
		}

		public bool IsFDS
		{
			get { return _isFds; }
			set { _isFds = value; }
		}

		#region IMnemonicPorts Implementation

		public int Count
		{
			get { return _isFourscore ? 4 : 2; }
		}

		// This is probably not necessary, but let's see how things go
		public IEnumerable<IMnemonicGenerator> AvailableGenerators
		{
			get
			{
				yield return ConsoleControls;

				for (int i = 0; i < Count; i++)
				{
					yield return _controllerPorts[i];
				}
			}
		}

		public IMnemonicGenerator ConsoleControls
		{
			get { return _isFds ? _fdsConsoleControls : _nesConsoleControls; }
		}

		public IMnemonicGenerator this[int portNum]
		{
			get
			{
				if (portNum < Count)
				{
					return _controllerPorts[portNum];
				}
				else
				{
					throw new ArgumentOutOfRangeException("portNum");
				}
			}

			set
			{
				if (portNum < Count)
				{
					// Eventually this will support zappers and FDS controllers, Arkanoid paddle, etc
					if (value is BooleanControllerMnemonicGenerator)
					{
						_controllerPorts[portNum] = value;
					}
					else
					{
						throw new InvalidOperationException("Invalid Mnemonic Generator for the given port");
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException("portNum");
				}
			}
		}

		#endregion

		#region Privates

		private bool _isFds;
		private bool _isFourscore;

		private static readonly Dictionary<string, char> _basicController = new Dictionary<string, char>
		{
			{ "Up", 'U' },
			{ "Down", 'D' },
			{ "Left", 'L' },
			{ "Right", 'R' },
			{ "Select", 's' },
			{ "Start", 'S' },
			{ "B", 'B' },
			{ "A", 'A' }
		};

		private readonly BooleanControllerMnemonicGenerator _nesConsoleControls = new BooleanControllerMnemonicGenerator(
			"Console",
			new Dictionary<string, char>
			{
				{ "Reset", 'r' },
				{ "Power", 'P' },
			}
		)
			{
				Source = Global.MovieOutputHardpoint,
				ControllerPrefix = String.Empty
			};

		private readonly BooleanControllerMnemonicGenerator _fdsConsoleControls = new BooleanControllerMnemonicGenerator(
			"Console",
			new Dictionary<string, char>
			{
				{ "Reset", 'r' },
				{ "Power", 'P' },
				{ "FDS Eject", 'E' },
				{ "FDS Insert 0", '0' },
				{ "FDS Insert 1", '1' },
			}
		)
		{
			Source = Global.MovieOutputHardpoint,
			ControllerPrefix = String.Empty
		};

		private readonly List<IMnemonicGenerator> _controllerPorts =
			new List<IMnemonicGenerator>
			{
				new BooleanControllerMnemonicGenerator("Player 1", _basicController)
				{
					Source = Global.MovieOutputHardpoint,
					ControllerPrefix = "P1"
				},
				new BooleanControllerMnemonicGenerator("Player 2", _basicController)
				{
					Source = Global.MovieOutputHardpoint,
					ControllerPrefix = "P2"
				},
				new BooleanControllerMnemonicGenerator("Player 3", _basicController)
				{
					Source = Global.MovieOutputHardpoint,
					ControllerPrefix = "P3"
				},
				new BooleanControllerMnemonicGenerator("Player 4", _basicController)
				{
					Source = Global.MovieOutputHardpoint,
					ControllerPrefix = "P4"
				}
			};

		#endregion
	}
}
