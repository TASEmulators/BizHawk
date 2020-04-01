/***********************************************************************
Copyright 2016 Stefan Hausotte

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*************************************************************************/

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PeNet.Utilities
{
    /// <summary>
    /// Different hashes for the PE file.
    /// </summary>
    public static class Hashes
    {
        /// <summary>
        ///     Compute the SHA-256 from a file.
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <returns>SHA-256 as 64 characters long hex-string</returns>
        public static string Sha256(string file)
        {
            byte[] hash;
            var sBuilder = new StringBuilder();

            using (var sr = new StreamReader(file))
            {
                var sha = new SHA256Managed();
                hash = sha.ComputeHash(sr.BaseStream);
            }

            foreach (var t in hash)
                sBuilder.Append(t.ToString("x2"));

            return sBuilder.ToString();
        }

        /// <summary>
        ///     Compute the SHA-256 from a byte array.
        /// </summary>
        /// <param name="buff">Binary as a byte buffer.</param>
        /// <returns>SHA-256 as 64 characters long hex-string</returns>
        public static string Sha256(byte[] buff)
        {
            var sBuilder = new StringBuilder();

            var sha = new SHA256Managed();
            var hash = sha.ComputeHash(buff);

            foreach (var t in hash)
                sBuilder.Append(t.ToString("x2"));

            return sBuilder.ToString();
        }

        /// <summary>
        ///     Compute the SHA-1 from a file.
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <returns>SHA-1 as 40 characters long hex-string</returns>
        public static string Sha1(string file)
        {
            byte[] hash;
            var sBuilder = new StringBuilder();

            using (var sr = new StreamReader(file))
            {
                var sha = new SHA1Managed();
                hash = sha.ComputeHash(sr.BaseStream);
            }

            foreach (var t in hash)
                sBuilder.Append(t.ToString("x2"));

            return sBuilder.ToString();
        }

        /// <summary>
        ///     Compute the SHA-1 from a byte array.
        /// </summary>
        /// <param name="buff">Binary as a byte buffer.</param>
        /// <returns>SHA-1 as 40 characters long hex-string</returns>
        public static string Sha1(byte[] buff)
        {
            var sBuilder = new StringBuilder();

            var sha = new SHA1Managed();
            var hash = sha.ComputeHash(buff);

            foreach (var t in hash)
                sBuilder.Append(t.ToString("x2"));

            return sBuilder.ToString();
        }

        /// <summary>
        ///     Compute the MD5 from a file.
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <returns>MD5 as 32 characters long hex-string</returns>
        public static string MD5(string file)
        {
            byte[] hash;
            var sBuilder = new StringBuilder();

            using (var sr = new StreamReader(file))
            {
                var sha = System.Security.Cryptography.MD5.Create();
                hash = sha.ComputeHash(sr.BaseStream);
            }

            foreach (var t in hash)
                sBuilder.Append(t.ToString("x2"));

            return sBuilder.ToString();
        }

        /// <summary>
        ///     Compute the MD5 from a byte array.
        /// </summary>
        /// <param name="buff">Binary as a byte buffer.</param>
        /// <returns>MD5 as 32 characters long hex-string</returns>
        public static string MD5(byte[] buff)
        {
            var sBuilder = new StringBuilder();

            var sha = System.Security.Cryptography.MD5.Create();
            var hash = sha.ComputeHash(buff);

            foreach (var t in hash)
                sBuilder.Append(t.ToString("x2"));

            return sBuilder.ToString();
        }
    }
}