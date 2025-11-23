// This file helps resolve type conflicts between BizHawk.Common's PolySharp-generated types
// and .NET 9's built-in types by explicitly mapping to .NET 9 implementations.
extern alias bizcommon;

global using IsExternalInit = System.Runtime.CompilerServices.IsExternalInit;
global using RequiresLocationAttribute = System.Runtime.CompilerServices.RequiresLocationAttribute;
