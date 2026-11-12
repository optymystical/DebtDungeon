using UnityEngine;

namespace DebtDungeon.AI
{

    public enum UIAnchorPosition
    {
        BottomLeft,
        TopLeft,
        BottomRight,
        TopRight
    }
    /// <summary>
    /// Developer diagnostic tool providing time-scale speed controls and unit status overlays.
    /// </summary>
    public class AIDebugController : MonoBehaviour
    {
        [Header("Debug Controls")]
        [Tooltip("Toggle drawing debug state labels in the Game view.")]
        [SerializeField] private bool _showGameViewLabels = true;

        [Header("UI Placement")]
        [Tooltip("Screen anchor corner for the time-scale control box.")]
        [SerializeField] private UIAnchorPosition _anchor = UIAnchorPosition.TopLeft;

        [Tooltip("Padding offset from screen edges.")]
        [SerializeField] private Vector2 _offset = new Vector2(20f, 20f);

        [SerializeField] private Vector2 _boxSize = new Vector2(270f, 70f);

        private float _currentTimeScale = 1.0f;

        private void OnGUI()
        {
            // Calculate screen position based on selected anchor
            Rect panelRect = CalculatePanelRect();

            // --- 1. TIME-SCALE SLIDER FOR COMBAT OBSERVATION ---
            GUILayout.BeginArea(panelRect, GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Time Scale: {_currentTimeScale:F2}x", GUILayout.Width(110));

            if (GUILayout.Button("0.5x", GUILayout.Width(40)))
            {
                _currentTimeScale = 0.5f;
            }
            if (GUILayout.Button("1.0x", GUILayout.Width(40)))
            {
                _currentTimeScale = 1.0f;
            }
            if (GUILayout.Button("2.0x", GUILayout.Width(40)))
            {
                _currentTimeScale = 2.0f;
            }
            GUILayout.EndHorizontal();

            float newTimeScale = GUILayout.HorizontalSlider(_currentTimeScale, 0.1f, 3.0f);
            if (Mathf.Abs(newTimeScale - _currentTimeScale) > 0.01f)
            {
                _currentTimeScale = newTimeScale;
                Time.timeScale = _currentTimeScale;
            }
            GUILayout.EndArea();

            // --- 2. GAME VIEW FLOATING STATE LABELS ---
            if (!_showGameViewLabels || Camera.main == null)
            {
                return;
            }

            FactionAI2D[] agents = FindObjectsByType<FactionAI2D>(FindObjectsSortMode.None);
            Camera cam = Camera.main;

            for (int i = 0; i < agents.Length; i++)
            {
                FactionAI2D ai = agents[i];
                if (ai == null)
                {
                    continue;
                }

                CombatAgent2D combat = ai.GetComponent<CombatAgent2D>();
                if (combat == null || !combat.IsAlive)
                {
                    continue;
                }

                Vector3 screenPos = cam.WorldToScreenPoint(ai.transform.position + Vector3.up * 0.85f);
                if (screenPos.z < 0)
                {
                    continue;
                } // Behind camera

                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 11;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = combat.Faction != null ? combat.Faction.DebugColor : Color.white;

                string labelText = $"{ai.CurrentState}\nHP: {combat.CurrentHealth}";
                Vector2 size = style.CalcSize(new GUIContent(labelText));
                Rect labelRect = new Rect(screenPos.x - size.x / 2f, Screen.height - screenPos.y - size.y / 2f, size.x, size.y);

                GUI.Label(labelRect, labelText, style);
            }
        }
        private Rect CalculatePanelRect()
        {
            float x = _offset.x;
            float y = _offset.y;

            switch (_anchor)
            {
                case UIAnchorPosition.TopLeft:
                    x = _offset.x;
                    y = _offset.y;
                    break;
                case UIAnchorPosition.TopRight:
                    x = Screen.width - _boxSize.x - _offset.x;
                    y = _offset.y;
                    break;
                case UIAnchorPosition.BottomLeft:
                    x = _offset.x;
                    y = Screen.height - _boxSize.y - _offset.y;
                    break;
                case UIAnchorPosition.BottomRight:
                    x = Screen.width - _boxSize.x - _offset.x;
                    y = Screen.height - _boxSize.y - _offset.y;
                    break;
            }

            return new Rect(x, y, _boxSize.x, _boxSize.y);
        }
    }
}
