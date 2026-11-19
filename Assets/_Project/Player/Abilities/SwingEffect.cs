using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// A brief slash sprite that shows where a melee attack landed, then fades out and removes itself.
    /// MeleeAttackDefinition spawns one per swing, sized to the attack's hit circle and turned to face the swing.
    /// Purely visual: it has no collider and deals no damage.
    /// </summary>
    public class SwingEffect : MonoBehaviour
    {
        private const float Lifetime = 0.15f;

        // The slash sprite is 32 px at 16 PPU, so it is 2 world units across at scale 1.
        private const float SpriteWorldSize = 2f;

        private SpriteRenderer _renderer;
        private Color _startColor;
        private float _startTime;

        /// <summary>
        /// Creates the effect at the hit circle's center. direction is the swing direction; radius is the hit radius,
        /// so the drawn slash covers the same area the attack actually checks.
        /// </summary>
        public static void Spawn(Sprite sprite, Vector2 center, Vector2 direction, float radius)
        {
            if (sprite == null)
            {
                return;
            }

            GameObject effectObject = new GameObject("Swing Effect");
            effectObject.transform.position = center;

            // The sprite points right (+X); turn it to face the swing.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effectObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            float scale = (radius * 2f) / SpriteWorldSize;
            effectObject.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "FX";

            SwingEffect effect = effectObject.AddComponent<SwingEffect>();
            effect._renderer = renderer;
            effect._startColor = renderer.color;
            effect._startTime = Time.time;
        }

        private void Update()
        {
            float progress = (Time.time - _startTime) / Lifetime;
            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Color color = _startColor;
            color.a = 1f - progress;
            _renderer.color = color;
        }
    }
}
