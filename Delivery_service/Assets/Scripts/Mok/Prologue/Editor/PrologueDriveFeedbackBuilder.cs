#if UNITY_EDITOR
using System;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class PrologueDriveFeedbackBuilder {
 public static void Apply(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var scene=d.gameObject.scene;
 if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-drive-audio-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
 var f=d.drive.GetComponent<PrologueDriveFeedback>();if(!f)f=d.drive.gameObject.AddComponent<PrologueDriveFeedback>();f.drive=d.drive;
 AudioSource Source(string name,string path,bool loop){var t=d.getawayCar.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(d.getawayCar,false);}var a=t.GetComponent<AudioSource>();if(!a)a=t.gameObject.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=loop;a.spatialBlend=0;a.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!a.clip)throw new Exception("Missing audio "+path);a.volume=loop?.16f:1;a.pitch=loop?.75f:1;return a;}
 f.engine=Source("Driving engine","Assets/Scripts/Yun/Audio/Clips/engine-low-loop.wav",true);f.impact=Source("Driving collision","Assets/Scripts/Yun/Audio/Clips/motorcycle-object-impact.mp3",false);
 EditorUtility.SetDirty(f);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
}}
#endif
