using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M1ValidateScenesV066
{
 public static void Validate()
 {
  var paths=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();if(paths.Length!=3)throw new System.Exception("Expected 3 enabled scenes, got "+paths.Length);
  var names=paths.Select(p=>Path.GetFileNameWithoutExtension(p)).ToArray();var report=new StringBuilder();report.AppendLine("M1 v0.6.6 场景完整性检查");report.AppendLine("日期: "+System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));report.AppendLine("结果: PASS");
  foreach(var path in paths)
  {
   var scene=EditorSceneManager.OpenScene(path);var manager=Object.FindAnyObjectByType<M1GameManager>();if(!manager)throw new System.Exception("Missing M1GameManager: "+path);
   if(!manager.player||!manager.enemy||!manager.beast)throw new System.Exception("Manager references incomplete: "+path);
   if(!manager.player.GetComponent<M1PlayerController>())throw new System.Exception("Player controller missing: "+path);
   if(!manager.gameObject.GetComponent<M1PetSystem>())throw new System.Exception("Pet system missing: "+path);
   var camera=Camera.main;if(!camera)throw new System.Exception("Main camera missing: "+path);var follow=camera.GetComponent<M1CameraFollow>();if(!follow||follow.target!=manager.player.transform)throw new System.Exception("Camera follow target wrong: "+path);
   var actors=Object.FindObjectsByType<M1Actor>();int pets=actors.Count(a=>a.isTameable);if(actors.Count(a=>a.isEnemy)!=1||actors.Count(a=>a.GetComponent<M1PlayerController>())!=1||pets!=3)throw new System.Exception("Actor counts mismatch: "+path);
   var species=actors.Where(a=>a.isTameable).Select(a=>a.speciesId).OrderBy(x=>x).ToArray();if(!species.SequenceEqual(new[]{"blue_demon","spirit_beast","wind_fox"}))throw new System.Exception("Pet species mismatch: "+path);
   var portals=Object.FindObjectsByType<M1Portal>();if(portals.Length!=2||portals.Any(p=>!names.Contains(p.targetScene)))throw new System.Exception("Portal target missing: "+path);
   if(Object.FindObjectsByType<MeshCollider>().Length<1)throw new System.Exception("Terrain collider missing: "+path);
   int missing=0;foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))if(tr.GetComponents<Component>().Any(c=>c==null))missing++;
   if(missing>0)throw new System.Exception("Missing scripts detected: "+missing+" in "+path);
   int renderers=Object.FindObjectsByType<Renderer>().Length;report.AppendLine(Path.GetFileName(path)+": 玩家 1 / 敌人 1 / 异兽 3 / 传送门 2 / MeshRenderer "+renderers+" / Missing Script 0");
  }
  string game=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;string outDir=Path.Combine(game,"04_验证记录","v0.6.6");Directory.CreateDirectory(outDir);File.WriteAllText(Path.Combine(outDir,"场景完整性检查.txt"),report.ToString(),Encoding.UTF8);Debug.Log(report.ToString());
 }
}
