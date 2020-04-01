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

namespace PeNet
{
    /// <summary>
    /// Constants from the .Net header.
    /// </summary>
    public static class DotNetConstants
    {
        /// <summary>
        /// IMAGE_COR20_HEADER Flags
        /// </summary>
        [Flags]
        public enum COMImageFlag : uint
        {
            /// <summary>
            /// Intermediate language only flag.
            /// </summary>
            COMIMAGE_FLAGS_ILONLY = 0x00000001,

            /// <summary>
            /// 32 bit required flag.
            /// </summary>
            COMIMAGE_FLAGS_32BITREQUIRED = 0x00000002,

            /// <summary>
            /// Intermediate language library flag.
            /// </summary>
            COMIMAGE_FLAGS_IL_LIBRARY = 0x00000004,

            /// <summary>
            /// Strong named signed flag.
            /// </summary>
            COMIMAGE_FLAGS_STRONGNAMESIGNED = 0x00000008,

            /// <summary>
            /// Native entry point flag.
            /// </summary>
            COMIMAGE_FLAGS_NATIVE_ENTRYPOINT = 0x00000010,

            /// <summary>
            /// Track debug data flag.
            /// </summary>
            COMIMAGE_FLAGS_TRACKDEBUGDATA = 0x00010000
        };

        /// <summary>
        /// MaskValid flags from the METADATATABELSHDR.
        /// The flags show, which tables are present.
        /// </summary>
        [Flags]
        public enum MaskValidFlags : ulong
        {
            /// <summary>
            /// Table Module is present.
            /// </summary>
            Module = 0x1,

            /// <summary>
            /// Table TypeRef is present.
            /// </summary>
            TypeRef = 0x2,

            /// <summary>
            /// Table TypeDef is present.
            /// </summary>
            TypeDef = 0x4,

            /// <summary>
            /// Table Field is present.
            /// </summary>
            Field = 0x10,

            /// <summary>
            /// Table MethodDef is present.
            /// </summary>
            MethodDef = 0x40,

            /// <summary>
            /// Table Param is present.
            /// </summary>
            Param = 0x100,

            /// <summary>
            /// Table InterfaceImpl is present.
            /// </summary>
            InterfaceImpl = 0x200,

            /// <summary>
            /// Table MemberRef is present.
            /// </summary>
            MemberRef = 0x400,

            /// <summary>
            /// Table Constant is present.
            /// </summary>
            Constant = 0x800,

            /// <summary>
            /// Table CustomAttribute is present.
            /// </summary>
            CustomAttribute = 0x1000,

            /// <summary>
            /// Table FieldMarshal is present.
            /// </summary>
            FieldMarshal = 0x2000,

            /// <summary>
            /// Table DeclSecurity is present.
            /// </summary>
            DeclSecurity = 0x4000,

            /// <summary>
            /// Table ClassLayout is present.
            /// </summary>
            ClassLayout = 0x8000,

            /// <summary>
            /// Table FieldLayout is present.
            /// </summary>
            FieldLayout = 0x10000,

            /// <summary>
            /// Table StandAloneSig is present.
            /// </summary>
            StandAloneSig = 0x20000,

            /// <summary>
            /// Table EventMap is present.
            /// </summary>
            EventMap = 0x40000,

            /// <summary>
            /// Table Event is present.
            /// </summary>
            Event = 0x100000,

            /// <summary>
            /// Table PropertyMap is present.
            /// </summary>
            PropertyMap = 0x200000,

            /// <summary>
            /// Table Property is present.
            /// </summary>
            Property = 0x800000,

            /// <summary>
            /// Table MethodSemantics is present.
            /// </summary>
            MethodSemantics = 0x1000000,

            /// <summary>
            /// Table MethodImpl is present.
            /// </summary>
            MethodImpl = 0x2000000,

            /// <summary>
            /// Table ModuleRef is present.
            /// </summary>
            ModuleRef = 0x4000000,

            /// <summary>
            /// Table TypeSpec is present.
            /// </summary>
            TypeSpec = 0x8000000,

            /// <summary>
            /// Table ImplMap is present.
            /// </summary>
            ImplMap = 0x10000000,

            /// <summary>
            /// Table FieldRVA is present.
            /// </summary>
            FieldRVA = 0x20000000,

            /// <summary>
            /// Table Assembly is present.
            /// </summary>
            Assembly = 0x100000000,

            /// <summary>
            /// Table AssemblyProcessor is present.
            /// </summary>
            AssemblyProcessor = 0x200000000,

            /// <summary>
            /// Table AssemblyOS is present.
            /// </summary>
            AssemblyOS = 0x400000000,

            /// <summary>
            /// Table AssemblyRef is present.
            /// </summary>
            AssemblyRef = 0x800000000,

            /// <summary>
            /// Table AssemblyRefProcessor is present.
            /// </summary>
            AssemblyRefProcessor = 0x1000000000,

            /// <summary>
            /// Table AssemblyRefOS is present.
            /// </summary>
            AssemblyRefOS = 0x2000000000,

            /// <summary>
            /// Table File is present.
            /// </summary>
            File = 0x4000000000,

            /// <summary>
            /// Table ExportedType is present.
            /// </summary>
            ExportedType = 0x8000000000,

            /// <summary>
            /// Table ManifestResource is present.
            /// </summary>
            ManifestResource = 0x10000000000,

            /// <summary>
            /// Table NestedClass is present.
            /// </summary>
            NestedClass = 0x20000000000,

            /// <summary>
            /// Table GenericParam is present.
            /// </summary>
            GenericParam = 0x40000000000,

            /// <summary>
            /// Table MethodSpec is present
            /// </summary>
            MethodSpec = 0x80000000000,

            /// <summary>
            /// Table GenericParamConstraint is present.
            /// </summary>
            GenericParamConstraint = 0x100000000000
        }

        //////////////////////////////////// 
        // Meta Data Tables Header Constants
        ////////////////////////////////////

        /// <summary>
        /// Lower 2 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum TypeDefOrRef : uint
        {
            /// <summary>
            /// Index into TypeDef table.
            /// </summary>
            TypeDef = 0,

            /// <summary>
            /// Index into TypeRef table.
            /// </summary>
            TypeRef = 1,

            /// <summary>
            /// Index into TypeSpec table.
            /// </summary>
            TypeSpec = 2
        }

        /// <summary>
        /// Lower 2 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum HasConstant : uint
        {
            /// <summary>
            /// Index into FieldDef.
            /// </summary>
            FieldDef = 0,

            /// <summary>
            /// Index into ParamDef.
            /// </summary>
            ParamDef = 1,

            /// <summary>
            /// Index into Property.
            /// </summary>
            Property = 2
        }

        /// <summary>
        /// Lower 5 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum HasCustomAttribute : uint
        {
            /// <summary>
            /// Index into MethodDef.
            /// </summary>
            MethodDef = 0,

            /// <summary>
            /// Index into FieldDef.
            /// </summary>
            FieldDef = 1,

            /// <summary>
            /// Index into TypeRef.
            /// </summary>
            TypeRef = 2,

            /// <summary>
            /// Index into TypeDef.
            /// </summary>
            TypeDef = 3,

            /// <summary>
            /// Index into ParamDef.
            /// </summary>
            ParamDef = 4,

            /// <summary>
            /// Index into InterfaceImpl.
            /// </summary>
            InterfaceImpl = 5,

            /// <summary>
            /// Index into MemberRef.
            /// </summary>
            MemberRef = 6,

            /// <summary>
            /// Index into Module.
            /// </summary>
            Module = 7,

            /// <summary>
            /// Index into Permission.
            /// </summary>
            Permission = 8,

            /// <summary>
            /// Index into Property.
            /// </summary>
            Property = 9,

            /// <summary>
            /// Index into Event.
            /// </summary>
            Event = 10,

            /// <summary>
            /// Index into StandAloneSig.
            /// </summary>
            StandAloneSig = 11,

            /// <summary>
            /// Index into ModuleRef.
            /// </summary>
            ModuleRef = 12,

            /// <summary>
            /// Index into TypeSpec.
            /// </summary>
            TypeSpec = 13,

            /// <summary>
            /// Index into Assembly.
            /// </summary>
            Assembly = 14,

            /// <summary>
            /// Index into AssemblyRef.
            /// </summary>
            AssemblyRef = 15,

            /// <summary>
            /// Index into File.
            /// </summary>
            File = 16,

            /// <summary>
            /// Index into ExportType.
            /// </summary>
            ExportedType = 17,

            /// <summary>
            /// Index into ManifestResource.
            /// </summary>
            ManifestResource = 18
        }

        /// <summary>
        /// Lower 2 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum HasFieldMarshall : uint
        {
            /// <summary>
            /// Index into FieldDef.
            /// </summary>
            FieldDef = 0,

            /// <summary>
            /// Index into ParamDef.
            /// </summary>
            ParamDef = 1
        }

        /// <summary>
        /// Lower 2 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum HasDeclSecurity : uint
        {
            /// <summary>
            /// Index into TypeDef.
            /// </summary>
            TypeDef = 0,

            /// <summary>
            /// Index into MethodDef.
            /// </summary>
            MethodDef = 1,

            /// <summary>
            /// Index into Assembly.
            /// </summary>
            Assembly = 2
        }

        /// <summary>
        /// Lower 3 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum MemberRefParent : uint
        {
            /// <summary>
            /// Index into TypeDef.
            /// </summary>
            TypeDef = 0,

            /// <summary>
            /// Index into TypeRef.
            /// </summary>
            TypeRef = 1,

            /// <summary>
            /// Index into ModuleRef.
            /// </summary>
            ModuleRef = 2,

            /// <summary>
            /// Index into MethodDef.
            /// </summary>
            MethodDef = 3,

            /// <summary>
            /// Index into TypeSpec.
            /// </summary>
            TypeSpec = 4
        }

        /// <summary>
        /// Lowest bit of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum HasSemantics : uint
        {
            /// <summary>
            /// Index into Event.
            /// </summary>
            Event = 0,

            /// <summary>
            /// Index into Property.
            /// </summary>
            Property = 1
        }

        /// <summary>
        /// Lowest bit of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum MethodDefOrRef : uint
        {
            /// <summary>
            /// Index into MethodDef.
            /// </summary>
            MethodDef = 0,

            /// <summary>
            /// Index into MemberRef.
            /// </summary>
            MemberRef = 1
        }

        /// <summary>
        /// Lowest bit of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum MemberForwarded : uint
        {
            /// <summary>
            /// Index into FieldDef.
            /// </summary>
            FieldDef = 0,

            /// <summary>
            /// Index into MethodDef.
            /// </summary>
            MethodDef = 1
        }

        /// <summary>
        /// Lower 3 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum CustomAttributeType : uint
        {
            /// <summary>
            /// Not used.
            /// </summary>
            NotUsed1 = 0,

            /// <summary>
            /// Not used.
            /// </summary>
            NotUsed2 = 1,

            /// <summary>
            /// Index into MethodDef.
            /// </summary>
            MethodDef = 2,

            /// <summary>
            /// Index into MemberRef.
            /// </summary>
            MemberRef = 3,

            /// <summary>
            /// Not used.
            /// </summary>
            NotUsed3 = 4
        }

        /// <summary>
        /// Lower 2 bits of Meta Data Tables index
        /// which specify to what the index points.
        /// </summary>
        public enum ResolutionScope : uint
        {
            /// <summary>
            /// Index into Module.
            /// </summary>
            Module = 0,

            /// <summary>
            /// Index into ModuleRef.
            /// </summary>
            ModuleRef = 1,

            /// <summary>
            /// Index into AssemblyRef.
            /// </summary>
            AssemblyRef = 2,

            /// <summary>
            /// Index into TypeRef.
            /// </summary>
            TypeRef = 3
        }
    }
}