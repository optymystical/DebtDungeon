using log4net.Util;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

namespace DebtDungeon.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private float _speed;
        public Vector2 _moveInput; // private
        public Vector3 _mouseWorldPos; // private
        public Vector2 _mouseDirection; // private

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // Unlock the cursor to the center of the screen
            Cursor.lockState = CursorLockMode.None;
            // Make the cursor visible
            Cursor.visible = true;
        }

        // Update is called once per frame
        private void Update()
        {
            _mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            _mouseWorldPos.z = 0;
            _mouseDirection = (_mouseWorldPos - transform.position).normalized;
            Debug.DrawRay(transform.position, _mouseDirection * 3, Color.red);

            // Can the player click left and right mouse button at once?
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Debug.Log("Left Click");
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Debug.Log("Right Click");
            }

            // Can the player click a mouse button and any of these keys at once?
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("1 Key");
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("2 Key");
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                Debug.Log("3 Key");
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                Debug.Log("4 Key");
            }
            else if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                Debug.Log("5 Key");
            }
            else if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                Debug.Log("Q Key");
            }
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                Debug.Log("F Key");
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log("E Key");
            }
            // Menu keys
            else if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                Debug.Log("I Key");
            }
            else if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Debug.Log("Tab Key");
            }
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Debug.Log("Esc Key");
            }

            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (scroll.y > 0)
            {
                Debug.Log("Scroll Up");
            }
            else if (scroll.y < 0)
            {
                Debug.Log("Scroll Down");
            }
        }

        private void FixedUpdate()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = _moveInput * _speed * Time.deltaTime;

                // Rotates sprite to face destination waypoint
                if (_moveInput != Vector2.zero)
                {
                    float angle = (Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg) - 90f;
                    transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
                _rb.angularVelocity = 0;
            }
        }

        // Gets movement input from user
        private void OnMove(InputValue inputValue)
        {
            _moveInput = inputValue.Get<Vector2>();
        }
    }
}
