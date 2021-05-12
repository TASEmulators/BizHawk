using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	class RandomAdapter : IInputAdapter
	{
		private static string[] possibleButtons;

		private static string randomInput = null;

		private static System.Timers.Timer timer = null;

		public RandomAdapter(int interval, bool enabled, bool fuckupCamera, bool fuckupMovement)
		{
			// this adapter get init every time you change a config or a platform or the emulator itself is loaded
			// therefore we will use "some kind of" static methods 
			if (timer != null)
			{
				timer.Stop();
			}

			if (enabled)
			{
				if (timer == null)
				{
					timer = new System.Timers.Timer(interval);
					timer.AutoReset = true;
					timer.Elapsed += TimerElapsed;
				}
				else
				{
					timer.Interval = interval;
				}

				possibleButtons = GenerateInputArray(fuckupCamera, fuckupMovement);

				timer.Start();
			}
			else
			{
				randomInput = null;
				timer = null;
			}
		}

		private static void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Console.WriteLine("The next Input will be random");

			Random random = new Random();

			var index = random.Next(0, possibleButtons.Length);

			randomInput = possibleButtons[index];
		}

		public ControllerDefinition Definition => Curr.Definition;

		public bool IsPressed(string button)
		{
			Console.WriteLine("Asking for Input {0}", button);

			if (randomInput != null && button == randomInput)
			{
				// todo lock
				Console.WriteLine("I sneaky pressed button {0}", button);
				randomInput = null;
				return true;
			}

			return Curr.IsPressed(button);
		}

		public int AxisValue(string name) => Curr.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Curr.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Curr.SetHapticChannelStrength(name, strength);

		public IController Source { get; set; }

		private IController Curr => Source ?? NullController.Instance;

		private string[] GenerateInputArray(bool fuckupCamera, bool fuckupMovement)
		{
			List<string> basics = new List<string>
			{
				"P1 Z",
				"P1 A",
				"P1 B"
			};

			if (fuckupCamera)
			{
				basics.AddRange(new string[]
				{
					"P1 R",
					"P1 L",
					"P1 C Left",
					"P1 C Up",
					"P1 C Down",
					"P1 C Right",
				});
			}

			if (fuckupMovement)
			{
				basics.AddRange(new string[]
				{
					"P1 A Left",
					"P1 A Up",
					"P1 A Down",
					"P1 A Right",
				});
			}

			return basics.ToArray();
		}
	}
}
