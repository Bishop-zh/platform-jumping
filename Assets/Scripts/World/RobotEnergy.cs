using UnityEngine;
using UnityEngine.Events;
namespace Rustbound.World
{
    public sealed class RobotEnergy : MonoBehaviour
    {
        [Min(1)] public int capacity=100;
        [Min(0)] public int initialCharge=20;
        public int Charge {get;private set;}
        public UnityEvent<int> onChargeChanged=new UnityEvent<int>();
        void Awake(){ResetCharge();}
        public void ResetCharge(){Charge=Mathf.Clamp(initialCharge,0,capacity);onChargeChanged.Invoke(Charge);}
        public bool Spend(int amount){if(amount<0||Charge<amount)return false;Charge-=amount;onChargeChanged.Invoke(Charge);return true;}
        public void Recharge(int amount){Charge=Mathf.Clamp(Charge+amount,0,capacity);onChargeChanged.Invoke(Charge);}
    }
}
