#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class MokLobbyInspect {
static MokLobbyInspect(){EditorApplication.delayCall+=Run;}
static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;var s=SceneManager.GetActiveScene();var lines=s.GetRootGameObjects().Select(g=>$"ROOT {g.name} {g.transform.position} rot {g.transform.eulerAngles}").ToList();foreach(var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(b!=null&&(b is MotorcycleController||b is PlayerMovement||b is PlayerMove||b is NPCControl))lines.Add($"SPECIAL {b.GetType().Name} {b.name} root={b.transform.root.name} pos={b.transform.position} rot={b.transform.eulerAngles}");File.WriteAllLines("Temp/Mok-lobby-inspect.txt",lines);}
}
#endif
