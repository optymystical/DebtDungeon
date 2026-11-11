using UnityEngine;
using UnityEngine.InputSystem;

namespace DebtDungeon.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private float _speed;
        public Vector2 _moveInput; // private

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
        
        }

        // Update is called once per frame
        private void Update()
        {

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
