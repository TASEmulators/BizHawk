using System.Linq;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieZone
	{
		private string[] _log;
		private string _inputKey;

		private string _sysId;

		/// <summary>
		/// The macro's controller, which might not have the same definition as the movie controller.
		/// </summary>
		private IMovieController _controller;

		/// <summary>
		/// The controller definition for the movie the macro is being applied to.
		/// </summary>
		private readonly ControllerDefinition _movieDefinition;

		public MovieZone(IMovie movie, int start, int length)
			: this(movie)
		{
			_inputKey = CleanInputKey(Bk2LogEntryGenerator.GenerateLogKey(_movieDefinition));
			_log = new string[length];

			// Get a IController that only contains buttons in key.
			InitController();

			string movieKey = CleanInputKey(Bk2LogEntryGenerator.GenerateLogKey(_controller.Definition));
			if (_inputKey == movieKey)
			{
				for (int i = 0; i < length; i++)
				{
					_log[i] = movie.GetInputLogEntry(i + start);
				}
			}
			else
			{
				for (int i = 0; i < length; i++)
				{
					_controller.SetFrom(movie.GetInputState(i + start));
					_log[i] = Bk2LogEntryGenerator.GenerateLogEntry(_controller);
				}
			}
		}

		private MovieZone(IMovie movie)
		{
			_movieDefinition = movie.Session.MovieController.Definition;
			_sysId = movie.SystemID;
		}

		private void InitController()
		{
			string[] keys = _inputKey.Split('|');
			ControllerDefinition d = new(_movieDefinition.Name);
			foreach (var k in keys)
			{
				if (_movieDefinition.BoolButtons.Contains(k))
				{
					d.BoolButtons.Add(k);
				}
				else
				{
					d.Axes.Add(k, _movieDefinition.Axes[k]);
				}
			}

			_controller = new Bk2Controller(d.MakeImmutable());
			d.BuildMnemonicsCache(_sysId);
		}

		public string Name { get; set; }
		public int Length => _log.Length;

		public bool Replace { get; set; } = true;
		public bool Overlay { get; set; }

		public string InputKey
		{
			get => _inputKey;
			set { _inputKey = value; ReSetLog(); }
		}

		private void ReSetLog()
		{
			IMovieController oldController = _controller;
			InitController();

			IMovieController temp = new Bk2Controller(_movieDefinition);
			for (int i = 0; i < Length; i++)
			{
				oldController.SetFromMnemonic(_log[i]);
				LatchFromSourceButtons(temp, oldController);
				_controller.SetFrom(temp);
				_log[i] = Bk2LogEntryGenerator.GenerateLogEntry(_controller);
			}
		}

		public void PlaceZone(IMovie movie, int start)
		{
			if (start > movie.InputLogLength)
			{
				// Cannot place a frame here. Find a nice way around this.
				return;
			}

			if (movie is ITasMovie tasMovie)
			{
				tasMovie.ChangeLog.BeginNewBatch($"Place Macro at {start}");
				tasMovie.SingleInvalidation(() => PlaceMacroInternal(movie, start));
				tasMovie.ChangeLog.EndBatch();
			}
			else
			{
				PlaceMacroInternal(movie, start);
			}
		}

		private void PlaceMacroInternal(IMovie movie, int start)
		{
			if (!Replace)
			{
				// Can't be done with a regular movie.
				(movie as ITasMovie)?.InsertEmptyFrame(start, Length);
			}

			if (Overlay)
			{
				// Overlay the frames.
				for (int i = 0; i < Length; i++)
				{
					int frame = i + start;
					_controller.SetFromMnemonic(_log[i]);
					IMovieController frameState = movie.GetInputState(frame) ?? new Bk2Controller(_movieDefinition);
					ORLatchFromSource(frameState, _controller);
					movie.PokeFrame(frame, frameState);
				}
			}
			else
			{
				// Copy over the frame.
				for (int i = 0; i < Length; i++)
				{
					int frame = i + start;
					_controller.SetFromMnemonic(_log[i]);
					IMovieController frameState = movie.GetInputState(frame) ?? new Bk2Controller(_movieDefinition);
					LatchFromSourceButtons(frameState, _controller);
					movie.PokeFrame(frame, frameState);
				}
			}
		}

		public FileWriteResult Save(string fileName)
		{
			// Save the controller definition/LogKey
			// Save the controller name and player count. (Only for the user.)
			// Save whether or not the macro should use overlay input, and/or replace

			return FileWriter.Write(fileName, (fs) =>
			{
				using var writer = new StreamWriter(fs);
				writer.WriteLine(InputKey);
				writer.WriteLine(_movieDefinition.Name);
				writer.WriteLine(_movieDefinition.PlayerCount.ToString());
				writer.WriteLine($"{Overlay},{Replace}");

				foreach (string line in _log)
				{
					writer.WriteLine(line);
				}
			});
		}

		public static MovieZone/*?*/ Load(string fileName, IDialogController dialogController, IMovie movie)
		{
			if (!File.Exists(fileName))
			{
				return null;
			}

			MovieZone macro = new(movie);
			string[] readText = File.ReadAllLines(fileName);

			// If the LogKey contains buttons/controls not accepted by the emulator,
			// tell the user and display the macro's controller name and player count
			macro._inputKey = readText[0];
			string key = CleanInputKey(Bk2LogEntryGenerator.GenerateLogKey(macro._movieDefinition));
			string[] emuKeys = key.Split('|');
			string[] macroKeys = macro._inputKey.Split('|');
			foreach (var macroKey in macroKeys)
			{
				if (!emuKeys.Contains(macroKey))
				{
					dialogController.ShowMessageBox($"The selected macro is not compatible with the current emulator core.\nMacro controller: {readText[1]}\nMacro player count: {readText[2]}", "Error");
					return null;
				}
			}

			// Settings
			string[] settings = readText[3].Split(',');
			macro.Overlay = Convert.ToBoolean(settings[0]);
			macro.Replace = Convert.ToBoolean(settings[1]);

			macro._log = new string[readText.Length - 4];
			readText.ToList().CopyTo(4, macro._log, 0, macro._log.Length);

			macro.Name = Path.GetFileNameWithoutExtension(fileName);

			// Get a IController that only contains buttons in key.
			macro.InitController();

			return macro;
		}

		private static string CleanInputKey(string rawKey)
		{
			string key = rawKey.Replace("#", ""); // Movies separate players with #, but that character has no meaning for us.
			key = key.Substring(startIndex: 0, length: key.Length - 1); // drop last |, so we don't have an empty button when we split
			return key;
		}

		private void LatchFromSourceButtons(IMovieController latching, IController source)
		{
			foreach (string button in source.Definition.BoolButtons)
			{
				latching.SetBool(button, source.IsPressed(button));
			}

			foreach (string name in source.Definition.Axes.Keys)
			{
				latching.SetAxis(name, source.AxisValue(name));
			}
		}

		private void ORLatchFromSource(IMovieController latching, IController source)
		{
			foreach (string button in latching.Definition.BoolButtons.Union(source.Definition.BoolButtons))
			{
				latching.SetBool(button, latching.IsPressed(button) | source.IsPressed(button));
			}

			foreach (string name in latching.Definition.Axes.Keys.Union(source.Definition.Axes.Keys))
			{
				var axisValue = source.AxisValue(name);
				if (axisValue != source.Definition.Axes[name].Neutral)
				{
					latching.SetAxis(name, axisValue);
				}
			}
		}
	}
}
