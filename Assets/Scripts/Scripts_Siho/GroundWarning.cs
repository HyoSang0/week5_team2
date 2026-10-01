using System.Collections;
using UnityEngine;

public class GroundWarning : MonoBehaviour
{
    public Transform fill;
    public LineRenderer outline;
    public GroundInitializer ground;

    public float warningTime = 3f;
    public int segments = 64;
    public float outlineWidth = 0.08f;


    [Header("테스트용")]
    public float testCenterX = 0f;
    public float testCenterZ = 0f;
    public float testRadius = 3f;

    void Start()
    {
        // 재사용 시 매번 Find를 다시 하지 않도록 비어 있을 때만 조회한다.
        if (ground == null)
            ground = GameObject.Find("GroundInitializer").GetComponent<GroundInitializer>();
        // Play(ground, testCenterX,testCenterZ, testRadius);
    }

    /// <summary>
    /// warning 반경을 중심으로 경고 표시를 시작한다.
    /// ground와 centerX/centerZ/radius를 받아 외곽선을 그리고 채우기 코루틴을 다시 시작한다.
    /// </summary>
    public void Play(GroundInitializer ground, float centerX, float centerZ, float radius)
    {
        transform.position = new Vector3(centerX, -0.89f, centerZ);
        if (ground == null)
            return;

        // 풀 재사용 대비: 이전 코루틴과 채움 상태를 초기화한다.
        StopAllCoroutines();
        fill.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        CreateOutline(radius);
        StartCoroutine(
            WarningRoutine(ground, centerX, centerZ, radius)
        );
    }

    private void CreateOutline(float radius)
    {
        outline.positionCount = segments;
        outline.loop = true;
        outline.useWorldSpace = false;

        outline.startWidth = outlineWidth;
        outline.endWidth = outlineWidth;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            outline.SetPosition(
                i,
                new Vector3(x, 0.01f, z)
            );
        }
    }

    private IEnumerator WarningRoutine(
        GroundInitializer ground,
        float centerX,
        float centerZ,
        float radius)
    {
        float time = 0f;

        while (time < warningTime)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(time / warningTime);

            float diameter =
                radius * 2f * progress;

            fill.localScale = new Vector3(
                diameter,
                0.005f,
                diameter
            );

            yield return null;
        }

        // 정확히 최종 크기
        fill.localScale = new Vector3(
            radius * 2f,
            0.005f,
            radius * 2f
        );

        // 땅 제거
        ground.DisableWave1(
            centerX,
            centerZ,
            radius
        );

        EffectPool.Release(gameObject);
    }
}