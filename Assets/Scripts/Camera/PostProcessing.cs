using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessing : MonoBehaviour
{
    [Header("Shader Debug")]
    [Range(-100f, 100f)] public float DebugWallThreshold;
    [Range(0.01f, 10f)] public float DebugNoiseScale;
    [Range(0.01f, 2f)] public float DebugFogIntensity;
    [Range(0f, 10f)] public float DebugMinFog;
    [Range(0.01f, 4f)] public float DebugFogSpeed;
    [Range(1f, 10f)] public float DebugVisionRadius;
    [Range(0.1f, 2)] public float DebugExp;

    public Material mat;
    public Transform player;
    public bool shaderTesting;
    private float angleOfIncident;

    private void OnEnable()
    {
        if (!shaderTesting)
        {
            enabled = false;
            return;
        }
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        angleOfIncident = GetComponent<CameraMovement>().angleOfIncident;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        mat.SetFloat("_DebugWallThreshold", DebugWallThreshold);
        mat.SetFloat("_NoiseScale", DebugNoiseScale);
        mat.SetFloat("_FogIntensity", DebugFogIntensity);
        mat.SetFloat("_MinFog", DebugMinFog);
        mat.SetFloat("_FogSpeed", DebugFogSpeed);
        Vector2 playerUVPos = Camera.main.WorldToViewportPoint(player.position);
        mat.SetVector("_PlayerUVPos", playerUVPos);
        Vector2 cameraOffset = Camera.main.WorldToViewportPoint(Vector3.zero);
        mat.SetVector("_CameraOffset", -cameraOffset);
        mat.SetFloat("_CameraOrthographicSize", Camera.main.orthographicSize);
        mat.SetFloat("_CameraAngleOfIncident", angleOfIncident);
        mat.SetFloat("_VisionRadius", DebugVisionRadius);
        mat.SetFloat("_Exp", DebugExp);
        Graphics.Blit(source, destination, mat);
    }
}
