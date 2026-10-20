using System.Linq;
using UnityEditor;
using UnityEngine;
public static class M1InspectAssetsV065
{
 public static void Inspect()
 {
  string[] paths={"Assets/Art/Models/External_KenneyNature/tree_oak.fbx","Assets/Art/Models/External_KenneyNature/tree_pineTallA_detailed.fbx","Assets/Art/Models/External_KenneyNature/rock_largeA.fbx","Assets/Art/Models/External_KenneyNature/grass_large.fbx","Assets/Art/Models/Blender_Ready/LanJiao_SpiritBeast.fbx","Assets/Art/Models/Blender_Custom/RedTailFox_v064/RedTail_SpiritFox_v064.fbx","Assets/Art/Models/External_Quaternius/BlueDemon.fbx"};
  foreach(var path in paths){var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);Debug.Log("ASSET "+path+" PRESENT="+(go!=null));if(!go)continue;foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true)){var m=mf.sharedMesh;if(m)Debug.Log("MESH "+path+" "+mf.name+" BOUNDS="+m.bounds.size+" VERTS="+m.vertexCount);}foreach(var sm in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())Debug.Log("MAT "+path+" "+sm.name+" COLOR="+sm.color+" SHADER="+(sm.shader?sm.shader.name:"NULL"));}
 }
}

