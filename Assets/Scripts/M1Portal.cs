using UnityEngine;
using UnityEngine.SceneManagement;

public class M1Portal : MonoBehaviour
{
    public string targetScene;
    public string destinationName;
    public float interactDistance = 2.8f;
    void Update()
    {
        if (!M1GameManager.Instance || !M1GameManager.Instance.IsGameplayActive || !M1GameManager.Instance.player) return;
        Vector3 playerPos = M1GameManager.Instance.player.transform.position;
        if (Vector3.Distance(playerPos, transform.position) <= interactDistance && Input.GetKeyDown(KeyCode.F))
        {
            M1GameManager.StartImmediatelyOnNextScene = true;
            SceneManager.LoadScene(targetScene);
        }
    }
    void OnGUI()
    {
        if (!M1GameManager.Instance || !M1GameManager.Instance.IsGameplayActive || !M1GameManager.Instance.player) return;
        if (Vector3.Distance(M1GameManager.Instance.player.transform.position, transform.position) <= interactDistance)
        {
            M1UISkin.Apply();
            // 放在底部灵杖面板（Screen.height-88 起）的上方，避免重叠
            GUI.Box(new Rect(Screen.width * .5f - 130, Screen.height - 136, 260, 38), "F   前往 · " + destinationName, M1UISkin.Prompt);
        }
    }
}
