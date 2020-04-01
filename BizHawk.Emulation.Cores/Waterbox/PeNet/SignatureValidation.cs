using System;
using System.Runtime.InteropServices;

namespace PeNet
{
    // Code taken from
    // http://geekswithblogs.net/robp/archive/2007/05/04/112250.aspx
    //
    internal static class SignatureValidation
    {
        [DllImport("Wintrust.dll", PreserveSig = true, SetLastError = false)]
        private static extern uint WinVerifyTrust(IntPtr hWnd, IntPtr pgActionID, IntPtr pWinTrustData);

        private static uint WinVerifyTrust(string fileName)
        {
            var wintrust_action_generic_verify_v2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");
            uint result;
            using (var fileInfo = new WINTRUST_FILE_INFO(fileName, Guid.Empty))

            using (var guidPtr = new UnmanagedPointer(
                Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid))),
                AllocMethod.HGlobal)
                )

            using (var wvtDataPtr = new UnmanagedPointer(
                Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA))),
                AllocMethod.HGlobal)
                )
            {
                var data = new WINTRUST_DATA(fileInfo);
                IntPtr pGuid = guidPtr;
                IntPtr pData = wvtDataPtr;
                Marshal.StructureToPtr(wintrust_action_generic_verify_v2, pGuid, true);
                Marshal.StructureToPtr(data, pData, true);
                result = WinVerifyTrust(IntPtr.Zero, pGuid, pData);
            }
            return result;
        }

        public static bool IsTrusted(string fileName)
        {
            return WinVerifyTrust(fileName) == 0;
        }
    }

    internal struct WINTRUST_FILE_INFO : IDisposable
    {
        public WINTRUST_FILE_INFO(string fileName, Guid subject)
        {
            cbStruct = (uint) Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
            pcwszFilePath = fileName;

            if (subject != Guid.Empty)
            {
                pgKnownSubject = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                Marshal.StructureToPtr(subject, pgKnownSubject, true);
            }
            else
            {
                pgKnownSubject = IntPtr.Zero;
            }
            hFile = IntPtr.Zero;
        }

        public uint cbStruct;

        [MarshalAs(UnmanagedType.LPTStr)] public string pcwszFilePath;

        public IntPtr hFile;

        public IntPtr pgKnownSubject;

        #region IDisposable Members

        public void Dispose()
        {
            if (pgKnownSubject == IntPtr.Zero) return;
            Marshal.DestroyStructure(pgKnownSubject, typeof(Guid));
            Marshal.FreeHGlobal(pgKnownSubject);
        }

        #endregion
    }

    internal enum AllocMethod
    {
        HGlobal,
        CoTaskMem
    }

    internal enum UnionChoice
    {
        File = 1,
        Catalog,
        Blob,
        Signer,
        Cert
    }

    internal enum UiChoice
    {
        All = 1,
        NoUI,
        NoBad,
        NoGood
    }

    internal enum RevocationCheckFlags
    {
        None = 0,
        WholeChain
    }

    internal enum StateAction
    {
        Ignore = 0,
        Verify,
        Close,
        AutoCache,
        AutoCacheFlush
    }

    internal enum TrustProviderFlags
    {
        UseIE4Trust = 1,
        NoIE4Chain = 2,
        NoPolicyUsage = 4,
        RevocationCheckNone = 16,
        RevocationCheckEndCert = 32,
        RevocationCheckChain = 64,
        RecovationCheckChainExcludeRoot = 128,
        Safer = 256,
        HashOnly = 512,
        UseDefaultOSVerCheck = 1024,
        LifetimeSigning = 2048
    }

    internal enum UIContext
    {
        Execute = 0,
        Install
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINTRUST_DATA : IDisposable
    {
        public WINTRUST_DATA(WINTRUST_FILE_INFO fileInfo)
        {
            cbStruct = (uint) Marshal.SizeOf(typeof(WINTRUST_DATA));
            pInfoStruct = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
            Marshal.StructureToPtr(fileInfo, pInfoStruct, false);

            dwUnionChoice = UnionChoice.File;
            pPolicyCallbackData = IntPtr.Zero;
            pSIPCallbackData = IntPtr.Zero;

            dwUIChoice = UiChoice.NoUI;
            fdwRevocationChecks = RevocationCheckFlags.None;
            dwStateAction = StateAction.Ignore;
            hWVTStateData = IntPtr.Zero;
            pwszURLReference = IntPtr.Zero;
            dwProvFlags = TrustProviderFlags.Safer;

            dwUIContext = UIContext.Execute;
        }

        public uint cbStruct;

        public IntPtr pPolicyCallbackData;

        public IntPtr pSIPCallbackData;

        public UiChoice dwUIChoice;

        public RevocationCheckFlags fdwRevocationChecks;

        public UnionChoice dwUnionChoice;

        public IntPtr pInfoStruct;

        public StateAction dwStateAction;

        public IntPtr hWVTStateData;

        private readonly IntPtr pwszURLReference;

        public TrustProviderFlags dwProvFlags;

        public UIContext dwUIContext;

        #region IDisposable Members

        public void Dispose()
        {
            if (dwUnionChoice == UnionChoice.File)
            {
                var info = new WINTRUST_FILE_INFO();
                Marshal.PtrToStructure(pInfoStruct, info);
                info.Dispose();
                Marshal.DestroyStructure(pInfoStruct, typeof(WINTRUST_FILE_INFO));
            }
            Marshal.FreeHGlobal(pInfoStruct);
        }

        #endregion
    }

    internal sealed class UnmanagedPointer : IDisposable
    {
        private readonly AllocMethod m_meth;
        private IntPtr m_ptr;

        internal UnmanagedPointer(IntPtr ptr, AllocMethod method)
        {
            m_meth = method;
            m_ptr = ptr;
        }

        ~UnmanagedPointer()
        {
            Dispose(false);
        }

        public static implicit operator IntPtr(UnmanagedPointer ptr)
        {
            return ptr.m_ptr;
        }

        #region IDisposable Members

        private void Dispose(bool disposing)
        {
            if (m_ptr != IntPtr.Zero)
            {
                if (m_meth == AllocMethod.HGlobal)
                {
                    Marshal.FreeHGlobal(m_ptr);
                }

                else if (m_meth == AllocMethod.CoTaskMem)
                {
                    Marshal.FreeCoTaskMem(m_ptr);
                }

                m_ptr = IntPtr.Zero;
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}