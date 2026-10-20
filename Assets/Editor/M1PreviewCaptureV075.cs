using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M1PreviewCaptureV075
{
    public static void CaptureHub(){Capture("Assets/Scenes/v0.7.5/M1_HubWorld.unity","A_B共用_星金营地_v0.7.5.png",new Vector3(0f,4.4f,-7.8f),new Vector3(0f,1.4f,15f),58f,330f);}
    public static void CaptureCharacter(){Capture("Assets/Scenes/v0.7.5/M1_HubWorld.unity","A_B共用_旅者与灵杖_v0.7.5.png",new Vector3(0f,2.1f,4.4f),new Vector3(0f,1.05f,0f),48f,100f);}
    static void Capture(string scene,string file,Vector3 position,Vector3 look,float fov,float far)
    {
        EditorSceneManager.OpenScene(scene);var cam=Camera.main;if(!cam)throw new System.Exception("Main camera missing.");var follow=cam.GetComponent<M1CameraFollow>();if(follow)Object.DestroyImmediate(follow);cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=fov;cam.farClipPlane=far;cam.transform.position=position;cam.transform.LookAt(look);
        var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;cam.targetTexture=rt;RenderTexture.active=rt;cam.Render();var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        string game=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;string path=Path.Combine(game,"05_成品交付","预览",file);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Debug.Log("Rendered v0.7.5 preview: "+path);
    }
}