using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class M1InspectQuaterniusAsset
{
 public static void Report(){const string path="Assets/Art/Models/External_Quaternius/BlueDemon.fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var all=AssetDatabase.LoadAllAssetsAtPath(path);var clips=all.OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Select(c=>c.name+" | "+c.length.ToString("F2")+"s").ToArray();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var lines="Model: "+path+"\nClips: "+clips.Length+"\n"+string.Join("\n",clips)+"\nRenderers: "+(prefab?prefab.GetComponentsInChildren<Renderer>(true).Length:0);File.WriteAllText("D:/ChatGPT文件/山海驭灵录/04_验证记录/BlueDemon_asset_inspection.txt",lines);Debug.Log(lines);}
}
