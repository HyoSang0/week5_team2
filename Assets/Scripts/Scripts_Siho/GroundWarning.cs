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
        ground = GameObject.Find("GroundInitializer").GetComponent<GroundInitializer>();
        // Play(ground, testCenterX,testCenterZ, testRadius);
    }
    public void Play(GroundInitializer ground, float centerX, float centerZ, float radius)
    {
        transform.position = new Vector3(centerX, -0.89f, centerZ);
        if(ground == null)
            return;

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
            float angle =i / segments * Mathf.PI * 2f;

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

        Destroy(gameObject);
    }
}