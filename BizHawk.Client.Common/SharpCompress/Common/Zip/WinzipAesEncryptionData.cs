
#if !NO_CRYPTO
using System;
using System.Security.Cryptography;
using SharpCompress.Converters;

namespace SharpCompress.Common.Zip
{
    internal class WinzipAesEncryptionData
    {
        private const int RFC2898_ITERATIONS = 1000;

        private readonly byte[] _salt;
        private readonly WinzipAesKeySize _keySize;
        private readonly byte[] _passwordVerifyValue;
        private readonly string _password;

        private byte[] _generatedVerifyValue;

        internal WinzipAesEncryptionData(WinzipAesKeySize keySize, byte[] salt, byte[] passwordVerifyValue,
                                         string password)
        {
            this._keySize = keySize;
            this._salt = salt;
            this._passwordVerifyValue = passwordVerifyValue;
            this._password = password;
            Initialize();
        }

        internal byte[] IvBytes
{
    get; set;
}
        internal byte[] KeyBytes
{
    get; set;
}

        private int KeySizeInBytes
        {
            get { return KeyLengthInBytes(_keySize);
}
        }

        internal static int KeyLengthInBytes(WinzipAesKeySize keySize)
        {
            switch (keySize)
            {
                case WinzipAesKeySize.KeySize128:
                    return 16;
                case WinzipAesKeySize.KeySize192:
                    return 24;
                case WinzipAesKeySize.KeySize256:
                    return 32;
            }
            throw new InvalidOperationException();
        }

        private void Initialize()
        {
            var rfc2898 = new Rfc2898DeriveBytes(_password, _salt, RFC2898_ITERATIONS);

            KeyBytes = rfc2898.GetBytes(KeySizeInBytes); // 16 or 24 or 32 ???
            IvBytes = rfc2898.GetBytes(KeySizeInBytes);
            _generatedVerifyValue = rfc2898.GetBytes(2);

            short verify = DataConverter.LittleEndian.GetInt16(_passwordVerifyValue, 0);
            if (_password != null)
            {
                short generated = DataConverter.LittleEndian.GetInt16(_generatedVerifyValue, 0);
                if (verify != generated)
                {
                    throw new InvalidFormatException("bad password");
                }
            }
        }
    }
}
#endif