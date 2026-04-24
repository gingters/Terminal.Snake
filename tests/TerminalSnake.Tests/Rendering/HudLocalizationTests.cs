using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class HudLocalizationTests
{
    [Theory]
    [InlineData("en", "Press H for help")]
    [InlineData("de", "H drücken für Hilfe")]
    [InlineData("fr", "Appuyez sur H pour l'aide")]
    [InlineData("es", "Pulsa H para la ayuda")]
    [InlineData("it", "Premi H per l'aiuto")]
    [InlineData("pt", "Pressione H para ajuda")]
    [InlineData("nl", "Druk op H voor hulp")]
    public void ForLanguageCode_returns_locale_specific_press_h_text(string code, string expected)
    {
        Assert.Equal(expected, HudLocalization.ForLanguageCode(code).PressHForHelp);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("zz")]
    [InlineData("klingon")]
    public void Unknown_or_missing_code_falls_back_to_english(string? code)
    {
        var strings = HudLocalization.ForLanguageCode(code);
        Assert.Equal(HudLocalization.Default, strings);
        Assert.Equal("Press H for help", strings.PressHForHelp);
    }

    [Theory]
    [InlineData("DE", "H drücken für Hilfe")]
    [InlineData("De", "H drücken für Hilfe")]
    public void Language_lookup_is_case_insensitive(string code, string expected)
    {
        Assert.Equal(expected, HudLocalization.ForLanguageCode(code).PressHForHelp);
    }

    [Theory]
    [InlineData("de_DE.UTF-8", "de")]
    [InlineData("en_US.UTF-8", "en")]
    [InlineData("fr_FR", "fr")]
    [InlineData("pt-BR", "pt")]
    [InlineData("es@euro", "es")]
    [InlineData("en", "en")]
    public void Detector_extracts_language_prefix(string lang, string expected)
    {
        var snapshot = SwapEnv("LANG", lang, ("LC_ALL", null), ("LC_MESSAGES", null));
        try
        {
            Assert.Equal(expected, HudLocalization.DetectLanguageCode());
        }
        finally
        {
            snapshot();
        }
    }

    [Theory]
    [InlineData("C")]
    [InlineData("POSIX")]
    public void Posix_default_locales_are_ignored(string value)
    {
        var snapshot = SwapEnv("LANG", value, ("LC_ALL", null), ("LC_MESSAGES", null));
        try
        {
            Assert.Null(HudLocalization.DetectLanguageCode());
        }
        finally
        {
            snapshot();
        }
    }

    [Fact]
    public void Lc_all_wins_over_lang()
    {
        var snapshot = SwapEnv("LC_ALL", "fr_FR.UTF-8", ("LANG", "de_DE.UTF-8"), ("LC_MESSAGES", null));
        try
        {
            Assert.Equal("fr", HudLocalization.DetectLanguageCode());
        }
        finally
        {
            snapshot();
        }
    }

    private static Action SwapEnv(string primary, string? value, params (string Name, string? Value)[] others)
    {
        var snapshots = new List<(string Name, string? Previous)>
        {
            (primary, Environment.GetEnvironmentVariable(primary)),
        };
        Environment.SetEnvironmentVariable(primary, value);
        foreach (var (name, val) in others)
        {
            snapshots.Add((name, Environment.GetEnvironmentVariable(name)));
            Environment.SetEnvironmentVariable(name, val);
        }
        return () =>
        {
            foreach (var (name, previous) in snapshots)
            {
                Environment.SetEnvironmentVariable(name, previous);
            }
        };
    }
}
