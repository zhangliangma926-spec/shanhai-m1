using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M1PreviewCaptureV066
{
    public static void CaptureHub()
    {
        const string scenePath="Assets/Scenes/v0.6.6/M1_HubWorld.unity";
        EditorSceneManager.OpenScene(scenePath);
        var cam=Camera.main;
        if(!cam)throw new System.Exception("Main camera missing from v0.6.6 hub.");
        var follow=cam.GetComponent<M1CameraFollow>();if(follow)Object.DestroyImmediate(follow);
        cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=60f;cam.farClipPlane=175f;cam.transform.position=new Vector3(0f,5.2f,-8f);cam.transform.LookAt(new Vector3(0f,2.2f,23f));
        var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;cam.targetTexture=rt;RenderTexture.active=rt;cam.Render();
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        string game=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string path=Path.Combine(game,"05_成品交付","预览","A_星金营地_v0.6.6_场景优化预览.png");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Debug.Log("Rendered preview: "+path);
    }
}



