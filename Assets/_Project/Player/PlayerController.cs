using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DebtDungeon.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private HitFeedback _hitFeedback;

        [Header("Settings")]
        [SerializeField] private float _baseMoveSpeed = 1;
        [SerializeField] private float _turnSpeed = 1;

        private Vector2 _moveInput;
        private Camera _camera;
        private Vector3 _mouseWorldPos; 
        private Vector2 _mouseDirection;

        private InputAction _moveAction;
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

        // Tests
        private int _currentBurdenStep = 0;
        private float _burdenMultiplier = 1f;

        // Water (from the floor) and status slows (from StatusEffects) are separate, so one never overwrites the other.
        private float _terrainMultiplier = 1f;
        private float _statusSpeedMultiplier = 1f;

        private void Awake()
        {
            _camera = Camera.main;
            _moveAction = InputSystem.actions.FindAction("Player/Move");
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
            // Make the cursor visible  NOT WORKING?
            Cursor.visible = true;

        }

        // Update is called once per frame
        private void Update()
        {
            // Core v1.4: no player input while paused or on the death/results screen.
            // Time.timeScale = 0 stops movement, but Update still runs and would still read keys.
            if (IsInputBlocked())
            {
                _moveInput = Vector2.zero;
                return;
            }

            GatherInput();
            FaceCursor();
        }

        // Blocked only in the frozen states, so sandbox scenes without a GameManager
        // (CurrentGameState stays at its MainMenu default there) still accept input.
        private static bool IsInputBlocked()
        {
            GameState state = GameEvents.CurrentGameState;
            return state == GameState.Paused || state == GameState.Dead || state == GameState.Won;
        }

        // FixedUpdate is called once per frame
        private void FixedUpdate()
        {
            bool isKnockingBack = _hitFeedback != null && _hitFeedback.IsKnockingBack;
            if (_rb != null && !isKnockingBack)
            {
                _rb.linearVelocity = _moveInput * _baseMoveSpeed * _burdenMultiplier * _terrainMultiplier * _statusSpeedMultiplier;

                // Rotates sprite to face destination while moving
                if (_moveInput != Vector2.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, _moveInput);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
                }
                _rb.angularVelocity = 0;
            }
        }

        // Reacts to keyboard and mouse input
        private void GatherInput()
        {
            // Read Move the same way as every other action (project-wide actions), instead of a
            // PlayerInput component also driving the same asset.
            _moveInput = _moveAction.ReadValue<Vector2>();

            _mouseWorldPos = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            _mouseWorldPos.z = 0;
            _mouseDirection = (_mouseWorldPos - transform.position).normalized;

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

        public void SetBurdenSteps(int steps)
        {
            _currentBurdenStep = Mathf.Clamp(steps, 0, 2);

            // Adjust _burdenMultiplier based on current Burden Step
            switch (_currentBurdenStep)
            {
                case 0:
                    _burdenMultiplier = 1f;
                    break;
                case 1:
                    _burdenMultiplier = 0.9f;
                    break;
                case 2:
                    _burdenMultiplier = 0.8f;
                    break;
            }
        }

        /// <summary>Speed multiplier from the ground under the player (water). 1 = normal.</summary>
        public void SetTerrainMultiplier(float multiplier)
        {
            _terrainMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>Speed multiplier from status effects (Slowed). 1 = normal. Set by StatusEffects.</summary>
        public void SetStatusSpeedMultiplier(float multiplier)
        {
            _statusSpeedMultiplier = Mathf.Max(0f, multiplier);
        }
    }
}
