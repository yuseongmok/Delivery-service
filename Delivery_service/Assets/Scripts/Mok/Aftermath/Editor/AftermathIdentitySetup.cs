#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static partial class AftermathSetup
    {
        public static void ImproveIdentity()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-identity-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                film = Object.FindFirstObjectByType<AftermathFilm>(); root = film.transform;
                var set = film.shots[4].set.transform;
                var photos = set.GetComponentsInChildren<Transform>(true).Where(t => t.name == "새 신분증 사진").ToArray();
                for (int i = 0; i < 2; i++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_" + (i == 0 ? "Hoodie" : "Jacket") + ".prefab");
                    var actor = Object.Instantiate(prefab); actor.transform.position = new Vector3(600 + i * 5, 0, 0);
                    foreach (var part in actor.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 30;
                    var animator = actor.GetComponentInChildren<Animator>(); if (animator) { animator.runtimeAnimatorController = film.firstPerson.mingi.animator.runtimeAnimatorController; animator.Rebind(); animator.Update(.1f); }
                    var renderers = actor.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    var camera = new GameObject("평상복 증명사진 촬영").AddComponent<Camera>();
                    Vector3 target = bounds.center + Vector3.up * bounds.size.y * .32f;
                    camera.transform.position = target + Vector3.forward * bounds.size.y * .94f; camera.transform.LookAt(target);
                    camera.cullingMask = 1 << 30; camera.fieldOfView = 27; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.35f, .43f, .47f);
                    var light = new GameObject("증명사진 조명").AddComponent<Light>(); light.type = LightType.Point; light.range = 10; light.intensity = 4;
                    light.transform.position = target + new Vector3(-1, 1, 2);
                    var rt = new RenderTexture(400, 500, 24); camera.targetTexture = rt; camera.Render();
                    var previous = RenderTexture.active; RenderTexture.active = rt;
                    var texture = new Texture2D(400, 500, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 400, 500), 0, 0); texture.Apply(); RenderTexture.active = previous;
                    string path = Folder + "새신분_평상복_" + i + ".png"; File.WriteAllBytes(path, texture.EncodeToPNG()); AssetDatabase.ImportAsset(path);
                    var material = photos[i].GetComponent<Renderer>().sharedMaterial;
                    material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); EditorUtility.SetDirty(material);
                    camera.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(texture);
                    Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(light.gameObject); Object.DestroyImmediate(actor);
                }
                var handoff = film.GetComponent<AftermathIdentityHandoff>(); if (!handoff) handoff = film.gameObject.AddComponent<AftermathIdentityHandoff>();
                handoff.player = film.firstPerson.exchangePlayer;
                handoff.dealer = set.GetComponentsInChildren<AftermathActor>(true).First(a => a.name == "신분 거래인");
                // Bring the dealer to the desk edge so the pushing hand can reach the envelope.
                var dealerPosition = handoff.dealer.transform.position;
                dealerPosition.x = 400.2f; dealerPosition.z = .55f;
                handoff.dealer.transform.position = dealerPosition;
                foreach (var motion in film.shots[4].motions)
                    if (motion.actor == handoff.dealer) { motion.from.position = dealerPosition; motion.to.position = dealerPosition; }
                handoff.envelope = set.GetComponentsInChildren<Transform>(true).First(t => t.name == "두 사람의 새 신분 봉투");
                handoff.rest = new Vector3(400.15f, .85f, .25f); handoff.offered = new Vector3(399.8f, .85f, -.44f); handoff.held = new Vector3(399.76f, .96f, -.55f);
                handoff.envelope.SetPositionAndRotation(handoff.rest, Quaternion.identity);
                foreach (var old in handoff.envelope.GetComponentsInChildren<Transform>(true).Where(t => t.name == "봉투 오른손 받침" || t.name == "거래인 봉투 밀기").ToArray()) Object.DestroyImmediate(old.gameObject);
                handoff.playerGrip = At("봉투 오른손 받침", handoff.envelope, handoff.rest + new Vector3(.36f, .015f, -.12f));
                handoff.dealerGrip = At("거래인 봉투 밀기", handoff.envelope, handoff.rest + new Vector3(.17f, .025f, .12f));
                film.firstPerson.identityHandoff = handoff;
                film.dialogue.lines[4].seconds = 7; EditorUtility.SetDirty(film.dialogue);
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/Aftermath-identity.txt", "Saved civilian portraits and staged first-person envelope handoff.");
                AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Logs/Aftermath-identity.txt", e.ToString()); EditorApplication.Exit(1); }
        }
    }
}
#endif
