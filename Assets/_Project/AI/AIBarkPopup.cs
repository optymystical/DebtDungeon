using UnityEngine;
using TMPro;

namespace DebtDungeon.AI
{
    public enum AIBarkType
    {
        Alert,       // "!" (Spotted enemy / entering Chase)
        Investigate, // "?" (Heard noise via NoiseEventBus)
        Flee,        // "RUN!" (Morale break / fleeing)
        Rally,       // "!!" (Called to faction raid rally)
        Confused     // "..." (Lost target / returning to post)
    }

    /// <summary>
    /// Self-contained world-space TextMeshPro popup displaying short AI intent icons (!, ?, ...).
    /// Call AIBarkPopup.Spawn(...) directly—no prefab required.
    /// </summary>
    public class AIBarkPopup : MonoBehaviour
    {
        private const float FloatSpeed = 0.9f;
        private const float LifetimeSeconds = 0.85f;
        private const float PopScaleMultiplier = 1.25f;

        private TMP_Text _textMesh;
        private float _elapsed;
        private Color _baseColor;
        private Vector3 _baseScale;

        public static void Spawn(Vector3 worldPosition, AIBarkType barkType)
        {
            GameObject popupObj = new GameObject($"AIBark_{barkType}");
            popupObj.transform.position = worldPosition;

            TextMeshPro tmp = popupObj.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rect = popupObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(3f, 1.5f);
            }

            MeshRenderer renderer = popupObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 55;
            }

            AIBarkPopup popup = popupObj.AddComponent<AIBarkPopup>();
            popup.Initialize(tmp, barkType);
        }

        private void Initialize(TMP_Text textMesh, AIBarkType barkType)
        {
            _textMesh = textMesh;
            _baseScale = transform.localScale;

            switch (barkType)
            {
                case AIBarkType.Alert:
                    _textMesh.text = "!";
                    _baseColor = new Color(1f, 0.25f, 0.2f);
                    break;
                case AIBarkType.Investigate:
                    _textMesh.text = "?";
                    _baseColor = new Color(1f, 0.88f, 0.25f);
                    break;
                case AIBarkType.Flee:
                    _textMesh.text = "RUN!";
                    _baseColor = new Color(1f, 0.55f, 0.15f);
                    break;
                case AIBarkType.Rally:
                    _textMesh.text = "!!";
                    _baseColor = new Color(0.95f, 0.35f, 1f);
                    break;
                default:
                    _textMesh.text = "...";
                    _baseColor = new Color(0.8f, 0.85f, 0.95f);
                    break;
            }

            _textMesh.color = _baseColor;
            transform.localScale = _baseScale * PopScaleMultiplier;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / LifetimeSeconds);

            transform.position += Vector3.up * (FloatSpeed * Time.deltaTime);
            transform.localScale = Vector3.Lerp(_baseScale * PopScaleMultiplier, _baseScale, Mathf.Clamp01(t * 3f));

            if (_textMesh != null)
            {
                Color c = _baseColor;
                c.a = 1f - Mathf.Pow(t, 2f);
                _textMesh.color = c;
            }

            if (_elapsed >= LifetimeSeconds)
            {
                Destroy(gameObject);
            }
        }
    }
}
