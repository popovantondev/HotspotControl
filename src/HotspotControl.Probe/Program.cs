using System.Security.Principal;
using HotspotControl.Probe;

using var identity = WindowsIdentity.GetCurrent();
if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.WriteLine("Bitte ohne Administratorrechte starten; sonst ist die Prüfung nicht aussagekräftig.");
    return 3;
}
Console.WriteLine("Hotspot Control — nur lesende Prüfung, ohne Administratorrechte");
var result = HotspotReader.Read();
Console.WriteLine(result.Message);
return result.ExitCode;
