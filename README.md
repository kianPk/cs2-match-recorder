# CS2 Match Recorder

CounterStrikeSharp plugin that records a **round-by-round timeline** of a CS2 match into JSON — kills, plants, defuses, clutches, and scoreboard snapshots.

Built for YGuard / dedicated nodes where you want post-match review without storing full `.dem` files.

## Features

- Round timeline with timestamps relative to round start
- Kill feed with weapon, headshot, wallbang, noscope flags
- Bomb plant / defuse / explode events
- End-of-round scoreboard snapshot (K/D/A, damage, ADR)
- Writes one file per map: `match_YYYYMMDD_HHMMSS_<map>.json`
- Idle on Practice pods (`SERVER_TYPE`) — same pattern as YGuard plugins

## Example output

```json
{
  "map": "de_mirage",
  "startedAt": "2026-10-19T12:00:00Z",
  "rounds": [
    {
      "n": 1,
      "winner": "CT",
      "reason": "elimination",
      "events": [
        { "t": 34.2, "type": "kill", "attacker": "s1mple", "victim": "ZyWoo", "weapon": "ak47", "hs": true }
      ],
      "scoreboard": { "CT": 1, "T": 0 }
    }
  ]
}
```

## Install

```text
addons/counterstrikesharp/plugins/MatchRecorder/
```

Config: `configs/plugins/MatchRecorder/MatchRecorder.json`

```json
{
  "OutputDirectory": "match_records",
  "PublicOnly": false,
  "IdleOnPractice": true
}
```

## Build

```bash
dotnet build -c Release
```

Requires .NET 8 + CounterStrikeSharp.

## License

MIT · Kian Zarandi ([@kianPk](https://github.com/kianPk))
