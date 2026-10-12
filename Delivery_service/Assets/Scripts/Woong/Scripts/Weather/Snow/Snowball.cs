using UnityEngine;

public class Snowball : MonoBehaviour
{
    public float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime); // 바닥에 굴러다니다가 3초 뒤 소멸
    }

    private void OnCollisionEnter(Collision collision)
    {
        WeatherManager wm = FindObjectOfType<WeatherManager>();

        // 플레이어나 오토바이에 맞았을 때 어지러움 유발
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<MotorcycleController>() != null)
        {
            if (wm != null)
            {
                wm.TriggerDizzyEffect();
            }
        }

        Destroy(gameObject);
    }
}