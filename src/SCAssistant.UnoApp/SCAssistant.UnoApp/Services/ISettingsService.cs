using SCAssistant.UnoApp.Models;

namespace SCAssistant.UnoApp.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Save();
    void Load();
}
