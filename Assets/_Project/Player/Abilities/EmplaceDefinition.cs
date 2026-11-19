using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// Shield III (Emplace): Q raises a shield wall a short way toward the mouse, across the aim direction.
    /// The wall has Health Per Power x Power (Build) hit points and lasts Duration seconds or until it breaks.
    /// Enemies are blocked by it and attack it; the player walks and shoots through it. It still reflects.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_", menuName = "Debt Dungeon/Abilities/Emplace Wall")]
    public class EmplaceDefinition : SkillDefinition
    {
        [Header("Wall")]
        [Tooltip("Prefab with a Health (Player faction) on the Player layer. ShieldWall is added when it spawns.")]
        [SerializeField] private GameObject _wallPrefab;

        [SerializeField] private StatType _powerStat = StatType.Build;
        [SerializeField, Min(0f)] private float _healthPerPower = 15f;
        [SerializeField, Min(0.1f)] private float _duration = 4f;

        [Tooltip("How far from the player the wall's center is, toward the mouse (units/tiles).")]
        [SerializeField, Min(0.5f)] private float _distance = 1.5f;

        [SerializeField, Min(0.5f)] private float _width = 3f;
        [SerializeField, Min(0.1f)] private float _thickness = 0.375f;

        [Tooltip("Wall sprite, lying along +X. Stretched to Width x Thickness.")]
        [SerializeField] private Sprite _wallSprite;
        [SerializeField] private Color _wallColor = new Color(0.45f, 0.6f, 1f, 0.85f);

        [Header("Reflect")]
        [SerializeField] private bool _reflect = true;
        [SerializeField] private Sprite _beamSprite;

        public override void Execute(SkillContext context)
        {
            if (_wallPrefab == null)
            {
                Debug.LogWarning($"{name} has no Wall Prefab.", this);
                return;
            }

            float power = Mathf.Max(1f, context.Runner.GetPower(_powerStat));
            int health = Mathf.Max(1, Mathf.RoundToInt(_healthPerPower * power));

            Vector2 center = (Vector2)context.User.transform.position + context.Direction * _distance;

            // The wall's long side runs across the aim: aiming up (90 degrees) gives a flat wall (0 degrees).
            float angle = Mathf.Atan2(context.Direction.y, context.Direction.x) * Mathf.Rad2Deg - 90f;
            GameObject wallObject = Instantiate(_wallPrefab, center, Quaternion.Euler(0f, 0f, angle));

            ShieldWall wall = wallObject.GetComponent<ShieldWall>();
            if (wall == null)
            {
                wall = wallObject.AddComponent<ShieldWall>();
            }

            wall.Initialize(context.User, health, _duration, _width, _thickness, _wallSprite, _wallColor,
                new ReflectSettings(_reflect, _beamSprite, _wallColor));
        }
    }
}
