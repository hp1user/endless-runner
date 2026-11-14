using UnityEngine;

namespace AcidSpitAdvanced
{
    // Only visuals. The collision controller never deforms its sweep volume.
    public class AcidProjectileVisual : MonoBehaviour
    {
        public Transform body;
        public Transform[] droplets;
        public ParticleSystem fineDroplets;
        [Range(3,6)] public int dropletCount = 5;
        [Range(0,1)] public float rippleIntensity = 0.35f;
        [Range(0.25f,1.5f)] public float trailDensity = 1;
        MaterialPropertyBlock block;
        MeshRenderer bodyRenderer;
        float age;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            bodyRenderer = body.GetComponent<MeshRenderer>();
        }
        void OnEnable()
        {
            age = 0;
            if (fineDroplets)
            {
                var emission = fineDroplets.emission; emission.rateOverTime = 22 * trailDensity;
                fineDroplets.Clear(); fineDroplets.Play();
            }
        }
        void Update()
        {
            age += Time.deltaTime;
            block.SetFloat("_RippleIntensity", rippleIntensity);
            bodyRenderer.SetPropertyBlock(block);
            for (int i=0; i<droplets.Length; i++)
            {
                var d = droplets[i]; bool visible = i < dropletCount;
                if (d.gameObject.activeSelf != visible) d.gameObject.SetActive(visible);
                if (!visible) continue;
                float phase = Mathf.Repeat(age * (1.6f+i*0.12f)+i*0.197f,1);
                float angle = i*2.4f+phase*0.8f;
                d.localPosition = new Vector3(Mathf.Cos(angle)*(0.22f+phase*0.5f),Mathf.Sin(angle)*(0.15f+phase*0.3f)-phase*phase*0.25f,-0.4f-phase*2.7f);
                float size = (0.19f+i*0.012f)*Mathf.Sin(Mathf.PI*phase);
                d.localScale = new Vector3(size,size,size*(1.7f+phase));
            }
        }
    }
}
