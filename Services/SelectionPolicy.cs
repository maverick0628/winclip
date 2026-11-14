using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace WinClip.Services;

[Flags]
public enum PolicyEffects
{
    None = 0,
    /// <summary>Never pop for this app.</summary>
    Deny = 1,
    /// <summary>Treat as a terminal: never send Ctrl+C or Ctrl+X.</summary>
    Terminal = 2,
    /// <summary>A drag may pop with the arrow cursor.</summary>
    AllowArrowDrag = 4,
}

/// <summary>What a rule is matched against.</summary>
public readonly record struct PolicyContext(string? ProcessName, string? WindowClass, SelectionGesture? Gesture);

/// <summary>
/// Per-app rules shipped as data in <c>Resources/selection-policy.json</c>, modelled on
/// PopClip's policy table, so app quirks are fixed without code changes.
/// </summary>
public sealed class SelectionPolicy
{
    private const string ResourceName = "WinClip.SelectionPolicy";

    private static readonly Lazy<SelectionPolicy> LazyDefault = new(LoadDefault);

    private readonly IReadOnlyList<CompiledRule> _rules;

    private SelectionPolicy(IReadOnlyList<CompiledRule> rules)
    {
        _rules = rules;
    }

    /// <summary>The shipped policy, or an empty one if the resource is missing or invalid.</summary>
    public static SelectionPolicy Default => LazyDefault.Value;

    public static SelectionPolicy Empty { get; } = new([]);

    public int RuleCount => _rules.Count;

    public PolicyEffects Evaluate(PolicyContext context)
    {
        var effects = PolicyEffects.None;
        foreach (var rule in _rules)
        {
            if (rule.Matches(context))
                effects |= rule.Effect;
        }
        return effects;
    }

    /// <summary>
    /// Parses and validates a policy document. Throws <see cref="FormatException"/> on bad JSON,
    /// an unknown key, group, effect or gesture, an empty group, or a rule with no process or
    /// window-class condition, so a typo fails the test suite instead of matching every app.
    /// </summary>
    public static SelectionPolicy Parse(string json)
    {
        PolicyDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<PolicyDocument>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Selection policy is not valid: {ex.Message}", ex);
        }
        if (document == null)
            throw new FormatException("Selection policy is empty.");

        var groups = document.Groups ?? new Dictionary<string, List<string>>();
        var rules = new List<CompiledRule>();
        foreach (var rule in document.Rules ?? [])
        {
            var name = rule.Name ?? "(unnamed)";
            if (!Enum.TryParse<PolicyEffects>(rule.Effect, ignoreCase: true, out var effect)
                || effect == PolicyEffects.None || !Enum.IsDefined(effect))
                throw new FormatException($"Rule '{name}': unknown effect '{rule.Effect}'.");

            var gestures = new List<SelectionGesture>();
            foreach (var g in rule.Gesture ?? [])
            {
                if (!Enum.TryParse<SelectionGesture>(g, ignoreCase: true, out var gesture) || !Enum.IsDefined(gesture))
                    throw new FormatException($"Rule '{name}': unknown gesture '{g}'.");
                gestures.Add(gesture);
            }

            var processes = Expand(rule.Process, groups, name);
            var windowClasses = Expand(rule.WindowClass, groups, name);
            if (processes.Count == 0 && windowClasses.Count == 0)
                throw new FormatException($"Rule '{name}': needs a process or windowClass condition.");

            rules.Add(new CompiledRule(processes, windowClasses, gestures, effect));
        }
        return new SelectionPolicy(rules);
    }

    private static SelectionPolicy LoadDefault()
    {
        try
        {
            using var stream = typeof(SelectionPolicy).Assembly.GetManifestResourceStream(ResourceName);
            if (stream == null) return Empty;
            using var reader = new StreamReader(stream);
            return Parse(reader.ReadToEnd());
        }
        catch
        {
            return Empty;
        }
    }

    internal static string ReadShippedJson()
    {
        using var stream = typeof(SelectionPolicy).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new FileNotFoundException(ResourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static List<Regex> Expand(List<string>? patterns, Dictionary<string, List<string>> groups, string ruleName)
    {
        var result = new List<Regex>();
        foreach (var pattern in patterns ?? [])
        {
            if (pattern.Length > 2 && pattern.StartsWith(':') && pattern.EndsWith(':'))
            {
                var group = pattern[1..^1];
                if (!groups.TryGetValue(group, out var members))
                    throw new FormatException($"Rule '{ruleName}': unknown group '{group}'.");
                if (members.Count == 0)
                    throw new FormatException($"Rule '{ruleName}': group '{group}' is empty.");
                result.AddRange(members.Select(Glob));
            }
            else
            {
                result.Add(Glob(pattern));
            }
        }
        return result;
    }

    private static Regex Glob(string pattern)
        => new("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private sealed record CompiledRule(
        IReadOnlyList<Regex> Processes,
        IReadOnlyList<Regex> WindowClasses,
        IReadOnlyList<SelectionGesture> Gestures,
        PolicyEffects Effect)
    {
        // Every condition a rule names must match; an unnamed condition matches anything.
        public bool Matches(PolicyContext context)
            => MatchesAny(Processes, context.ProcessName)
            && MatchesAny(WindowClasses, context.WindowClass)
            && (Gestures.Count == 0 || (context.Gesture is { } g && Gestures.Contains(g)));

        private static bool MatchesAny(IReadOnlyList<Regex> patterns, string? value)
            => patterns.Count == 0 || (value != null && patterns.Any(p => p.IsMatch(value)));
    }

    private sealed class PolicyDocument
    {
        public string? Comment { get; set; }
        public Dictionary<string, List<string>>? Groups { get; set; }
        public List<PolicyRuleDto>? Rules { get; set; }
    }

    private sealed class PolicyRuleDto
    {
        public string? Name { get; set; }
        public string? Comment { get; set; }
        public List<string>? Process { get; set; }
        public List<string>? WindowClass { get; set; }
        public List<string>? Gesture { get; set; }
        public string? Effect { get; set; }
    }
}
