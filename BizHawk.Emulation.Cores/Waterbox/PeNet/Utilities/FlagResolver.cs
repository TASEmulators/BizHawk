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

using System;
using System.Collections.Generic;
using System.Text;

namespace PeNet.Utilities
{
    /// <summary>
    /// Parser functions for different flags in the PE header.
    /// </summary>
    public static class FlagResolver
    {
        /// <summary>
        ///     Converts the section name (UTF-8 byte array) to a string.
        /// </summary>
        /// <param name="name">Section name byte array.</param>
        /// <returns>String representation of the section name.</returns>
        public static string ResolveSectionName(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd((char) 0);
        }

        /// <summary>
        ///     Resolves the target machine number to a string containing
        ///     the name of the target machine.
        /// </summary>
        /// <param name="targetMachine">Target machine value from the COFF header.</param>
        /// <returns>Name of the target machine as string.</returns>
        public static string ResolveTargetMachine(ushort targetMachine)
        {
            var tm = "unknown";
            switch (targetMachine)
            {
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_I386:
                    tm = "Intel 386";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_I860:
                    tm = "Intel i860";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_R3000:
                    tm = "MIPS R3000";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_R4000:
                    tm = "MIPS little endian (R4000)";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_R10000:
                    tm = "MIPS R10000";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_WCEMIPSV2:
                    tm = "MIPS little endian WCI v2";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_OLDALPHA:
                    tm = "old Alpha AXP";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_ALPHA:
                    tm = "Alpha AXP";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_SH3:
                    tm = "Hitachi SH3";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_SH3DSP:
                    tm = "Hitachi SH3 DSP";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_SH3E:
                    tm = "Hitachi SH3E";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_SH4:
                    tm = "Hitachi SH4";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_SH5:
                    tm = "Hitachi SH5";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_ARM:
                    tm = "ARM little endian";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_THUMB:
                    tm = "Thumb";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_AM33:
                    tm = "Matsushita AM33";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_POWERPC:
                    tm = "PowerPC little endian";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_POWERPCFP:
                    tm = "PowerPC with floating point support";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_IA64:
                    tm = "Intel IA64";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_MIPS16:
                    tm = "MIPS16";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_M68K:
                    tm = "Motorola 68000 series";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_ALPHA64:
                    tm = "Alpha AXP 64-bit";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_MIPSFPU:
                    tm = "MIPS with FPU";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_TRICORE:
                    tm = "Tricore";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_CEF:
                    tm = "CEF";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_MIPSFPU16:
                    tm = "MIPS16 with FPU";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_EBC:
                    tm = "EFI Byte Code";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_AMD64:
                    tm = "AMD AMD64";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_M32R:
                    tm = "Mitsubishi M32R little endian";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_CEE:
                    tm = "clr pure MSIL";
                    break;
            }

            return tm;
        }

        /// <summary>
        ///     Resolves the characteristics attribute from the COFF header to an
        ///     object which holds all the characteristics a boolean properties.
        /// </summary>
        /// <param name="characteristics">File header characteristics.</param>
        /// <returns>Object with all characteristics as boolean properties.</returns>
        public static FileCharacteristics ResolveFileCharacteristics(ushort characteristics)
        {
            return new FileCharacteristics(characteristics);
        }

        /// <summary>
        ///     Resolve the resource identifier of resource entries
        ///     to a human readable string with a meaning.
        /// </summary>
        /// <param name="id">Resource identifier.</param>
        /// <returns>String representation of the ID.</returns>
        public static string ResolveResourceId(uint id)
        {
            switch (id)
            {
                case (uint) Constants.ResourceGroupIDs.Cursor:
                    return "Cursor";
                case (uint) Constants.ResourceGroupIDs.Bitmap:
                    return "Bitmap";
                case (uint) Constants.ResourceGroupIDs.Icon:
                    return "Icon";
                case (uint) Constants.ResourceGroupIDs.Menu:
                    return "Menu";
                case (uint) Constants.ResourceGroupIDs.Dialog:
                    return "Dialog";
                case (uint) Constants.ResourceGroupIDs.String:
                    return "String";
                case (uint) Constants.ResourceGroupIDs.FontDirectory:
                    return "FontDirectory";
                case (uint) Constants.ResourceGroupIDs.Fonst:
                    return "Fonst";
                case (uint) Constants.ResourceGroupIDs.Accelerator:
                    return "Accelerator";
                case (uint) Constants.ResourceGroupIDs.RcData:
                    return "RcData";
                case (uint) Constants.ResourceGroupIDs.MessageTable:
                    return "MessageTable";
                case (uint) Constants.ResourceGroupIDs.GroupIcon:
                    return "GroupIcon";
                case (uint) Constants.ResourceGroupIDs.Version:
                    return "Version";
                case (uint) Constants.ResourceGroupIDs.DlgInclude:
                    return "DlgInclude";
                case (uint) Constants.ResourceGroupIDs.PlugAndPlay:
                    return "PlugAndPlay";
                case (uint) Constants.ResourceGroupIDs.VXD:
                    return "VXD";
                case (uint) Constants.ResourceGroupIDs.AnimatedCurser:
                    return "AnimatedCurser";
                case (uint) Constants.ResourceGroupIDs.AnimatedIcon:
                    return "AnimatedIcon";
                case (uint) Constants.ResourceGroupIDs.HTML:
                    return "HTML";
                case (uint) Constants.ResourceGroupIDs.Manifest:
                    return "Manifest";
                default:
                    return "unknown";
            }
        }

        /// <summary>
        ///     Resolve the subsystem attribute to a human readable string.
        /// </summary>
        /// <param name="subsystem">Subsystem attribute.</param>
        /// <returns>Subsystem as readable string.</returns>
        public static string ResolveSubsystem(ushort subsystem)
        {
            var ss = "unknown";
            switch (subsystem)
            {
                case 1:
                    ss = "native";
                    break;
                case 2:
                    ss = "Windows/GUI";
                    break;
                case 3:
                    ss = "Windows non-GUI";
                    break;
                case 5:
                    ss = "OS/2";
                    break;
                case 7:
                    ss = "POSIX";
                    break;
                case 8:
                    ss = "Native Windows 9x Driver";
                    break;
                case 9:
                    ss = "Windows CE";
                    break;
                case 0xA:
                    ss = "EFI Application";
                    break;
                case 0xB:
                    ss = "EFI boot service device";
                    break;
                case 0xC:
                    ss = "EFI runtime driver";
                    break;
                case 0xD:
                    ss = "EFI ROM";
                    break;
                case 0xE:
                    ss = "XBox";
                    break;
            }
            return ss;
        }

        /// <summary>
        ///     Resolves the section flags to human readable strings.
        /// </summary>
        /// <param name="sectionFlags">Sections flags from the SectionHeader object.</param>
        /// <returns>List with flag names for the section.</returns>
        public static List<string> ResolveSectionFlags(uint sectionFlags)
        {
            var st = new List<string>();
            foreach (var flag in (Constants.SectionFlags[]) Enum.GetValues(typeof(Constants.SectionFlags)))
            {
                if ((sectionFlags & (uint) flag) == (uint) flag)
                {
                    st.Add(flag.ToString());
                }
            }
            return st;
        }

        /// <summary>
        ///     Resolve flags from the IMAGE_COR20_HEADER COM+ 2 (CLI) header to
        ///     their string representation.
        /// </summary>
        /// <param name="comImageFlags">Flags from IMAGE_COR20_HEADER.</param>
        /// <returns>List with resolved flag names.</returns>
        public static List<string> ResolveCOMImageFlags(uint comImageFlags)
        {
            var st = new List<string>();
            foreach (var flag in (DotNetConstants.COMImageFlag[]) Enum.GetValues(typeof(DotNetConstants.COMImageFlag)))
            {
                if ((comImageFlags & (uint) flag) == (uint) flag)
                {
                    st.Add(flag.ToString());
                }
            }
            return st;
        }

        /// <summary>
        ///     Resolve which tables are present in the .Net header based
        ///     on the MaskValid flags from the METADATATABLESHDR.
        /// </summary>
        /// <param name="maskValid">MaskValid value from the METADATATABLESHDR</param>
        /// <returns>List with present table names.</returns>
        public static List<string> ResolveMaskValidFlags(ulong maskValid)
        {
            var st = new List<string>();
            foreach (var flag in (DotNetConstants.MaskValidFlags[]) Enum.GetValues(typeof(DotNetConstants.MaskValidFlags)))
            {
                if ((maskValid & (ulong) flag) == (ulong) flag)
                {
                    st.Add(flag.ToString());
                }
            }
            return st;
        }
    }
}