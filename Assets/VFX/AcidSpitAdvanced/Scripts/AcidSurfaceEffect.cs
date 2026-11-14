using UnityEngine;

namespace AcidSpitAdvanced
{
    public class AcidSurfaceEffect : MonoBehaviour
    {
        public bool isPuddle;
        public Transform liquidMesh;
        public MeshRenderer liquidRenderer;
        public ParticleSystem spray;
        public ParticleSystem vapor;
        public GameObject puddlePrefab;
        public float duration = 3;
        public bool vaporEnabled = true;
        [Range(1,12)] public float bubbleCount = 5;
        float age;
        float size = 1;
        MaterialPropertyBlock block;

        void Awake() { block = new MaterialPropertyBlock(); }
        public void Begin(float scale = 1)
        {
            age=0; size=scale;
            if (spray) { spray.Clear(); spray.Play(); }
            if (vapor)
            {
                vapor.Clear();
                if (vaporEnabled) vapor.Play(); else vapor.Stop();
            }
            if (!isPuddle && puddlePrefab)
            {
                var p = AcidPool.Get(puddlePrefab,transform.position+Vector3.up*0.012f,Quaternion.Euler(0,Random.Range(0,360),0),16);
                if (p) p.GetComponent<AcidSurfaceEffect>().Begin(scale);
            }
            Apply(0);
        }
        void Update()
        {
            age += Time.deltaTime;
            if (AcidPool.SurfaceVelocity != null) transform.position += AcidPool.SurfaceVelocity()*Time.deltaTime;
            Apply(age);
            if (age>=duration) gameObject.SetActive(false);
        }
        void Apply(float t)
        {
            if (isPuddle)
            {
                float grow = Mathf.SmoothStep(0.12f,1,t/0.2f);
                float fade = Mathf.Clamp01((t-(duration-0.65f))/0.65f);
                liquidMesh.localScale = Vector3.one*(size*grow*(1-fade*0.85f));
                block.SetFloat("_FadeAmount",fade);
                block.SetFloat("_BubbleScale",bubbleCount);
                liquidRenderer.SetPropertyBlock(block);
            }
            else
            {
                float p = Mathf.Clamp01(t/0.42f);
                liquidMesh.gameObject.SetActive(p<1);
                liquidMesh.localScale = new Vector3(size*Mathf.Lerp(0.12f,1.5f,p),size*(1-p)*(0.65f+Mathf.Sin(p*Mathf.PI)*0.8f),size*Mathf.Lerp(0.12f,1.5f,p));
            }
        }
    }
}
