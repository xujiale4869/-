using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace Deepsea.Editor
{
    // All visible meshes in this sample use Deepsea/StylizedSurface, Ocean or
    // SoftLine. URP's default material reference would otherwise compile hundreds
    // of thousands of unused Lit variants. Custom UsePass variants belong to
    // Deepsea/StylizedSurface and are kept. Remove this filter when adding Lit art.
    public sealed class SampleShaderFilter : IPreprocessShaders
    {
        public int callbackOrder { get { return 0; } }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if(shader.name=="Universal Render Pipeline/Lit"||shader.name=="Standard")data.Clear();
        }
    }
}
