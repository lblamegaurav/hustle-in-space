using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SpaceSim.Editor
{
    [InitializeOnLoad]
    public static class ForceScriptCompilation
    {
        static ForceScriptCompilation()
        {
            EditorApplication.delayCall += Request;
        }

        [MenuItem("Space Sim/Force Script Recompile", priority = 30)]
        public static void Request()
        {
            Debug.Log("[Space Sim] Requesting CompilationPipeline.RequestScriptCompilation()...");
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}
