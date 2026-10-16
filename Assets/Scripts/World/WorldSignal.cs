using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Rustbound.World
{
    public sealed class WorldSignal : MonoBehaviour,IWorldReset
    {
        public bool plate;
        public Sprite off,on;
        [Min(0)] public int batteryCost;
        [Min(1)] public int energyPerBattery=40;
        [Tooltip("按钮/拉杆可反复开关；供电只在首次操作时消耗。压板默认为持续踩压模式。")]
        public bool toggle;
        bool paid;
        public UnityEvent<bool> onChanged=new UnityEvent<bool>();
        public UnityEvent onInsufficientPower=new UnityEvent();
        public bool Active { get; private set; }
        public int ActivationEnergyCost=>plate||paid?0:batteryCost*energyPerBattery;
        [Tooltip("压板默认松开后关闭；开启此项则首次踩下后保持开启。压板始终免费。")]
        public bool latchPlate;
        readonly HashSet<Collider2D> occupants=new HashSet<Collider2D>();
        void Awake(){if(plate)batteryCost=0;}
        public void Activate(){TryActivate();}
        public bool TryActivate()
        {
            if(Active){if(toggle&&!plate){Active=false;GetComponent<SpriteRenderer>().sprite=off;onChanged.Invoke(false);}return true;}
            if(!plate&&!paid&&batteryCost>0 && (LevelSession.Current==null||!LevelSession.Current.TrySpendBatteries(batteryCost*energyPerBattery)))
            {onInsufficientPower.Invoke();PlayerHint.Show("电量不足，请先充能");return false;}
            paid=true;Active=true;GetComponent<SpriteRenderer>().sprite=on;onChanged.Invoke(true);return true;
        }
        public void Press(Collider2D c){if(plate&&c!=null&&(c.GetComponentInParent<PlayerMotor>()!=null||c.GetComponentInParent<PushableCrate>()!=null)){occupants.Add(c);if(!Active)TryActivate();}}
        public void Release(Collider2D c){if(!plate)return;occupants.Remove(c);if(occupants.Count==0&&!latchPlate&&Active){Active=false;GetComponent<SpriteRenderer>().sprite=off;onChanged.Invoke(false);}}
        void OnTriggerEnter2D(Collider2D c)=>Press(c);
        void OnTriggerExit2D(Collider2D c)=>Release(c);
        void OnTriggerStay2D(Collider2D c)=>Press(c);
        public void ResetWorld(bool full){occupants.Clear();if(full||(plate&&!latchPlate)){paid=false;Active=false;onChanged.Invoke(false);}GetComponent<SpriteRenderer>().sprite=Active?on:off;}
    }
}
