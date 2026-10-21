using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

#pragma warning disable CS0618 // FindObjectsOfType is fine for this use

namespace FishRespawn
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class FishRespawnPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.crborga.fishrespawn";
        public const string PluginName = "Fish Respawn";
        public const string PluginVersion = "1.0.0";

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _radius;
        private ConfigEntry<float> _minSpawnDistance;
        private ConfigEntry<int> _maxFishNearby;
        private ConfigEntry<float> _intervalSeconds;
        private ConfigEntry<int> _maxSpawnsPerTick;
        private ConfigEntry<float> _minWaterDepth;
        private ConfigEntry<float> _maxFishDepth;
        private ConfigEntry<string> _fallbackFish;
        private ConfigEntry<float> _fishLifetime;
        private ConfigEntry<bool> _biomeAccurate;
        private ConfigEntry<bool> _anyBait;

        private const float SurfaceClearance = 0.75f; // min distance below the water surface
        private const float FloorClearance = 0.5f;    // min distance above the seabed

        private float _timer;
        private readonly List<SpawnedFish> _spawned = new List<SpawnedFish>();

        private struct SpawnedFish
        {
            public Fish Fish;
            public float ExpiresAt;
        }
        private readonly List<SpawnSystem.SpawnData> _fishSpawns = new List<SpawnSystem.SpawnData>();
        private bool _fishSpawnsLoaded;
        private readonly List<GameObject> _biomeCandidates = new List<GameObject>();
        private readonly List<ItemDrop> _allBaits = new List<ItemDrop>();
        private readonly HashSet<int> _baitApplied = new HashSet<int>(); // instance IDs already given extra baits
        private readonly List<GameObject> _fallbackPrefabs = new List<GameObject>();
        private string _cachedFallbackString;

        private void Awake()
        {
            _enabled = Config.Bind("General", "Enabled", true, "Enable forced fish respawns while fishing.");
            _radius = Config.Bind("General", "Radius", 30f, "Radius (m) around your fishing float to keep stocked with fish.");
            _minSpawnDistance = Config.Bind("General", "MinSpawnDistance", 6f, "Fish never spawn closer than this (m) to the float, so they don't pop in on top of it.");
            _maxFishNearby = Config.Bind("General", "MaxFishNearby", 8, "Stop spawning once this many fish are within Radius.");
            _intervalSeconds = Config.Bind("General", "SpawnIntervalSeconds", 5f, "Seconds between respawn checks while your line is out.");
            _maxSpawnsPerTick = Config.Bind("General", "MaxSpawnsPerCheck", 1, "Maximum fish spawned per check.");
            _minWaterDepth = Config.Bind("Spawning", "MinWaterDepth", 2f, "Water must be at least this deep (m) at the spawn point.");
            _maxFishDepth = Config.Bind("Spawning", "MaxFishDepth", 5f, "Fish spawn at most this far below the surface (m).");
            _fallbackFish = Config.Bind("Spawning", "FallbackFish", "Fish1,Fish2",
                "Comma-separated fish prefab names used when no biome fish data is available (or BiomeAccurate is off).");
            _fishLifetime = Config.Bind("Spawning", "SpawnedFishLifetimeSeconds", 120f,
                "Fish spawned by this mod are removed after this many seconds (unless hooked). 0 keeps them permanently.");
            _biomeAccurate = Config.Bind("Spawning", "BiomeAccurate", true,
                "Only spawn fish that the game itself spawns in the biome (and water depth) at the spawn point, using the game's spawn tables.");
            _anyBait = Config.Bind("Bait", "AnyBait", true,
                "Let every fish be caught with any fishing bait, not just its preferred bait.");
        }

        private void Update()
        {
            RemoveExpiredFish();

            if (!_enabled.Value) return;

            _timer += Time.deltaTime;
            if (_timer < _intervalSeconds.Value) return;
            _timer = 0f;

            TryRespawn();
        }

        // Removes mod-spawned fish once their lifetime is up, but never one that is currently hooked.
        private void RemoveExpiredFish()
        {
            if (_spawned.Count == 0) return;

            float now = Time.time;
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                Fish fish = _spawned[i].Fish;
                if (fish == null) // already destroyed (caught, killed, or zone unloaded)
                {
                    _spawned.RemoveAt(i);
                    continue;
                }

                if (now < _spawned[i].ExpiresAt || fish.IsHooked()) continue;

                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(fish.gameObject);
                else Destroy(fish.gameObject);
                _spawned.RemoveAt(i);
            }
        }

        private void TryRespawn()
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null || ZoneSystem.instance == null) return;

            FishingFloat bobber = FindOwnFloat(player);
            if (bobber == null) return;

            Vector3 center = bobber.transform.position;
            float sqrRadius = _radius.Value * _radius.Value;
            bool anyBait = _anyBait.Value;

            // Count fish already around the float (Fish.Instances is the game's own list, so no scene scan).
            int count = 0;
            foreach (IMonoUpdater updater in Fish.Instances)
            {
                Fish fish = updater as Fish;
                if (fish == null || (fish.transform.position - center).sqrMagnitude > sqrRadius) continue;
                count++;
                if (anyBait) ApplyAnyBait(fish);
            }

            int toSpawn = Mathf.Min(_maxSpawnsPerTick.Value, _maxFishNearby.Value - count);
            if (toSpawn <= 0) return;

            if (_biomeAccurate.Value) LoadFishSpawns();

            float lifetime = _fishLifetime.Value;
            for (int i = 0; i < toSpawn; i++)
            {
                if (!TryPickSpawn(center, out GameObject prefab, out Vector3 pos)) continue;

                GameObject spawned = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                Fish spawnedFish = spawned.GetComponent<Fish>();
                if (spawnedFish == null) continue;

                if (anyBait) ApplyAnyBait(spawnedFish);
                if (lifetime > 0f) _spawned.Add(new SpawnedFish { Fish = spawnedFish, ExpiresAt = Time.time + lifetime });
            }
        }

        // Adds every fishing bait to the fish's accepted-bait list (existing entries are left alone).
        private void ApplyAnyBait(Fish fish)
        {
            if (fish.m_baits == null || !_baitApplied.Add(fish.GetInstanceID())) return;
            if (_baitApplied.Count > 1024) _baitApplied.Clear(); // drop IDs of long-gone fish

            RefreshAllBaits();

            // Give new baits the same chance as the fish's best existing bait, so the scale always matches the game's.
            float chance = 1f;
            if (fish.m_baits.Count > 0)
            {
                chance = 0f;
                foreach (Fish.BaitSetting setting in fish.m_baits)
                    if (setting.m_chance > chance) chance = setting.m_chance;
            }

            foreach (ItemDrop bait in _allBaits)
            {
                bool known = false;
                foreach (Fish.BaitSetting setting in fish.m_baits)
                {
                    if (setting.m_bait != null && setting.m_bait.name == bait.name)
                    {
                        known = true;
                        break;
                    }
                }

                if (!known)
                    fish.m_baits.Add(new Fish.BaitSetting { m_bait = bait, m_chance = chance });
            }
        }

        private void RefreshAllBaits()
        {
            if (_allBaits.Count > 0 || ObjectDB.instance == null) return;

            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                if (item == null || !item.name.StartsWith("FishingBait")) continue;
                ItemDrop drop = item.GetComponent<ItemDrop>();
                if (drop != null) _allBaits.Add(drop);
            }
        }

        // The float you cast is owned by your client, so pick the nearest owned float.
        private static FishingFloat FindOwnFloat(Player player)
        {
            FishingFloat best = null;
            float bestDist = float.MaxValue;

            foreach (FishingFloat f in FishingFloat.GetAllInstances())
            {
                ZNetView nview = f.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid() || !nview.IsOwner()) continue;

                float d = (f.transform.position - player.transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = f;
                }
            }

            return best;
        }

        private GameObject PickFallbackPrefab()
        {
            RefreshFallbackPrefabs();
            return _fallbackPrefabs.Count > 0 ? _fallbackPrefabs[Random.Range(0, _fallbackPrefabs.Count)] : null;
        }

        private void RefreshFallbackPrefabs()
        {
            if (_cachedFallbackString == _fallbackFish.Value && _fallbackPrefabs.Count > 0) return;

            _cachedFallbackString = _fallbackFish.Value;
            _fallbackPrefabs.Clear();

            foreach (string raw in _fallbackFish.Value.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0) continue;

                GameObject prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab != null && prefab.GetComponent<Fish>() != null)
                    _fallbackPrefabs.Add(prefab);
                else
                    Logger.LogWarning($"Fallback fish prefab '{name}' not found or has no Fish component.");
            }
        }

        // Reads the game's own spawn tables once and keeps the entries that spawn fish.
        private void LoadFishSpawns()
        {
            if (_fishSpawnsLoaded) return;

            foreach (SpawnSystem system in FindObjectsOfType<SpawnSystem>())
            {
                if (system.m_spawnLists == null) continue;

                foreach (SpawnSystemList list in system.m_spawnLists)
                {
                    if (list == null || list.m_spawners == null) continue;
                    foreach (SpawnSystem.SpawnData data in list.m_spawners)
                    {
                        if (data.m_enabled && data.m_prefab != null && data.m_prefab.GetComponent<Fish>() != null)
                            _fishSpawns.Add(data);
                    }
                }

                _fishSpawnsLoaded = true;
                Logger.LogInfo($"Found {_fishSpawns.Count} fish spawn entries in the game's spawn tables.");
                return;
            }
        }

        // Picks a fish that the game would spawn in this biome / ocean depth. Returns null if none fit.
        private GameObject PickPrefabAt(Vector3 p, float waterDepth)
        {
            if (!_biomeAccurate.Value || _fishSpawns.Count == 0)
                return PickFallbackPrefab(); // no spawn table data available

            Heightmap.Biome biome = Heightmap.FindBiome(p);
            _biomeCandidates.Clear();

            foreach (SpawnSystem.SpawnData data in _fishSpawns)
            {
                if ((data.m_biome & biome) == 0) continue;
                if (data.m_minOceanDepth != data.m_maxOceanDepth &&
                    (waterDepth < data.m_minOceanDepth || waterDepth > data.m_maxOceanDepth)) continue;
                if (!string.IsNullOrEmpty(data.m_requiredGlobalKey) &&
                    !ZoneSystem.instance.GetGlobalKey(data.m_requiredGlobalKey)) continue;

                _biomeCandidates.Add(data.m_prefab);
            }

            return _biomeCandidates.Count > 0 ? _biomeCandidates[Random.Range(0, _biomeCandidates.Count)] : null;
        }

        private bool TryPickSpawn(Vector3 center, out GameObject prefab, out Vector3 result)
        {
            float waterLevel = ZoneSystem.instance.m_waterLevel;
            float minWater = Mathf.Max(_minWaterDepth.Value, SurfaceClearance + FloorClearance);
            float minDist = _minSpawnDistance.Value;
            float maxDist = Mathf.Max(minDist + 1f, _radius.Value);
            float depthCap = Mathf.Max(SurfaceClearance, _maxFishDepth.Value);

            for (int attempt = 0; attempt < 15; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(minDist, maxDist);
                Vector3 p = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                float ground = ZoneSystem.instance.GetGroundHeight(p);
                float depth = waterLevel - ground;
                if (depth < minWater) continue;

                GameObject candidate = PickPrefabAt(p, depth);
                if (candidate == null) continue;

                // Fish only pick swim waypoints where the water depth is inside their own m_minDepth..m_maxDepth.
                Fish template = candidate.GetComponent<Fish>();
                if (template != null && (depth < template.m_minDepth || (template.m_maxDepth > 0f && depth > template.m_maxDepth))) continue;

                // Never deeper than the user's cap or than the floor clearance allows.
                float maxDepth = Mathf.Min(depthCap, depth - FloorClearance);
                if (maxDepth < SurfaceClearance) continue; // too shallow for a fish here

                float fishDepth = Random.Range(SurfaceClearance, maxDepth);
                prefab = candidate;
                result = new Vector3(p.x, waterLevel - fishDepth, p.z);
                return true;
            }

            prefab = null;
            result = Vector3.zero;
            return false;
        }
    }
}
