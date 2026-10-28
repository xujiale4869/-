using UnityEngine;

namespace Deepsea
{
    // This prefab contains the reusable coast, not a second gameplay implementation.
    public sealed class CoastBlockoutRegion : MonoBehaviour
    {
        public Transform water;
        public Transform referenceFocus, landingFeet, berth, lighthouse;
        public Material clay;
        public int rockMasses, terrainTriangles;
        public string layoutVersion="EastCoast-Blockout-3-Natural";
    }
}
