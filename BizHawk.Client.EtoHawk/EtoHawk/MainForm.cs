using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto;
using Eto.Drawing;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

namespace BizHawk.Client.EtoHawk
{
    public partial class MainForm : Form
    {
        private Thread _worker;
        private bool _running;
        private readonly Throttle _throttle;
        private bool _unthrottled;
        private bool _runloopFrameProgress;
        private long _frameAdvanceTimestamp;
        private long _frameRewindTimestamp;
        private int _runloopFps;
        private int _runloopLastFps;
        private bool _runloopFrameadvance;
        private long _runloopSecond;
        private bool _runloopLastFf;
        private bool _inResizeLoop;
        private bool _suspended; //True if a modal dialog appears
        private readonly InputCoalescer HotkeyCoalescer = new InputCoalescer();

        public MainForm()
        {
            InitBizHawk();
            _throttle = new Throttle();
            InitializeWindow();
            
            _running = true;
            _worker = new Thread(ProgramRunLoop);
            _worker.Start();
        }

        private void InitBizHawk()
        {
            GlobalWin.MainForm = this;
            Global.Rewinder = new Rewinder
            {
                //MessageCallback = GlobalWin.OSD.AddMessage
            };

            Global.ControllerInputCoalescer = new ControllerInputCoalescer();
            Global.FirmwareManager = new FirmwareManager();
            Global.MovieSession = new MovieSession
            {
                Movie = MovieService.DefaultInstance,
                MovieControllerAdapter = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter,
                //MessageCallback = GlobalWin.OSD.AddMessage,
                //AskYesNoCallback = StateErrorAskUser,
                PauseCallback = PauseEmulator,
                //ModeChangedCallback = SetMainformMovieInfo
            };

            //Icon = Properties.Resources.logo;
            Global.Game = GameInfo.NullInstance;

            string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.ini");
            Global.Config = ConfigService.Load<Config>(iniPath);
            Global.Config.ResolveDefaults();
            
            Input.Initialize();
            InitControls();

            var comm = CreateCoreComm();
            CoreFileProvider.SyncCoreCommInputSignals(comm);
            Global.Emulator = new NullEmulator(comm, Global.Config.GetCoreSettings<NullEmulator>());
            Global.ActiveController = new Controller(NullEmulator.NullController);
            Global.AutoFireController = Global.AutofireNullControls;
            Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();

            Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.OpenAL; //Temporary, remove later
            try { GlobalWin.Sound = new Sound(IntPtr.Zero); }
            catch
            {
                string message = "Couldn't initialize sound device! Try changing the output method in Sound config.";
                if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.DirectSound)
                    message = "Couldn't initialize DirectSound! Things may go poorly for you. Try changing your sound driver to 44.1khz instead of 48khz in mmsys.cpl.";
                MessageBox.Show(message, "Initialization Error", MessageBoxButtons.OK, MessageBoxType.Error);

                Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.Dummy;
                GlobalWin.Sound = new Sound(IntPtr.Zero);
            }
            GlobalWin.Sound.StartSound();
            InputManager.RewireInputChain();
            /*GlobalWin.Tools = new ToolManager(this);*/
            RewireSound();
            GlobalWin.DisplayManager = new DisplayManager();
        }

        private static void InitControls()
        {
            var controls = new Controller(
                new ControllerDefinition
                {
                    Name = "Emulator Frontend Controls",
                    BoolButtons = Global.Config.HotkeyBindings.Select(x => x.DisplayName).ToList()
                });

            foreach (var b in Global.Config.HotkeyBindings)
            {
                controls.BindMulti(b.DisplayName, b.Bindings);
            }

            Global.ClientControls = controls;
            Global.AutofireNullControls = new AutofireController(NullEmulator.NullController, Global.Emulator);

        }

        private void RewireSound()
        {
            /*if (_dumpProxy != null)
            {
                // we're video dumping, so async mode only and use the DumpProxy.
                // note that the avi dumper has already rewired the emulator itself in this case.
                GlobalWin.Sound.SetAsyncInputPin(_dumpProxy);
            }
            else*/
            {
                Global.Emulator.EndAsyncSound();
                GlobalWin.Sound.SetSyncInputPin(Global.Emulator.SyncSoundProvider);
            }
        }
        public void ProgramRunLoop()
        {
            //CheckMessages();
            //LogConsole.PositionConsole();

            while (_running)
            {
                if (_suspended)
                {
                    Thread.Sleep(33);
                    continue;
                }
                Input.Instance.Update();

                // handle events and dispatch as a hotkey action, or a hotkey button, or an input button
                ProcessInput();
                Global.ClientControls.LatchFromPhysical(HotkeyCoalescer);

                Global.ActiveController.LatchFromPhysical(Global.ControllerInputCoalescer);

                /*Global.ActiveController.ApplyAxisConstraints(
                    (Global.Emulator is N64 && Global.Config.N64UseCircularAnalogConstraint) ? "Natural Circle" : null);*/

                Global.ActiveController.OR_FromLogical(Global.ClickyVirtualPadController);
                Global.AutoFireController.LatchFromPhysical(Global.ControllerInputCoalescer);

                if (Global.ClientControls["Autohold"])
                {
                    Global.StickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
                    Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.AutoFireController.PressedButtons);
                }
                else if (Global.ClientControls["Autofire"])
                {
                    Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
                }

                // autohold/autofire must not be affected by the following inputs
                Global.ActiveController.Overrides(Global.LuaAndAdaptor);

                if (Global.Config.DisplayInput) // Input display wants to update even while paused
                {
                    GlobalWin.DisplayManager.NeedsToPaint = true;
                }

                StepRunLoop_Core();
                StepRunLoop_Throttle();

                if (GlobalWin.DisplayManager.NeedsToPaint)
                {
                    Render();
                }

                //CheckMessages();

                Thread.Sleep(0);
            }

        }

        private void Render()
        {
            if (_running)
            {
                //Invalidate doesn't force the update, which may skip frames, but Update blocks and slows things down.
                //So invalidate wins for the moment, until we switch to OpenGL.
                Application.Instance.Invoke(new Action(() => _viewport.Invalidate()));
                GlobalWin.DisplayManager.NeedsToPaint = false;
            }
        }

        private void StepRunLoop_Throttle()
        {
            SyncThrottle();
            _throttle.signal_frameAdvance = _runloopFrameadvance;
            _throttle.signal_continuousframeAdvancing = _runloopFrameProgress;

            _throttle.Step(true, -1);
        }

        public void ProcessInput()
        {
            ControllerInputCoalescer conInput = Global.ControllerInputCoalescer as ControllerInputCoalescer;

            for (; ; )
            {

                // loop through all available events
                var ie = Input.Instance.DequeueEvent();
                if (ie == null) { break; }

                // useful debugging:
                // Console.WriteLine(ie);

                // TODO - wonder what happens if we pop up something interactive as a response to one of these hotkeys? may need to purge further processing

                // look for hotkey bindings for this key
                var triggers = Global.ClientControls.SearchBindings(ie.LogicalButton.ToString());
                /*if (triggers.Count == 0)
                {
                    // Maybe it is a system alt-key which hasnt been overridden
                    if (ie.EventType == Input.InputEventType.Press)
                    {
                        if (ie.LogicalButton.Alt && ie.LogicalButton.Button.Length == 1)
                        {
                            var c = ie.LogicalButton.Button.ToLower()[0];
                            if ((c >= 'a' && c <= 'z') || c == ' ')
                            {
                                SendAltKeyChar(c);
                            }
                        }
                        if (ie.LogicalButton.Alt && ie.LogicalButton.Button == "Space")
                        {
                            SendPlainAltKey(32);
                        }
                    }

                    // ordinarily, an alt release with nothing else would move focus to the menubar. but that is sort of useless, and hard to implement exactly right.
                }*/

                // zero 09-sep-2012 - all input is eligible for controller input. not sure why the above was done. 
                // maybe because it doesnt make sense to me to bind hotkeys and controller inputs to the same keystrokes

                // adelikat 02-dec-2012 - implemented options for how to handle controller vs hotkey conflicts.  This is primarily motivated by computer emulation and thus controller being nearly the entire keyboard
                bool handled;
                switch (Global.Config.Input_Hotkey_OverrideOptions)
                {
                    default:
                    case 0: // Both allowed
                        conInput.Receive(ie);

                        handled = false;
                        if (ie.EventType == Input.InputEventType.Press)
                        {
                            handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
                        }

                        // hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
                        if (!handled)
                        {
                            //HotkeyCoalescer.Receive(ie);
                        }

                        break;
                    case 1: // Input overrides Hokeys
                        conInput.Receive(ie);
                        if (!Global.ActiveController.HasBinding(ie.LogicalButton.ToString()))
                        {
                            handled = false;
                            if (ie.EventType == Input.InputEventType.Press)
                            {
                                handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
                            }

                            // hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
                            if (!handled)
                            {
                                //HotkeyCoalescer.Receive(ie);
                            }
                        }
                        break;
                    case 2: // Hotkeys override Input
                        handled = false;
                        if (ie.EventType == Input.InputEventType.Press)
                        {
                            handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
                        }

                        // hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
                        if (!handled)
                        {
                            //HotkeyCoalescer.Receive(ie);
                            conInput.Receive(ie);
                        }

                        break;
                }

            } // foreach event

            // also handle floats
            /*conInput.AcceptNewFloats(Input.Instance.GetFloats().Select(o =>
            {
                var video = Global.Emulator.VideoProvider();
                // hackish
                if (o.Item1 == "WMouse X")
                {
                    var P = GlobalWin.DisplayManager.UntransformPoint(new Point((int)o.Item2, 0));
                    float x = P.X / (float)video.BufferWidth;
                    return new Tuple<string, float>("WMouse X", x * 20000 - 10000);
                }

                if (o.Item1 == "WMouse Y")
                {
                    var P = GlobalWin.DisplayManager.UntransformPoint(new Point(0, (int)o.Item2));
                    float y = P.Y / (float)video.BufferHeight;
                    return new Tuple<string, float>("WMouse Y", y * 20000 - 10000);
                }

                return o;
            }));*/
        }

        public bool IsLagFrame
        {
            get
            {
                /*if (Global.Emulator.CanPollInput())
                {
                    return Global.Emulator.AsInputPollable().IsLagFrame;
                }*/

                return false;
            }
        }

        private void StepRunLoop_Core(bool force = false)
        {
            var runFrame = false;
            _runloopFrameadvance = false;
            var currentTimestamp = Stopwatch.GetTimestamp();
            var suppressCaptureRewind = false;

            double frameAdvanceTimestampDeltaMs = (double)(currentTimestamp - _frameAdvanceTimestamp) / Stopwatch.Frequency * 1000.0;
            bool frameProgressTimeElapsed = frameAdvanceTimestampDeltaMs >= Global.Config.FrameProgressDelayMs;

            if (Global.Config.SkipLagFrame && IsLagFrame && frameProgressTimeElapsed && Global.Emulator.Frame > 0)
            {
                runFrame = true;
            }

            if (Global.ClientControls["Frame Advance"] || PressFrameAdvance)
            {
                // handle the initial trigger of a frame advance
                if (_frameAdvanceTimestamp == 0)
                {
                    PauseEmulator();
                    runFrame = true;
                    _runloopFrameadvance = true;
                    _frameAdvanceTimestamp = currentTimestamp;
                }
                else
                {
                    // handle the timed transition from countdown to FrameProgress
                    if (frameProgressTimeElapsed)
                    {
                        runFrame = true;
                        _runloopFrameProgress = true;
                        UnpauseEmulator();
                    }
                }
            }
            else
            {
                // handle release of frame advance: do we need to deactivate FrameProgress?
                if (_runloopFrameProgress)
                {
                    _runloopFrameProgress = false;
                    PauseEmulator();
                }

                _frameAdvanceTimestamp = 0;
            }

            if (!EmulatorPaused)
            {
                runFrame = true;
            }

            bool isRewinding = suppressCaptureRewind = false;// Rewind(ref runFrame, currentTimestamp);

            if (UpdateFrame)
            {
                runFrame = true;
            }

            var genSound = false;
            var coreskipaudio = false;
            if (runFrame || force)
            {
                var isFastForwarding = Global.ClientControls["Fast Forward"] || IsTurboing;
                var updateFpsString = _runloopLastFf != isFastForwarding;
                _runloopLastFf = isFastForwarding;

                // client input-related duties
                //GlobalWin.OSD.ClearGUIText();

                //Global.CheatList.Pulse();

                //zero 03-may-2014 - moved this before call to UpdateToolsBefore(), since it seems to clear the state which a lua event.framestart is going to want to alter
                Global.ClickyVirtualPadController.FrameTick();
                Global.LuaAndAdaptor.FrameTick();

                /*if (GlobalWin.Tools.Has<LuaConsole>())
                {
                    GlobalWin.Tools.LuaConsole.LuaImp.CallFrameBeforeEvent();
                }*/

                /*if (!IsTurboing)
                {
                    GlobalWin.Tools.UpdateToolsBefore();
                }
                else
                {
                    GlobalWin.Tools.FastUpdateBefore();
                }*/

                _runloopFps++;

                if ((double)(currentTimestamp - _runloopSecond) / Stopwatch.Frequency >= 1.0)
                {
                    _runloopLastFps = _runloopFps;
                    _runloopSecond = currentTimestamp;
                    _runloopFps = 0;
                    updateFpsString = true;
                }

                if (updateFpsString)
                {
                    var fps_string = _runloopLastFps + " fps";
                    if (isRewinding)
                    {
                        if (IsTurboing || isFastForwarding)
                        {
                            fps_string += " <<<<";
                        }
                        else
                        {
                            fps_string += " <<";
                        }
                    }
                    else if (IsTurboing)
                    {
                        fps_string += " >>>>";
                    }
                    else if (isFastForwarding)
                    {
                        fps_string += " >>";
                    }

                    //GlobalWin.OSD.FPS = fps_string;
                }

                //CaptureRewind(suppressCaptureRewind);

                if (!_runloopFrameadvance)
                {
                    genSound = true;
                }
                else if (!Global.Config.MuteFrameAdvance)
                {
                    genSound = true;
                }

                Global.MovieSession.HandleMovieOnFrameLoop();

                coreskipaudio = IsTurboing;// && _currAviWriter == null;

                {
                    bool render = !_throttle.skipnextframe;// || _currAviWriter != null;
                    bool renderSound = !coreskipaudio;
                    Global.Emulator.FrameAdvance(render, renderSound);
                }

                Global.MovieSession.HandleMovieAfterFrameLoop();

                GlobalWin.DisplayManager.NeedsToPaint = true;
                //Global.CheatList.Pulse();

                /*if (!PauseAVI)
                {
                    AvFrameAdvance();
                }*/

                if (IsLagFrame && Global.Config.AutofireLagFrames)
                {
                    Global.AutoFireController.IncrementStarts();
                }
                Global.AutofireStickyXORAdapter.IncrementLoops(IsLagFrame);

                PressFrameAdvance = false;

                /*if (GlobalWin.Tools.Has<LuaConsole>())
                {
                    GlobalWin.Tools.LuaConsole.LuaImp.CallFrameAfterEvent();
                }*/

                /*if (!IsTurboing)
                {
                    UpdateToolsAfter();
                }
                else
                {
                    GlobalWin.Tools.FastUpdateAfter();
                }

                if (IsSeeking && Global.Emulator.Frame == PauseOnFrame.Value)
                {
                    PauseEmulator();
                    PauseOnFrame = null;
                }*/
            }

            if (Global.ClientControls["Rewind"] || PressRewind)
            {
                //UpdateToolsAfter();
                PressRewind = false;
            }

            if (UpdateFrame)
            {
                UpdateFrame = false;
            }

            bool outputSilence = !genSound || coreskipaudio;
            GlobalWin.Sound.UpdateSound(outputSilence);
        }

        private void SyncThrottle()
        {
            // "unthrottled" = throttle was turned off with "Toggle Throttle" hotkey
            // "turbo" = throttle is off due to the "Turbo" hotkey being held
            // They are basically the same thing but one is a toggle and the other requires a
            // hotkey to be held. There is however slightly different behavior in that turbo
            // skips outputting the audio. There's also a third way which is when no throttle
            // method is selected, but the clock throttle determines that by itself and
            // everything appears normal here.

            var rewind = Global.Rewinder.RewindActive && (Global.ClientControls["Rewind"] || PressRewind);
            var fastForward = Global.ClientControls["Fast Forward"] || FastForward;
            var turbo = IsTurboing;

            int speedPercent = fastForward ? Global.Config.SpeedPercentAlternate : Global.Config.SpeedPercent;

            if (rewind)
            {
                speedPercent = Math.Max(speedPercent * Global.Config.RewindSpeedMultiplier / Global.Rewinder.RewindFrequency, 5);
            }

            Global.DisableSecondaryThrottling = _unthrottled || turbo || fastForward || rewind;

            // realtime throttle is never going to be so exact that using a double here is wrong
            _throttle.SetCoreFps(Global.Emulator.CoreComm.VsyncRate);
            _throttle.signal_paused = EmulatorPaused;
            _throttle.signal_unthrottle = _unthrottled || turbo;
            _throttle.signal_overrideSecondaryThrottle = (fastForward || rewind) && (Global.Config.SoundThrottle || Global.Config.VSyncThrottle || Global.Config.VSync);
            _throttle.SetSpeedPercent(speedPercent);
        }

        public string CurrentlyOpenRom;
        public bool PauseAVI = false;
        public bool PressFrameAdvance = false;
        public bool PressRewind = false;
        public bool FastForward = false;
        public bool TurboFastForward = false;
        public bool RestoreReadWriteOnStop = false;
        public bool UpdateFrame = false;
        public bool AllowInput = true;

        private int? _pauseOnFrame;
        public int? PauseOnFrame // If set, upon completion of this frame, the client wil pause
        {
            get { return _pauseOnFrame; }
            set
            {
                _pauseOnFrame = value;
                //SetPauseStatusbarIcon();

                if (value == null) // TODO: make an Event handler instead, but the logic here is that after turbo seeking, tools will want to do a real update when the emulator finally pauses
                {
                    //GlobalWin.Tools.UpdateToolsBefore();
                    //GlobalWin.Tools.UpdateToolsAfter();
                }
            }
        }

        public bool IsSeeking
        {
            get { return PauseOnFrame.HasValue; }
        }

        public bool IsTurboSeeking
        {
            get
            {
                return PauseOnFrame.HasValue && Global.Config.TurboSeek;
            }
        }

        public bool IsTurboing
        {
            get
            {
                return Global.ClientControls["Turbo"] || IsTurboSeeking;
            }
        }

        #region Pause

        private bool _emulatorPaused;
        public bool EmulatorPaused
        {
            get
            {
                return _emulatorPaused;
            }

            private set
            {
                _emulatorPaused = value;
                if (OnPauseChanged != null)
                {
                    OnPauseChanged(this, new PauseChangedEventArgs(_emulatorPaused));
                }
            }
        }

        public delegate void PauseChangedEventHandler(object sender, PauseChangedEventArgs e);
        public event PauseChangedEventHandler OnPauseChanged;

        public class PauseChangedEventArgs : EventArgs
        {
            public PauseChangedEventArgs(bool paused)
            {
                Paused = paused;
            }

            public bool Paused { get; private set; }
        }

        public void PauseEmulator()
        {
            EmulatorPaused = true;
            //SetPauseStatusbarIcon();
        }

        public void UnpauseEmulator()
        {
            EmulatorPaused = false;
            //SetPauseStatusbarIcon();
        }

        public void TogglePause()
        {
            EmulatorPaused ^= true;
            //SetPauseStatusbarIcon();

            // TODO: have tastudio set a pause status change callback, or take control over pause
            /*if (GlobalWin.Tools.Has<TAStudio>())
            {
                GlobalWin.Tools.UpdateValues<TAStudio>();
            }*/
        }
        #endregion

        private void MainForm_Closed(object sender, EventArgs e)
        {
            if (_running)
            {
                Shutdown();
            }
        }
        
        private void Shutdown()
        {
            _running = false;
            _worker.Join(5000);
            Application.Instance.Quit();
        }

        private void viewport_Paint(object sender, PaintEventArgs e)
        {
            if (Global.Emulator != null) 
            {
                var video = Global.Emulator.VideoProvider();
                Bitmap img = new Bitmap(video.BufferWidth, video.BufferHeight, PixelFormat.Format32bppRgb);
                BitmapData data = img.Lock();
                int[] buffer = (int[])(video.GetVideoBuffer().Clone());
                //Buffer is cloned to prevent tearing. The emulation thread is running independent of drawing, 
                //does not block, can (and will) update the framebuffer while we draw it.
                if (img.Platform.IsMac)
                {
                    //Colors are reversed on OSX, even though it's Little Endian just like on Windows.
                    //I think this is a bug in Eto framework. This hack won't be needed when I bring back OpenGL, or if Eto gets fixed.
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        int x = buffer [i];
                        x = (x >> 16 & 0xFF) + (x & 0xFF00) + ((x << 16) & 0xFF0000);
                        buffer[i] = x;
                    }
                }
                Marshal.Copy(buffer, 0, data.Data, buffer.Length);
                data.Dispose();
                e.Graphics.DrawImage(img, 0, 0, _viewport.Width, _viewport.Height);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.Black, new RectangleF(0, 0, _viewport.Width, _viewport.Height));
            }
        }

        private void OpenRom()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            _suspended = true;
            if (ofd.ShowDialog(this) == DialogResult.Ok)
            {
                _suspended = false;
                RomLoader loader = new RomLoader();
                var nextComm = CreateCoreComm();
                CoreFileProvider.SyncCoreCommInputSignals(nextComm);
                bool result = loader.LoadRom(ofd.FileName, nextComm);
                if (result)
                {
                    Global.Emulator = loader.LoadedEmulator;
                    Global.Game = loader.Game;
                    CoreFileProvider.SyncCoreCommInputSignals(nextComm);
                    InputManager.SyncControls();

                    /*if (Global.Emulator is TI83 && Global.Config.TI83autoloadKeyPad)
                    {
                        GlobalWin.Tools.Load<TI83KeyPad>();
                    }*/

                    if (loader.LoadedEmulator is NES)
                    {
                        var nes = loader.LoadedEmulator as NES;
                        if (!string.IsNullOrWhiteSpace(nes.GameName))
                        {
                            Global.Game.Name = nes.GameName;
                        }

                        Global.Game.Status = nes.RomStatus;
                    }
                    else if (loader.LoadedEmulator is QuickNES)
                    {
                        var qns = loader.LoadedEmulator as QuickNES;
                        if (!string.IsNullOrWhiteSpace(qns.BootGodName))
                        {
                            Global.Game.Name = qns.BootGodName;
                        }
                        if (qns.BootGodStatus.HasValue)
                        {
                            Global.Game.Status = qns.BootGodStatus.Value;
                        }
                    }

                    Global.Rewinder.ResetRewindBuffer();

                    /*if (Global.Emulator.CoreComm.RomStatusDetails == null && loader.Rom != null)
                    {
                        Global.Emulator.CoreComm.RomStatusDetails = string.Format(
                            "{0}\r\nSHA1:{1}\r\nMD5:{2}\r\n",
                            loader.Game.Name,
                            loader.Rom.RomData.HashSHA1(),
                            loader.Rom.RomData.HashMD5());
                    }*/

                    if (Global.Emulator.BoardName != null)
                    {
                        Console.WriteLine("Core reported BoardID: \"{0}\"", Global.Emulator.BoardName);
                    }

                    // restarts the lua console if a different rom is loaded.
                    // im not really a fan of how this is done..
                    /*if (Global.Config.RecentRoms.Empty || Global.Config.RecentRoms.MostRecent != loader.CanonicalFullPath)
                    {
                        GlobalWin.Tools.Restart<LuaConsole>();
                    }*/

                    Global.Config.RecentRoms.Add(loader.CanonicalFullPath);
                    //JumpLists.AddRecentItem(loader.CanonicalFullPath);

                    // Don't load Save Ram if a movie is being loaded
                    /*if (!Global.MovieSession.MovieIsQueued && File.Exists(PathManager.SaveRamPath(loader.Game)))
                    {
                        LoadSaveRam();
                    }

                    GlobalWin.Tools.Restart();

                    if (Global.Config.LoadCheatFileByGame)
                    {
                        if (Global.CheatList.AttemptToLoadCheatFile())
                        {
                            GlobalWin.OSD.AddMessage("Cheats file loaded");
                        }
                    }

                    SetWindowText();
                    CurrentlyOpenRom = loader.CanonicalFullPath;
                    HandlePlatformMenus();
                    _stateSlots.Clear();
                    UpdateCoreStatusBarButton();
                    UpdateDumpIcon();
                    SetMainformMovieInfo();
                    */
                    Global.Rewinder.CaptureRewindState();

                    Global.StickyXORAdapter.ClearStickies();
                    Global.StickyXORAdapter.ClearStickyFloats();
                    Global.AutofireStickyXORAdapter.ClearStickies();

                    RewireSound();
                    /*ToolHelpers.UpdateCheatRelatedTools(null, null);
                    if (Global.Config.AutoLoadLastSaveSlot && _stateSlots.HasSlot(Global.Config.SaveSlot))
                    {
                        LoadQuickSave("QuickSave" + Global.Config.SaveSlot);
                    }

                    if (Global.FirmwareManager.RecentlyServed.Count > 0)
                    {
                        Console.WriteLine("Active Firmwares:");
                        foreach (var f in Global.FirmwareManager.RecentlyServed)
                        {
                            Console.WriteLine("  {0} : {1}", f.FirmwareId, f.Hash);
                        }
                    }*/

                    EnableControls();

                    //return true;
                }
            }
            _suspended = false;
        }

        CoreComm CreateCoreComm()
        {
            CoreComm ret = new CoreComm(ShowMessageCoreComm, NotifyCoreComm);
            //ret.RequestGLContext = () => GlobalWin.GLManager.CreateGLContext();
            //ret.ActivateGLContext = (gl) => GlobalWin.GLManager.Activate((GLManager.ContextRef)gl);
            //ret.DeactivateGLContext = () => GlobalWin.GLManager.Deactivate();
            return ret;
        }

        private void ShowMessageCoreComm(string message)
        {
            MessageBox.Show(Application.Instance.MainForm, message, "Warning", MessageBoxButtons.OK);
        }

        private void NotifyCoreComm(string message)
        {
            //GlobalWin.OSD.AddMessage(message);
        }
    }

}
