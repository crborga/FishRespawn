# Changelog

## 1.0.2

### Changed
- Minimum water depth now follows the game's own rules (each fish's spawn-table ocean depth and its own
  minimum swim depth) instead of a custom setting.

### Removed
- The `MinWaterDepth` config option. Any existing `MinWaterDepth` line in your config file is now ignored and can be deleted.

### Fixed
- Fish could spawn outside the area counted against `MaxFishNearby` when `Radius` was set below
  `MinSpawnDistance + 1`, causing the mod to keep spawning fish. Counting and spawning now use the same radius.
- Fishing baits were rescanned for every fish every half second when the game had none loaded. They are now scanned once.

### Performance
- Your fishing float is cached instead of searched for every frame.
- Spawn attempts on land are rejected before the biome and spawn-table lookups.
- Duplicate fish-removal code consolidated into one helper.

## 1.0.1
- Previous release.
