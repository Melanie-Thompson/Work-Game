using UnityEngine;
using TMPro;

public class RetroArcadeText : MonoBehaviour
{
    [Header("Text Component")]
    public TMP_Text textComponent;
    
    [Header("Movement")]
    public float riseSpeed = 50f;
    public bool destroyWhenOffScreen = true;
    public float destroyHeight = 2000f;

    [Header("Sine Wave Letter Movement")]
    public bool enableSineWaveMovement = true;
    public float sineWaveAmplitude = 10f;
    public float sineWaveSpeed = 2f;
    public float letterDelay = 0.3f;
    
    [Header("Wobble Settings")]
    [Range(0f, 10f)]
    public float wobbleIntensity = 3f;
    [Range(0f, 5f)]
    public float wobbleSpeed = 2f;
    public bool individualCharacterWobble = true;
    
    [Header("Shimmer/Flicker")]
    public bool enableShimmer = true;
    [Range(0f, 5f)]
    public float shimmerSpeed = 3f;
    public Color shimmerColorA = Color.white;
    public Color shimmerColorB = Color.cyan;
    [Range(0f, 1f)]
    public float flickerIntensity = 0.1f;
    
    [Header("CRT Scan Lines")]
    public bool enableScanLines = true;
    [Range(0f, 20f)]
    public float scanLineSpeed = 5f;
    [Range(0f, 0.5f)]
    public float scanLineDarkness = 0.3f;
    public int scanLineFrequency = 4;
    
    private TMP_TextInfo textInfo;
    private float scanLineOffset = 0f;
    
    void Start()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();
    }
    
    void Update()
    {
        // Rise up the screen (works for both RectTransform and regular Transform)
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            // UI element - use localPosition
            rectTransform.localPosition += Vector3.up * riseSpeed * Time.deltaTime;

            // Destroy when off screen
            if (destroyWhenOffScreen && rectTransform.localPosition.y > destroyHeight)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            // World space object
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;

            // Destroy when off screen
            if (destroyWhenOffScreen && transform.position.y > destroyHeight)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Apply sine wave or wobble effect (sine wave now handles shimmer internally)
        if (enableSineWaveMovement)
            ApplySineWaveEffect();
        else
        {
            ApplyWobbleEffect();

            // Only apply shimmer separately if NOT using sine wave
            if (enableShimmer)
                ApplyShimmerEffect();
        }

        // Apply scan line effect
        if (enableScanLines)
            ApplyScanLineEffect();
    }
    
    void ApplySineWaveEffect()
    {
        textComponent.ForceMeshUpdate();
        textInfo = textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            // Calculate sine wave offset
            float phaseOffset = i * letterDelay;
            float sineValue = Mathf.Sin((Time.time * sineWaveSpeed) + phaseOffset);
            float yOffset = sineValue * sineWaveAmplitude;

            Vector3 offset = new Vector3(0, yOffset, 0);

            // Apply to all 4 vertices of the character
            vertices[vertexIndex + 0] += offset;
            vertices[vertexIndex + 1] += offset;
            vertices[vertexIndex + 2] += offset;
            vertices[vertexIndex + 3] += offset;

            // Apply rainbow color per letter if shimmer is enabled
            if (enableShimmer)
            {
                float hue = ((Time.time * shimmerSpeed * 0.1f) + (i * 0.05f)) % 1f;
                Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
                Color32 color32 = rainbowColor;

                colors[vertexIndex + 0] = color32;
                colors[vertexIndex + 1] = color32;
                colors[vertexIndex + 2] = color32;
                colors[vertexIndex + 3] = color32;
            }
        }

        // Update mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            if (enableShimmer)
            {
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            }
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
    
    void ApplyWobbleEffect()
    {
        textComponent.ForceMeshUpdate();
        textInfo = textComponent.textInfo;
        
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;
                
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            
            // Character-specific or wave-based wobble
            float charOffset = individualCharacterWobble ? i * 0.5f : 0f;
            
            float wobbleX = Mathf.Sin(Time.time * wobbleSpeed + charOffset) * wobbleIntensity;
            float wobbleY = Mathf.Cos(Time.time * wobbleSpeed * 1.3f + charOffset * 0.7f) * wobbleIntensity * 0.4f;
            
            // Add some random micro-jitter for that unstable CRT feel
            wobbleX += Mathf.PerlinNoise(Time.time * 10f, i) * wobbleIntensity * 0.3f;
            
            Vector3 offset = new Vector3(wobbleX, wobbleY, 0);
            
            // Apply to all 4 vertices of the character
            vertices[vertexIndex + 0] += offset;
            vertices[vertexIndex + 1] += offset;
            vertices[vertexIndex + 2] += offset;
            vertices[vertexIndex + 3] += offset;
        }
        
        // Update mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
    
    void ApplyShimmerEffect()
    {
        // Rainbow cycle using HSV
        float hue = (Time.time * shimmerSpeed * 0.1f) % 1f;
        Color baseColor = Color.HSVToRGB(hue, 1f, 1f);

        // Add random flicker
        float flicker = 1f - Random.Range(0f, flickerIntensity);
        baseColor *= flicker;

        textComponent.color = baseColor;
    }
    
    void ApplyScanLineEffect()
    {
        scanLineOffset += scanLineSpeed * Time.deltaTime;
        
        if (textInfo == null)
            return;
        
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;
                
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            
            // Get character's screen position
            float charY = vertices[vertexIndex].y;
            
            // Calculate scan line darkness based on position
            float scanLine = Mathf.Abs(Mathf.Sin((charY + scanLineOffset) * scanLineFrequency));
            float darkness = 1f - (scanLine * scanLineDarkness);
            
            // Apply darkness to all vertices
            for (int j = 0; j < 4; j++)
            {
                Color32 color = colors[vertexIndex + j];
                colors[vertexIndex + j] = new Color32(
                    (byte)(color.r * darkness),
                    (byte)(color.g * darkness),
                    (byte)(color.b * darkness),
                    color.a
                );
            }
        }
        
        // Update colors
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}