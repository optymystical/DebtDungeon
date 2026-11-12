using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

namespace DebtDungeon.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private float _baseMoveSpeed = 1;
        private Vector2 _moveInput;
        private PlayerInput _playerInput;
        private Vector3 _mouseWorldPos; 
        private Vector2 _mouseDirection;
        [SerializeField] private float _turnSpeed = 1;

        private InputAction _attackAction;
        private InputAction _ability1Action;
        private InputAction _ability2Action;
        private InputAction _ability3Action;
        private InputAction _ability4Action;
        private InputAction _ability5Action;
        private InputAction _defensiveAction;
        private InputAction _interactAction;
        private InputAction _useItemAction;
        private InputAction _openInventoryAction;
        private InputAction _openSkillsAction;
        private InputAction _pauseAction;

        private void Awake()
        {
            _attackAction = InputSystem.actions.FindAction("Player/Attack");
            _ability1Action = InputSystem.actions.FindAction("Player/Ability 1");
            _ability2Action = InputSystem.actions.FindAction("Player/Ability 2");
            _ability3Action = InputSystem.actions.FindAction("Player/Ability 3");
            _ability4Action = InputSystem.actions.FindAction("Player/Ability 4");
            _ability5Action = InputSystem.actions.FindAction("Player/Ability 5");
            _defensiveAction = InputSystem.actions.FindAction("Player/Defensive Ability");
            _interactAction = InputSystem.actions.FindAction("Player/Interact");
            _useItemAction = InputSystem.actions.FindAction("Player/Use Item");
            _openInventoryAction = InputSystem.actions.FindAction("Player/Open Inventory");
            _openSkillsAction = InputSystem.actions.FindAction("Player/Open Skills");
            _pauseAction = InputSystem.actions.FindAction("Player/Pause");
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // Unlock the cursor to the center of the screen
            Cursor.lockState = CursorLockMode.None;
            // Make the cursor visible  NOT WORKING
            Cursor.visible = true;

            _playerInput = GetComponent<PlayerInput>();
        }

        // Update is called once per frame
        private void Update()
        {
            GatherInput();
            FaceCursor();
        }

        // FixedUpdate is called once per frame
        private void FixedUpdate()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = _moveInput * _baseMoveSpeed;

                // Rotates sprite to face destination while moving
                if (_moveInput != Vector2.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, _moveInput);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
                }
                _rb.angularVelocity = 0;
            }
        }

        // Gets movement input from user
        private void OnMove(InputValue inputValue)
        {
            _moveInput = inputValue.Get<Vector2>();
        }

        // Reacts to keyboard and mouse input
        private void GatherInput()
        {
            _mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            _mouseWorldPos.z = 0;
            _mouseDirection = (_mouseWorldPos - transform.position).normalized;
            //Debug.DrawRay(transform.position, _mouseDirection * 3, Color.red);

            // Can the player click left and right mouse button at once?
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Not used for now
            }
            else if (_attackAction.WasPressedThisFrame())
            {
                
            }

            // Can the player click a mouse button and any of these keys at once?
            if (_ability1Action.WasPressedThisFrame())
            {
                
            }
            else if (_ability2Action.WasPressedThisFrame())
            {
                
            }
            else if (_ability3Action.WasPressedThisFrame())
            {
                
            }
            else if (_ability4Action.WasPressedThisFrame())
            {
                
            }
            else if (_ability5Action.WasPressedThisFrame())
            {
                
            }
            else if (_defensiveAction.WasPressedThisFrame())
            {
                
            }
            else if (_interactAction.WasPressedThisFrame())
            {
                
            }
            else if (_useItemAction.WasPressedThisFrame())
            {
                // Not used for now
            }
            // Menu keys
            else if (_openInventoryAction.WasPressedThisFrame())
            {
                // Not used for now
            }
            else if (_openSkillsAction.WasPressedThisFrame())
            {
                
            }
            else if (_pauseAction.WasPressedThisFrame())
            {
                
            }

            // Scroll through consumables
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (scroll.y > 0)
            {
                // Not used for now
            }
            else if (scroll.y < 0)
            {
                // Not used for now
            }
        }

        private void FaceCursor()
        {
            // Rotates sprite to face cursor when stationary
            if (_moveInput == Vector2.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, _mouseDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
            }
        }
    }
}
