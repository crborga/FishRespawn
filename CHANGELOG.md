# Changelog

## 1.0.1
- Fixed "Any bait" so it applies to all fish, not just those near your float at the moment of a respawn check.
- "Any bait" now works independently of the spawning settings.
- The mod now does nothing unless you have a fishing float out. Fish it spawned are removed when you stop fishing.
- Fixed fish spawning at the wrong depth: they now spawn at the depth their species normally swims at, using the game's own depth rules, and no longer have to be moved there afterward.
- Removed the `MaxFishDepth` setting, since each species' own depth range now decides spawn depth. A leftover `MaxFishDepth` line in an existing config file is ignored and can be deleted.
- Reorganized the config file into clearer sections: General, Spawn Area, Spawn Rate, Fish and Bait. Settings that moved reset to their defaults, so re-enter any custom values.

## 1.0.0
- Initial release.
- Keeps fish stocked around your fishing float while your line is out.
- Biome-accurate spawning using the game's spawn tables, with each fish's own depth range respected.
- Safe spawn depths: fish stay below the surface and above the seabed.
- Mod-spawned fish are temporary (configurable lifetime) and are never removed while hooked.
- Optional "any bait" mode.
