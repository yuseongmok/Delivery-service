using System.Collections.Generic;
using DeliveryService.Yun.Audio;
using UnityEditor;
using UnityEngine;

namespace DeliveryService.Yun.Audio.Editor
{
    [CustomEditor(typeof(SoundLibrary))]
    public class SoundLibraryEditor : UnityEditor.Editor
    {
        private readonly List<string> issues = new List<string>();

        [MenuItem("Assets/Create/Delivery Service/Yun/Sound Library (All Slots)")]
        private static void CreateFilledLibrary()
        {
            var library = CreateInstance<SoundLibrary>();
            library.FillMissingEntries();
            ProjectWindowUtil.CreateAsset(library, "SoundLibrary.asset");
        }

        public override void OnInspectorGUI()
        {
            var library = (SoundLibrary)target;
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("슬롯 도구", EditorStyles.boldLabel);

            int missing = library.CountMissingIds();
            EditorGUILayout.HelpBox(
                missing == 0
                    ? $"모든 SoundId 슬롯이 있습니다. ({SoundLibrary.DefinedSoundIdCount}개)"
                    : $"비어 있는 SoundId 슬롯이 {missing}개 있습니다. 아래 버튼은 없는 항목만 추가하고 기존 음원·설정은 유지합니다.",
                missing == 0 ? MessageType.Info : MessageType.Warning);

            if (GUILayout.Button("없는 SoundId 빈 슬롯 채우기"))
            {
                Undo.RecordObject(library, "Fill missing sound slots");
                int added = library.FillMissingEntries();
                EditorUtility.SetDirty(library);
                serializedObject.Update();
                EditorGUILayout.HelpBox(
                    added == 0
                        ? "추가할 슬롯이 없습니다."
                        : $"{added}개의 빈 슬롯을 추가했습니다.",
                    MessageType.Info);
            }

            library.CollectIssues(issues);
            if (issues.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("검사 결과", EditorStyles.boldLabel);
                for (int i = 0; i < issues.Count; i++)
                {
                    EditorGUILayout.HelpBox(issues[i], MessageType.Warning);
                }
            }
        }
    }
}
