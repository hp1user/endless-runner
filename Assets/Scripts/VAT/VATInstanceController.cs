using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class VATInstanceController : MonoBehaviour
{
    [Header("VAT Quadrant Settings")]
    [Tooltip("0 = Top-Left, 1 = Top-Right, 2 = Bottom-Left, 3 = Bottom-Right")]
    [Range(0, 3)]
    public int quadrantIndex = 0;

    [Header("Animation Settings")]
    [Tooltip("The index of the animation to play (e.g. 0, 1)")]
    public int animationIndex = 0;
    
    [Tooltip("Total animations baked into this quadrant of the texture")]
    public int totalAnimations = 2;

    [Tooltip("Playback speed multiplier (e.g. 200 for a 1.28s clip at 256 frames)")]
    public float animationSpeed = 200f;

    [Tooltip("Total vertices of this specific mesh")]
    public float totalVertices = 272f;

    private const string quadrantPropName = "_QuadrantSelector";
    private const string animIndexPropName = "_AnimationIndex";
    private const string totalAnimsPropName = "_TotalAnimations";
    private const string animSpeedPropName = "_AnimationSpeed";
    private const string totalVertsPropName = "_TotalVertices";

    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    private void OnEnable()
    {
        meshRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        UpdateProperties();
    }

    private void OnValidate()
    {
        UpdateProperties();
    }

    private void Update()
    {
        // Keep property blocks updated at runtime
        UpdateProperties();
    }

    public void UpdateProperties()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<Renderer>();
        }

        if (meshRenderer == null) return;

        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        meshRenderer.GetPropertyBlock(propBlock);

        // Apply instance-specific values using Shader Graph reference names
        propBlock.SetFloat(quadrantPropName, (float)quadrantIndex);
        propBlock.SetFloat(animIndexPropName, (float)animationIndex);
        propBlock.SetFloat(totalAnimsPropName, (float)totalAnimations);
        propBlock.SetFloat(animSpeedPropName, animationSpeed);
        propBlock.SetFloat(totalVertsPropName, totalVertices);

        meshRenderer.SetPropertyBlock(propBlock);
    }
}
