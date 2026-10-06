using UnityEngine;


public class PhoneController : MonoBehaviour
{
    [Header("핸드폰")]
    [Tooltip("움직일 핸드폰 오브젝트의 Transform")]
    [SerializeField] private Transform phoneTransform;

    [Header("핸드폰 위치")]
    [Tooltip("핸드폰을 꺼냈을 때의 로컬 위치")]
    [SerializeField] private Vector3 shownPosition =
        new Vector3(0f, -0.15f, 0.45f);

    [Tooltip("핸드폰을 집어넣었을 때의 로컬 위치")]
    [SerializeField] private Vector3 hiddenPosition =
        new Vector3(0f, -0.65f, 0.45f);

    [Header("애니메이션")]
    [Tooltip("핸드폰이 올라오고 내려가는 속도")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("움직임을 더 자연스럽게 만드는 부드러움 정도")]
    [SerializeField] private float smoothness = 12f;

    // 현재 핸드폰을 꺼낸 상태인지 여부
    private bool isPhoneOpen = false;

    public void ClosePhone()
    {
        isPhoneOpen = false;

        if (phoneTransform != null)
        {
            phoneTransform.localPosition = hiddenPosition;
        }
    }



    private void Start()
    {
        if (phoneTransform == null)
            return;

        // 게임 시작 시 핸드폰은 화면 아래에 숨겨둔다.
        phoneTransform.localPosition = hiddenPosition;
    }


    private void Update()
    {
        if (phoneTransform == null)
            return;



        if (Input.GetKeyDown(KeyCode.M))
        {
            TogglePhone();
        }



        Vector3 targetPosition =
            isPhoneOpen
                ? shownPosition
                : hiddenPosition;


        float interpolation =
            1f - Mathf.Exp(
                -smoothness * moveSpeed * Time.deltaTime
            );

        phoneTransform.localPosition =
            Vector3.Lerp(
                phoneTransform.localPosition,
                targetPosition,
                interpolation
            );
    }


    public void TogglePhone()
    {
        isPhoneOpen = !isPhoneOpen;
    }


    public bool IsPhoneOpen()
    {
        return isPhoneOpen;
    }
}