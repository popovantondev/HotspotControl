using HotspotControl.Windows;
var cases = new (string Name, string Ssid, string Password, bool Valid)[]
{
    ("Keep existing password", "Testnetz", "", true),
    ("Minimum password", "Testnetz", "12345678", true),
    ("Maximum lengths", new string('A', 32), new string('B', 63), true),
    ("Empty name", "", "12345678", false),
    ("Long name", new string('A', 33), "12345678", false),
    ("Short password", "Testnetz", "1234567", false),
    ("Long password", "Testnetz", new string('A', 64), false),
    ("Control character", "Testnetz", "1234567\n", false),
    ("Non ASCII password", "Testnetz", "Passwört123", false)
};
foreach (var item in cases)
    if ((HotspotActions.ValidateSettings(item.Ssid, item.Password) is null) != item.Valid)
        throw new Exception("Validierungsprüfung fehlgeschlagen.");
Console.WriteLine($"{cases.Length} Validierungsprüfungen erfolgreich. Keine Netzwerkänderungen.");
