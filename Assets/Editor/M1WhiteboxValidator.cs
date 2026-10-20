using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M1WhiteboxValidator
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/v0.4.6_Whitebox/M1_HubWorld.unity",
        "Assets/Scenes/v0.4.6_Whitebox/M1_ForestWorld.unity",
        "Assets/Scenes/v0.4.6_Whitebox/M1_CanyonWorld.unity",
        "Assets/Scenes/v0.4.6_Rendered/M1_HubWorld.unity",
        "Assets/Scenes/v0.4.6_Rendered/M1_ForestWorld.unity",
        "Assets/Scenes/v0.4.6_Rendered/M1_CanyonWorld.unity"
    };

    public static void Validate()
    {
        int failures=0;
        foreach(string path in Scenes)
        {
            Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var actors=Object.FindObjectsByType<M1Actor>();
            var managers=Object.FindObjectsByType<M1GameManager>();
            var portals=Object.FindObjectsByType<M1Portal>();
            int players=actors.Count(a=>a&&a.GetComponent<M1PlayerController>());
            int enemies=actors.Count(a=>a&&a.isEnemy);
            int tameable=actors.Count(a=>a&&a.isTameable);
            int missingScripts=0;
            foreach(var go in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject))
                if(go.GetComponents<Component>().Any(c=>c==null))missingScripts++;
            bool managerRefs=managers.Length==1&&managers[0].player&&managers[0].enemy&&managers[0].beast;
            bool pass=players==1&&enemies==1&&tameable>=4&&portals.Length>=2&&managerRefs&&missingScripts==0;
            Debug.Log($"WHITEBOX_VALIDATE scene={scene.name} pass={pass} players={players} enemies={enemies} tameable={tameable} portals={portals.Length} managerRefs={managerRefs} missingScripts={missingScripts}");
            if(!pass)failures++;
        }
        if(failures>0)throw new System.Exception("Whitebox validation failed in "+failures+" scene(s). See WHITEBOX_VALIDATE log lines.");
        Debug.Log("WHITEBOX_VALIDATE_ALL_PASS scenes="+Scenes.Length);
    }
}


