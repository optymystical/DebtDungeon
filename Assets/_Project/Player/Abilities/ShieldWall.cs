using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// The Emplace wall. Its own Health (Player faction) makes enemies treat it as a target and lets their hits land
    /// on it; this component then soaks every hit from its own pool (so the Health itself never drops and never
    /// "dies" as the player would), reflects if enabled, and removes the wall when the pool is empty or time is up.
    /// Collisions with the player are ignored so the player can walk through; the player's attacks already skip
    /// Player-faction Health.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ShieldWall : MonoBehaviour, IDamageAbsorber
    {
        private GameObject _user;
        private ReflectSettings _reflect;
        private int _remaining;

        public void Initialize(GameObject user, int health, float duration, float width, float thickness,
            Sprite sprite, Color color, ReflectSettings reflect)
        {
            _user = user;
            _remaining = health;
            _reflect = reflect;

            SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingLayerName = "Actors";

            // The sprite is 1 x 1 unit (16 x 16 px at 16 PPU), so the scale is the wall's size.
            transform.localScale = new Vector3(width, thickness, 1f);

            BoxCollider2D wallCollider = gameObject.AddComponent<BoxCollider2D>();
            wallCollider.size = Vector2.one;

            if (user != null)
            {
                foreach (Collider2D userCollider in user.GetComponentsInChildren<Collider2D>())
                {
                    Physics2D.IgnoreCollision(wallCollider, userCollider);
                }
            }

            GetComponent<Health>().AddAbsorber(this, true);
            Destroy(gameObject, duration);
        }

        public int AbsorbDamage(DamageInfo damage)
        {
            if (_remaining <= 0)
            {
                return 0;
            }

            int absorbed = Mathf.Min(_remaining, damage.Amount);
            _remaining -= absorbed;
            _reflect.TryReflect(_user, damage, absorbed);

            if (_remaining <= 0)
            {
                Destroy(gameObject);
            }

            // Always 0: the wall's Health never takes damage, so it can't raise the player's death events.
            return 0;
        }
    }
}
