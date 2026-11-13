using UnityEngine;
using UnityEngine.InputSystem;

namespace DebtDungeon.Player
{
    public class MouseLean : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _player;
        [SerializeField] private Camera _Camera;

        [Header("Settings")]
        [Range(0f, 1f)] public float _leanSensitivity = 0.2f;

        private Vector2 targetPosition;

        private void Update()
        {
            Vector3 mouseWorldPos = _Camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 mouseOffset = (Vector2)mouseWorldPos - (Vector2)_player.position;

            // A sensitivity of 0.2 means the camera leans 20% of the distance toward the mouse
            targetPosition = (Vector2)_player.position + (mouseOffset * _leanSensitivity);

            transform.position = targetPosition;
        }
    }
}
