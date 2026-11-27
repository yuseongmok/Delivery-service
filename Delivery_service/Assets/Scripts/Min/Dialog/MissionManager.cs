using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    public sealed class MissionManager : MonoBehaviour
    {
        public enum MissionState
        {
            DeliverPizza,
            DeliveryCutscene,
            ReturnToMingi,
            MingiCutscene,
            GoToDestination,
            Completed
        }

        [Serializable]
        public class CutsceneLine
        {
            [TextArea(2, 5)]
            public string text;
            public Transform cameraPoint;
        }

        [Serializable]
        public class CutsceneSequence
        {
            [Header("컷신 전용 오브젝트 (시작 시 활성화 -> 종료 시 비활성화)")]
            public GameObject cutsceneCanvas;
            public GameObject cutsceneNpc;
            public GameObject cutsceneCamera;
            public GameObject[] extraCutsceneObjects;

            [Header("대사 출력 텍스트 UI")]
            public Text dialogueText;

            [Header("대사 목록")]
            public CutsceneLine[] lines;
        }

        [Header("미션 상태 (확인용)")]
        [SerializeField] private MissionState currentState = MissionState.DeliverPizza;

        [Header("게임플레이 오브젝트")]
        public GameObject player;
        public GameObject deliveryNpc;
        public GameObject mingi;
        public AftermathActor mingiActor;
        public GameObject[] extraGameplayObjects;

        [Header("피자 배달 및 민기 복귀 위치")]
        public Transform deliveryTarget;
        public Transform mingiTarget;
        public float interactDistance = 3.5f;
        public bool requireInteractKeyForDelivery = true;

        [Header("민기 대화 후 이동할 다음 목표 위치")]
        public Transform nextTarget;
        public string nextTargetMarkerLabel = "목적지";
        public bool requireInteractAtNextTarget = false;

        [Header("피자 배달 완료 시 컷신 설정")]
        public CutsceneSequence deliveryCutscene;

        [Header("민기 복귀 후 상호작용 시 컷신 설정")]
        public CutsceneSequence mingiCutscene;

        [Header("대사 안내 문구 설정")]
        public bool showControlHint = true;

        [Header("UI")]
        public Text objective;
        public Text marker;
        public string deliveryMarkerLabel = "피자 배달";
        public string mingiMarkerLabel = "민기와 대화";
        public float markerHeight = 1.6f;

        private CutsceneSequence activeSequence;
        private int currentLineIndex;
        private float inputCooldownUntil;

        private ToppingInventory playerInventory;
        private PlayerCombat playerCombat;

        public MissionState State => currentState;
        public bool IsInCutscene => currentState == MissionState.DeliveryCutscene || currentState == MissionState.MingiCutscene;

        private void Awake()
        {
            ResolvePlayerReferences();
            ResolveMingiReferences();

            SetCutsceneObjectsActive(deliveryCutscene, false);
            SetCutsceneObjectsActive(mingiCutscene, false);

            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }

        private void ResolvePlayerReferences()
        {
            if (player == null)
            {
                GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
                if (foundPlayer != null)
                {
                    player = foundPlayer;
                }
                else
                {
                    var combat = FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
                    if (combat != null) player = combat.gameObject;
                }
            }

            if (player != null)
            {
                if (playerInventory == null) playerInventory = player.GetComponent<ToppingInventory>();
                if (playerCombat == null) playerCombat = player.GetComponent<PlayerCombat>();
            }
        }

        private bool IsValidSceneGameplayMingi(GameObject candidate)
        {
            if (candidate == null) return false;
            if (!candidate.scene.IsValid()) return false;
            if (mingiCutscene != null && candidate == mingiCutscene.cutsceneNpc) return false;
            if (deliveryCutscene != null && candidate == deliveryCutscene.cutsceneNpc) return false;
            return true;
        }

        private void ResolveMingiReferences()
        {
            if (mingiActor != null && IsValidSceneGameplayMingi(mingiActor.gameObject))
            {
                mingi = mingiActor.gameObject;
            }
            else if (IsValidSceneGameplayMingi(mingi))
            {
                if (mingiActor == null || !IsValidSceneGameplayMingi(mingiActor.gameObject))
                {
                    mingiActor = mingi.GetComponentInChildren<AftermathActor>(true);
                }
            }
            else
            {
                mingi = null;
                mingiActor = null;

                var actors = FindObjectsByType<AftermathActor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < actors.Length; i++)
                {
                    if (actors[i] == null || !IsValidSceneGameplayMingi(actors[i].gameObject)) continue;
                    if (actors[i].name.Contains("민기"))
                    {
                        mingiActor = actors[i];
                        mingi = actors[i].gameObject;
                        break;
                    }
                }

                if (mingi == null)
                {
                    GameObject found = GameObject.Find("민기");
                    if (IsValidSceneGameplayMingi(found))
                    {
                        mingi = found;
                        mingiActor = found.GetComponentInChildren<AftermathActor>(true);
                    }
                }
            }
        }

        private MotorcycleController GetActiveBike()
        {
            if (playerCombat != null && playerCombat.RiddenBike != null)
                return playerCombat.RiddenBike;

            var bikes = FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);
            for (int i = 0; i < bikes.Length; i++)
            {
                if (bikes[i] != null && bikes[i].isDriven)
                    return bikes[i];
            }

            return null;
        }

        private Vector3 GetCurrentPlayerPosition()
        {
            MotorcycleController activeBike = GetActiveBike();
            if (activeBike != null)
                return activeBike.transform.position;

            if (player != null)
                return player.transform.position;

            return Vector3.zero;
        }

        private void Start()
        {
            currentState = MissionState.DeliverPizza;
            UpdateObjectiveUI();
            UpdateMarkerUI();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            SyncDeliveryNavigation();

            if (IsInCutscene)
            {
                if (marker != null && marker.gameObject.activeSelf)
                {
                    marker.gameObject.SetActive(false);
                }

                if (Time.unscaledTime < inputCooldownUntil) return;

                if (PrologueKeyboard.Pressed(Key.Space))
                {
                    FinishActiveCutscene();
                    return;
                }

                if (PrologueKeyboard.Pressed(Key.F))
                {
                    AdvanceCutsceneLine();
                }
                return;
            }

            UpdateObjectiveUI();
            UpdateMarkerUI();

            if (Time.unscaledTime < inputCooldownUntil) return;

            if (currentState == MissionState.DeliverPizza)
            {
                Transform target = GetDeliveryTransform();
                if (target != null && target.gameObject.activeInHierarchy && IsPlayerNear(target))
                {
                    if (!requireInteractKeyForDelivery || PrologueKeyboard.Pressed(Key.F))
                    {
                        TryCompletePizzaDelivery();
                    }
                }
            }
            else if (currentState == MissionState.ReturnToMingi)
            {
                Transform target = GetMingiTransform();
                if (target != null && target.gameObject.activeInHierarchy && IsPlayerNear(target))
                {
                    if (PrologueKeyboard.Pressed(Key.F))
                    {
                        StartCutscene(mingiCutscene, MissionState.MingiCutscene);
                    }
                }
            }
            else if (currentState == MissionState.GoToDestination)
            {
                if (nextTarget != null && nextTarget.gameObject.activeInHierarchy && IsPlayerNear(nextTarget))
                {
                    if (!requireInteractAtNextTarget || PrologueKeyboard.Pressed(Key.F))
                    {
                        CompleteMission();
                    }
                }
            }
        }

        // 피자 배달 단계에서는 휴대폰 내비 '배달지'를 배달 대상으로 맞추고, 단계가 끝나면 해제한다.
        private NavigationManager navigation;
        private Transform navigationTarget;

        private void SyncDeliveryNavigation()
        {
            Transform target = currentState == MissionState.DeliverPizza && !GameModeSession.IsChallenge ? GetDeliveryTransform() : null;
            if (target != null && !target.gameObject.activeInHierarchy) target = null;
            if (target == navigationTarget) return;
            if (navigation == null) navigation = FindFirstObjectByType<NavigationManager>(FindObjectsInactive.Include);
            if (navigation == null) return;
            if (navigationTarget != null) navigation.ClearStoryDelivery();
            navigationTarget = target;
            if (target != null) navigation.SetStoryDelivery(target, deliveryMarkerLabel);
        }

        private void OnDisable()
        {
            if (navigation != null && navigationTarget != null) navigation.ClearStoryDelivery();
            navigationTarget = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            MotorcycleController activeBike = GetActiveBike();
            bool isPlayerCollider = other.CompareTag("Player") ||
                                    (player != null && other.transform.IsChildOf(player.transform)) ||
                                    (activeBike != null && other.transform.IsChildOf(activeBike.transform));

            if (!isPlayerCollider) return;

            if (currentState == MissionState.DeliverPizza && !requireInteractKeyForDelivery)
            {
                TryCompletePizzaDelivery();
            }
            else if (currentState == MissionState.GoToDestination && !requireInteractAtNextTarget && nextTarget != null)
            {
                CompleteMission();
            }
        }

        private Transform GetDeliveryTransform()
        {
            if (deliveryTarget != null) return deliveryTarget;
            if (deliveryNpc != null) return deliveryNpc.transform;
            return null;
        }

        private Transform GetMingiTransform()
        {
            ResolveMingiReferences();
            if (mingiTarget != null) return mingiTarget;
            if (mingiActor != null) return mingiActor.transform;
            if (mingi != null) return mingi.transform;
            return null;
        }

        private bool IsPlayerNear(Transform target)
        {
            ResolvePlayerReferences();
            if (target == null) return false;

            MotorcycleController activeBike = GetActiveBike();
            if (activeBike != null)
            {
                return Vector3.Distance(activeBike.transform.position, target.position) <= interactDistance;
            }

            if (player == null) return false;
            return Vector3.Distance(player.transform.position, target.position) <= interactDistance;
        }

        private void TryCompletePizzaDelivery()
        {
            ResolvePlayerReferences();

            bool delivered = false;
            if (playerInventory != null)
            {
                if (playerInventory.HasPackagedPizzas())
                {
                    playerInventory.ClearPackagedPizzas();
                    delivered = true;
                }
                else if (playerInventory.HasPizza())
                {
                    GameObject pizzaObj = playerInventory.RemovePizza();
                    if (pizzaObj != null) Destroy(pizzaObj);
                    delivered = true;
                }
            }

            if (!delivered)
            {
                if (objective != null)
                {
                    objective.text = "배달할 피자를 소지하고 있지 않습니다.";
                }
                return;
            }

            StartCutscene(deliveryCutscene, MissionState.DeliveryCutscene);
        }

        private void StartCutscene(CutsceneSequence sequence, MissionState cutsceneState)
        {
            ResolveMingiReferences();

            MotorcycleController activeBike = GetActiveBike();
            if (activeBike != null)
            {
                activeBike.ExitBike();
            }

            currentState = cutsceneState;
            activeSequence = sequence;
            currentLineIndex = 0;
            inputCooldownUntil = Time.unscaledTime + 0.25f;

            if (marker != null) marker.gameObject.SetActive(false);
            if (objective != null) objective.text = "";

            if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            {
                FinishActiveCutscene();
                return;
            }

            SetGameplayObjectsActive(false);
            SetCutsceneObjectsActive(sequence, true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ApplyCurrentLine();
        }

        private void AdvanceCutsceneLine()
        {
            if (activeSequence == null || activeSequence.lines == null)
            {
                FinishActiveCutscene();
                return;
            }

            inputCooldownUntil = Time.unscaledTime + 0.2f;
            currentLineIndex++;

            if (currentLineIndex < activeSequence.lines.Length)
            {
                ApplyCurrentLine();
            }
            else
            {
                FinishActiveCutscene();
            }
        }

        private void ApplyCurrentLine()
        {
            if (activeSequence == null || activeSequence.lines == null || currentLineIndex >= activeSequence.lines.Length)
                return;

            CutsceneLine line = activeSequence.lines[currentLineIndex];

            if (activeSequence.dialogueText != null)
            {
                activeSequence.dialogueText.text = line.text;
            }

            if (activeSequence.cutsceneCamera != null && line.cameraPoint != null)
            {
                activeSequence.cutsceneCamera.transform.position = line.cameraPoint.position;
                activeSequence.cutsceneCamera.transform.rotation = line.cameraPoint.rotation;
            }
        }

        public void FinishActiveCutscene()
        {
            MissionState endedState = currentState;
            inputCooldownUntil = Time.unscaledTime + 0.35f;

            if (activeSequence != null)
            {
                SetCutsceneObjectsActive(activeSequence, false);
                activeSequence = null;
            }

            SetGameplayObjectsActive(true);
            RestorePlayerControl();

            if (endedState == MissionState.DeliveryCutscene)
            {
                currentState = MissionState.ReturnToMingi;
            }
            else if (endedState == MissionState.MingiCutscene)
            {
                currentState = nextTarget != null ? MissionState.GoToDestination : MissionState.Completed;
            }

            UpdateObjectiveUI();
            UpdateMarkerUI();
        }

        private void CompleteMission()
        {
            currentState = MissionState.Completed;
            UpdateObjectiveUI();
            UpdateMarkerUI();
        }

        private void SetGameplayObjectsActive(bool active)
        {
            ResolveMingiReferences();
            SetActiveSafe(deliveryNpc, active);
            if (active)
            {
                SetActiveSafe(mingi, true);
            }
            SetActiveSafe(player, active);

            if (extraGameplayObjects != null)
            {
                for (int i = 0; i < extraGameplayObjects.Length; i++)
                {
                    if (!active && extraGameplayObjects[i] == mingi) continue;
                    SetActiveSafe(extraGameplayObjects[i], active);
                }
            }
        }

        private void SetCutsceneObjectsActive(CutsceneSequence sequence, bool active)
        {
            if (sequence == null) return;

            SetActiveSafe(sequence.cutsceneCanvas, active);
            if (active || sequence.cutsceneNpc != mingi)
            {
                SetActiveSafe(sequence.cutsceneNpc, active);
            }
            SetActiveSafe(sequence.cutsceneCamera, active);

            if (sequence.extraCutsceneObjects != null)
            {
                for (int i = 0; i < sequence.extraCutsceneObjects.Length; i++)
                {
                    if (!active && sequence.extraCutsceneObjects[i] == mingi) continue;
                    SetActiveSafe(sequence.extraCutsceneObjects[i], active);
                }
            }
        }

        private void RestorePlayerControl()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (player != null)
            {
                if (player.TryGetComponent<CharacterController>(out var cc))
                {
                    cc.enabled = true;
                }

                if (player.TryGetComponent<PlayerMove>(out var move))
                {
                    move.enabled = true;
                    move.isControllable = true;
                }

                CameraMove camMove = player.GetComponentInChildren<CameraMove>(true);
                if (camMove != null)
                {
                    camMove.SyncViewFromTransform();
                    camMove.enabled = true;
                }
            }
        }

        private Camera GetPlayerCamera()
        {
            MotorcycleController activeBike = GetActiveBike();
            if (activeBike != null && activeBike.bikeCamera != null)
            {
                Camera bikeCam = activeBike.bikeCamera.GetComponentInChildren<Camera>();
                if (bikeCam != null) return bikeCam;
            }

            if (playerCombat != null && playerCombat.View != null)
                return playerCombat.View;

            if (player != null)
            {
                Camera childCam = player.GetComponentInChildren<Camera>();
                if (childCam != null) return childCam;
            }

            return Camera.main;
        }

        private void UpdateObjectiveUI()
        {
            if (objective == null) return;

            switch (currentState)
            {
                case MissionState.DeliverPizza:
                    objective.text = "지정된 장소로 피자를 배달하세요.";
                    break;
                case MissionState.ReturnToMingi:
                    objective.text = "민기에게 돌아가 보고하세요.";
                    break;
                case MissionState.GoToDestination:
                    objective.text = "지정된 위치로 이동하세요.";
                    break;
                case MissionState.Completed:
                    objective.text = "배달 임무 완료";
                    break;
            }
        }

        private void UpdateMarkerUI()
        {
            if (marker == null) return;

            ResolvePlayerReferences();
            MotorcycleController activeBike = GetActiveBike();
            Camera cam = GetPlayerCamera();

            bool isPlayerActiveOrRiding = (player != null && player.activeInHierarchy) || activeBike != null;

            if (!isPlayerActiveOrRiding || cam == null || IsInCutscene || currentState == MissionState.Completed)
            {
                marker.gameObject.SetActive(false);
                return;
            }

            Transform target = null;
            string label = "";

            if (currentState == MissionState.DeliverPizza)
            {
                target = GetDeliveryTransform();
                label = deliveryMarkerLabel;
            }
            else if (currentState == MissionState.ReturnToMingi)
            {
                target = GetMingiTransform();
                label = mingiMarkerLabel;
            }
            else if (currentState == MissionState.GoToDestination)
            {
                target = nextTarget;
                label = nextTargetMarkerLabel;
            }

            if (target != null && target.gameObject.activeInHierarchy)
            {
                Vector3 currentPlayerPos = GetCurrentPlayerPosition();
                marker.gameObject.SetActive(true);
                StoryTargetDiamond.Place(
                    marker,
                    cam,
                    target.position + Vector3.up * markerHeight,
                    currentPlayerPos,
                    label);
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
        }

        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}