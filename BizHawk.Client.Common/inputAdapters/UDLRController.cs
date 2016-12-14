using System.Collections.Generic;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Filters input for things called Up and Down while considering the client's AllowUD_LR option. 
	/// This is a bit gross but it is unclear how to do it more nicely
	/// </summary>
	public class UD_LR_ControllerAdapter : IController
	{
		public ControllerDefinition Definition
		{
			get { return Source.Definition; }
		}

		public bool IsPressed(string button)
		{
			bool PriorityUD_LR = !Global.Config.AllowUD_LR && !Global.Config.ForbidUD_LR; // implied by neither of the others being set (left as non-enum for back-compatibility)

			if (Global.Config.AllowUD_LR)
			{
				return Source.IsPressed(button);
			}

			string prefix;

			//" C " is for N64 "P1 C Up" and the like, which should not be subject to mutexing

			//regarding the unpressing and UDLR logic...... don't think about it. don't question it. don't look at it.

			if (button.Contains("Down") && !button.Contains(" C "))
			{
				if (!Source.IsPressed(button))
				{
					Unpresses.Remove(button);
				}

				prefix = button.GetPrecedingString("Down");
				string other = prefix + "Up";
				if (Source.IsPressed(other))
				{
					if (Unpresses.Contains(button))
					{
						return false;
					}

					if (Global.Config.ForbidUD_LR)
					{
						return false;
					}

					Unpresses.Add(other);
				}
				else
				{
					Unpresses.Remove(button);
				}
			}

			if (button.Contains("Up") && !button.Contains(" C "))
			{
				if (!Source.IsPressed(button))
				{
					Unpresses.Remove(button);
				}

				prefix = button.GetPrecedingString("Up");
				string other = prefix + "Down";
				if (Source.IsPressed(other))
				{
					if (Unpresses.Contains(button))
					{
						return false;
					}

					if (Global.Config.ForbidUD_LR)
					{
						return false;
					}

					Unpresses.Add(other);
				}
				else
				{
					Unpresses.Remove(button);
				}
			}

			if (button.Contains("Right") && !button.Contains(" C "))
			{
				if (!Source.IsPressed(button))
				{
					Unpresses.Remove(button);
				}

				prefix = button.GetPrecedingString("Right");
				string other = prefix + "Left";
				if (Source.IsPressed(other))
				{
					if (Unpresses.Contains(button))
					{
						return false;
					}

					if (Global.Config.ForbidUD_LR)
					{
						return false;
					}

					Unpresses.Add(other);
				}
				else
				{
					Unpresses.Remove(button);
				}
			}

			if (button.Contains("Left") && !button.Contains(" C "))
			{
				if (!Source.IsPressed(button))
				{
					Unpresses.Remove(button);
				}

				prefix = button.GetPrecedingString("Left");
				string other = prefix + "Right";
				if (Source.IsPressed(other))
				{
					if (Unpresses.Contains(button))
					{
						return false;
					}

					if (Global.Config.ForbidUD_LR)
					{
						return false;
					}

					Unpresses.Add(other);
				}
				else
				{
					Unpresses.Remove(button);
				}
			}

			return Source.IsPressed(button);
		}

		// The float format implies no U+D and no L+R no matter what, so just passthru
		public float GetFloat(string name)
		{
			return Source.GetFloat(name);
		}

		private readonly HashSet<string> Unpresses = new HashSet<string>();

		public IController Source { get; set; }
	}
}
