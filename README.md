# Fish Respawn

Fishing spots in Valheim can run dry. **Fish Respawn** keeps the water around your fishing float stocked, so there's always something to catch.

While your line is out, the mod checks the area around your float every few seconds. If there are fewer fish than you've allowed, it spawns more, in the right biome, in water that suits them.

> **Please read the Changelog tab before updating.** Some versions rename or move config settings, which resets them to their defaults, so you may need to re-enter your own values.
>
> **Found a bug?** Please report it on [GitHub](https://github.com/crborga/FishRespawn/issues).

## Features

- **Keeps fish around your float.** Tops up fish within a configurable radius while your line is out, never closer than a minimum distance so fish don't pop in on top of the float.
- **Biome-accurate.** Fish are chosen from the game's own spawn tables, so you get the fish that belong in the biome and water depth where they appear.
- **Natural depth.** Fish spawn at the depth they would normally swim at, using the game's own depth rules for each species, and only in water deep enough for them.
- **Temporary fish.** Fish the mod spawns are removed after a set time (default 2 minutes), and all of them are removed when you stop fishing. A fish that is currently hooked is never removed.
- **Any bait.** Optionally lets every fish be caught with any fishing bait, not just its preferred one.
- **Client-side install.** Only you need the mod. The server and other players don't need to install anything.

## Installation

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Install Fish Respawn with your mod manager, or put `FishRespawn.dll` in `BepInEx/plugins`.
3. Start the game once. The config file is created at `BepInEx/config/com.crborga.fishrespawn.cfg`.

## Configuration

Edit the config file while the game is closed.

> **Please note:** some changes may not take effect until you relog.

| Section | Setting | Default | What it does |
|---|---|---|---|
| General | `Enabled` | true | Turns forced fish respawns on or off. |
| Spawn Area | `Radius` | 30 | Radius (m) around your float to keep stocked with fish. |
| Spawn Area | `MinSpawnDistance` | 6 | Fish never spawn closer than this (m) to the float. |
| Spawn Area | `MinWaterDepth` | 2 | Water must be at least this deep (m) at the spawn point, in addition to the fish's own minimum. |
| Spawn Rate | `MaxFishNearby` | 8 | Stop spawning once this many fish are within `Radius`. |
| Spawn Rate | `SpawnIntervalSeconds` | 5 | Seconds between respawn checks while your line is out. |
| Spawn Rate | `MaxSpawnsPerCheck` | 1 | Maximum fish spawned per check. |
| Fish | `BiomeAccurate` | true | Only spawn fish the game itself spawns in the biome and water depth at the spawn point. |
| Fish | `FallbackFish` | Fish1,Fish2 | Fish prefab names used when no biome data is available, or when `BiomeAccurate` is off. |
| Fish | `SpawnedFishLifetimeSeconds` | 120 | Mod-spawned fish are removed after this long, unless hooked. `0` keeps them permanently. |
| Bait | `AnyBait` | true | Every fish can be caught with any fishing bait. |

## Notes

- Spawned fish are normal networked fish, so other players near your float may see them too.
- The mod only does anything while you have a fishing float out. When you put the rod away it removes the fish it spawned and goes idle.
- If another mod changes fish prefabs or spawn tables, results may differ from the descriptions above.

## Troubleshooting

- **No fish appear:** check the BepInEx log for `Found N fish spawn entries`. If it reports 0, the mod falls back to `FallbackFish`, which must be valid fish prefab names.
- **Fish look out of place:** try lowering `MaxFishNearby` or turning on `BiomeAccurate`.
