using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vortice.XAudio2;
using static Vortice.MediaFoundation.MediaFactory;

namespace SharpAudio.XA2
{
    internal sealed class XA2Engine : AudioEngine
    {
        internal IXAudio2MasteringVoice MasterVoice { get; }

        public override AudioBackend BackendType => AudioBackend.XAudio2;
        public IXAudio2 Device { get; }

        private const uint RPC_E_CHANGED_MODE = 0x80010106;
        private const uint COINIT_MULTITHREADED = 0x0;
        private const uint COINIT_APARTMENTTHREADED = 0x2;

        [DllImport("ole32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint CoInitializeEx([In, Optional] IntPtr pvReserved, [In] uint dwCoInit);

        static XA2Engine()
        {
            var hr = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);
            if (hr == RPC_E_CHANGED_MODE)
            {
                hr = CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED);
            }
        }

        private static string GetDeviceID(string deviceName)
        {
	        if (deviceName is null) return null;
	        using var devices = MFEnumAudioDeviceSources();
	        return devices.FirstOrDefault(capDevice => capDevice.FriendlyName == deviceName)?.AudioEndPointID;
        }

        public XA2Engine(AudioEngineOptions options)
        {
            Device = XAudio2.XAudio2Create();
            MasterVoice = Device.CreateMasteringVoice(options.SampleChannels, options.SampleRate, deviceId: GetDeviceID(options.DeviceName));
        }

        protected override void PlatformDispose()
        {
            MasterVoice.Dispose();
            Device.Dispose();
        }

        public override AudioBuffer CreateBuffer()
        {
            return new XA2Buffer();
        }

        public override AudioSource CreateSource(Submixer mixer = null)
        {
            return new XA2Source(this, (XA2Submixer) mixer);
        }

        public override Audio3DEngine Create3DEngine()
        {
            return new XA23DEngine(this);
        }

        public override Submixer CreateSubmixer()
        {
            return new XA2Submixer(this);
        }

        public static IEnumerable<string> GetDeviceNames()
        {
	        using var devices = MFEnumAudioDeviceSources();
	        return devices.Select(capDevice => capDevice.FriendlyName);
        }
    }
}
