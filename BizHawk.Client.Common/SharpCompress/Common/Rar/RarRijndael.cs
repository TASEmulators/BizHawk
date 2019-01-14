#if !NO_CRYPTO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SharpCompress.Crypto;

namespace SharpCompress.Common.Rar
{
    internal class RarRijndael : IDisposable
    {
        internal const int CRYPTO_BLOCK_SIZE = 16;

        private readonly string _password;
        private readonly byte[] _salt;
        private byte[] _aesInitializationVector;
        private RijndaelEngine _rijndael;

        private RarRijndael(string password, byte[] salt)
        {
            _password = password;
            _salt = salt;
        }

        private byte[] ComputeHash(byte[] input)
        {
            var sha = SHA1.Create();
            return sha.ComputeHash(input);
        }

        private void Initialize()
        {

            _rijndael = new RijndaelEngine();
            _aesInitializationVector = new byte[CRYPTO_BLOCK_SIZE];
            int rawLength = 2*_password.Length;
            byte[] rawPassword = new byte[rawLength + 8];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(_password);
            for (int i = 0; i < _password.Length; i++)
            {
                rawPassword[i*2] = passwordBytes[i];
                rawPassword[i*2 + 1] = 0;
            }
            for (int i = 0; i < _salt.Length; i++)
            {
                rawPassword[i + rawLength] = _salt[i];
            }


            const int noOfRounds = (1 << 18);
            IList<byte> bytes = new List<byte>();
            byte[] digest;

            //TODO slow code below, find ways to optimize
            for (int i = 0; i < noOfRounds; i++)
            {
                bytes.AddRange(rawPassword);

                bytes.AddRange(new[]
                {
                    (byte) i, (byte) (i >> 8), (byte) (i >> CRYPTO_BLOCK_SIZE)
                });
                if (i%(noOfRounds/CRYPTO_BLOCK_SIZE) == 0)
                {
                    digest = ComputeHash(bytes.ToArray());
                    _aesInitializationVector[i/(noOfRounds/CRYPTO_BLOCK_SIZE)] = digest[19];
                }
            }

            digest = ComputeHash(bytes.ToArray());
            //slow code ends

            byte[] aesKey = new byte[CRYPTO_BLOCK_SIZE];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    aesKey[i*4 + j] = (byte)
                        (((digest[i*4]*0x1000000) & 0xff000000 |
                          (uint) ((digest[i*4 + 1]*0x10000) & 0xff0000) |
                          (uint) ((digest[i*4 + 2]*0x100) & 0xff00) |
                          (uint) (digest[i*4 + 3] & 0xff)) >> (j*8));
                }
            }

            _rijndael.Init(false, new KeyParameter(aesKey));

        }

        public static RarRijndael InitializeFrom(string password, byte[] salt)
        {
            var rijndael = new RarRijndael(password, salt);
            rijndael.Initialize();
            return rijndael;
        }

        public byte[] ProcessBlock(byte[] cipherText)
        {
            var plainText = new byte[CRYPTO_BLOCK_SIZE];
            var decryptedBytes = new List<byte>();
            _rijndael.ProcessBlock(cipherText, 0, plainText, 0);

            for (int j = 0; j < plainText.Length; j++)
            {
                decryptedBytes.Add((byte) (plainText[j] ^ _aesInitializationVector[j%16])); //32:114, 33:101
            }

            for (int j = 0; j < _aesInitializationVector.Length; j++)
            {
                _aesInitializationVector[j] = cipherText[j];
            }
            return decryptedBytes.ToArray();
        }

        public void Dispose()
        {
        }
    }
}
#endif