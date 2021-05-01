using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle ("PlotterWriterConsoleUI")]
[assembly: AssemblyProduct ("PlotterWriter")]
[assembly: AssemblyDescription ("PlotterWriter console user interface")]
#if (_x64 || _WIN64)
    #if DEBUG
        [assembly: AssemblyConfiguration ("Debug x64")]
    #else
        [assembly: AssemblyConfiguration ("Release x64")]
    #endif
#elif _x86
    #if DEBUG
        [assembly: AssemblyConfiguration ("Debug x86")]
    #else
        [assembly: AssemblyConfiguration ("Release x86")]
    #endif
#elif Win32
    #if DEBUG
        [assembly: AssemblyConfiguration ("Debug Win32")]
    #else
        [assembly: AssemblyConfiguration ("Release Win32")]
    #endif
#else
    #if DEBUG
        [assembly: AssemblyConfiguration ("Debug")]
    #else
        [assembly: AssemblyConfiguration ("Release")]
    #endif
#endif
[assembly: AssemblyCompany ("Sacred Cat Software")]
[assembly: AssemblyCopyright ("Copyright © Sacred Cat Software 2021")]
[assembly: AssemblyTrademark ("SCatSoft")]
[assembly: AssemblyCulture ("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible (false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid ("22b266e5-f104-4c79-889b-0b5cbeb6167f")]

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
[assembly: AssemblyVersion ("0.2.4.01")]
[assembly: AssemblyFileVersion ("0.1.2.01")]
