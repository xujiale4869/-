using UnityEngine;

namespace Deepsea
{
    [CreateAssetMenu(menuName = "Deepsea/Bay Settings", fileName = "BaySettings")]
    public sealed class BaySettings : ScriptableObject
    {
        [Header("一级近海工作艇")]
        [Range(2, 12)] public float topSpeed = 7.5f;
        public float reverseSpeed = 2.6f;
        public float acceleration = 2.8f;
        public float steering = 58f;
        public float fuelSeconds = 720f;
        public int cargoSlots = 10;
        public int passengerSlots = 4;
        [Header("探测与交互")]
        public float scanRadius = 30f;
        public float improvedScanRadius = 48f;
        public float scanCooldown = 6f;
        public float interactionRadius = 5.5f;
        public float interactionSpeed = 2.2f;
        public float dockingRadius = 9f;
        [Header("村庄建设：试验价格，尚未平衡")]
        public int winchParts = 6;
        public int winchEquipment = 1;
        public int chartParts = 4;
        public int chartRecords = 1;
        public int netParts = 4;
        public int netEquipment = 1;
        [Header("海况与镜头")]
        public float waveHeight = .15f;
        public float cameraSize = 23f;
        public float cameraPitch = 55f;
        public float seaLimit = 160f;
        public Color seaColor = new Color(.045f, .15f, .19f);
        public Color fogColor = new Color(.12f, .22f, .26f);
    }
}
