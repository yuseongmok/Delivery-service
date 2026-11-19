#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static partial class AftermathSetup
    {
        public static void SeparateShopKeys()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-shopkeys-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                film = Object.FindFirstObjectByType<AftermathFilm>(); root = film.transform;
                var shot = film.shots[5]; var shop = film.shots[6].set;
                var originalKey = film.shots[4].set.GetComponentsInChildren<Transform>(true).First(t => t.name == "새 매장 열쇠");
                originalKey.gameObject.SetActive(false);
                var old = shop.transform.Find("민기가 구한 매장 열쇠"); if (old) Object.DestroyImmediate(old.gameObject);
                var key = Object.Instantiate(originalKey, shop.transform); key.name = "민기가 구한 매장 열쇠"; key.gameObject.SetActive(true);
                film.firstPerson.shopKeyProp = key.gameObject;
                key.position = new Vector3(.78f, 1.22f, 6.80f); key.rotation = Quaternion.Euler(70, 0, 0);
                var grip = At("민기 열쇠 쥐는 손", key, key.position + new Vector3(0, .01f, .08f));
                var mingi = film.firstPerson.mingi; var player = film.firstPerson.shopPlayer;
                shot.set = shop; shot.shopKeyCard = true; shot.night = false; shot.fadeIn = shot.fadeOut = true;
                shot.fieldOfView = 55; shot.subtitleDelay = 2.6f; shot.subtitleTail = .4f; shot.lookFrom = null;
                shot.lookAt.position = key.position;
                shot.motions = new[] {
                    Move(mingi.transform, new Vector3(1,0,7.2f), new Vector3(1,0,7.2f), 180, mingi, 0, 0, null, grip),
                    Move(player.transform, new Vector3(1,0,5.7f), new Vector3(1,0,5.7f), 0, player)
                };
                shot.name = "며칠 뒤 · 민기가 구한 가게";
                film.dialogue.shopKeyCard = "며칠 뒤 · 새로 구한 가게";
                film.dialogue.lines[5].scene = shot.name;
                film.dialogue.lines[5].seconds = 10;
                film.dialogue.lines[5].subtitle = "민기: 며칠 발품 좀 팔았지. 남은 돈으로 내가 구한 가게야. 열쇠도 받아 왔어.\n이제 여기서 진짜 피자만 만드는 거야.";
                EditorUtility.SetDirty(film.dialogue); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
                AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
