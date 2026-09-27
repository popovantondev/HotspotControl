using HotspotControl.Core;
using HotspotControl.Windows;
using HotspotControl.App;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

int passed = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException($"Prüfung fehlgeschlagen: {description}");
    passed++;
}
async Task Throws<T>(Func<Task> action, string description) where T : Exception
{
    try { await action(); }
    catch (T) { passed++; return; }
    throw new InvalidOperationException($"Erwarteter Fehler fehlt: {description}");
}
async Task WaitIdle(OperationRunner runner)
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    while (runner.IsBusy) await Task.Delay(1, deadline.Token);
}

var cases = new (string Ssid, string Password, bool Valid)[]
{
    ("Testnetz", "", true), ("Testnetz", "12345678", true),
    (new string('A', 32), new string('B', 63), true),
    ("", "12345678", false), ("   ", "12345678", false),
    (new string('A', 33), "12345678", false), ("Testnetz", "1234567", false),
    ("Testnetz", new string('A', 64), false), ("Testnetz", "1234567\n", false),
    ("Testnetz", "Passwört123", false), ("Test\nnetz", "12345678", false)
};
foreach (var item in cases)
    Check((NetworkInput.Validate(item.Ssid, item.Password) is null) == item.Valid, "Eingabevalidierung");

var runner = new OperationRunner();
int idleEvents = 0;
runner.BecameIdle += (_, _) => Interlocked.Increment(ref idleEvents);
Check(await runner.RunAsync(() => Task.FromResult(42), TimeSpan.FromSeconds(1)) == 42, "Ergebnis wird zurückgegeben");
Check(!runner.IsBusy, "Sperre nach Erfolg frei");
await Throws<InvalidOperationException>(() => runner.RunAsync<int>(() => throw new InvalidOperationException(), TimeSpan.FromSeconds(1)), "Synchroner Fehler");
Check(!runner.IsBusy, "Sperre nach Fehler frei");

var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
await Throws<TimeoutException>(() => runner.RunAsync(() => pending.Task, TimeSpan.FromMilliseconds(50)), "Begrenzte Wartezeit");
Check(runner.IsBusy, "Native Anfrage behält Sperre nach Timeout");
int calls = 0;
await Throws<OperationBusyException>(() => runner.RunAsync(() => { calls++; return Task.FromResult(0); }, TimeSpan.FromSeconds(1)), "Keine zweite Aktion nach Timeout");
Check(calls == 0, "Abgewiesene Aktion hat keine Nebenwirkung");
pending.SetResult(1);
await WaitIdle(runner);
Check(Volatile.Read(ref idleEvents) >= 1, "Late operation emits idle notification");
Check(await runner.RunAsync(() => Task.FromResult(7), TimeSpan.FromSeconds(1)) == 7, "Nach verspätetem Ende wieder nutzbar");

using (var cancelled = new CancellationTokenSource())
{
    cancelled.Cancel();
    await Throws<OperationCanceledException>(() => runner.RunAsync(() => { calls++; return Task.FromResult(0); }, TimeSpan.FromSeconds(1), cancelled.Token), "Abbruch vor Start");
    Check(calls == 0 && !runner.IsBusy, "Abgebrochene Anfrage startet nicht");
}
var late = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
using (var cancel = new CancellationTokenSource())
{
    var waiting = runner.RunAsync(() => late.Task, TimeSpan.FromSeconds(1), cancel.Token);
    cancel.Cancel();
    await Throws<OperationCanceledException>(() => waiting, "Abbruch beendet Warten");
    Check(runner.IsBusy, "Abbruch gibt native Sperre nicht vorzeitig frei");
    late.SetException(new InvalidOperationException("Test"));
    await WaitIdle(runner);
}
Check(!runner.IsBusy, "Verspäteter Fehler gibt Sperre frei");

int attempts = 0, delays = 0;
Task Pause(TimeSpan delay, CancellationToken token) { token.ThrowIfCancellationRequested(); delays++; return Task.CompletedTask; }
var success = await AutoStartPolicy.RunAsync(_ => Task.FromResult(++attempts < 3
    ? new StartAttempt(false, true, MessageCode.NoInternetProfile) : new StartAttempt(true, false, MessageCode.Enabled)), Pause);
Check(success.Success && attempts == 3 && delays == 2, "Wartet auf verspätete Internetverbindung");
attempts = delays = 0;
var denied = await AutoStartPolicy.RunAsync(_ => { attempts++; return Task.FromResult(new StartAttempt(false, false, MessageCode.AccessDenied)); }, Pause);
Check(!denied.Success && attempts == 1 && delays == 0, "Kein Wiederholen bei Zugriffsverweigerung");
attempts = delays = 0;
await AutoStartPolicy.RunAsync(_ => { attempts++; return Task.FromResult(new StartAttempt(false, true, MessageCode.NoInternetProfile)); }, Pause, maximumAttempts: 4);
Check(attempts == 4 && delays == 3, "Begrenzte Zahl von Versuchen");
using (var cancel = new CancellationTokenSource())
{
    attempts = 0;
    await Throws<OperationCanceledException>(() => AutoStartPolicy.RunAsync(_ =>
    {
        attempts++; cancel.Cancel(); return Task.FromResult(new StartAttempt(false, true, MessageCode.NoInternetProfile));
    }, Pause, cancellationToken: cancel.Token), "Manuelles Handeln bricht Autostart ab");
    Check(attempts == 1, "Keine Folgeaktion nach Abbruch");
}
Check(!WindowsHotspotService.DescribeNativeStatus(5).Retryable, "Dauerhafter Anbieterfehler wird nicht wiederholt");
Check(WindowsHotspotService.DescribeNativeStatus(8).Retryable, "Vorübergehende Verbindung darf wiederholt werden");
Check(!WindowsHotspotService.DescribeNativeStatus(999).Success && !WindowsHotspotService.DescribeNativeStatus(999).Retryable,
    "Unbekannter Windows-Code bleibt Fehler");
Check(WindowsHotspotService.DescribeError(new Exception("SECRET NETWORK DATA")).Code == MessageCode.NativeFailure,
    "Fehlermeldung enthält keine privaten Exception-Daten");

foreach (var ssid in new[] { "", " Demo", "Demo ", new string('A', 33), "Demo\n", "Dém o" })
    Check(NetworkInput.Validate(ssid, "") == MessageCode.InvalidSsid, "SSID rejected before native call");
foreach (var password in new[] { "1234567", new string('A', 64), "pass\nword", "pässword123" })
    Check(NetworkInput.Validate("Demo", password) == MessageCode.InvalidPassword, "Password rejected before native call");
Check(NetworkInput.Validate("A", "") is null && NetworkInput.Validate(new string('A', 32), new string('B', 63)) is null, "Boundary input accepted");
Check(NetworkSettingsGuard.Check("old", "new", HotspotControl.Core.Contracts.HotspotState.Off, "Demo", "", 0, [0]) == MessageCode.ContextChanged,
    "Changed profile blocks configuration");
Check(NetworkSettingsGuard.Check("old", null, HotspotControl.Core.Contracts.HotspotState.Off, "Demo", "", 0, [0]) == MessageCode.ContextChanged,
    "Unknown profile blocks configuration");
Check(NetworkSettingsGuard.Check("same", "same", HotspotControl.Core.Contracts.HotspotState.On, "Demo", "", 0, [0]) == MessageCode.MustTurnOff,
    "Enabled hotspot blocks configuration");
Check(NetworkSettingsGuard.Check("same", "same", HotspotControl.Core.Contracts.HotspotState.Off, "Demo", "", 2, [0]) == MessageCode.UnsupportedBand,
    "Unsupported band blocks configuration");
Check(NetworkSettingsGuard.Check("same", "same", HotspotControl.Core.Contracts.HotspotState.Off, "Demo", "", 0, [0]) is null,
    "Valid context and settings accepted");
var unknownDevice = DeviceProjection.FromReported(1, [null, ""], null);
Check(unknownDevice.HostNames.Count == 0 && unknownDevice.MacAddress is null,
    "Missing device fields remain unknown");
var nativeBoundary = new WindowsHotspotService();
Check((await nativeBoundary.SaveSettingsAsync("test", " Demo", "", 0)).Code == MessageCode.InvalidSsid &&
      nativeBoundary.OperationState == OperationState.Idle,
    "Invalid input is rejected without starting a Windows request");
Check((await nativeBoundary.SaveSettingsAsync("test", "Demo", "", 9)).Code == MessageCode.UnsupportedBand &&
      nativeBoundary.OperationState == OperationState.Idle,
    "Invalid band is rejected without starting a Windows request");

var preferencesDirectory = Path.Combine(Path.GetTempPath(), "HotspotControl-Checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(preferencesDirectory);
try
{
    var path = Path.Combine(preferencesDirectory, "preferences.json");
    var file = new PreferencesFile(path);
    Check(file.Read().Status == PreferencesReadStatus.Missing && !file.Read().Preferences.AutoEnableHotspot, "Missing preferences safe");
    File.WriteAllText(path, "{\"AutoEnableHotspot\":true}");
    Check(file.Read().Preferences.AutoEnableHotspot, "Old true retained");
    var migrated = file.Update(current => current with { Language = "ru" });
    Check(migrated.AutoEnableHotspot && file.Read().Preferences.Language == "ru" && file.Read().Preferences.SchemaVersion == 1, "Migration retains adjacent field");
    File.WriteAllText(path, "{\"AutoEnableHotspot\":false}");
    Check(!file.Read().Preferences.AutoEnableHotspot, "Old false retained");
    foreach (var invalid in new[] { "null", "{", "{\"AutoEnableHotspot\":\"true\"}",
        "{\"AutoEnableHotspot\":null}", "{\"Language\":\"fr\"}", "{\"SchemaVersion\":\"1\"}" })
    {
        File.WriteAllText(path, invalid);
        Check(file.Read().Status == PreferencesReadStatus.Invalid && !file.Read().Preferences.AutoEnableHotspot, "Invalid preferences disable auto-start");
    }
    var recoveryCount = Directory.GetFiles(preferencesDirectory, "*.recovery-*").Length;
    file.Update(current => current with { Language = "de" });
    Check(Directory.GetFiles(preferencesDirectory, "*.recovery-*").Length == recoveryCount + 1, "Corrupt file preserved before explicit update");
    File.WriteAllText(path, "{\"SchemaVersion\":2,\"AutoEnableHotspot\":true}");
    Check(file.Read().Status == PreferencesReadStatus.FutureVersion, "Future schema detected");
    await Throws<InvalidOperationException>(() => Task.Run(() => file.Update(current => current with { Language = "en" })), "Future schema not overwritten");
    Check(File.ReadAllText(path).Contains("\"SchemaVersion\":2"), "Future file unchanged");
    var blockedParent = Path.Combine(preferencesDirectory, "blocked-parent");
    File.WriteAllText(blockedParent, "unchanged");
    var blockedFile = new PreferencesFile(Path.Combine(blockedParent, "preferences.json"));
    await Throws<IOException>(() => Task.Run(() => blockedFile.Update(current => current with { Language = "en" })), "Failed write is observable");
    Check(File.ReadAllText(blockedParent) == "unchanged", "Failed write preserves original file");
}
finally { Directory.Delete(preferencesDirectory, true); }

Check(StartupShortcutPolicy.Evaluate(false, null, null, "C:\\Apps\\Hotspot.exe", "", _ => true) == StartupShortcutState.Disabled,
    "Missing shortcut disabled");
Check(StartupShortcutPolicy.Evaluate(true, "C:\\Old\\Hotspot.exe", "", "C:\\Apps\\Hotspot.exe", "", _ => false) == StartupShortcutState.NeedsRepair,
    "Stale shortcut requests repair");
Check(StartupShortcutPolicy.Evaluate(true, "C:\\Apps\\Hotspot.exe", "", "C:\\Apps\\Hotspot.exe", "", _ => true) == StartupShortcutState.Enabled,
    "Current shortcut enabled");
Check(StartupShortcutPolicy.Evaluate(true, "C:\\Apps\\Hotspot.exe", "--wrong", "C:\\Apps\\Hotspot.exe", "", _ => true) == StartupShortcutState.NeedsRepair,
    "Wrong shortcut arguments request repair");
var instanceName = @"Local\HotspotControl.Checks." + Guid.NewGuid().ToString("N");
int restoreRequests = 0;
using (var first = InstanceCoordinator.Acquire(instanceName))
{
    Check(first.IsFirst, "First instance owns mutex");
    first.Listen(() => Interlocked.Increment(ref restoreRequests));
    using var second = InstanceCoordinator.Acquire(instanceName);
    Check(!second.IsFirst, "Second instance does not own mutex");
    second.NotifyFirst();
    Check(SpinWait.SpinUntil(() => Volatile.Read(ref restoreRequests) == 1, TimeSpan.FromSeconds(2)),
        "Second instance signals first instance");
}
using (var third = InstanceCoordinator.Acquire(instanceName))
    Check(third.IsFirst, "Instance resources released after shutdown");
Check(!StartupDecision.MayAutoStart(false, PreferencesReadStatus.Valid, new(true)),
    "No automatic network action before choosing a language");
Check(!StartupDecision.MayAutoStart(true, PreferencesReadStatus.Valid, new(true, "de")),
    "No auto start flag takes precedence");
Check(!StartupDecision.MayAutoStart(false, PreferencesReadStatus.Invalid, new(true, "de")),
    "Corrupt preferences disable automatic start");
Check(StartupDecision.MayAutoStart(false, PreferencesReadStatus.Valid, new(true, "ru")),
    "Existing automatic start survives language migration");

HashSet<string> ResourceKeys(CultureInfo culture) =>
    TextCatalog.Resources.GetResourceSet(culture, true, false)!.Cast<DictionaryEntry>()
        .Select(item => (string)item.Key).ToHashSet(StringComparer.Ordinal);
var baseKeys = ResourceKeys(CultureInfo.InvariantCulture);
foreach (var language in TextCatalog.Languages)
{
    var catalog = new TextCatalog(language);
    var localKeys = ResourceKeys(language == "en" ? CultureInfo.InvariantCulture : catalog.Culture);
    Check(baseKeys.SetEquals(localKeys), "Translation key parity: " + language);
    foreach (var key in baseKeys)
    {
        var placeholders = Regex.Matches(TextCatalog.Resources.GetString(key, CultureInfo.InvariantCulture)!, @"\{\d+\}")
            .Select(match => match.Value).ToHashSet();
        var translated = Regex.Matches(catalog[key], @"\{\d+\}")
            .Select(match => match.Value).ToHashSet();
        Check(placeholders.SetEquals(translated), "Translation parameter parity: " + key + "/" + language);
        _ = string.Format(catalog.Culture, catalog[key], "1", "2", "3");
    }
}
Check(new TextCatalog("fr").Language == "en", "Unsupported language falls back to English");
Console.WriteLine($"{passed} Prüfungen bestanden. Keine Netzwerkänderungen.");
