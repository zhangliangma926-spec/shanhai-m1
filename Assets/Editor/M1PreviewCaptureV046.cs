using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class M1PreviewCaptureV046
{
    public static void CaptureHub()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/v0.4.6_Rendered/M1_HubWorld.unity",OpenSceneMode.Single);
        var cameraObject=GameObject.FindWithTag("MainCamera");
        if(!cameraObject)throw new System.Exception("MainCamera missing in v0.4.6 rendered Hub.");
        var camera=cameraObject.GetComponent<Camera>();
        var follow=cameraObject.GetComponent<M1CameraFollow>();if(follow)follow.enabled=false;
        camera.transform.position=new Vector3(0,7.2f,-12.5f);
        camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,1.5f,8.5f)-camera.transform.position,Vector3.up);
        camera.fieldOfView=55f;camera.aspect=16f/9f;camera.allowHDR=true;camera.allowMSAA=true;
        var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        string output=@"D:\游戏开发\项目\山海驭灵录_M1\05_成品交付\预览\A_青金营地_v0.4.6_渲染预览.png";
        Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllBytes(output,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(image);rt.Release();
        Debug.Log("Rendered v0.4.6 preview: "+output);
    }
}
