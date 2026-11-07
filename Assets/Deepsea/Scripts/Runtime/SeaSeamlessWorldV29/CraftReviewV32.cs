using System.Collections;
using System.Linq;
using UnityEngine;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        const string CraftShaderV32 = "Deepsea/StylizedCoastalCraftV32";

        IEnumerator ReviewCraftV32()
        {
            Check(Screen.width == 1920 && Screen.height == 1080, "v32 capture is 1920x1080");
            Check(housesV31.All(h =>
            {
                var model = h.GetChild(0);
                var mesh = model.GetComponent<MeshFilter>().sharedMesh;
                var materials = model.GetComponent<MeshRenderer>().sharedMaterials;
                return mesh.name.StartsWith("Village_V32_") && mesh.colors.Length == mesh.vertexCount
                    && materials.Length == mesh.subMeshCount && materials.Any(m => m.name == "V32_CoastalSlate")
                    && materials.Any(m => m.name == "V32_SeaStone") && materials.Any(m => m.shader.name != CraftShaderV32);
            }), "v32 all three authored houses render baked tint, AO and separate material slots");

            var annex = boatyardAnnexV31.GetComponent<MeshFilter>();
            var collision = boatyardAnnexV31.GetComponent<MeshCollider>();
            Check(annex.sharedMesh.name == "Village_V32_Slipway"
                && collision != null && collision.sharedMesh != annex.sharedMesh,
                "v32 crafted slipway uses original collision mesh");
            Check(courtsV31.All(c => c.GetComponentsInChildren<MeshRenderer>()
                .Any(r => r.enabled && r.sharedMaterial.name == "V32_WeatheredPlank")),
                "v32 craft boards survive forecourt static combining");
            Check(bay.VerifyCraftPierV32(true) && bay.VerifyCraftPierV32(false),
                "v32 pier preview and placed factory share board geometry and retain collider rules");

            var front = game.exterior.Find("室外活动/海洋小馆 · 码头入口");
            var deck = front != null ? front.Find("小馆木平台") : null;
            Check(deck != null && deck.GetComponent<MeshFilter>().sharedMesh.name == "V32_SeparateDeckBoards",
                "v32 gallery landing renders separated boards");
            Check(game.world.GetComponentsInChildren<MeshRenderer>().Any(r =>
                r.name.StartsWith("EN_Pier_") && r.sharedMaterials.Any(m => m != null && m.shader.name == CraftShaderV32)),
                "v32 main pier has a crafted wood material slot");

            var tavern = housesV31[1];
            Vector3 harbor = Vector3.Lerp(tavern.position,front != null ? front.position : game.dockBerth,.58f);
            harbor += Vector3.right * 4f;
            harbor.y = 1.2f;
            yield return VillageFrameV31("v32-preview",harbor,20f,44f,-66f);
            yield return VillageFrameV31("v32-wood-and-house-craft-closeup",
                tavern.TransformPoint(new Vector3(.35f,2f,2.7f)),7.0f,37f,tavern.eulerAngles.y-151f);
            yield return VillageFrameV31("v32-slipway-craft",annex.GetComponent<Renderer>().bounds.center,
                4.8f,40f,-90f);
        }
    }
}
