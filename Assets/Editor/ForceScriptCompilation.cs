using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SpaceSim.Editor
{
    namespace SpaceSim.Editor
{
    public static class ForceScriptCompilation
    {
        [UnityEditor.MenuItem("Space Sim/Force Script Recompile", priority = 30)]
        public static void Request()
        {
            UnityEngine.Debug.Log("[Space Sim] Requesting CompilationPipeline.RequestScriptCompilation()...");
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
    }
}
}
