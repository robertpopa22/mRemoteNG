using System.Reflection;

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// Pin a fixed AssemblyVersion. The '*' wildcard (with Deterministic=false) generated a new
// build/revision on every compile, so mRemoteNG.exe's embedded strong reference could disagree
// with the ObjectListView.dll shipped in a build, causing a runtime
// "Could not load file or assembly 'ObjectListView, Version=2.9.3.*'" FileNotFoundException (#122).
[assembly: AssemblyVersion("2.9.3.0")]
[assembly: AssemblyFileVersion("2.9.3")]
[assembly: AssemblyInformationalVersion("2.9.3")]
[assembly: System.CLSCompliant(true)]