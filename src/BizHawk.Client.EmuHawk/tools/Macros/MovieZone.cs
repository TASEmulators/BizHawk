using System.Linq;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class MovieZone
	{
		private readonly IEmulator _emulator;
		private readonly ToolManager _tools;
		private readonly IMovieSession _movieSession;
		private readonly string[] _log;
		private readonly IMovieController _targetController;
		private string _inputKey;
		private IMovieController _controller;

		public MovieZone(IEmulator emulator, ToolManager tools, IMovieSession movieSession, int start, int length, string key = "")
			: this(emulator, tools, movieSession)
		{
			if (key == "")
			{
				key = Bk2LogEntryGenerator.GenerateLogKey(movieSession.MovieController.Definition);
			}

			key = key.Replace("#", "");
			key = key.Substring(startIndex: 0, length: key.Length - 1); // drop last char

			_inputKey = key;
			Length = length;
			_log = new string[length];

			// Get a IController that only contains buttons in key.
			InitController(_inputKey);

			string movieKey = Bk2LogEntryGenerator.GenerateLogKey(_controller.Definition).Replace("#", "");
			movieKey = movieKey.Substring(startIndex: 0, length: movieKey.Length - 1); // drop last char
			if (key == movieKey)
			{
				for (int i = 0; i < length; i++)
				{
					_log[i] = movieSession.Movie.GetInputLogEntry(i + start);
				}
			}
			else
			{
				for (int i = 0; i < length; i++)
				{
					_controller.SetFrom(movieSession.Movie.GetInputState(i + start));
					_log[i] = Bk2LogEntryGenerator.GenerateLogEntry(_controller);
				}
			}
		}

		private MovieZone(IEmulator emulator, ToolManager tools, IMovieSession movieSession)
		{
			_emulator = emulator;
			_tools = tools;
			_movieSession = movieSession;

			_targetController = movieSession.GenerateMovieController();
			_targetController.SetFrom(_targetController); // Reference and create all buttons
		}

		private void InitController(string key)
		{
			string[] keys = key.Split('|');
			ControllerDefinition d = new(_emulator.ControllerDefinition.Name);
			foreach (var k in keys)
			{
				if (_emulator.ControllerDefinition.BoolButtons.Contains(k))
				{
					d.BoolButtons.Add(k);
				}
				else
				{
					d.Axes.Add(k, _emulator.ControllerDefinition.Axes[k]);
				}
			}

			_controller = _movieSession.GenerateMovieController(d.MakeImmutable());
		}

		public string Name { get; set; }
		public int Start { get; set; }
		public int Length { get; set; }

		public bool Replace { get; set; } = true;
		public bool Overlay { get; set; }

		public string InputKey
		{
			get => _inputKey;
			set { _inputKey = value; ReSetLog(); }
		}

		private void ReSetLog()
		{
			// Get a IController that only contains buttons in key.
			string[] keys = _inputKey.Split('|');
			ControllerDefinition d = new(_emulator.ControllerDefinition.Name);
			foreach (var key in keys)
			{
				if (_emulator.ControllerDefinition.BoolButtons.Contains(key))
				{
					d.BoolButtons.Add(key);
				}
				else
				{
					d.Axes.Add(key, _emulator.ControllerDefinition.Axes[key]);
				}
			}

			var newController = _movieSession.GenerateMovieController(d.MakeImmutable());

			// Reset all buttons in targetController (it may still have buttons that aren't being set here set true)
			_targetController.SetFromMnemonic(Bk2LogEntryGenerator.EmptyEntry(_targetController));
			for (int i = 0; i < Length; i++)
			{
				_controller.SetFromMnemonic(_log[i]);
				LatchFromSourceButtons(_targetController, _controller);
				newController.SetFrom(_targetController);
				_log[i] = Bk2LogEntryGenerator.GenerateLogEntry(newController);
			}

			_controller = newController;
		}

		public void PlaceZone(IMovie movie, Config config)
		{
			if (movie is ITasMovie tasMovie)
			{
				tasMovie.ChangeLog.BeginNewBatch($"Place Macro at {Start}");
			}

			if (Start > movie.InputLogLength)
			{
				// Cannot place a frame here. Find a nice way around this.
				return;
			}

			// Can't be done with a regular movie.
			if (!Replace && movie is ITasMovie tasMovie2)
			{
				tasMovie2.InsertEmptyFrame(Start, Length);
			}

			if (Overlay)
			{
				for (int i = 0; i < Length; i++)
				{ // Overlay the frames.
					_controller.SetFromMnemonic(_log[i]);
					LatchFromSourceButtons(_targetController, _controller);
					ORLatchFromSource(_targetController, movie.GetInputState(i + Start));
					movie.PokeFrame(i + Start, _targetController);
				}
			}
			else
			{
				// Copy over the frame.
				for (int i = 0; i < Length; i++)
				{
					_controller.SetFromMnemonic(_log[i]);
					LatchFromSourceButtons(_targetController, _controller);
					movie.PokeFrame(i + Start, _targetController);
				}
			}

			if (movie is ITasMovie tasMovie3) // Assume TAStudio is open?
			{
				tasMovie3.ChangeLog.EndBatch();
				if (_emulator.Frame > Start)
				{
					// TODO: Go to start of macro? Ask TAStudio to do that?

					// TasMovie.InvalidateAfter(Start) [this is private]
					// Load last state, Emulate to Start

					// Or do this, if TAStudio has to be open.
					if (_tools.IsLoaded<TAStudio>())
					{
						_tools.TAStudio.GoToFrame(Start);
					}

					_tools.UpdateToolsBefore();
					_tools.UpdateToolsAfter();
				}
				else if (_tools.IsLoaded<TAStudio>())
				{
					_tools.UpdateValues<TAStudio>();
				}
			}

			if (movie.InputLogLength >= _emulator.Frame)
			{
				movie.SwitchToPlay();
				config.Movies.MovieEndAction = MovieEndAction.Record; // TODO: this is a bad place to do this, and introduces a config dependency
			}
		}

		public void Save(string fileName)
		{
			// Save the controller definition/LogKey
			// Save the controller name and player count. (Only for the user.)
			// Save whether or not the macro should use overlay input, and/or replace
			string[] header = new string[4];
			header[0] = InputKey;
			header[1] = _emulator.ControllerDefinition.Name;
			header[2] = _emulator.ControllerDefinition.PlayerCount.ToString();
			header[3] = $"{Overlay},{Replace}";

			File.WriteAllLines(fileName, header);
			File.AppendAllLines(fileName, _log);
		}

		public MovieZone(string fileName, IDialogController dialogController, IEmulator emulator, IMovieSession movieSession, ToolManager tools)
			: this(emulator, tools, movieSession)
		{
			if (!File.Exists(fileName))
			{
				return;
			}

			string[] readText = File.ReadAllLines(fileName);

			// If the LogKey contains buttons/controls not accepted by the emulator,
			//	tell the user and display the macro's controller name and player count
			_inputKey = readText[0];
			string key = Bk2LogEntryGenerator.GenerateLogKey(_movieSession.MovieController.Definition);
			key = key.Replace("#", "");
			key = key.Substring(startIndex: 0, length: key.Length - 1); // drop last char
			string[] emuKeys = key.Split('|');
			string[] macroKeys = _inputKey.Split('|');
			foreach (var macro in macroKeys)
			{
				if (!emuKeys.Contains(macro))
				{
					dialogController.ShowMessageBox($"The selected macro is not compatible with the current emulator core.\nMacro controller: {readText[1]}\nMacro player count: {readText[2]}", "Error");
					return;
				}
			}

			// Settings
			string[] settings = readText[3].Split(',');
			Overlay = Convert.ToBoolean(settings[0]);
			Replace = Convert.ToBoolean(settings[1]);

			_log = new string[readText.Length - 4];
			readText.ToList().CopyTo(4, _log, 0, _log.Length);
			Length = _log.Length;
			Start = 0;

			Name = Path.GetFileNameWithoutExtension(fileName);

			// Get a IController that only contains buttons in key.
			InitController(_inputKey);
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
			foreach (string button in latching.Definition.BoolButtons)
			{
				latching.SetBool(button, latching.IsPressed(button) | source.IsPressed(button));
			}

			foreach (string name in latching.Definition.Axes.Keys)
			{
				var axisValue = source.AxisValue(name);
				if (axisValue == source.Definition.Axes[name].Neutral)
				{
					latching.SetAxis(name, axisValue);
				}
			}
		}
	}
}
