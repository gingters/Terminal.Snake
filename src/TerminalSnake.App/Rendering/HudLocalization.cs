namespace TerminalSnake.Rendering;

public sealed record HudStrings(
    string Title,
    string LevelLabel,
    string DemoIndicator,
    string PressHForHelp,
    string HelpTab,
    string HelpEnter,
    string HelpR,
    string HelpH,
    string HelpD,
    string HelpQ,
    string HelpLevels);

public static class HudLocalization
{
    private const string Fallback = "en";

    // English is the baseline; other entries only need to override the
    // language-sensitive copy. All keys share the same structure so a new
    // locale is a single dictionary entry.
    private static readonly IReadOnlyDictionary<string, HudStrings> Locales =
        new Dictionary<string, HudStrings>(StringComparer.OrdinalIgnoreCase)
        {
            [Fallback] = new(
                Title: "TerminalSnake",
                LevelLabel: "Level",
                DemoIndicator: "Auto-play",
                PressHForHelp: "Press H for help",
                HelpTab: "Tab / Shift+Tab — cycle selection",
                HelpEnter: "Enter / Space — release selected snake",
                HelpR: "R — restart level",
                HelpH: "H — toggle help",
                HelpD: "D — re-enable auto-play",
                HelpQ: "Q / Esc — quit",
                HelpLevels: "1-9, 0 — jump to level (0 = 10)"),
            ["de"] = new(
                Title: "TerminalSnake",
                LevelLabel: "Level",
                DemoIndicator: "Auto-Spiel",
                PressHForHelp: "H drücken für Hilfe",
                HelpTab: "Tab / Umschalt+Tab — Schlange wählen",
                HelpEnter: "Enter / Leertaste — Schlange befreien",
                HelpR: "R — Level neu starten",
                HelpH: "H — Hilfe ein-/ausblenden",
                HelpD: "D — Auto-Spiel aktivieren",
                HelpQ: "Q / Esc — beenden",
                HelpLevels: "1-9, 0 — Level springen (0 = 10)"),
            ["fr"] = new(
                Title: "TerminalSnake",
                LevelLabel: "Niveau",
                DemoIndicator: "Démonstration",
                PressHForHelp: "Appuyez sur H pour l'aide",
                HelpTab: "Tab / Maj+Tab — sélectionner un serpent",
                HelpEnter: "Entrée / Espace — libérer le serpent",
                HelpR: "R — recommencer le niveau",
                HelpH: "H — afficher l'aide",
                HelpD: "D — réactiver la démonstration",
                HelpQ: "Q / Échap — quitter",
                HelpLevels: "1-9, 0 — aller au niveau (0 = 10)"),
            ["es"] = new(
                Title: "TerminalSnake",
                LevelLabel: "Nivel",
                DemoIndicator: "Demostración",
                PressHForHelp: "Pulsa H para la ayuda",
                HelpTab: "Tab / Mayús+Tab — seleccionar serpiente",
                HelpEnter: "Intro / Espacio — liberar serpiente",
                HelpR: "R — reiniciar nivel",
                HelpH: "H — mostrar ayuda",
                HelpD: "D — reactivar la demostración",
                HelpQ: "Q / Esc — salir",
                HelpLevels: "1-9, 0 — saltar al nivel (0 = 10)"),
            ["it"] = new(
                Title: "TerminalSnake",
                LevelLabel: "Livello",
                DemoIndicator: "Dimostrazione",
                PressHForHelp: "Premi H per l'aiuto",
                HelpTab: "Tab / Maiusc+Tab — selezionare serpente",
                HelpEnter: "Invio / Spazio — rilasciare serpente",
                HelpR: "R — ricomincia livello",
                HelpH: "H — mostra aiuto",
                HelpD: "D — riattiva la dimostrazione",
                HelpQ: "Q / Esc — esci",
                HelpLevels: "1-9, 0 — salta al livello (0 = 10)"),
            ["pt"] = new(
                Title: "TerminalSnake",
                LevelLabel: "Nível",
                DemoIndicator: "Reprodução automática",
                PressHForHelp: "Pressione H para ajuda",
                HelpTab: "Tab / Shift+Tab — selecionar cobra",
                HelpEnter: "Enter / Espaço — libertar cobra",
                HelpR: "R — reiniciar nível",
                HelpH: "H — alternar ajuda",
                HelpD: "D — reativar a reprodução automática",
                HelpQ: "Q / Esc — sair",
                HelpLevels: "1-9, 0 — ir para o nível (0 = 10)"),
            ["nl"] = new(
                Title: "TerminalSnake",
                LevelLabel: "Level",
                DemoIndicator: "Auto-modus",
                PressHForHelp: "Druk op H voor hulp",
                HelpTab: "Tab / Shift+Tab — slang selecteren",
                HelpEnter: "Enter / Spatie — slang vrijlaten",
                HelpR: "R — level herstarten",
                HelpH: "H — hulp tonen",
                HelpD: "D — auto-modus herinschakelen",
                HelpQ: "Q / Esc — afsluiten",
                HelpLevels: "1-9, 0 — naar level springen (0 = 10)"),
        };

    public static HudStrings Default => Locales[Fallback];

    public static HudStrings ForLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return Default;
        }
        return Locales.TryGetValue(languageCode, out var strings) ? strings : Default;
    }

    public static HudStrings ForCurrentEnvironment()
        => ForLanguageCode(DetectLanguageCode());

    /// <summary>
    /// Reads the preferred language from the standard POSIX environment
    /// variables. Returns the two-letter prefix (e.g. "de_DE.UTF-8" → "de").
    /// </summary>
    public static string? DetectLanguageCode()
    {
        foreach (var variable in new[] { "LC_ALL", "LC_MESSAGES", "LANG" })
        {
            var value = Environment.GetEnvironmentVariable(variable);
            var code = ExtractLanguagePrefix(value);
            if (!string.IsNullOrEmpty(code))
            {
                return code;
            }
        }
        return null;
    }

    private static string? ExtractLanguagePrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (value is "C" or "POSIX")
        {
            return null;
        }
        var cutoff = value.IndexOfAny(new[] { '_', '.', '-', '@' });
        var prefix = cutoff > 0 ? value[..cutoff] : value;
        return prefix.ToLowerInvariant();
    }
}
