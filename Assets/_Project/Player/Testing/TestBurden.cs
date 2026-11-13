using UnityEngine;

namespace DebtDungeon.Player
{
    public class TestBurden : MonoBehaviour
    {
        [SerializeField] private PlayerController _targetScript;
        [SerializeField] private bool _testing = false;
        private bool _active = false;

        private void Update()
        {
            if (_active && _testing)
            {
                TestSetBurdenSteps(1);
                _active = false;
            }
            if (!_testing)
            {
                _active = true;
            }
        }

        private void TestSetBurdenSteps(int amount) {
            _targetScript.SetBurdenSteps(amount);
        }
    }
}
