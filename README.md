# Fish Respawn

A BepInEx mod for Valheim that keeps fish around your fishing float so you're never left waiting on an empty
spot. While your line is out, the mod tops up the water around the float with fish. When you stop fishing,
the fish it spawned are removed.

## Features

- Keeps up to a set number of fish within a radius of your float while your line is out.
- Fish are chosen from the game's own spawn tables, so you get the fish that belong to the biome and ocean
  depth you're fishing in.
- Fish swim at the depth they would normally use in the game.
- Optionally lets every fish be caught with any fishing bait.
- Does nothing when you aren't fishing.

## Configuration

| Section | Setting | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Turns forced fish respawns on or off. |
| Spawn Area | `Radius` | `30` | Radius (m) around your float to keep stocked with fish. |
| Spawn Area | `MinSpawnDistance` | `6` | Fish never spawn closer than this (m) to the float. |
| Spawn Rate | `MaxFishNearby` | `8` | Stop spawning once this many fish are within the radius. |
| Spawn Rate | `SpawnIntervalSeconds` | `5` | Seconds between respawn checks while your line is out. |
| Spawn Rate | `MaxSpawnsPerCheck` | `1` | Maximum fish spawned per check. |
| Fish | `BiomeAccurate` | `true` | Only spawn fish the game spawns in the biome and water depth at the spawn point. |
| Fish | `FallbackFish` | `Fish1,Fish2` | Comma-separated fish prefab names used when no biome data is available or `BiomeAccurate` is off. |
| Fish | `SpawnedFishLifetimeSeconds` | `120` | Mod-spawned fish are removed after this long (unless hooked). `0` keeps them permanently. |
| Bait | `AnyBait` | `true` | Every fish can be caught with any fishing bait. |

### Notes

- **Water depth** follows the game's rules: each fish's spawn-table ocean depth and its own minimum swim
  depth. There is no separate depth setting.
- **Minimum spawn distance** is applied when a fish spawns. Fish can still swim toward your float afterwards,
  and `AnyBait` makes them more interested in it.
- If `Radius` is smaller than `MinSpawnDistance + 1`, the mod uses `MinSpawnDistance + 1` as the outer radius.
- With `SpawnedFishLifetimeSeconds` set to `0`, spawned fish are not tracked, so they are not removed when you stop fishing.
- `AnyBait` is not undone if you turn it off during a session. Restart the game to restore normal baits.

## Upgrading to 1.0.2

The `MinWaterDepth` setting was removed. You can delete that line from your config file. See
[CHANGELOG.md](CHANGELOG.md) for details.
