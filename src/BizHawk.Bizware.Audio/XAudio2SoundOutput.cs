using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Client.Common;
using BizHawk.Common;

using Vortice.MediaFoundation;
using Vortice.Multimedia;
using Vortice.XAudio2;

namespace BizHawk.Bizware.Audio
{
	public sealed class XAudio2SoundOutput : ISoundOutput
	{
		private bool _disposed;
		private readonly IHostAudioManager _sound;
		private readonly DeferredXAudio2ErrorCallback _deferredErrorCallback;
		private IXAudio2 _device;
		private IXAudio2MasteringVoice _masteringVoice;
		private IXAudio2SourceVoice _sourceVoice;
		private BufferPool _bufferPool;
		private long _runningSamplesQueued;

		private static string GetDeviceId(string deviceName)
		{
			if (string.IsNullOrEmpty(deviceName))
			{
				return null;
			}

			using var enumerator = new IMMDeviceEnumerator();
			var devices = enumerator.EnumAudioEndpoints(DataFlow.Render);
			var device = devices.FirstOrDefault(capDevice => capDevice.FriendlyName == deviceName);
			if (device is null)
			{
				return null;
			}

			const string MMDEVAPI_TOKEN = @"\\?\SWD#MMDEVAPI#";
			const string DEVINTERFACE_AUDIO_RENDER = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
			return $"{MMDEVAPI_TOKEN}{device.Id}{DEVINTERFACE_AUDIO_RENDER}";
		}

		private void ResetToDefaultDevice()
		{
			var wasPlaying = _sourceVoice != null;
			_sourceVoice?.Dispose();
			_bufferPool?.Dispose();
			_masteringVoice.Dispose();
			_device.Dispose();

			_device = XAudio2.XAudio2Create();
			_device.CriticalError += (_, _) => _deferredErrorCallback.OnCriticalError();
			_masteringVoice = _device.CreateMasteringVoice(
				inputChannels: _sound.ChannelCount,
				inputSampleRate: _sound.SampleRate);

			if (wasPlaying)
			{
				StartSound();
			}
		}

		public XAudio2SoundOutput(IHostAudioManager sound, string chosenDeviceName)
		{
			_sound = sound;
			_device = XAudio2.XAudio2Create();
			// this is for fatal errors which require resetting to the default audio device
			// note that this won't be called on the main thread, so we'll defer the reset to the main thread
			_deferredErrorCallback = new(ResetToDefaultDevice);
			_device.CriticalError += (_, _) => _deferredErrorCallback.OnCriticalError();
			_masteringVoice = _device.CreateMasteringVoice(
				inputChannels: _sound.ChannelCount, 
				inputSampleRate: _sound.SampleRate,
				deviceId: GetDeviceId(chosenDeviceName));
		}

		public void Dispose()
		{
			if (_disposed) return;

			_masteringVoice.Dispose();
			_device.Dispose();
			_deferredErrorCallback.Dispose();

			_disposed = true;
		}

		public static IEnumerable<string> GetDeviceNames()
		{
			using var enumerator = new IMMDeviceEnumerator();
			var devices = enumerator.EnumAudioEndpoints(DataFlow.Render);
			return devices.Select(capDevice => capDevice.FriendlyName);
		}

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			_sourceVoice.Volume = (float)volume;
		}

		public void StartSound()
		{
			BufferSizeSamples = _sound.MillisecondsToSamples(_sound.ConfigBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			var format = new WaveFormat(_sound.SampleRate, _sound.BytesPerSample * 8, _sound.ChannelCount);
			_sourceVoice = _device.CreateSourceVoice(format);

			_bufferPool = new();
			_runningSamplesQueued = 0;

			_sourceVoice.Start();
		}

		public void StopSound()
		{
			_sourceVoice.Stop();
			_sourceVoice.Dispose();
			_sourceVoice = null;

			_bufferPool.Dispose();
			_bufferPool = null;

			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			var isInitializing = _runningSamplesQueued == 0;
			var voiceState = _sourceVoice.State;
			var detectedUnderrun = !isInitializing && voiceState.BuffersQueued == 0;
			var samplesAwaitingPlayback = _runningSamplesQueued - (long)voiceState.SamplesPlayed;
			var samplesNeeded = (int)Math.Max(BufferSizeSamples - samplesAwaitingPlayback, 0);
			if (isInitializing || detectedUnderrun)
			{
				_sound.HandleInitializationOrUnderrun(detectedUnderrun, ref samplesNeeded);
			}
			return samplesNeeded;
		}

		public void WriteSamples(short[] samples, int sampleOffset, int sampleCount)
		{
			if (sampleCount == 0) return;
			_bufferPool.Release(_sourceVoice.State.BuffersQueued);
			var byteCount = sampleCount * _sound.BlockAlign;
			var item = _bufferPool.Obtain(byteCount);
			samples.AsSpan(sampleOffset * _sound.BlockAlign / 2, byteCount / 2)
				.CopyTo(item.AudioBuffer.AsSpan<short>());
			item.AudioBuffer.AudioBytes = byteCount;
			_sourceVoice.SubmitSourceBuffer(item.AudioBuffer);
			_runningSamplesQueued += sampleCount;
		}

		private class BufferPool : IDisposable
		{
			private readonly List<BufferPoolItem> _availableItems = new();
			private readonly Queue<BufferPoolItem> _obtainedItems = new();

			public void Dispose()
			{
				foreach (var item in _availableItems.Concat(_obtainedItems))
				{
					item.AudioBuffer.Dispose();
				}
				_availableItems.Clear();
				_obtainedItems.Clear();
			}

			public BufferPoolItem Obtain(int length)
			{
				var item = GetAvailableItem(length) ?? new BufferPoolItem(length);
				_obtainedItems.Enqueue(item);
				return item;
			}

			private BufferPoolItem GetAvailableItem(int length)
			{
				var foundIndex = -1;
				for (var i = 0; i < _availableItems.Count; i++)
				{
					if (_availableItems[i].MaxLength >= length && (foundIndex == -1 || _availableItems[i].MaxLength < _availableItems[foundIndex].MaxLength))
						foundIndex = i;
				}
				if (foundIndex == -1) return null;
				var item = _availableItems[foundIndex];
				_availableItems.RemoveAt(foundIndex);
				item.AudioBuffer.AudioBytes = item.MaxLength; // this might have shrunk from earlier use, set it back to MaxLength so AsSpan() works as expected
				return item;
			}

			public void Release(int buffersQueued)
			{
				while (_obtainedItems.Count > buffersQueued)
					_availableItems.Add(_obtainedItems.Dequeue());
			}

			public class BufferPoolItem
			{
				public int MaxLength { get; }
				public AudioBuffer AudioBuffer { get; }

				public BufferPoolItem(int length)
				{
					MaxLength = length;
					AudioBuffer = new(length, BufferFlags.None);
				}
			}
		}

		private sealed class DeferredXAudio2ErrorCallback : IDisposable
		{
			private const int WM_CLOSE = 0x0010;
			private const int WM_DEFERRED_ERROR_CALLBACK = 0x0400 + 1;

			private static readonly WmImports.WNDPROC _wndProc = WndProc;

			private static readonly Lazy<IntPtr> _deferredXAudio2CallbackWindowAtom = new(() =>
			{
				var wc = default(WmImports.WNDCLASSW);
				wc.lpfnWndProc = _wndProc;
				wc.hInstance = LoaderApiImports.GetModuleHandleW(null);
				wc.lpszClassName = "DeferredXAudio2ErrorCallbackClass";

				var atom = WmImports.RegisterClassW(ref wc);
				if (atom == IntPtr.Zero)
				{
					throw new InvalidOperationException("Failed to register deferred XAudio2 error callback window class");
				}

				return atom;
			});

			private static IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
			{
				var ud = WmImports.GetWindowLongPtrW(hWnd, WmImports.GWLP_USERDATA);
				if (ud == IntPtr.Zero)
				{
					return WmImports.DefWindowProcW(hWnd, uMsg, wParam, lParam);
				}

				if (uMsg != WM_DEFERRED_ERROR_CALLBACK)
				{
					if (uMsg == WM_CLOSE)
					{
						WmImports.SetWindowLongPtrW(hWnd, WmImports.GWLP_USERDATA, IntPtr.Zero);
						GCHandle.FromIntPtr(ud).Free();
					}

					return WmImports.DefWindowProcW(hWnd, uMsg, wParam, lParam);
				}

				// reset to the default audio device

				var deferredCallback = (DeferredXAudio2ErrorCallback)GCHandle.FromIntPtr(ud).Target;
				deferredCallback.ResetToDefaultDeviceCallback();

				return WmImports.DefWindowProcW(hWnd, uMsg, wParam, lParam);
			}

			private readonly Action ResetToDefaultDeviceCallback;
			private IntPtr _deferredErrorCallbackWindow;

			public DeferredXAudio2ErrorCallback(Action resetToDefaultDeviceCallback)
			{
				ResetToDefaultDeviceCallback = resetToDefaultDeviceCallback;

				const int WS_CHILD = 0x40000000;
				_deferredErrorCallbackWindow = WmImports.CreateWindowExW(
					dwExStyle: 0,
					lpClassName: _deferredXAudio2CallbackWindowAtom.Value,
					lpWindowName: "DeferredXAudio2ErrorCallback",
					dwStyle: WS_CHILD,
					X: 0,
					Y: 0,
					nWidth: 1,
					nHeight: 1,
					hWndParent: WmImports.HWND_MESSAGE,
					hMenu: IntPtr.Zero,
					hInstance: LoaderApiImports.GetModuleHandleW(null),
					lpParam: IntPtr.Zero);

				if (_deferredErrorCallbackWindow == IntPtr.Zero)
				{
					throw new InvalidOperationException("Failed to create deferred XAudio2 error callback window");
				}

				var handle = GCHandle.Alloc(this, GCHandleType.Normal);
				WmImports.SetWindowLongPtrW(_deferredErrorCallbackWindow, WmImports.GWLP_USERDATA, GCHandle.ToIntPtr(handle));
			}

			public void OnCriticalError()
				=> WmImports.PostMessageW(_deferredErrorCallbackWindow, WM_DEFERRED_ERROR_CALLBACK, IntPtr.Zero, IntPtr.Zero);

			public void Dispose()
			{
				if (_deferredErrorCallbackWindow != IntPtr.Zero)
				{
					WmImports.DestroyWindow(_deferredErrorCallbackWindow);
					_deferredErrorCallbackWindow = IntPtr.Zero;
				}
			}
		}
	}
}
