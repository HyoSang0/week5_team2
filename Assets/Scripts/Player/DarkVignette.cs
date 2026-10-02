using UnityEngine;
using UnityEngine.UI;

public class DarkVignette : MonoBehaviour
{
    [Header("UI 연결")]
    public Image vignetteImage;
    private Material vignetteMaterial;
    public float maxDarkness = 0.85f;
    [Header("Shader에 있는 Vignette Size 값")]
    private int sizePropertyId;
    [Tooltip("체력이 100%일 때의 Vignette Size")]
    private float minSize = 0.1f;
    [Tooltip("체력이 0%일 때의 Vignette Size")]
    private float maxSize = 3f;
    void Start()
    {
        if (vignetteImage != null)
        {
            //원본 Material을 복사하여 새로운 Material 생성 및 할당
            vignetteMaterial = new Material(vignetteImage.material);
            vignetteImage.material = vignetteMaterial;

            // Shader 내부의 변수 이름을 ID로 변환하여 가져오기
            sizePropertyId = Shader.PropertyToID("_VignetteSize");
            vignetteMaterial.SetFloat(sizePropertyId, minSize);
        }
    }

    /// <summary>
    /// 플레이어의 체력이 변할 때(피격, 회복 등) 호출하여 어두운 정도 조절
    /// </summary>
    /// <param name="currentHp">현재 체력</param>
    /// <param name="maxHp">최대 체력</param>
    public void UpdateVignetteDarkness(float currentHp, float maxHp)
    {
        if (vignetteMaterial == null || maxHp <= 0) return;

        // 1. 체력 비율 계산 (0.0 ~ 1.0)
        float hpRatio = Mathf.Clamp01(currentHp / maxHp);

        // 2. 체력에 맞춰 Size 값 보간
        float targetSize = Mathf.Lerp(maxSize, minSize, hpRatio);

        // 3. 머티리얼의 파라미터 업데이트
        vignetteMaterial.SetFloat(sizePropertyId, targetSize);
    }
}
