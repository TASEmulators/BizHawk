using System;
using System.Text;

namespace SharpCompress.Common
{
    public class ArchiveEncoding
    {
        /// <summary>
        /// Default encoding to use when archive format doesn't specify one.
        /// </summary>
        public Encoding Default { get; set; }

        /// <summary>
        /// ArchiveEncoding used by encryption schemes which don't comply with RFC 2898.
        /// </summary>
        public Encoding Password { get; set; }

        /// <summary>
        /// Set this encoding when you want to force it for all encoding operations.
        /// </summary>
        public Encoding Forced { get; set; }

        /// <summary>
        /// Set this when you want to use a custom method for all decoding operations.
        /// </summary>
        /// <returns>string Func(bytes, index, length)</returns>
        public Func<byte[], int, int, string> CustomDecoder { get; set; }

        public ArchiveEncoding()
        {
#if NETSTANDARD1_0
            Default = Encoding.GetEncoding("cp437");
            Password = Encoding.GetEncoding("cp437");
#else    
            Default = Encoding.GetEncoding(437);
            Password = Encoding.GetEncoding(437);
#endif            
        }

#if NETSTANDARD1_3 || NETSTANDARD2_0
        static ArchiveEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
#endif

        public string Decode(byte[] bytes)
        {
            return Decode(bytes, 0, bytes.Length);
        }

        public string Decode(byte[] bytes, int start, int length)
        {
            return GetDecoder().Invoke(bytes, start, length);
        }

        public string DecodeUTF8(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public byte[] Encode(string str)
        {
            return GetEncoding().GetBytes(str);
        }

        public Encoding GetEncoding()
        {
            return Forced ?? Default ?? Encoding.UTF8;
        }

        public Func<byte[], int, int, string> GetDecoder()
        {
            return CustomDecoder ?? ((bytes, index, count) => GetEncoding().GetString(bytes, index, count));
        }
    }
}