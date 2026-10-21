using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// v0.8.4 自动自检：只有带 -m1selftest 参数启动时才运行，平时不出现。
/// 在真实游戏里检查：动物挨打逃跑、打死（无“保留 1 点生命”）、死亡倒地并消失、按时在别处重新出现、
/// 盘旋飞鸟被打死掉到地上、玄岩谷地的游荡骸骨主动追击；并截几张名牌血条的图。结果写进 Player.log（SELFTEST 开头），然后退出。
/// 用法：Shanhai_M1.exe -m1selftest "截图目录"
/// </summary>
public class M1SelfTest : MonoBehaviour
{
    static string shotDir; static int fails;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(args, "-m1selftest"); if (i < 0) return;
        shotDir = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : Application.persistentDataPath;
        Directory.CreateDirectory(shotDir);
        var go = new GameObject("M1SelfTest"); DontDestroyOnLoad(go); go.AddComponent<M1SelfTest>();
    }
    static void Check(bool ok, string what) { if (!ok) fails++; Debug.Log("SELFTEST " + (ok ? "PASS " : "FAIL ") + what); }

    IEnumerator Start()
    {
        yield return Begin();
        var gm = M1GameManager.Instance; var player = gm.player.transform;
        var all = M1Actor.All.Where(a => a && a.gameObject.activeInHierarchy).ToList();
        Debug.Log($"SELFTEST 营地：可猎杀动物 {all.Count(a => a.isWildlife)}、怪物 {all.Count(a => a.isEnemy && a.GetComponent<M1AmbientCritter>())}、野生灵兽 {all.Count(a => a.isTameable)}");
        Check(all.Any(a => a.isWildlife), "营地有可猎杀动物");

        // 1) 名牌：走到一群动物旁边截图
        var herd = all.Where(a => a.isWildlife).OrderBy(a => Vector3.Distance(a.transform.position, player.position)).First();
        Teleport(player, herd.transform.position + new Vector3(0f, 0f, -9f), herd.transform.position); yield return new WaitForSeconds(1.5f);
        yield return Shot("01_营地动物名牌");

        // 2) 挨打逃跑
        Vector3 p0 = herd.transform.position; herd.TakeDamage(5f); yield return new WaitForSeconds(1.5f);
        float away = Vector3.Distance(herd.transform.position, player.position) - Vector3.Distance(p0, player.position);
        Check(away > 1.5f, $"{herd.displayName} 挨打后逃跑，离玩家远了 {away:F1} 米");

        // 3) 打死：生命归零（没有保留 1 点）、倒地、几秒后消失、按时在别处出现
        herd.respawnSeconds = 4f; Vector3 deathAt = herd.transform.position;
        herd.TakeDamage(herd.health + 50f);
        Check(herd.IsDead && herd.health <= 0f, $"{herd.displayName} 被打死，生命 {herd.health}");
        Check(!herd.GetComponent<Collider>().enabled, "死后碰撞体关闭（尸体不挡路）");
        yield return new WaitForSeconds(.4f); yield return Shot("02_倒地");
        yield return new WaitForSeconds(4.8f);
        Check(!herd.gameObject.activeSelf, "倒地几秒后消失");
        yield return new WaitForSeconds(5f);
        float moved = Vector3.Distance(herd.transform.position, deathAt);
        Check(herd.gameObject.activeSelf && !herd.IsDead && Mathf.Approximately(herd.health, herd.maxHealth), $"{herd.displayName} 重新出现，生命 {herd.health}/{herd.maxHealth}");
        Check(moved > 15f, $"在别处出现（离死亡点 {moved:F0} 米）");

        // 4) 野生灵兽可以打死（以前最多打到 1 点）
        var beast = all.Where(a => a.isTameable && !a.tamed).OrderBy(a => Vector3.Distance(a.transform.position, player.position)).First();
        beast.respawnSeconds = 0f; beast.TakeDamage(beast.maxHealth * .95f);
        Check(beast.health > 0f && beast.health <= beast.maxHealth * .1f, $"{beast.displayName} 打到 {beast.health:F0}，进入可结契范围");
        beast.TakeDamage(beast.maxHealth); Check(beast.IsDead, $"{beast.displayName} 继续打就死亡");

        // 5) 盘旋飞鸟：打死后掉到地上
        var bird = all.Where(a => a.selfMoving).OrderBy(a => Vector3.Distance(a.transform.position, player.position)).FirstOrDefault();
        if (bird)
        {
            float h0 = bird.transform.position.y; bird.respawnSeconds = 0f; bird.TakeDamage(999f); yield return new WaitForSeconds(1f);
            float ground = M1InfiniteWorld.Instance ? M1TerrainNoise.Height(M1InfiniteWorld.Instance.kind, bird.transform.position.x, bird.transform.position.z) : 0f;
            Check(bird.IsDead && Mathf.Abs(bird.transform.position.y - ground) < 1.5f, $"{bird.displayName} 从 {h0 - ground:F0} 米高处掉到地面");
        }
        else Check(false, "营地有盘旋飞鸟");

        // 6) 玄岩谷地：游荡骸骨靠近就追击
        SceneManager.LoadScene(2); yield return Begin(); gm = M1GameManager.Instance; player = gm.player.transform;
        var canyon = M1Actor.All.Where(a => a && a.gameObject.activeInHierarchy).ToList();
        Debug.Log($"SELFTEST 谷地：可猎杀动物 {canyon.Count(a => a.isWildlife)}、怪物 {canyon.Count(a => a.isEnemy && a.GetComponent<M1AmbientCritter>())}");
        var skel = canyon.Where(a => a.isEnemy && a.aggressive && a.GetComponent<M1AmbientCritter>()).OrderBy(a => Vector3.Distance(a.transform.position, player.position)).FirstOrDefault();
        if (skel)
        {
            Teleport(player, skel.transform.position + skel.transform.forward * 7f, skel.transform.position); yield return new WaitForSeconds(.2f);
            float d0 = Vector3.Distance(skel.transform.position, player.position); yield return new WaitForSeconds(2f);
            float d1 = Vector3.Distance(skel.transform.position, player.position);
            Check(d1 < d0 - 1.5f, $"{skel.displayName} 主动追击（{d0:F1} → {d1:F1} 米）");
            yield return Shot("03_谷地怪物追击");
        }
        else Check(false, "谷地有主动攻击的怪物");
        var zebra = canyon.FirstOrDefault(a => a.isEnemy && !a.aggressive && !a.selfMoving); // 斑马：挨打才反击
        Check(zebra && zebra.Engaged == false, "斑马平时不主动攻击");

        Debug.Log(fails == 0 ? "SELFTEST 全部通过" : "SELFTEST 有 " + fails + " 项未通过");
        Application.Quit();
    }

    static IEnumerator Begin()
    {
        yield return null; yield return null; // 换场景后等旧的管理器销毁
        float t = 0f; while ((!M1GameManager.Instance || !M1GameManager.Instance.player) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
        M1GameManager.Instance.StartGame(); yield return new WaitForSecondsRealtime(1f); // 标题页把时间缩放设为 0，这里用真实时间
    }
    static void Teleport(Transform player, Vector3 at, Vector3? face = null)
    {
        var cc = player.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
        if (M1InfiniteWorld.Instance) at.y = M1TerrainNoise.Height(M1InfiniteWorld.Instance.kind, at.x, at.z);
        player.position = at; if (cc) cc.enabled = true;
        if (face.HasValue) { var d = face.Value - at; d.y = 0f; if (d.sqrMagnitude > .01f) player.rotation = Quaternion.LookRotation(d); } // 面向目标，镜头跟在身后能拍到
    }
    /// 24 位 BMP（工程没有启用图片编码模块，不能 EncodeToPNG）；ReadPixels 的行序本来就是自下而上，和 BMP 一致
    static byte[] Bmp(Texture2D tex)
    {
        int w = tex.width, h = tex.height, stride = (w * 3 + 3) & ~3, size = 54 + stride * h; var px = tex.GetPixels32(); var b = new byte[size];
        void I32(int at, int v) { b[at] = (byte)v; b[at + 1] = (byte)(v >> 8); b[at + 2] = (byte)(v >> 16); b[at + 3] = (byte)(v >> 24); }
        b[0] = (byte)'B'; b[1] = (byte)'M'; I32(2, size); I32(10, 54); I32(14, 40); I32(18, w); I32(22, h); b[26] = 1; b[28] = 24; I32(34, stride * h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { var c = px[y * w + x]; int o = 54 + y * stride + x * 3; b[o] = c.b; b[o + 1] = c.g; b[o + 2] = c.r; }
        return b;
    }
    /// 截图：帧末读取屏幕（含名牌、HUD 等 IMGUI）。工程没有启用 ScreenCapture 模块，所以用 ReadPixels。
    static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(shotDir, name + ".bmp"), Bmp(tex)); Destroy(tex);
        Debug.Log("SELFTEST 截图 " + name);
    }
}
