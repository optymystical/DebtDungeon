using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class PlayerEnergy : MonoBehaviour
    {
        [SerializeField] private int _energyMax = 100;
        [SerializeField] private int _energyCurrent = 100;
        [SerializeField] private int _rechargeRate = 10; // per second

        private float _timer = 0f;
        public int EnergyMax => _energyMax;
        public int EnergyCurrent => _energyCurrent;

        // Update is called once per frame
        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= 1f)
            {
                RechargeEnergy(_rechargeRate);
                _timer = 0f; 
            }
        }

        public void RechargeEnergy(int rechargeRate)
        {
            _energyCurrent += rechargeRate;
            if (_energyCurrent >= _energyMax)
            {
                _energyCurrent = _energyMax;
            }
        }

        public bool UseEnergy(int energy)
        {
            if (_energyCurrent - energy < 0)
            {
                return false;
            }
            _energyCurrent -= energy;
            return true;
        }
    }
}
