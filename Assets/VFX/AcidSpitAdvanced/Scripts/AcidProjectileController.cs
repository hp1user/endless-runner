using System;
using UnityEngine;

namespace AcidSpitAdvanced
{
    public class AcidProjectileController : MonoBehaviour
    {
        public GameObject impactPrefab;
        public GameObject puddlePrefab;
        public LayerMask collisionMask = ~0;
        public LayerMask groundMask = ~0;
        public float collisionRadius = 0.58f;
        public float lifetime = 8;
        public bool groundSkimming;
        public float skimHeight = 0.85f;
        public float patchSpacing = 2.5f;
        public int maxSkimPatches = 4;
        public event Action<AcidProjectileController, RaycastHit> Impact;
        public int ImpactCount { get; private set; }
        public float Age => age;
        readonly RaycastHit[] hits = new RaycastHit[12];
        Vector3 velocity;
        float age,gravity,remainingPatchDistance;
        int patches;
        bool launched;
        float stopPlane;

        public static AcidProjectileController Launch(GameObject prefab, Vector3 origin, Vector3 direction, float speed, float arcGravity=0, float impactPlaneZ=float.NaN)
        {
            var go=AcidPool.Get(prefab,origin,Quaternion.LookRotation(direction.normalized),8);
            if (!go) return null;
            var c=go.GetComponent<AcidProjectileController>();
            c.velocity=direction.normalized*speed;
            c.gravity=arcGravity; c.age=0; c.patches=0; c.remainingPatchDistance=0;
            c.stopPlane=impactPlaneZ; c.launched=true; c.ImpactCount=0; c.Impact=null;
            return c;
        }
        public static AcidProjectileController LaunchAt(GameObject prefab, Vector3 origin, Vector3 target, float speed, float arcGravity=0)
        {
            Vector3 direction=target-origin;
            if (arcGravity>0) direction.y+=0.5f*arcGravity*Mathf.Pow(direction.magnitude/Mathf.Max(speed,0.01f),2);
            return Launch(prefab,origin,direction,speed,arcGravity);
        }
        void OnDisable() { launched=false; Impact=null; }
        void Update()
        {
            if (!launched) return;
            float dt=Time.deltaTime; age+=dt;
            if (age>=lifetime) { gameObject.SetActive(false); return; }
            Vector3 old=transform.position;
            Vector3 step=velocity*dt+Vector3.down*(0.5f*gravity*dt*dt);
            velocity+=Vector3.down*gravity*dt;
            float length=step.magnitude;
            if (length<0.00001f) return;
            int count=Physics.SphereCastNonAlloc(old,collisionRadius,step/length,hits,length,collisionMask,QueryTriggerInteraction.Collide);
            int closest=-1; float nearest=float.MaxValue;
            for (int i=0;i<count;i++)
            {
                if (!hits[i].collider || hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance<nearest) { nearest=hits[i].distance; closest=i; }
            }
            if (closest>=0) { Detonate(hits[closest].point,hits[closest]); return; }
            Vector3 next=old+step;
            // Optional gameplay plane is swept too, so low framerates cannot skip the hit.
            if (!float.IsNaN(stopPlane) && (old.z-stopPlane)*(next.z-stopPlane)<=0)
            {
                float f=Mathf.Abs((old.z-stopPlane)/(next.z-old.z));
                Vector3 p=Vector3.Lerp(old,next,f); transform.position=p;
                Detonate(p,default); return;
            }
            transform.position=next;
            transform.rotation=Quaternion.LookRotation(velocity);
            if (groundSkimming && patches<maxSkimPatches)
            {
                remainingPatchDistance-=length;
                if (remainingPatchDistance<=0)
                {
                    remainingPatchDistance=Mathf.Max(0.5f,patchSpacing);
                    if (Physics.Raycast(next,Vector3.down,out var ground,skimHeight,groundMask,QueryTriggerInteraction.Ignore) && ground.normal.y>0.65f)
                    {
                        var p=AcidPool.Get(puddlePrefab,ground.point+Vector3.up*0.035f,Quaternion.Euler(0,UnityEngine.Random.Range(0,360),0),16);
                        if (p) { p.GetComponent<AcidSurfaceEffect>().Begin(0.4f); patches++; }
                    }
                }
            }
        }
        void Detonate(Vector3 point, RaycastHit hit)
        {
            if (!launched) return;
            launched=false; ImpactCount++;
            Impact?.Invoke(this,hit);
            Vector3 ground=point;
            if (Physics.Raycast(point+Vector3.up*2,Vector3.down,out var road,6,groundMask,QueryTriggerInteraction.Ignore) && road.normal.y>0.65f)
                ground=road.point;
            else ground.y=0;
            var fx=AcidPool.Get(impactPrefab,ground+Vector3.up*0.04f,Quaternion.Euler(0,UnityEngine.Random.Range(0,360),0),8);
            if (fx) fx.GetComponent<AcidSurfaceEffect>().Begin();
            gameObject.SetActive(false);
        }
    }
}
