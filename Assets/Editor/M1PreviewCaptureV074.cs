using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M1PreviewCaptureV074
{
    public static void CaptureCharacter()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/v0.7.4/M1_HubWorld.unity");
        var cam=Camera.main;if(!cam)throw new System.Exception("Main camera missing.");var follow=cam.GetComponent<M1CameraFollow>();if(follow)Object.DestroyImmediate(follow);
        cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=48f;cam.farClipPlane=100f;cam.transform.position=new Vector3(0f,2.1f,4.4f);cam.transform.LookAt(new Vector3(0f,1.05f,0f));
        var rt=new RenderTexture(1200,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;cam.targetTexture=rt;RenderTexture.active=rt;cam.Render();var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        string game=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;string path=Path.Combine(game,"05_成品交付","预览","B_旅者五官与法杖_v0.7.4.png");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Debug.Log("Rendered character closeup: "+path);
    }
    public static void CaptureHub()
    {
        const string scenePath="Assets/Scenes/v0.7.4/M1_HubWorld.unity";
        EditorSceneManager.OpenScene(scenePath);
        var cam=Camera.main;if(!cam)throw new System.Exception("Main camera missing in v0.7.4 hub.");
        var follow=cam.GetComponent<M1CameraFollow>();if(follow)Object.DestroyImmediate(follow);
        cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=58f;cam.farClipPlane=330f;cam.transform.position=new Vector3(0f,4.4f,-7.8f);cam.transform.LookAt(new Vector3(0f,1.4f,15f));
        var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;cam.targetTexture=rt;RenderTexture.active=rt;cam.Render();
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        string game=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string path=Path.Combine(game,"05_成品交付","预览","A_星金术式与十灵原型_v0.7.4_渲染图.png");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Debug.Log("Rendered preview: "+path);
    }
}



