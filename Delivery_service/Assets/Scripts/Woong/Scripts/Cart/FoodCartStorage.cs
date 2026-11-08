using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Mok
{
    public class FoodCartStorage : MonoBehaviour, IInteractable
    {
        [Header("카트 도어 및 내부 설정")]
        public Transform doorHinge;
        [Tooltip("피자가 카트 내부에 쌓일 기준 위치 ")]
        public Transform contentsRoot;

        [Header("도어 회전 설정")]
        public Vector3 openRotationEuler = new Vector3(0f, 0f, -105f);
        public float doorAnimDuration = 0.3f;
        public float transferAnimDuration = 0.75f;

        [Header("수납 용량 설정")]
        public int maxCapacity = 10;

        private List<PizzaData> storedPizzas = new List<PizzaData>();
        public int StoredCount => storedPizzas.Count;
        public bool IsBusy { get; private set; }

        private Quaternion closedRotation;
        private GameObject movingStack;
        private GameObject handDisplay;

        private void Awake()
        {
            if (doorHinge == null) doorHinge = transform;
            closedRotation = doorHinge.localRotation;
        }

        private void Start()
        {
            RefreshInternalVisual();
        }

        public void Interact(ToppingInventory inventory)
        {
            if (IsBusy || inventory == null) return;

            bool storing = inventory.HasPackagedPizzas();

            if (storing)
            {
                // 손에 든 포장 피자를 카트에 넣을 때
                if (storedPizzas.Count >= maxCapacity)
                {
                    Debug.Log("카트 수납 공간이 가득 찼습니다!");
                    return;
                }
            }
            else
            {
                // 카트에서 피자를 꺼내 내 손으로 들 때
                if (storedPizzas.Count == 0 || inventory.HasItem()) return;
            }

            var expected = new List<PizzaData>(storing ? inventory.GetHeldPackagedPizzas() : storedPizzas);

            bool Commit()
            {
                var current = storing ? inventory.GetHeldPackagedPizzas() : storedPizzas;
                if (current.Count != expected.Count || (!storing && inventory.HasItem())) return false;
                for (int i = 0; i < current.Count; i++)
                {
                    if (!ReferenceEquals(current[i], expected[i])) return false;
                }

                if (storing)
                {
                    storedPizzas.AddRange(inventory.ClearPackagedPizzas());
                }
                else
                {
                    inventory.AddPackagedDataStack(new List<PizzaData>(storedPizzas));
                    storedPizzas.Clear();
                }
                return true;
            }

            StartCoroutine(AnimateTransfer(inventory, storing, expected.Count, Commit));
        }

        private IEnumerator AnimateTransfer(ToppingInventory inventory, bool storing, int count, Func<bool> commit)
        {
            IsBusy = true;

            yield return RotateDoor(closedRotation, closedRotation * Quaternion.Euler(openRotationEuler));

            var preview = inventory.GetComponent<CarriedItemVisual>();
            handDisplay = preview != null ? preview.Display : null;
            Vector3 handWorldPos = handDisplay != null ? handDisplay.transform.position : inventory.HoldPoint.position;

            if (storing && handDisplay != null) handDisplay.SetActive(false);
            if (!storing && contentsRoot != null) contentsRoot.gameObject.SetActive(false);

            // 이동 연출용 임시 피자 박스 스택 생성
            Transform refTransform = contentsRoot != null ? contentsRoot : transform;
            movingStack = CreateBoxStack(refTransform, count);

            Vector3 insideLocal = new Vector3(0f, 0.04f, 0f);
            Vector3 outsideLocal = refTransform.InverseTransformPoint(handWorldPos);
            Vector3 aboveLocal = new Vector3(0f, 0.65f, 0f);
            Vector3 approachLocal = new Vector3(outsideLocal.x, Mathf.Max(0.65f, outsideLocal.y), outsideLocal.z);

            // 피자 박스 이동 애니메이션 
            for (float t = 0; t < transferAnimDuration; t += Time.deltaTime)
            {
                float f = Mathf.Clamp01(t / transferAnimDuration);
                if (!storing) f = 1f - f;  

                if (f < 0.25f)
                    movingStack.transform.localPosition = Vector3.Lerp(outsideLocal, approachLocal, Mathf.SmoothStep(0f, 1f, f / 0.25f));
                else if (f < 0.65f)
                    movingStack.transform.localPosition = Vector3.Lerp(approachLocal, aboveLocal, Mathf.SmoothStep(0f, 1f, (f - 0.25f) / 0.4f));
                else
                    movingStack.transform.localPosition = Vector3.Lerp(aboveLocal, insideLocal, Mathf.SmoothStep(0f, 1f, (f - 0.65f) / 0.35f));

                yield return null;
            }

            commit();

            if (movingStack != null) Destroy(movingStack);
            if (handDisplay != null) handDisplay.SetActive(true);
            handDisplay = null;

            if (contentsRoot != null) contentsRoot.gameObject.SetActive(true);
            RefreshInternalVisual();

            yield return RotateDoor(doorHinge.localRotation, closedRotation);
            IsBusy = false;
        }

        private IEnumerator RotateDoor(Quaternion from, Quaternion to)
        {
            for (float t = 0; t < doorAnimDuration; t += Time.deltaTime)
            {
                doorHinge.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / doorAnimDuration));
                yield return null;
            }
            doorHinge.localRotation = to;
        }

        private GameObject CreateBoxStack(Transform parent, int count)
        {
            var root = new GameObject("Cart Pizza Stack");
            root.transform.SetParent(parent, false);
            var prefab = Resources.Load<GameObject>("MokPizzaArt/Box");
            if (prefab == null) return root;

            int visualCount = Mathf.Min(8, count);
            for (int i = 0; i < visualCount; i++)
            {
                var box = Instantiate(prefab, root.transform, false);
                box.transform.localScale = Vector3.one * 0.45f;
                box.transform.localPosition = Vector3.up * (0.055f * i);
                foreach (var c in box.GetComponentsInChildren<Collider>()) Destroy(c);
            }
            foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2; //레케 : 레이캐스트라는 뜻
            return root;
        }

        public void RefreshInternalVisual()
        {
            if (contentsRoot == null) return;
            foreach (Transform t in contentsRoot)
            {
                t.gameObject.SetActive(false);
                Destroy(t.gameObject);
            }
            if (storedPizzas.Count > 0)
            {
                CreateBoxStack(contentsRoot, storedPizzas.Count);
            }
        }
    }
}