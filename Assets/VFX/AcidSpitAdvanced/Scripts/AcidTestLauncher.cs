using UnityEngine;

namespace AcidSpitAdvanced
{
    public class AcidTestLauncher : MonoBehaviour
    {
        public GameObject projectilePrefab;
        public float speed=14;
        public float interval=3;
        [Range(0.5f,1.5f)] public float intensity=1;
        public float arcGravity;
        public bool groundSkimming;
        public bool autoFire=true;
        public Vector3 target=new Vector3(0,0.6f,0);
        public AcidProjectileController latest;
        float next;
        void Start()
        {
            AcidPool.SurfaceVelocity=null;
            AcidPool.Prewarm(projectilePrefab,4);
            var c=projectilePrefab.GetComponent<AcidProjectileController>();
            AcidPool.Prewarm(c.impactPrefab,4); AcidPool.Prewarm(c.puddlePrefab,16);
        }
        public static System.Collections.IEnumerator FreezeAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Time.timeScale=0;
        }
        void Update() { if (autoFire && Time.time>=next) { next=Time.time+interval; Fire(); } }
        [ContextMenu("Fire acid")]
        public void Fire()
        {
            latest=AcidProjectileController.LaunchAt(projectilePrefab,transform.position,target,speed,arcGravity);
            if (!latest) return;
            latest.transform.localScale=Vector3.one*intensity;
            latest.groundSkimming=groundSkimming;
        }

#if UNITY_EDITOR
        // Editor-only validation drives real Update/Physics frames, then captures the camera.
        public string validationReport;
        public System.Collections.IEnumerator Validate()
        {
            autoFire=false; Time.timeScale=1;
            float previousCapture=Time.captureDeltaTime;
            Time.captureDeltaTime=1f/60;
            ClearEffects();
            var cam=Camera.main;
            latest=AcidProjectileController.Launch(projectilePrefab,new Vector3(0,1.35f,9),Vector3.back,14,0,0);
            latest.collisionMask=0; latest.groundSkimming=false;
            yield return new WaitForSeconds(0.25f);
            Capture(cam,"test-flight-final");
            yield return new WaitForSeconds(0.48f);
            Capture(cam,"test-impact-final");
            bool planeHit=latest.ImpactCount==1 && !latest.gameObject.activeSelf;
            yield return new WaitForSeconds(0.7f);
            Capture(cam,"test-puddle-final");
            yield return new WaitForSeconds(3.1f);
            bool returned=Object.FindObjectsByType<AcidSurfaceEffect>(FindObjectsSortMode.None).Length==0;

            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name="Temporary swept-collision validation target";
            obstacle.transform.position=new Vector3(0,1.3f,0);
            obstacle.transform.localScale=new Vector3(2,2,0.1f);
            Physics.SyncTransforms();
            latest=AcidProjectileController.Launch(projectilePrefab,new Vector3(0,1.35f,10),Vector3.back,1400);
            latest.collisionMask=~0; bool colliderHit=false;
            latest.Impact+=(c,h)=>colliderHit=h.collider==obstacle.GetComponent<Collider>();
            yield return new WaitForSeconds(0.08f);
            bool sweptHit=colliderHit && latest.ImpactCount==1;
            Destroy(obstacle); ClearEffects();

            latest=AcidProjectileController.Launch(projectilePrefab,new Vector3(0,5,10),Vector3.back,14);
            latest.collisionMask=0; latest.groundSkimming=true;
            yield return new WaitForSeconds(0.3f);
            bool airborne=Object.FindObjectsByType<AcidSurfaceEffect>(FindObjectsSortMode.None).Length==0;
            ClearEffects();
            latest=AcidProjectileController.Launch(projectilePrefab,new Vector3(0,0.6f,10),Vector3.back,14);
            latest.collisionMask=0; latest.groundSkimming=true;
            yield return new WaitForSeconds(0.3f);
            int skimCount=Object.FindObjectsByType<AcidSurfaceEffect>(FindObjectsSortMode.None).Length;
            bool skimming=skimCount>0 && skimCount<=latest.maxSkimPatches;
            ClearEffects();
            var puddle=projectilePrefab.GetComponent<AcidProjectileController>().puddlePrefab;
            int accepted=0;
            for(int i=0;i<20;i++)
            {
                var p=AcidPool.Get(puddle,new Vector3(i*0.01f,0.03f,4),Quaternion.identity,16);
                if(p) { accepted++; p.GetComponent<AcidSurfaceEffect>().Begin(); }
            }
            bool bounded=accepted==16;
            ClearEffects();
            validationReport="Plane impact exactly once: "+planeHit+"\nSurface effects returned after lifetime: "+returned+"\n1400 units/sec swept collider hit: "+sweptHit+"\nAirborne shot leaves no puddles: "+airborne+"\nGround-skimming spaced patches: "+skimming+" ("+skimCount+")\nStrict puddle pool cap 16: "+bounded;
            System.IO.File.WriteAllText("Captures/AcidSpitAdvanced/validation.txt",validationReport);
            Debug.Log("[AcidSpitAdvanced Validation]\n"+validationReport);
            Time.captureDeltaTime=previousCapture; Time.timeScale=0;
        }
        static void ClearEffects()
        {
            foreach(var c in Object.FindObjectsByType<AcidProjectileController>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            foreach(var c in Object.FindObjectsByType<AcidSurfaceEffect>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        }
        public static void Capture(Camera cam,string name)
        {
            const int width=720,height=1280;
            var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;float oldAspect=cam.aspect;
            var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=rt;cam.aspect=(float)width/height;cam.Render();RenderTexture.active=rt;
                tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
                System.IO.Directory.CreateDirectory("Captures/AcidSpitAdvanced");
                System.IO.File.WriteAllBytes("Captures/AcidSpitAdvanced/"+name+".png",tex.EncodeToPNG());
            }
            finally { cam.targetTexture=oldTarget;cam.aspect=oldAspect;RenderTexture.active=oldActive;Destroy(tex);rt.Release();Destroy(rt); }
        }
#endif
    }
}
