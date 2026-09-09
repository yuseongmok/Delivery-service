using System.Collections.Generic;
using System.Text;
using DeliveryService.Yun.Audio;
using UnityEditor;
using UnityEngine;

namespace DeliveryService.Yun.Audio.Editor
{
    public static class AudioSystemVerifier
    {
        [MenuItem("Tools/Delivery Service/Yun/Run Audio System Verification")]
        private static void RunFromMenu()
        {
            var report = new StringBuilder();
            bool staticOk = RunStaticChecks(report);
            bool playOk = true;
            if (Application.isPlaying)
            {
                playOk = RunPlayModeChecks(report);
            }
            else
            {
                report.AppendLine("[실행] Play Mode가 아니라 런타임 재생 검사는 건너뛰었다.");
            }

            string text = report.ToString();
            if (staticOk && playOk)
            {
                Debug.Log("Yun Audio 검증 성공\n" + text);
                EditorUtility.DisplayDialog("Yun Audio 검증", "정적 검사가 통과했다.\n\n" + TrimForDialog(text), "OK");
            }
            else
            {
                Debug.LogError("Yun Audio 검증 실패\n" + text);
                EditorUtility.DisplayDialog("Yun Audio 검증", "검사에 실패했다.\n\n" + TrimForDialog(text), "OK");
            }
        }

        private static string TrimForDialog(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 1200)
            {
                return text;
            }

            return text.Substring(0, 1200) + "\n...";
        }

        private static bool RunStaticChecks(StringBuilder report)
        {
            bool ok = true;
            if (SoundLibrary.DefinedSoundIdCount != 69)
            {
                report.AppendLine($"[실패] SoundId 개수가 69가 아니다: {SoundLibrary.DefinedSoundIdCount}");
                ok = false;
            }
            else
            {
                report.AppendLine("[통과] SoundId 69개");
            }

            var library = ScriptableObject.CreateInstance<SoundLibrary>();
            try
            {
                if (library.EntryCount != 0)
                {
                    report.AppendLine("[실패] 새 라이브러리가 비어 있지 않다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 빈 라이브러리 생성");
                }

                int added = library.FillMissingEntries();
                if (added != SoundLibrary.DefinedSoundIdCount || library.EntryCount != SoundLibrary.DefinedSoundIdCount)
                {
                    report.AppendLine($"[실패] 슬롯 채우기: added={added}, count={library.EntryCount}, expected={SoundLibrary.DefinedSoundIdCount}");
                    ok = false;
                }
                else
                {
                    report.AppendLine($"[통과] 모든 SoundId 빈 슬롯 채움 ({added}개)");
                }

                SoundEntry first = library.Entries[0];
                AudioClip[] originalClips = first.clips;
                float originalVolume = first.volume;
                int addedAgain = library.FillMissingEntries();
                if (addedAgain != 0 || first.clips != originalClips || !Mathf.Approximately(first.volume, originalVolume))
                {
                    report.AppendLine("[실패] 두 번째 Fill이 기존 슬롯을 덮어썼다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 재Fill 시 기존 음원·설정 유지");
                }

                var issues = new List<string>();
                library.CollectIssues(issues);
                if (issues.Count != 0)
                {
                    report.AppendLine("[실패] 기본 슬롯에서 문제가 보고되었다: " + issues[0]);
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 기본 슬롯 검사 문제 없음");
                }

                DuplicateFirstEntry(library);
                library.CollectIssues(issues);
                bool foundDuplicate = Contains(issues, "중복");
                bool foundPitch = false;
                bool foundDistance = false;

                SerializedObject so = new SerializedObject(library);
                SerializedProperty entries = so.FindProperty("entries");
                SerializedProperty pitch = entries.GetArrayElementAtIndex(1).FindPropertyRelative("pitchRange");
                pitch.vector2Value = new Vector2(1.2f, 0.8f);
                SerializedProperty minDistance = entries.GetArrayElementAtIndex(2).FindPropertyRelative("minDistance");
                SerializedProperty maxDistance = entries.GetArrayElementAtIndex(2).FindPropertyRelative("maxDistance");
                minDistance.floatValue = 30f;
                maxDistance.floatValue = 5f;
                so.ApplyModifiedPropertiesWithoutUndo();
                library.CollectIssues(issues);
                foundPitch = Contains(issues, "피치");
                foundDistance = Contains(issues, "최소 거리가 최대 거리");

                if (!foundDuplicate || !foundPitch || !foundDistance)
                {
                    report.AppendLine($"[실패] 편집 검사 누락 duplicate={foundDuplicate} pitch={foundPitch} distance={foundDistance}");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 중복 ID·잘못된 피치·거리 설정을 Inspector 검사로 확인");
                }

                if (library.TryGetEntry(library.Entries[0].id, out SoundEntry lookup) && lookup != null)
                {
                    report.AppendLine("[통과] 중복 시 첫 항목만 조회");
                }
                else
                {
                    report.AppendLine("[실패] TryGetEntry가 항목을 찾지 못했다.");
                    ok = false;
                }
            }
            finally
            {
                Object.DestroyImmediate(library);
            }

            return ok;
        }

        private static bool RunPlayModeChecks(StringBuilder report)
        {
            if (AudioManager.Instance != null)
            {
                report.AppendLine("[실행] 이미 AudioManager가 있어 임시 재생 검사는 건너뛰었다.");
                return true;
            }

            bool ok = true;
            var library = ScriptableObject.CreateInstance<SoundLibrary>();
            library.FillMissingEntries();
            AudioClip clip = AudioClip.Create("YunAudioVerify", 22050, 1, 44100, false);
            AssignClip(library, SoundId.UiClick, clip);
            AssignClip(library, SoundId.PlayerJump, clip);
            AssignClip(library, SoundId.OvenLoop, clip);
            AssignClip(library, SoundId.MotorcycleEngineLoop, clip);

            var host = new GameObject("YunAudioVerify_AudioManager");
            AudioManager manager = host.AddComponent<AudioManager>();
            manager.SetLibrary(library);
            manager.RebuildPoolForEditorTests(2);

            try
            {
                SoundHandle empty = manager.Play(SoundId.OrderAccept);
                if (empty.IsValid)
                {
                    report.AppendLine("[실패] 빈 클립 목록을 재생했다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 빈 클립은 재생하지 않고 Invalid 핸들을 반환");
                }

                SoundHandle missing = manager.Play((SoundId)123456);
                if (missing.IsValid)
                {
                    report.AppendLine("[실패] 미등록 ID를 재생했다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 미등록 ID는 예외 없이 건너뜀");
                }

                SoundHandle positioned = manager.PlayAt(SoundId.PlayerJump, new Vector3(4f, 0f, 2f));
                if (!positioned.IsValid)
                {
                    report.AppendLine("[실패] 위치 지정 3D 재생이 실패했다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 위치 지정 3D 재생");
                    manager.Stop(positioned);
                }

                manager.RebuildPoolForEditorTests(3);
                SoundHandle a = manager.Play(SoundId.UiClick);
                SoundHandle b = manager.Play(SoundId.PlayerJump);
                SoundHandle sameId = manager.Play(SoundId.UiClick);
                if (!a.IsValid || !b.IsValid || !sameId.IsValid || manager.ActiveCount != 3)
                {
                    report.AppendLine($"[실패] 동시 재생 실패 a={a.IsValid} b={b.IsValid} same={sameId.IsValid} active={manager.ActiveCount}");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 서로 다른 SoundId 및 같은 SoundId 동시 재생");
                }

                manager.StopAll();
                manager.RebuildPoolForEditorTests(2);
                a = manager.Play(SoundId.UiClick);
                b = manager.Play(SoundId.PlayerJump);
                SoundHandle sameA = manager.Play(SoundId.UiClick);
                if (!sameA.IsValid)
                {
                    report.AppendLine("[실패] 같은 SoundId 추가 재생이 거부되었다. 원샷 훔치기가 동작해야 한다.");
                    ok = false;
                }
                else if (a.IsValid)
                {
                    report.AppendLine("[실패] 풀이 가득일 때 가장 오래된 원샷 핸들이 무효가 되지 않았다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 풀이 가득이면 가장 오래된 원샷만 재사용하고 이전 핸들은 무효");
                }

                manager.StopAll();
                manager.RebuildPoolForEditorTests(2);
                SoundHandle loopA = manager.Play(SoundId.OvenLoop);
                SoundHandle loopB = manager.PlayFollow(SoundId.MotorcycleEngineLoop, host.transform);
                SoundHandle loopC = manager.Play(SoundId.OvenLoop);
                if (!loopA.IsValid || !loopB.IsValid || loopC.IsValid || manager.ActiveCount != 2)
                {
                    report.AppendLine($"[실패] 반복음 보호 실패 loopC={loopC.IsValid} active={manager.ActiveCount}");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 풀이 반복음으로 가득하면 새 재생을 건너뛰고 반복음을 빼앗지 않음");
                }

                loopA.Stop();
                if (loopA.IsValid || !loopB.IsPlaying)
                {
                    report.AppendLine("[실패] 개별 정지가 다른 반복음까지 멈췄거나 핸들이 남아 있다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 반복음 개별 정지");
                }

                loopB.SetVolume(0.25f);
                loopB.SetPitch(1.1f);
                manager.SetCategoryVolume(SoundCategory.Sfx, 0.5f);
                if (!loopB.IsPlaying)
                {
                    report.AppendLine("[실패] 볼륨 변경 후 반복음이 멈췄다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 재생 중 인스턴스·카테고리 볼륨 변경");
                }

                manager.Stop(SoundId.MotorcycleEngineLoop);
                if (loopB.IsValid)
                {
                    report.AppendLine("[실패] SoundId 전체 정지가 핸들을 무효로 만들지 못했다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 같은 SoundId 전체 정지");
                }

                var followTarget = new GameObject("YunAudioVerify_Follow");
                SoundHandle follow = manager.PlayFollow(SoundId.OvenLoop, followTarget.transform);
                Object.DestroyImmediate(followTarget);
                if (follow.IsValid)
                {
                    report.AppendLine("[실패] 따라가던 대상이 파괴된 뒤에도 핸들이 유효하다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 따라가던 대상 파괴 시 재생 정리");
                }

                manager.RebuildPoolForEditorTests(2);
                SoundHandle first = manager.Play(SoundId.UiClick);
                manager.Stop(first);
                SoundHandle reused = manager.Play(SoundId.PlayerJump);
                first.Stop();
                first.SetVolume(0f);
                first.SetPitch(0.1f);
                if (!reused.IsPlaying)
                {
                    report.AppendLine("[실패] 만료된 핸들이 재사용된 재생을 멈췄다.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("[통과] 만료된 핸들은 재사용 슬롯을 제어하지 않음");
                }

                manager.StopAll();
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(clip);
            }

            return ok;
        }

        private static void AssignClip(SoundLibrary library, SoundId id, AudioClip clip)
        {
            if (!library.TryGetEntry(id, out SoundEntry entry) || entry == null)
            {
                return;
            }

            entry.clips = new[] { clip };
        }

        private static void DuplicateFirstEntry(SoundLibrary library)
        {
            SerializedObject so = new SerializedObject(library);
            SerializedProperty entries = so.FindProperty("entries");
            entries.InsertArrayElementAtIndex(0);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool Contains(List<string> issues, string token)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i] != null && issues[i].IndexOf(token, System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
