using System.Collections.Generic;
using DebtDungeon.AI;
using DebtDungeon.Core;
using DebtDungeon.Player;
using DebtDungeon.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Showcase Level (debug only, remove for release builds): F9 in Main takes the player to a test area off the
    /// side of the generated floor, F9 again goes back. A small "F9" hint sits in the bottom-right corner whenever
    /// the game is in the Playing state.
    ///
    /// Layout (built in code the first time it's opened, as tilemaps):
    ///   - a safe room at the top, where the player arrives and the station stands. Enemies can't path into it.
    ///   - a narrow passage down into a corridor, with the goblin side to the left and the spider side to the right.
    /// Each side is that faction's territory: its units are anchored (and leashed) there, and their raid target is
    /// the other side, so raids cross the corridor the same way they cross the floor.
    /// Function-key cheats (spawn, camps, force a raid, clear, level up, heal, ghost mode) are listed on screen.
    /// Put it on the GameManager's object in Main, next to PlayerBootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShowcaseRoom : MonoBehaviour
    {
        // Layout in tiles, relative to the area's center (one tile = one unit). RectInt(x, y, width, height).
        private static readonly RectInt GoblinSide = new RectInt(-34, -12, 20, 24);
        private static readonly RectInt SpiderSide = new RectInt(14, -12, 20, 24);
        private static readonly RectInt Corridor = new RectInt(-14, -3, 28, 6);
        private static readonly RectInt Passage = new RectInt(-1, 3, 3, 4);
        private static readonly RectInt SafeRoom = new RectInt(-6, 7, 13, 7);

        private static readonly Vector2 GoblinHome = new Vector2(-24f, 0f);
        private static readonly Vector2 SpiderHome = new Vector2(24f, 0f);
        private static readonly Vector2 PlayerArrival = new Vector2(0f, 10f);
        private static readonly Vector2 StationSpot = new Vector2(4f, 11f);

        [Tooltip("Leave empty to use the PlayerBootstrap on this object.")]
        [SerializeField] private PlayerBootstrap _bootstrap;

        [Header("Area")]
        [Tooltip("World position of the area's center (rounded to a whole tile). The generated floor covers (0, 0) to (80, 80); the area reaches about 35 tiles left and right of this point.")]
        [SerializeField] private Vector2 _roomCenter = new Vector2(-60f, 40f);
        [SerializeField] private Sprite _floorSprite;
        [SerializeField] private Sprite _wallSprite;

        [Header("Enemies")]
        [SerializeField] private GameObject _goblinGruntPrefab;
        [SerializeField] private GameObject _goblinSlingerPrefab;
        [SerializeField] private GameObject _skitterlingPrefab;
        [SerializeField] private GameObject _webWeaverPrefab;
        [SerializeField] private GameObject _goblinCampPrefab;
        [SerializeField] private GameObject _spiderNestPrefab;

        [Header("Props")]
        [SerializeField] private GameObject _stationPrefab;

        // Things this component spawned, so Clear can remove them. Units a camp spawns are found by area instead.
        private readonly List<GameObject> _spawned = new List<GameObject>();

        // Tiles (relative to the center) the enemies may path through: both sides and the corridor.
        // The passage and the safe room are left out, which is what keeps the safe room safe.
        private readonly HashSet<Vector2Int> _enemyWalkable = new HashSet<Vector2Int>();

        private GameObject _roomRoot;
        private Vector2Int _center;
        private bool _isInRoom;
        private Vector3 _returnPosition;
        private LayerMask _wallMask;
        private int _wallLayer;
        private Faction _nextRaider = Faction.Goblin;

        private GameObject _hint;
        private TextMeshProUGUI _hintText;
        private GameObject _controlsPanel;
        private TextMeshProUGUI _controlsText;

        // Ghost mode (F8): enemies can't see or hurt the player. Original layers are kept to restore them.
        private const string GhostLayerName = "Ignore Raycast";
        private bool _isGhost;
        private readonly Dictionary<GameObject, int> _layersBeforeGhost = new Dictionary<GameObject, int>();
        private SpriteRenderer _playerSprite;
        private Color _spriteColorBeforeGhost;

        private void Awake()
        {
            if (_bootstrap == null)
            {
                _bootstrap = GetComponent<PlayerBootstrap>();
            }

            _center = Vector2Int.RoundToInt(_roomCenter);
            _wallLayer = LayerMask.NameToLayer("Walls");
            _wallMask = LayerMask.GetMask("Walls");
            BuildUi();
        }

        private void Update()
        {
            bool playing = GameEvents.CurrentGameState == GameState.Playing;
            SetActive(_hint, playing);
            SetActive(_controlsPanel, playing && _isInRoom);

            Keyboard keyboard = Keyboard.current;
            if (!playing || keyboard == null || _bootstrap == null || _bootstrap.PlayerObject == null)
            {
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                if (_isInRoom)
                {
                    LeaveRoom();
                }
                else
                {
                    EnterRoom();
                }
                return;
            }

            if (!_isInRoom)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                SpawnGoblins();
            }
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                SpawnSpiders();
            }
            else if (keyboard.f3Key.wasPressedThisFrame)
            {
                SpawnCamps();
            }
            else if (keyboard.f4Key.wasPressedThisFrame)
            {
                ForceRaid();
            }
            else if (keyboard.f5Key.wasPressedThisFrame)
            {
                ClearEnemies();
            }
            else if (keyboard.f6Key.wasPressedThisFrame)
            {
                LevelUp();
            }
            else if (keyboard.f7Key.wasPressedThisFrame)
            {
                RestorePlayer();
            }
            else if (keyboard.f8Key.wasPressedThisFrame)
            {
                SetGhost(!_isGhost);
            }
        }

        // ---- Entering and leaving ----

        private void EnterRoom()
        {
            bool firstVisit = _roomRoot == null;
            if (firstVisit)
            {
                BuildArea();
            }

            _returnPosition = _bootstrap.PlayerObject.transform.position;
            MovePlayer(At(PlayerArrival));
            _isInRoom = true;
            UpdateTexts();

            if (firstVisit)
            {
                SpawnGoblins();
                SpawnSpiders();
            }
        }

        private void LeaveRoom()
        {
            MovePlayer(_returnPosition);
            _isInRoom = false;
            UpdateTexts();
        }

        // Moves both the Rigidbody and the Transform, the same way FloorIntegration places the player.
        private void MovePlayer(Vector3 position)
        {
            GameObject player = _bootstrap.PlayerObject;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
            player.transform.position = position;
        }

        /// <summary>World position of a point given relative to the area's center.</summary>
        private Vector3 At(Vector2 relative)
        {
            return new Vector3(_center.x + relative.x, _center.y + relative.y, 0f);
        }

        // ---- Cheats ----

        // Each faction spawns on its own side, anchored to that side and aimed at the other side for raids.
        private void SpawnGoblins()
        {
            SpawnUnit(_goblinGruntPrefab, new Vector2(-26f, 3f), GoblinHome, SpiderHome);
            SpawnUnit(_goblinSlingerPrefab, new Vector2(-22f, -4f), GoblinHome, SpiderHome);
        }

        private void SpawnSpiders()
        {
            SpawnUnit(_skitterlingPrefab, new Vector2(26f, 3f), SpiderHome, GoblinHome);
            SpawnUnit(_webWeaverPrefab, new Vector2(22f, -4f), SpiderHome, GoblinHome);
        }

        // A camp at the back of each side. Destroying one opens the exit there (if no spawner has fallen yet).
        private void SpawnCamps()
        {
            SpawnSpawner(_goblinCampPrefab, new Vector2(-29f, 8f), SpiderHome);
            SpawnSpawner(_spiderNestPrefab, new Vector2(29f, 8f), GoblinHome);
        }

        private void SpawnUnit(GameObject prefab, Vector2 position, Vector2 home, Vector2 rivalHome)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject unit = Instantiate(prefab, At(position), Quaternion.identity);
            _spawned.Add(unit);

            // The area isn't part of the generated floor, so walkability comes from the area's own tile set.
            // Home = this faction's side (where it wanders and leashes); rival = where its raids go.
            FactionAI2D ai = unit.GetComponent<FactionAI2D>();
            if (ai != null)
            {
                ai.SetFloorWalkableQuery(IsEnemyWalkable);
                ai.ConfigureRole(UnitRole.Resident, At(home), _wallMask, null, At(rivalHome));
            }
        }

        private void SpawnSpawner(GameObject prefab, Vector2 position, Vector2 rivalHome)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject spawnerObject = Instantiate(prefab, At(position), Quaternion.identity);
            _spawned.Add(spawnerObject);

            // No floor: the units it spawns path with the Walls layer instead (Larry's fallback),
            // are anchored at the camp, and raid the other side.
            FactionSpawner2D spawner = spawnerObject.GetComponent<FactionSpawner2D>();
            if (spawner != null)
            {
                spawner.ConfigureFromFloor(null, null, _wallMask, At(rivalHome));
            }
        }

        // Sends every unit of one faction in the area to raid the other side, then the next press sends the other
        // faction. Raiders aren't leashed; they sweep the target side, then go home (Larry's raid logic).
        private void ForceRaid()
        {
            Faction raider = _nextRaider;
            Vector3 target = At(raider == Faction.Goblin ? SpiderHome : GoblinHome);

            // A unit can have several colliders, so remember who was already sent.
            HashSet<FactionAI2D> sent = new HashSet<FactionAI2D>();
            foreach (Collider2D collider in Physics2D.OverlapBoxAll(AreaCenter(), AreaSize(), 0f))
            {
                FactionAI2D unit = collider.GetComponentInParent<FactionAI2D>();
                if (unit == null || sent.Contains(unit))
                {
                    continue;
                }

                Health health = unit.GetComponent<Health>();
                if (health != null && health.Faction == raider)
                {
                    sent.Add(unit);
                    unit.TriggerRaidState(target);
                }
            }

            _nextRaider = raider == Faction.Goblin ? Faction.Spider : Faction.Goblin;
            UpdateTexts();
        }

        // Removes everything this component spawned, plus any unit standing in the area.
        private void ClearEnemies()
        {
            foreach (GameObject spawnedObject in _spawned)
            {
                if (spawnedObject != null)
                {
                    Destroy(spawnedObject);
                }
            }
            _spawned.Clear();

            foreach (Collider2D collider in Physics2D.OverlapBoxAll(AreaCenter(), AreaSize(), 0f))
            {
                FactionAI2D unit = collider.GetComponentInParent<FactionAI2D>();
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }
        }

        // The same as PlayerBootstrap's "Grant XP To Next Level": exactly enough XP, through the normal path.
        private void LevelUp()
        {
            CharacterProgress progress = _bootstrap.Character != null ? _bootstrap.Character.Progress : null;
            if (progress == null || progress.IsMaxLevel)
            {
                return;
            }

            progress.AddXp(progress.XpToNextLevel - progress.CurrentXp);
        }

        private void RestorePlayer()
        {
            if (_bootstrap.PlayerHealth != null)
            {
                _bootstrap.PlayerHealth.Heal(_bootstrap.PlayerHealth.MaxHealth);
            }

            if (_bootstrap.Defenses != null)
            {
                _bootstrap.Defenses.RepairArmor();
            }

            PlayerEnergy energy = _bootstrap.PlayerObject.GetComponent<PlayerEnergy>();
            if (energy != null)
            {
                energy.RechargeEnergy(energy.EnergyMax);
            }
        }

        // ---- Ghost mode ----

        /// <summary>
        /// Ghost mode on: the player moves to the Ignore Raycast layer, which no enemy's target mask includes, so
        /// enemies stop noticing them; every enemy already targeting the player forgets them; Health ignores all hits
        /// (Core v1.9); poison and slow are cleared; the sprite goes half transparent. Off restores all of it.
        /// It stays on after leaving the room, so the floor's AI can be watched too.
        /// </summary>
        private void SetGhost(bool ghost)
        {
            GameObject player = _bootstrap.PlayerObject;
            if (player == null || ghost == _isGhost)
            {
                return;
            }

            _isGhost = ghost;
            Health health = _bootstrap.PlayerHealth;
            if (health != null)
            {
                health.SetInvulnerable(ghost);
            }

            if (ghost)
            {
                _layersBeforeGhost.Clear();
                int ghostLayer = LayerMask.NameToLayer(GhostLayerName);
                foreach (Transform part in player.GetComponentsInChildren<Transform>())
                {
                    _layersBeforeGhost[part.gameObject] = part.gameObject.layer;
                    part.gameObject.layer = ghostLayer;
                }

                // A one-off search when the key is pressed (not every frame): make current chasers let go.
                if (health != null)
                {
                    foreach (FactionAI2D enemy in FindObjectsByType<FactionAI2D>(FindObjectsSortMode.None))
                    {
                        enemy.ForgetTarget(health);
                    }
                }

                StatusEffects status = player.GetComponent<StatusEffects>();
                if (status != null)
                {
                    status.RemoveStatus(StatusEffectType.Poisoned);
                    status.RemoveStatus(StatusEffectType.Slowed);
                }

                _playerSprite = player.GetComponentInChildren<SpriteRenderer>();
                if (_playerSprite != null)
                {
                    _spriteColorBeforeGhost = _playerSprite.color;
                    Color faded = _spriteColorBeforeGhost;
                    faded.a = 0.45f;
                    _playerSprite.color = faded;
                }
            }
            else
            {
                foreach (KeyValuePair<GameObject, int> entry in _layersBeforeGhost)
                {
                    if (entry.Key != null)
                    {
                        entry.Key.layer = entry.Value;
                    }
                }
                _layersBeforeGhost.Clear();

                if (_playerSprite != null)
                {
                    _playerSprite.color = _spriteColorBeforeGhost;
                }
            }

            UpdateTexts();
        }

        // ---- Area ----

        // For the AI's pathfinding: a world tile is walkable if it's on either side or in the corridor.
        private bool IsEnemyWalkable(Vector2Int worldTile)
        {
            return _enemyWalkable.Contains(worldTile - _center);
        }

        // A box around the whole area (sides, corridor and safe room), for Clear and Force raid.
        private Vector2 AreaCenter()
        {
            return new Vector2(_center.x, _center.y + 1f);
        }

        private Vector2 AreaSize()
        {
            return new Vector2(SpiderSide.xMax - GoblinSide.xMin + 2f, SafeRoom.yMax - GoblinSide.yMin + 2f);
        }

        private void BuildArea()
        {
            RectInt[] floorParts = { GoblinSide, SpiderSide, Corridor, Passage, SafeRoom };

            // Every floor tile, then the enemy-walkable subset.
            HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
            foreach (RectInt part in floorParts)
            {
                AddTiles(part, floorTiles);
            }
            AddTiles(GoblinSide, _enemyWalkable);
            AddTiles(SpiderSide, _enemyWalkable);
            AddTiles(Corridor, _enemyWalkable);

            // A wall goes on every non-floor tile that touches a floor tile (including diagonally), so the outline
            // follows any shape without listing walls by hand.
            HashSet<Vector2Int> wallTiles = new HashSet<Vector2Int>();
            foreach (Vector2Int tile in floorTiles)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Vector2Int neighbor = new Vector2Int(tile.x + dx, tile.y + dy);
                        if (!floorTiles.Contains(neighbor))
                        {
                            wallTiles.Add(neighbor);
                        }
                    }
                }
            }

            // One Grid holds both tilemaps. It's shifted by half a tile so tile (x, y) is centered on (x, y),
            // which matches how the AI's pathfinding rounds positions to tiles.
            _roomRoot = new GameObject("Showcase Area");
            _roomRoot.AddComponent<Grid>();
            _roomRoot.transform.position = new Vector3(_center.x - 0.5f, _center.y - 0.5f, 0f);

            Tile floorTile = ScriptableObject.CreateInstance<Tile>();
            floorTile.sprite = _floorSprite;
            floorTile.colliderType = Tile.ColliderType.None;

            Tile wallTile = ScriptableObject.CreateInstance<Tile>();
            wallTile.sprite = _wallSprite;
            wallTile.colliderType = Tile.ColliderType.Grid;

            CreateTilemap("Floor", "Floor", floorTiles, floorTile, false);
            CreateTilemap("Walls", "Walls", wallTiles, wallTile, true);

            if (_stationPrefab != null)
            {
                Instantiate(_stationPrefab, At(StationSpot), Quaternion.identity, _roomRoot.transform);
            }
        }

        private static void AddTiles(RectInt area, HashSet<Vector2Int> tiles)
        {
            for (int x = area.xMin; x < area.xMax; x++)
            {
                for (int y = area.yMin; y < area.yMax; y++)
                {
                    tiles.Add(new Vector2Int(x, y));
                }
            }
        }

        private void CreateTilemap(string objectName, string sortingLayer, HashSet<Vector2Int> tiles, Tile tile, bool isWall)
        {
            GameObject tilemapObject = new GameObject(objectName);
            tilemapObject.transform.SetParent(_roomRoot.transform, false);

            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingLayerName = sortingLayer;

            foreach (Vector2Int cell in tiles)
            {
                tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
            }

            if (isWall)
            {
                // On the Walls layer, so walls block movement, line of sight and projectiles like the floor's.
                tilemapObject.layer = _wallLayer;
                tilemapObject.AddComponent<TilemapCollider2D>();
            }
        }

        // ---- UI ----

        private void UpdateTexts()
        {
            string ghostTag = _isGhost ? "   <color=#8FD0FF>GHOST ON</color>" : string.Empty;
            _hintText.text = (_isInRoom ? "F9  Back to the floor" : "F9  Showcase room") + ghostTag;
            _controlsText.text = BuildControlsText();
        }

        private string BuildControlsText()
        {
            string raidLine = _nextRaider == Faction.Goblin ? "goblins raid the spider side" : "spiders raid the goblin side";
            return
                "<b>SHOWCASE ROOM</b> (debug)\n" +
                "Goblins: left side   Spiders: right side\n" +
                "F1  Spawn goblins (Grunt, Slinger)\n" +
                "F2  Spawn spiders (Skitter, Web Weaver)\n" +
                "F3  Spawn a goblin camp and spider nest\n" +
                "F4  Force raid: " + raidLine + "\n" +
                "F5  Clear enemies\n" +
                "F6  Level up\n" +
                "F7  Heal, refill energy, repair armor\n" +
                "F8  Ghost mode: " + (_isGhost ? "<color=#8FD0FF>ON</color>" : "off") + " (enemies ignore you)\n" +
                "F9  Back to the floor\n" +
                "Safe room (top): enemies can't follow you in. Station (E) here.";
        }

        private void BuildUi()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "Showcase Canvas", 1);

            // Bottom-right hint, inside the 90% safe area like the rest of the HUD.
            RectTransform hint = UIKit.CreateGroup(canvas.transform, "Showcase Hint");
            hint.anchorMin = new Vector2(0.95f, 0.05f);
            hint.anchorMax = new Vector2(0.95f, 0.05f);
            hint.pivot = new Vector2(1f, 0f);
            hint.anchoredPosition = Vector2.zero;
            hint.sizeDelta = new Vector2(420f, 30f);
            _hintText = UIKit.CreateText(hint, "F9  Showcase room", 18f, TextAlignmentOptions.BottomRight, UIKit.MutedText);
            UIKit.Stretch(_hintText.rectTransform);
            _hint = hint.gameObject;

            // Top-right list of cheats, shown only inside the area.
            RectTransform panel = UIKit.CreatePanel(canvas.transform, "Showcase Controls", new Color(0f, 0f, 0f, 0.6f));
            panel.anchorMin = new Vector2(0.95f, 0.95f);
            panel.anchorMax = new Vector2(0.95f, 0.95f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(520f, 380f);

            _controlsText = UIKit.CreateText(panel, BuildControlsText(), 20f, TextAlignmentOptions.TopLeft, UIKit.TextColor);
            UIKit.Stretch(_controlsText.rectTransform);
            _controlsText.rectTransform.offsetMin = new Vector2(16f, 12f);
            _controlsText.rectTransform.offsetMax = new Vector2(-16f, -12f);
            UIKit.FitText(_controlsText, 14f);
            _controlsPanel = panel.gameObject;
            _controlsPanel.SetActive(false);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
