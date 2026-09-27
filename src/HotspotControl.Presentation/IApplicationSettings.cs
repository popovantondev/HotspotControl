using HotspotControl.Core.Contracts;

namespace HotspotControl.Presentation;

public enum StartupLinkState { Disabled, Enabled, NeedsRepair, Unavailable }

public interface IApplicationSettings
{
    StartupLinkState StartupState { get; }
    bool AutoEnableHotspot { get; }
    string Language { get; }
    MessageCode? SetStartWithWindows(bool enabled);
    MessageCode? SetAutoEnableHotspot(bool enabled);
    MessageCode? SetLanguage(string language);
}
