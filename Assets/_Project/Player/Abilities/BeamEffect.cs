using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// A brief line between two points (Crackle's jumps, Reflect's return bolt), then it fades and removes itself.
    /// The sprite is 1 unit long and lies along +X; it is stretched to the distance and turned to face the target.
    /// Purely visual. Also builds the colored ring "auras" used by Shield and Brace.
    /// </summary>
    public class BeamEffect : MonoBehaviour
    {
        private const float Lifetime = 0.18f;

        private SpriteRenderer _renderer;
        private Color _startColor;
        private float _startTime;

        public static void Spawn(Sprite sprite, Vector2 from, Vector2 to, Color color)
        {
            if (sprite == null)
            {
                return;
            }

            Vector2 offset = to - from;
            GameObject beamObject = new GameObject("Beam Effect");
            beamObject.transform.position = from + offset * 0.5f;
            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            beamObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            beamObject.transform.localScale = new Vector3(offset.magnitude, 1f, 1f);

            SpriteRenderer renderer = beamObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = "FX";

            BeamEffect effect = beamObject.AddComponent<BeamEffect>();
            effect._renderer = renderer;
            effect._startColor = color;
            effect._startTime = Time.time;
        }

        /// <summary>
        /// A colored ring that follows parent (child object). The ring sprite is 1 unit across at scale 1.
        /// The caller destroys it when the effect ends.
        /// </summary>
        public static GameObject CreateAura(Sprite sprite, Transform parent, float diameter, Color color)
        {
            if (sprite == null || parent == null)
            {
                return null;
            }

            GameObject auraObject = new GameObject("Aura Effect");
            auraObject.transform.SetParent(parent, false);
            auraObject.transform.localScale = new Vector3(diameter, diameter, 1f);

            SpriteRenderer renderer = auraObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = "FX";
            return auraObject;
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
            color.a = _startColor.a * (1f - progress);
            _renderer.color = color;
        }
    }
}
