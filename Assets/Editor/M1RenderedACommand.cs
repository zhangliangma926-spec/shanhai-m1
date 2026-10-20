using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class M1RenderedACommand
{
 public static void Build(){M1WorldBuilderV044.BuildAll();string project=Directory.GetParent(Application.dataPath).FullName;string game=Directory.GetParent(project).FullName;string folder=Path.Combine(game,"05_成品交付","Windows","v0.4.4");string exe=Path.Combine(folder,"Shanhai_M1.exe");if(File.Exists(exe))throw new Exception("Refusing to overwrite existing build: "+exe);Directory.CreateDirectory(folder);string[] scenes={"Assets/Scenes/v0.4.4/M1_HubWorld.unity","Assets/Scenes/v0.4.4/M1_ForestWorld.unity","Assets/Scenes/v0.4.4/M1_CanyonWorld.unity"};var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=exe,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});string info="Version: v0.4.4 A_青金营地 渲染+宠物面板\nResult: "+report.summary.result+"\nScene count: 3\nScenes: "+string.Join(", ",scenes)+"\nOutput: "+exe+"\nSize bytes: "+report.summary.totalSize;File.WriteAllText(Path.Combine(folder,"BUILD_INFO.txt"),info);Debug.Log(info);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Rendered A build failed: "+report.summary.result);}
 public static void CaptureHub(){string project=Directory.GetParent(Application.dataPath).FullName;string game=Directory.GetParent(project).FullName;var scene=EditorSceneManager.OpenScene("Assets/Scenes/v0.4.4/M1_HubWorld.unity",OpenSceneMode.Single);var cam=GameObject.FindWithTag("MainCamera").GetComponent<Camera>();var follow=cam.GetComponent<M1CameraFollow>();if(follow)follow.enabled=false;cam.transform.position=new Vector3(0,5.2f,-8.5f);cam.transform.rotation=Quaternion.LookRotation(new Vector3(0,1.6f,8)-cam.transform.position,Vector3.up);cam.fieldOfView=50;cam.Render();var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();string dir=Path.Combine(game,"05_成品交付","预览");Directory.CreateDirectory(dir);string output=Path.Combine(dir,"A_青金营地_实时渲染预览.png");File.WriteAllBytes(output,image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();Debug.Log("Rendered preview: "+output);}
}

