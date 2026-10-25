using MatchRecorder;
using Xunit;

public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
}

public class SummaryTests
{
    [Fact]
    public void IsOvertimeRound_AfterRegulation()
    {
        Assert.False(MatchRecorderPlugin.IsOvertimeRound(24));
        Assert.True(MatchRecorderPlugin.IsOvertimeRound(25));
    }

    [Fact]
    public void Summary_ReturnsMapRoundsDuration()
    {
        var clock = new FakeClock();
        var plugin = new MatchRecorderPlugin(new MatchRecorderConfig { IdleOnPractice = false }, clock);
        plugin.OnServerType("Public");
        plugin.OnMapStart("de_dust2");
        plugin.OnRoundStart(1);
        clock.UtcNow = clock.UtcNow.AddSeconds(90);
        plugin.OnRoundEnd("CT", "elimination", 1, 0);

        var summary = plugin.Summary();
        Assert.NotNull(summary);
        Assert.Equal("de_dust2", summary!.Map);
        Assert.Equal(1, summary.Rounds);
        Assert.Equal(90, summary.DurationSeconds);
        Assert.False(summary.Overtime);
    }
}
