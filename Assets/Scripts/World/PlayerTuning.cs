using UnityEngine;
namespace Rustbound.World
{
    [CreateAssetMenu(menuName="Rustbound/Player tuning")]
    public sealed class PlayerTuning : ScriptableObject
    {
        public float moveSpeed=4.5f;
        public float gravity=28;
        public float jumpHeight=2.2f;
        public float dashSpeed=10;
        public float dashDuration=.15f;
        public float dashCooldown=1.2f;
        public float jumpGrace=.1f;
    }
}
