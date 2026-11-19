using System.Collections.Generic;
using DebtDungeon.Core;
using TMPro;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Floating damage numbers over anything that takes a hit: white on enemies, yellow with "!" on a critical,
    /// red on the player, grey "Blocked" when armor or Shroud soaked the whole hit.
    /// Listens to Core's GameEvents.ActorDamaged (v1.8), so it works for every unit without touching their prefabs.
    /// Put it on the GameManager's object in the Main scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageNumbers : MonoBehaviour
    {
        private static readonly Color EnemyHitColor = Color.white;
        private static readonly Color CriticalColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color PlayerHitColor = new Color(1f, 0.4f, 0.35f, 1f);
        private static readonly Color BlockedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Tooltip("How long a number stays on screen, in seconds.")]
        [SerializeField, Min(0.1f)] private float _lifetime = 0.7f;

        [Tooltip("How far a number floats up over its lifetime, in world units.")]
        [SerializeField] private float _riseDistance = 0.8f;

        [Tooltip("Where a number starts, above the target's position.")]
        [SerializeField] private float _startHeight = 0.6f;

        [Tooltip("World-space font size (Matthew's HitText uses 4).")]
        [SerializeField, Min(0.5f)] private float _fontSize = 4f;

        // One number currently on screen.
        private struct ActiveNumber
        {
            public TextMeshPro Text;
            public Vector3 StartPosition;
            public float StartTime;
            public Color Color;
        }

        private readonly List<ActiveNumber> _active = new List<ActiveNumber>();

        // Finished numbers are hidden and reused instead of destroyed, so a big fight doesn't create garbage.
        private readonly Stack<TextMeshPro> _pool = new Stack<TextMeshPro>();

        private int _fxSortingLayerId;

        private void Awake()
        {
            _fxSortingLayerId = SortingLayer.NameToID("FX");
        }

        private void OnEnable()
        {
            GameEvents.ActorDamaged += HandleActorDamaged;
        }

        private void OnDisable()
        {
            GameEvents.ActorDamaged -= HandleActorDamaged;
        }

        private void HandleActorDamaged(Health target, DamageInfo damage, int damageTaken)
        {
            if (target == null)
            {
                return;
            }

            string label;
            Color color;
            if (damageTaken <= 0)
            {
                label = "Blocked";
                color = BlockedColor;
            }
            else if (target.Faction == Faction.Player)
            {
                label = damageTaken.ToString();
                color = PlayerHitColor;
            }
            else if (damage.IsCritical)
            {
                label = damageTaken + "!";
                color = CriticalColor;
            }
            else
            {
                label = damageTaken.ToString();
                color = EnemyHitColor;
            }

            // A small sideways jitter keeps quick repeat hits from stacking exactly on top of each other.
            Vector3 start = target.transform.position + new Vector3(Random.Range(-0.2f, 0.2f), _startHeight, 0f);
            Show(label, color, start);
        }

        private void Show(string label, Color color, Vector3 position)
        {
            TextMeshPro text = _pool.Count > 0 ? _pool.Pop() : CreateText();
            text.gameObject.SetActive(true);
            text.text = label;
            text.color = color;
            text.transform.position = position;

            _active.Add(new ActiveNumber
            {
                Text = text,
                StartPosition = position,
                StartTime = Time.time,
                Color = color
            });
        }

        private TextMeshPro CreateText()
        {
            GameObject textObject = new GameObject("Damage Number");
            textObject.transform.SetParent(transform, false);

            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.fontSize = _fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(3f, 1f);
            text.sortingLayerID = _fxSortingLayerId;
            text.sortingOrder = 10;
            return text;
        }

        private void Update()
        {
            // Walk backwards so finished numbers can be removed from the list while looping.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveNumber number = _active[i];
                float progress = (Time.time - number.StartTime) / _lifetime;

                if (progress >= 1f || number.Text == null)
                {
                    if (number.Text != null)
                    {
                        number.Text.gameObject.SetActive(false);
                        _pool.Push(number.Text);
                    }
                    _active.RemoveAt(i);
                    continue;
                }

                number.Text.transform.position = number.StartPosition + Vector3.up * (_riseDistance * progress);

                // Fully visible for the first half, then fades out.
                Color color = number.Color;
                color.a = progress < 0.5f ? 1f : 1f - (progress - 0.5f) * 2f;
                number.Text.color = color;
            }
        }
    }
}
