using UnityEngine;
using Rustbound.UI;
namespace Rustbound.World
{
    public sealed class WorldHud : MonoBehaviour
    {
        public SpriteNumberLabel fragments,time,deaths,level;
        public UnityEngine.UI.Image dashBar;
        public UnityEngine.UI.Button settingsButton,pauseButton;
        public UnityEngine.UI.Image levelName;
        public Sprite[] levelNames;
        public SpriteNumberLabel batteries,bag;
        LevelSession boundSession;
        public void Bind(LevelSession session)
        {
            Unbind();boundSession=session;
            if(settingsButton!=null)settingsButton.onClick.AddListener(OpenSettings);
            if(pauseButton!=null)pauseButton.onClick.AddListener(OpenSettings);
            if(session!=null&&levelName!=null&&levelNames!=null&&session.level>0&&session.level<=levelNames.Length)levelName.sprite=levelNames[session.level-1];
            Refresh(session);
        }
        void OpenSettings(){if(boundSession!=null)boundSession.OpenSettings();}
        void Unbind(){if(settingsButton!=null)settingsButton.onClick.RemoveListener(OpenSettings);if(pauseButton!=null)pauseButton.onClick.RemoveListener(OpenSettings);boundSession=null;}
        void OnDestroy()=>Unbind();
        static void Value(SpriteNumberLabel label,string value){if(label!=null)label.SetValue(value);}
        public void Refresh(LevelSession session)
        {
            if(session==null||session.player==null)return;
            int s=(int)session.Seconds;
            Value(fragments,session.Collected.ToString("00")+"/"+session.Total.ToString("00"));
            var energy=session.player.GetComponent<RobotEnergy>();
            int charge=energy!=null&&energy.capacity>0?Mathf.RoundToInt(100f*energy.Charge/energy.capacity):0;
            Value(batteries,charge+"%");Value(bag,energy!=null?energy.Charge.ToString():"0");
            Value(time,(s/60).ToString("00")+":"+(s%60).ToString("00")+"."+((int)(session.Seconds*100)%100).ToString("00"));
            Value(deaths,session.Deaths.ToString());Value(level,session.level.ToString("00"));
            if(dashBar!=null)dashBar.fillAmount=1-session.player.DashRemaining/Mathf.Max(.01f,session.player.tuning!=null?session.player.tuning.dashCooldown:1.2f);
        }
    }
}
