using HotspotControl.Core;
using HotspotControl.Windows;

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
    Check((HotspotActions.ValidateSettings(item.Ssid, item.Password) is null) == item.Valid, "Eingabevalidierung");

var runner = new OperationRunner();
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
    ? new StartAttempt(false, true, "Warten") : new StartAttempt(true, false, "Erfolg")), Pause);
Check(success.Success && attempts == 3 && delays == 2, "Wartet auf verspätete Internetverbindung");
attempts = delays = 0;
var denied = await AutoStartPolicy.RunAsync(_ => { attempts++; return Task.FromResult(new StartAttempt(false, false, "Verweigert")); }, Pause);
Check(!denied.Success && attempts == 1 && delays == 0, "Kein Wiederholen bei Zugriffsverweigerung");
attempts = delays = 0;
await AutoStartPolicy.RunAsync(_ => { attempts++; return Task.FromResult(new StartAttempt(false, true, "Warten")); }, Pause, maximumAttempts: 4);
Check(attempts == 4 && delays == 3, "Begrenzte Zahl von Versuchen");
using (var cancel = new CancellationTokenSource())
{
    attempts = 0;
    await Throws<OperationCanceledException>(() => AutoStartPolicy.RunAsync(_ =>
    {
        attempts++; cancel.Cancel(); return Task.FromResult(new StartAttempt(false, true, "Warten"));
    }, Pause, cancellationToken: cancel.Token), "Manuelles Handeln bricht Autostart ab");
    Check(attempts == 1, "Keine Folgeaktion nach Abbruch");
}
var message = new StatusMessage();
message.SetOperation("Windows erlaubt diese Aktion nicht.");
Check(message.ForRefresh("Aktualisiert") == "Windows erlaubt diese Aktion nicht.", "Automatisches Aktualisieren bewahrt Fehler");
message.ClearOperation();
Check(message.ForRefresh("Aktualisiert") == "Aktualisiert", "Manuelles Aktualisieren kann Meldung quittieren");
Check(!HotspotActions.DescribeStatus(5).Retryable, "Dauerhafter Anbieterfehler wird nicht wiederholt");
Check(HotspotActions.DescribeStatus(8).Retryable, "Vorübergehende Verbindung darf wiederholt werden");
Check(!HotspotActions.DescribeStatus(999).Success && !HotspotActions.DescribeStatus(999).Retryable, "Unbekannter Windows-Code bleibt Fehler");
Check(!HotspotActions.Error(new Exception("SECRET NETWORK DATA")).Message.Contains("SECRET"), "Fehlermeldung enthält keine privaten Exception-Daten");
Console.WriteLine($"{passed} Prüfungen bestanden. Keine Netzwerkänderungen.");
