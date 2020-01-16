using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BizHawk.Client.ApiHawk;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("HelloWorld")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("HelloWorld")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//As you can see this is a dedicated attribute. You can write the name of your tool, a small description
//and give an icon. The icon should be compiled as an embedded resource and you must give the entire path
[assembly: BizHawkExternalTool("Hello World", "My first External tool for BizHawk emulator", "icon_Hello.ico")]

//This attribute say what the is for. here, I don't anything
//It's equivalent to [assembly: BizHawkExternalToolUsage(BizHawkExternalToolUsage.Global, string.Empty)]
//Here is an example for an emulator specific: BizHawkExternalToolUsage(BizHawkExternalToolUsage.EmulatorSpecific, EmulatedSystem.NES)]
//Here is an example for an game specific: BizHawkExternalToolUsage(BizHawkExternalToolUsage.GameSpecific, "6B47BB75D16514B6A476AA0C73A683A2A4C18765")] => Super Mario World (USA)
//By setting this, your tool is contextualized, that mean you can't load it if emulator is in state you don't want
//It avoid crash
[assembly: BizHawkExternalToolUsage()]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("288d598f-1019-4ea2-802d-14d3cc73ee90")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
