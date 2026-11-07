using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        private static bool CoralContrastV311
        {
            get
            {
#if DEEPSEA_V31_1
                return true;
#else
                return false;
#endif
            }
        }

        private sealed class CoralTreatmentV311
        {
            public Transform Plant;
            public MeshRenderer Renderer;
            public Material OriginalMaterial;
            public Material EnhancedMaterial;
            public Vector3 OriginalScale;
            public Vector3 EnhancedScale;
        }

        private readonly List<CoralTreatmentV311> coralTreatmentsV311 = new List<CoralTreatmentV311>();

        private void ApplyCoralContrastV311()
        {
            if (!CoralContrastV311 || coralTreatmentsV311.Count != 0) return;

            var table = CreateCoralMaterialV311(
                1, "V311_AmberTableCyanRim", new Color(.35f, .98f, .92f),
                2.2f, 1.75f, 1.35f, .85f, true);
            var anemone = CreateCoralMaterialV311(
                0, "V311_MagentaAnemoneGoldTips", new Color(1f, .85f, .35f),
                2f, 2.0125f, 1.6875f, .95f, false);

            foreach (var plant in floraV31)
            {
                int species = morphologyV31[plant];
                if (species != 4 && species != 5) continue;

                var renderer = plant.GetComponent<MeshRenderer>();
                var original = plant.localScale;
                var multiplier = species == 4 ? new Vector3(1.25f, 1.05f, 1.25f)
                                              : new Vector3(1.15f, 1.22f, 1.15f);
                var wanted = Vector3.Scale(original, multiplier);
                var bounds = plant.GetComponent<MeshFilter>().sharedMesh.bounds;
                var treatment = new CoralTreatmentV311
                {
                    Plant = plant,
                    Renderer = renderer,
                    OriginalMaterial = renderer.sharedMaterial,
                    EnhancedMaterial = species == 4 ? table : anemone,
                    OriginalScale = original,
                    EnhancedScale = FitCoralGrowthV311(plant, bounds, original, wanted)
                };
                coralTreatmentsV311.Add(treatment);
                SetCoralTreatmentV311(treatment, true);
            }
        }

        private Material CreateCoralMaterialV311(
            int band, string name, Color tint, float power, float rim, float edge, float glow, bool table)
        {
            var material = new Material(_v31CoralSpeciesMats[band]) { name = name };
            material.SetColor("_RimColor", tint);
            material.SetFloat("_RimPower", power);
            material.SetFloat("_RimIntensity", rim);
            material.SetFloat("_EdgeGlowBoost", edge);
            material.SetFloat("_SubsurfaceGlow", glow);
            material.SetFloat("_TableGrowthProfile", table ? 1f : 0f);
            _owned.Add(material);
            return material;
        }

        private static Vector3 FitCoralGrowthV311(Transform plant, Bounds bounds, Vector3 original, Vector3 wanted)
        {
            // Interpolate only the growth. The original safe fit is never shrunk.
            var parentMatrix = plant.parent.localToWorldMatrix;
            var originalMatrix = parentMatrix * Matrix4x4.TRS(plant.localPosition, plant.localRotation, original);
            var wantedMatrix = parentMatrix * Matrix4x4.TRS(plant.localPosition, plant.localRotation, wanted);
            float ceiling = SeaLifeRuntime.Sea - .30f;
            float fraction = 1f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                float start = originalMatrix.MultiplyPoint3x4(corner).y;
                float end = wantedMatrix.MultiplyPoint3x4(corner).y;
                if (end > ceiling && end > start)
                    fraction = Mathf.Min(fraction, Mathf.Clamp01((ceiling - start) / (end - start)));
            }
            return Vector3.Lerp(original, wanted, fraction);
        }

        private static void SetCoralTreatmentV311(CoralTreatmentV311 treatment, bool enhanced)
        {
            treatment.Plant.localScale = enhanced ? treatment.EnhancedScale : treatment.OriginalScale;
            treatment.Renderer.sharedMaterial = enhanced ? treatment.EnhancedMaterial : treatment.OriginalMaterial;
        }

        private void SetCoralTreatmentV311(bool enhanced)
        {
            foreach (var treatment in coralTreatmentsV311)
                SetCoralTreatmentV311(treatment, enhanced);
        }

        [Serializable]
        private sealed class CoralEvidenceV311
        {
            public int tables, anemones, nested, limitedGrowth;
            public float minimumTipClearance;
            public CoralSiteV311[] views;
        }

        [Serializable]
        private sealed class CoralSiteV311
        {
            public string species;
            public Vector3 position;
            public float rootDepth, closeSailFov, farSailFov;
            public int index;
        }

        private int SelectCoralViewV311(Transform[] plants)
        {
            // Prefer an exposed specimen under the ordinary sailing camera direction.
            var direction = game.view.transform.forward;
            float best = float.NegativeInfinity;
            int selected = 0;
            for (int i = 0; i < plants.Length; i++)
            {
                var plant = plants[i];
                var center = plant.GetComponent<Renderer>().bounds.center;
                var depth = SeaLifeRuntime.Sea - plant.position.y;
                float neighbor = floraV31.Where(t => t != plant)
                    .Min(t => Vector3.Distance(t.position, plant.position));
                bool blocked = Physics.Linecast(center - direction * 104f, center,
                    ~0, QueryTriggerInteraction.Ignore);
                float score = Mathf.Min(neighbor, 4f) - Mathf.Abs(depth - 2.2f) * 2f;
                if (blocked) score -= 20f;
                if (!SeaBayExpansionAndBuildSpaceV28.ValidBoatPosition(plant.position.x + 4.8f, plant.position.z + 1.8f))
                    score -= 30f;
                if (score <= best) continue;
                selected = i;
                best = score;
            }
            return selected;
        }

        private IEnumerator ReviewCoralContrastV311()
        {
            var report = new CoralEvidenceV311
            {
                tables = coralTreatmentsV311.Count(c => morphologyV31[c.Plant] == 4),
                anemones = coralTreatmentsV311.Count(c => morphologyV31[c.Plant] == 5),
                nested = coralTreatmentsV311.Count(c => c.Plant.parent != worldRoot),
                views = new CoralSiteV311[2],
                minimumTipClearance = float.PositiveInfinity
            };

            Check(report.tables == speciesV31[4] && report.anemones == speciesV31[5] &&
                  report.tables > 0 && report.anemones > 0,
                "v31.1 all table corals and anemones receive their species treatment");
            Check(report.nested > 0, "v31.1 nested meadow specimens are included");
            Check(coralTreatmentsV311.Select(c => c.EnhancedMaterial).Distinct().Count() == 2 &&
                  coralTreatmentsV311.All(c => c.Renderer.sharedMaterial == c.EnhancedMaterial &&
                    c.EnhancedMaterial.shader.isSupported && c.EnhancedMaterial.GetFloat("_RimIntensity") > 0),
                "v31.1 exactly two supported contrast materials are shared");
            Check(floraV31.Where(t => morphologyV31[t] != 4 && morphologyV31[t] != 5)
                .All(t => t.GetComponent<MeshRenderer>().sharedMaterial.GetFloat("_RimIntensity") == 0),
                "v31.1 other flora retain their original lighting");

            foreach (var coral in coralTreatmentsV311)
            {
                bool table = morphologyV31[coral.Plant] == 4;
                if (coral.EnhancedScale.y < coral.OriginalScale.y * (table ? 1.05f : 1.22f) - .001f)
                    report.limitedGrowth++;
                foreach (var point in coral.Plant.GetComponent<MeshFilter>().sharedMesh.vertices)
                    report.minimumTipClearance = Mathf.Min(report.minimumTipClearance,
                        SeaLifeRuntime.Sea - coral.Plant.TransformPoint(point).y);
            }
            Check(report.minimumTipClearance >= .299f, "v31.1 enlarged specimens preserve the water clearance");

            float originalZoom = customSailZoom;
            var arguments = Environment.GetCommandLineArgs();
            for (int kind = 4; kind <= 5; kind++)
            {
                string name = kind == 4 ? "table-coral" : "anemone";
                var plants = coralTreatmentsV311.Where(c => morphologyV31[c.Plant] == kind)
                    .Select(c => c.Plant).ToArray();
                int chosen = SelectCoralViewV311(plants);
                int option = Array.IndexOf(arguments, "-coral-" + name + "-sample");
                if (option >= 0 && option + 1 < arguments.Length &&
                    int.TryParse(arguments[option + 1], out int requested) &&
                    requested >= 0 && requested < plants.Length)
                    chosen = requested;
                var sample = plants[chosen];

                if (arguments.Contains("-coral-survey"))
                {
                    for (int i = 0; i < plants.Length; i++)
                    {
                        var candidate = plants[i];
                        yield return VillageFrameV31(name + "-survey-" + i,
                            candidate.GetComponent<Renderer>().bounds.center, 1.9f, 36f, -18f);
                    }
                }
                var site = new CoralSiteV311
                {
                    species = name,
                    index = chosen,
                    position = sample.position,
                    rootDepth = SeaLifeRuntime.Sea - sample.position.y
                };
                report.views[kind - 4] = site;
                Vector3 target = sample.GetComponent<Renderer>().bounds.center;
                float yaw = -18f;

                SetCoralTreatmentV311(false);
                yield return VillageFrameV31(name + "-detail-before", target, 1.9f, 36f, yaw);
                SetCoralTreatmentV311(true);
                yield return VillageFrameV31(name + "-detail-after", target, 1.9f, 36f, yaw);

                var boatPosition = new Vector3(sample.position.x + 4.8f, SeaLifeRuntime.Sea, sample.position.z + 1.8f);
                game.PlaceBoat(boatPosition, 90);
                game.SnapWaterCamera();
                for (int zoom = 0; zoom < 2; zoom++)
                {
                    customSailZoom = zoom == 0 ? 8.5f : 34f;
                    yield return new WaitForSeconds(1.6f);
                    var screen = game.view.WorldToViewportPoint(target);
                    Check(!game.view.orthographic && screen.z > 0 && screen.x > 0 && screen.x < 1 &&
                          screen.y > 0 && screen.y < 1, name + " target lies inside the normal sailing view");
                    if (zoom == 0) site.closeSailFov = game.view.fieldOfView;
                    else site.farSailFov = game.view.fieldOfView;
                    string shot = name + (zoom == 0 ? "-sail-near" : "-sail-far");
                    SetCoralTreatmentV311(false);
                    yield return Capture(shot + "-before");
                    SetCoralTreatmentV311(true);
                    yield return Capture(shot + "-after");
                }
                Check(site.farSailFov > site.closeSailFov + 10f, name + " native sailing view changes field of view");
            }
            customSailZoom = originalZoom;
            File.WriteAllText(Path.Combine(evidence, "coral-contrast.json"), JsonUtility.ToJson(report, true));
        }
    }
}
