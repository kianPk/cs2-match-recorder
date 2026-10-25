using System.Text.Json;
using System.Text.Json.Serialization;

namespace MatchRecorder;

/// <summary>
/// CounterStrikeSharp-oriented match recorder.
/// Wire Event* handlers from CSS in your host; this core stays testable without the game.
/// </summary>
public sealed class MatchRecorderPlugin
{
    private readonly MatchRecorderConfig _config;
    private readonly IClock _clock;
    private MatchSession? _session;
    private RoundState? _round;
    private DateTime _roundStarted;

    public MatchRecorderPlugin(MatchRecorderConfig config, IClock? clock = null)
    {
        _config = config;
        _clock = clock ?? SystemClock.Instance;
    }

    public bool IsActive { get; private set; }

    /// <summary>True when the match has entered overtime (scoreboard past regulation).</summary>
    public bool IsOvertime { get; private set; }

    /// <summary>Current round number being played (0 before first round).</summary>
    public int CurrentRoundNumber { get; private set; }

    public void OnServerType(string serverType)
    {
        if (_config.IdleOnPractice && serverType.Equals("Practice", StringComparison.OrdinalIgnoreCase))
        {
            IsActive = false;
            return;
        }

        IsActive = true;
    }

    public void OnMapStart(string mapName)
    {
        if (!IsActive) return;
        Flush();
        IsOvertime = false;
        CurrentRoundNumber = 0;
        _session = new MatchSession
        {
            Map = mapName,
            StartedAt = _clock.UtcNow
        };
    }

    public void OnRoundStart(int roundNumber)
    {
        if (!IsActive || _session is null) return;
        CurrentRoundNumber = roundNumber;
        IsOvertime = IsOvertimeRound(roundNumber);
        _roundStarted = _clock.UtcNow;
        _round = new RoundState { N = roundNumber, Overtime = IsOvertime };
    }

    /// <summary>Overtime helper: rounds beyond regulation (default MR12 → round &gt; 24).</summary>
    public static bool IsOvertimeRound(int roundNumber, int regulationRounds = 24) =>
        roundNumber > regulationRounds;

    public void OnKill(KillEvent kill)
    {
        if (_round is null) return;
        _round.Events.Add(new TimelineEvent
        {
            T = Elapsed(),
            Type = "kill",
            Attacker = kill.Attacker,
            Victim = kill.Victim,
            Weapon = kill.Weapon,
            Hs = kill.Headshot,
            Wallbang = kill.Wallbang,
            Noscope = kill.Noscope
        });
    }

    public void OnBomb(string kind, string? actor = null)
    {
        if (_round is null) return;
        _round.Events.Add(new TimelineEvent
        {
            T = Elapsed(),
            Type = kind,
            Attacker = actor
        });
    }

    public void OnRoundEnd(string winner, string reason, int scoreCt, int scoreT)
    {
        if (!IsActive || _session is null || _round is null) return;
        _round.Winner = winner;
        _round.Reason = reason;
        _round.Scoreboard = new ScoreboardSnap { CT = scoreCt, T = scoreT };
        if (scoreCt >= 12 && scoreT >= 12) IsOvertime = true;
        _session.Rounds.Add(_round);
        _round = null;
    }

    /// <summary>JSON-serializable match summary (map, rounds, duration).</summary>
    public MatchSummary? Summary()
    {
        if (_session is null) return null;
        var ended = _clock.UtcNow;
        return new MatchSummary
        {
            Map = _session.Map,
            Rounds = _session.Rounds.Count,
            CurrentRound = CurrentRoundNumber,
            Overtime = IsOvertime,
            StartedAt = _session.StartedAt,
            DurationSeconds = Math.Round((ended - _session.StartedAt).TotalSeconds, 2),
            ScoreCt = _session.Rounds.LastOrDefault()?.Scoreboard?.CT,
            ScoreT = _session.Rounds.LastOrDefault()?.Scoreboard?.T
        };
    }

    public string? Flush()
    {
        if (_session is null || _session.Rounds.Count == 0) return null;

        Directory.CreateDirectory(_config.OutputDirectory);
        var stamp = _session.StartedAt.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(_config.OutputDirectory, $"match_{stamp}_{_session.Map}.json");
        var json = JsonSerializer.Serialize(_session, JsonOpts);
        File.WriteAllText(path, json);
        _session = null;
        CurrentRoundNumber = 0;
        IsOvertime = false;
        return path;
    }

    private double Elapsed() => Math.Round((_clock.UtcNow - _roundStarted).TotalSeconds, 2);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class MatchRecorderConfig
{
    public string OutputDirectory { get; set; } = "match_records";
    public bool IdleOnPractice { get; set; } = true;
    public bool PublicOnly { get; set; }
}

public sealed class KillEvent
{
    public required string Attacker { get; init; }
    public required string Victim { get; init; }
    public required string Weapon { get; init; }
    public bool Headshot { get; init; }
    public bool Wallbang { get; init; }
    public bool Noscope { get; init; }
}

public sealed class MatchSession
{
    public string Map { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public List<RoundState> Rounds { get; set; } = new();
}

public sealed class RoundState
{
    public int N { get; set; }
    public bool Overtime { get; set; }
    public string? Winner { get; set; }
    public string? Reason { get; set; }
    public ScoreboardSnap? Scoreboard { get; set; }
    public List<TimelineEvent> Events { get; set; } = new();
}

public sealed class ScoreboardSnap
{
    public int CT { get; set; }
    public int T { get; set; }
}

public sealed class TimelineEvent
{
    public double T { get; set; }
    public string Type { get; set; } = "";
    public string? Attacker { get; set; }
    public string? Victim { get; set; }
    public string? Weapon { get; set; }
    public bool? Hs { get; set; }
    public bool? Wallbang { get; set; }
    public bool? Noscope { get; set; }
}

public sealed class MatchSummary
{
    public string Map { get; set; } = "";
    public int Rounds { get; set; }
    public int CurrentRound { get; set; }
    public bool Overtime { get; set; }
    public DateTime StartedAt { get; set; }
    public double DurationSeconds { get; set; }
    public int? ScoreCt { get; set; }
    public int? ScoreT { get; set; }
}
