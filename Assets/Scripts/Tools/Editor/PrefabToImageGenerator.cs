using UnityEditor;
using UnityEngine;
using System.IO;

public class PrefabToImageGenerator : EditorWindow
{
    private GameObject targetPrefab;
    private GameObject previewInstance;
    private PreviewRenderUtility previewUtility;
    private Vector2 dragRotation = new Vector2(15f, -30f);
    private Vector2 panOffset = Vector2.zero; // 상하좌우 이동을 위한 변수 추가
    private float zoom = 2.0f;
    private int imageSize = 1024;

    [MenuItem("Tools/Prefab to Image Generator")]
    public static void ShowWindow()
    {
        GetWindow<PrefabToImageGenerator>("Model Imager").minSize = new Vector2(400, 550);
    }

    private void OnEnable()
    {
        previewUtility = new PreviewRenderUtility();
        previewUtility.cameraFieldOfView = 30f;
        previewUtility.camera.nearClipPlane = 0.01f;
        previewUtility.camera.farClipPlane = 1000f;

        previewUtility.lights[0].intensity = 1.5f;
        previewUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0f);
        previewUtility.lights[1].intensity = 1.0f;
    }

    private void OnDisable()
    {
        DestroyPreviewInstance();
        if (previewUtility != null)
        {
            previewUtility.Cleanup();
            previewUtility = null;
        }
    }

    private void DestroyPreviewInstance()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "• 좌클릭 드래그: 회전\n" +
            "• 휠 드래그: 상하좌우 이동 (Pan)\n" +
            "• 휠 스크롤: 줌인/줌아웃\n" +
            "(저장 시 완벽한 투명 배경 + 조명이 적용됩니다.)", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);

        if (EditorGUI.EndChangeCheck())
        {
            DestroyPreviewInstance();
            if (targetPrefab != null)
            {
                previewInstance = Instantiate(targetPrefab);
                previewInstance.hideFlags = HideFlags.HideAndDontSave;

                foreach (var script in previewInstance.GetComponentsInChildren<MonoBehaviour>())
                {
                    script.enabled = false;
                }

                previewUtility.AddSingleGO(previewInstance);

                // 프리팹 변경 시 카메라 상태 초기화
                dragRotation = new Vector2(15f, -30f);
                panOffset = Vector2.zero;
                zoom = 2.0f;
            }
        }

        imageSize = EditorGUILayout.IntSlider("Image Size", imageSize, 256, 2048);

        if (targetPrefab == null) return;

        GUILayout.Space(10);

        Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));

        HandleInput(previewRect);

        if (Event.current.type == EventType.Repaint)
        {
            RenderPreview(previewRect);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Save as Transparent PNG", GUILayout.Height(40)))
        {
            SaveImage();
        }
    }

    private void HandleInput(Rect previewRect)
    {
        Event e = Event.current;
        if (previewRect.Contains(e.mousePosition))
        {
            // 좌클릭 드래그: 회전
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                dragRotation.x += e.delta.y * 2f;
                dragRotation.y -= e.delta.x * 2f;
                e.Use();
                Repaint();
            }
            // 휠 클릭(마우스 중앙 버튼) 드래그: 상하좌우 이동
            else if (e.type == EventType.MouseDrag && e.button == 2)
            {
                // 줌 상태에 비례해서 이동 속도를 맞춰 자연스럽게 조절
                float panSpeed = 0.015f * zoom;
                panOffset.x -= e.delta.x * panSpeed;
                panOffset.y += e.delta.y * panSpeed; // 유니티 GUI 좌표계 특성상 Y축 반전
                e.Use();
                Repaint();
            }
            // 마우스 휠 스크롤: 줌인/줌아웃
            else if (e.type == EventType.ScrollWheel)
            {
                zoom += e.delta.y * 0.05f;
                zoom = Mathf.Clamp(zoom, 0.1f, 10f);
                e.Use();
                Repaint();
            }
        }
    }

    private void DoRenderSetup()
    {
        if (previewInstance == null) return;

        previewInstance.transform.position = Vector3.zero;
        previewInstance.transform.rotation = Quaternion.Euler(dragRotation.x, dragRotation.y, 0);

        Bounds bounds = new Bounds(previewInstance.transform.position, Vector3.zero);
        Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;

        foreach (var r in renderers)
        {
            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (!hasBounds) bounds.extents = Vector3.one;

        float maxExtent = bounds.extents.magnitude;
        float distance = (maxExtent * 2.0f) * zoom;

        // 카메라의 초점(Focus) 위치에 Pan Offset 적용
        Vector3 focusPoint = bounds.center + (Vector3.right * panOffset.x) + (Vector3.up * panOffset.y);

        previewUtility.camera.transform.position = focusPoint - Vector3.forward * distance;
        previewUtility.camera.transform.LookAt(focusPoint);

        previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
        previewUtility.camera.backgroundColor = new Color(0, 0, 0, 0);
    }

    private void RenderPreview(Rect rect)
    {
        previewUtility.BeginPreview(rect, GUIStyle.none);
        DoRenderSetup();
        previewUtility.camera.Render();
        Texture resultRender = previewUtility.EndPreview();

        GUI.DrawTexture(rect, resultRender, ScaleMode.ScaleToFit, true);
    }

    private void SaveImage()
    {
        Rect captureRect = new Rect(0, 0, imageSize, imageSize);

        previewUtility.BeginPreview(captureRect, GUIStyle.none);
        DoRenderSetup();

        RenderTexture rt = RenderTexture.GetTemporary(imageSize, imageSize, 24, RenderTextureFormat.ARGB32);
        RenderTexture oldTarget = previewUtility.camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;

        previewUtility.camera.targetTexture = rt;
        RenderTexture.active = rt;

        previewUtility.camera.Render();

        Texture2D captureTexture = new Texture2D(imageSize, imageSize, TextureFormat.RGBA32, false);
        captureTexture.ReadPixels(new Rect(0, 0, imageSize, imageSize), 0, 0);
        captureTexture.Apply();

        previewUtility.camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        RenderTexture.ReleaseTemporary(rt);

        previewUtility.EndPreview();

        byte[] bytes = captureTexture.EncodeToPNG();
        DestroyImmediate(captureTexture);

        string defaultName = $"{targetPrefab.name}_Icon";
        string path = EditorUtility.SaveFilePanel("Save Transparent PNG", "Assets", defaultName, "png");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log($"[Model Imager] 성공적으로 저장되었습니다: {path}");
        }
    }
}