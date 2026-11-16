using DebtDungeon.Core;
using DebtDungeon.Player;
using DebtDungeon.Progression;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Spawns the player in the Main scene and wires Progression onto it: stats and max HP, armor/Shroud,
    /// XP from kills, burden move speed and (once Player has an ability runner) the ability granter.
    /// The character comes from RunSession (the creator's choices, rebuilt fresh every run). If the scene was
    /// opened directly, a fallback preset is used so the scene still plays.
    /// Runs before other scripts (execution order -100) so the HUD and skill screen can read Character in Start.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class PlayerBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject _playerPrefab;

        [Tooltip("Where the player appears. Leave empty to use this object's position.")]
        [SerializeField] private Transform _spawnPoint;

        [SerializeField] private ProgressionCatalog _catalog;

        [Tooltip("Used when the Main scene is opened directly (no character from the creator). Empty = the catalog's first preset.")]
        [SerializeField] private PresetDefinition _fallbackPreset;

        private PlayerController _controller;
        private AbilityGranter _abilityGranter;

        /// <summary>The character for this run.</summary>
        public CharacterData Character { get; private set; }

        /// <summary>The spawned player object.</summary>
        public GameObject PlayerObject { get; private set; }

        /// <summary>The player's Health (Core).</summary>
        public Health PlayerHealth { get; private set; }

        /// <summary>The player's armor and Shroud (Progression).</summary>
        public PlayerDefenses Defenses { get; private set; }

        /// <summary>The ProgressionCatalog, for screens that list trees and items.</summary>
        public ProgressionCatalog Catalog => _catalog;

        private void Awake()
        {
            if (_playerPrefab == null || _catalog == null)
            {
                Debug.LogError("PlayerBootstrap needs the player prefab and the ProgressionCatalog assigned.", this);
                enabled = false;
                return;
            }

            Character = RunSession.CreateCharacterForRun();
            if (Character == null)
            {
                Character = CharacterData.CreateFromPreset(GetFallbackPreset(), "Test Debtor", _catalog.LevelCurve);
            }

            SpawnPlayer();
        }

        private void OnDestroy()
        {
            if (Character != null)
            {
                Character.Loadout.LoadoutChanged -= ApplyBurden;
            }

            _abilityGranter?.Dispose();
        }

        private void SpawnPlayer()
        {
            Vector3 position = _spawnPoint != null ? _spawnPoint.position : transform.position;
            PlayerObject = Instantiate(_playerPrefab, position, Quaternion.identity);

            PlayerHealth = PlayerObject.GetComponent<Health>();
            if (PlayerHealth == null)
            {
                Debug.LogError("The player prefab needs Core's Health component.", PlayerObject);
                return;
            }

            // Progression's player components are added here, so the Player prefab stays Player-only.
            GetOrAdd<PlayerStats>(PlayerObject).Initialize(Character.Stats);
            Defenses = GetOrAdd<PlayerDefenses>(PlayerObject);
            Defenses.Initialize(Character.Loadout);
            GetOrAdd<PlayerProgression>(PlayerObject).Initialize(Character);

            _controller = PlayerObject.GetComponent<PlayerController>();
            ApplyBurden();
            Character.Loadout.LoadoutChanged += ApplyBurden;

            // Abilities: granted only once Player has a component implementing Core's IAbilityUser.
            IAbilityUser abilityUser = PlayerObject.GetComponent<IAbilityUser>();
            if (abilityUser != null)
            {
                _abilityGranter = new AbilityGranter(Character.Loadout, Character.Skills, abilityUser);
            }
            else
            {
                Debug.LogWarning("Player has no IAbilityUser yet (Ability framework card). Skills can be bought but aren't granted.", PlayerObject);
            }
        }

        // Burden lives on the loadout (Progression); movement lives on the controller (Player). Game connects them.
        private void ApplyBurden()
        {
            if (_controller != null)
            {
                _controller.SetBurdenSteps(Character.Loadout.BurdenSteps);
            }
        }

        private PresetDefinition GetFallbackPreset()
        {
            if (_fallbackPreset != null)
            {
                return _fallbackPreset;
            }

            Archetype firstArchetype = _catalog.GetArchetypes()[0];
            return _catalog.GetPresets(firstArchetype)[0];
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
