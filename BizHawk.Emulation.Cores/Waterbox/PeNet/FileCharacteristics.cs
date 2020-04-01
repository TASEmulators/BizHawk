using System.Text;
using ExtensionMethods = PeNet.Utilities.ExtensionMethods;

namespace PeNet
{
    /// <summary>
    ///     Describes which file characteristics based on the
    ///     file header are set.
    /// </summary>
    public class FileCharacteristics
    {
        /// <summary>
        ///     Create an object that contains all possible file characteristics
        ///     flags resolve to boolean properties.
        /// </summary>
        /// <param name="characteristics">Characteristics from the file header.</param>
        public FileCharacteristics(ushort characteristics)
        {
            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_RELOCS_STRIPPED) > 0)
                RelocStripped = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_EXECUTABLE_IMAGE) > 0)
                ExecutableImage = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_LINE_NUMS_STRIPPED) > 0)
                LineNumbersStripped = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_LOCAL_SYMS_STRIPPED) > 0)
                LocalSymbolsStripped = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_AGGRESIVE_WS_TRIM) > 0)
                AggressiveWsTrim = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_LARGE_ADDRESS_AWARE) > 0)
                LargeAddressAware = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_BYTES_REVERSED_LO) > 0)
                BytesReversedLo = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_32BIT_MACHINE) > 0)
                Machine32Bit = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_DEBUG_STRIPPED) > 0)
                DebugStripped = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP) >
                0)
                RemovableRunFromSwap = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_NET_RUN_FROM_SWAP) > 0)
                NetRunFroMSwap = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_SYSTEM) > 0)
                System = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_DLL) > 0)
                DLL = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_UP_SYSTEM_ONLY) > 0)
                UpSystemOnly = true;

            if ((characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_BYTES_REVERSED_HI) > 0)
                BytesReversedHi = true;
        }

        /// <summary>
        ///     Relocation stripped,
        /// </summary>
        public bool RelocStripped { get; private set; }

        /// <summary>
        ///     Is an executable image.
        /// </summary>
        public bool ExecutableImage { get; private set; }

        /// <summary>
        ///     Line numbers stripped.
        /// </summary>
        public bool LineNumbersStripped { get; private set; }

        /// <summary>
        ///     Local symbols stripped.
        /// </summary>
        public bool LocalSymbolsStripped { get; private set; }

        /// <summary>
        ///     (OBSOLTETE) Aggressively trim the working set.
        /// </summary>
        public bool AggressiveWsTrim { get; private set; }

        /// <summary>
        ///     Application can handle addresses larger than 2 GB.
        /// </summary>
        public bool LargeAddressAware { get; private set; }

        /// <summary>
        ///     (OBSOLTETE) Bytes of word are reversed.
        /// </summary>
        public bool BytesReversedLo { get; private set; }

        /// <summary>
        ///     Supports 32 Bit words.
        /// </summary>
        public bool Machine32Bit { get; private set; }

        /// <summary>
        ///     Debug stripped and stored in a separate file.
        /// </summary>
        public bool DebugStripped { get; private set; }

        /// <summary>
        ///     If the image is on a removable media, copy and run it from the swap file.
        /// </summary>
        public bool RemovableRunFromSwap { get; private set; }

        /// <summary>
        ///     If the image is on the network, copy and run it from the swap file.
        /// </summary>
        public bool NetRunFroMSwap { get; private set; }

        /// <summary>
        ///     The image is a system file.
        /// </summary>
        public bool System { get; private set; }

        /// <summary>
        ///     Is a dynamic loaded library and executable but cannot
        ///     be run on its own.
        /// </summary>
        public bool DLL { get; private set; }

        /// <summary>
        ///     Image should be run only on uniprocessor.
        /// </summary>
        public bool UpSystemOnly { get; private set; }

        /// <summary>
        ///     (OBSOLETE) Reserved.
        /// </summary>
        public bool BytesReversedHi { get; private set; }

        /// <summary>
        ///     Return string representation of all characteristics.
        /// </summary>
        /// <returns>Return string representation of all characteristics.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("File Characteristics\n");
            sb.Append(ExtensionMethods.PropertiesToString(this, "{0,-30}:{1,10:X}\n"));
            return sb.ToString();
        }
    }
}