using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AcidSpitAdvanced.Editor
{
    public static class AcidSpitAssetBuilder
    {
        const string Root="Assets/VFX/AcidSpitAdvanced/";
        static Color Hex(string s) { ColorUtility.TryParseHtmlString(s,out var c); return c; }
        [MenuItem("Tools/VFX/Build Advanced Acid Spit")]
        public static void Build()
        {
            var droplet=SaveMesh(DropMesh(),"AcidDroplet");
            var puddle=SaveMesh(RadialMesh(false),"AcidPuddle");
            var splash=SaveMesh(RadialMesh(true),"AcidSplash");
            var acid=Material("AcidBody","AcidSpitAdvanced/ProjectileURP");
            acid.SetColor("_DarkColor",Hex("#126B23"));
            acid.SetColor("_BaseColor",Hex("#77FF21"));
            acid.SetColor("_HotColor",Hex("#E5FF55"));
            var residue=Material("AcidResidue","AcidSpitAdvanced/PuddleURP");
            var vapor=Material("AcidVapor","AcidSpitAdvanced/SparseVaporURP");

            var p=new GameObject("AcidPuddle_Advanced");
            var pe=p.AddComponent<AcidSurfaceEffect>(); pe.isPuddle=true; pe.duration=3;
            var pm=MeshObject("Bubbling corrosive film",p.transform,puddle,residue);
            pe.liquidMesh=pm.transform; pe.liquidRenderer=pm.GetComponent<MeshRenderer>();
            pe.vapor=Particles("Sizzling vapor (6 max)",p.transform,droplet,vapor,6,false,true);
            GameObject puddlePrefab=SavePrefab(p,"AcidPuddle_Advanced",false);

            var impact=new GameObject("AcidImpact_Advanced");
            var ie=impact.AddComponent<AcidSurfaceEffect>(); ie.duration=0.85f; ie.puddlePrefab=puddlePrefab;
            var im=MeshObject("Jagged liquid crown",impact.transform,splash,acid);
            ie.liquidMesh=im.transform; ie.liquidRenderer=im.GetComponent<MeshRenderer>();
            ie.spray=Particles("Ballistic splash droplets (12)",impact.transform,droplet,acid,16,true,false);
            GameObject impactPrefab=SavePrefab(impact,"AcidImpact_Advanced",false);

            var projectile=new GameObject("AcidProjectile_Advanced");
            var controller=projectile.AddComponent<AcidProjectileController>();
            controller.impactPrefab=impactPrefab; controller.puddlePrefab=puddlePrefab;
            var visual=projectile.AddComponent<AcidProjectileVisual>();
            var body=MeshObject("Pressurized toxic slug",projectile.transform,droplet,acid);
            body.transform.localScale=new Vector3(0.71f,0.62f,1.15f);
            body.transform.localRotation=Quaternion.Euler(24,-28,12);
            visual.body=body.transform;
            visual.droplets=new Transform[6];
            for(int i=0;i<6;i++) visual.droplets[i]=MeshObject("Separating droplet "+i,projectile.transform,droplet,acid).transform;
            visual.fineDroplets=Particles("Fine ballistic droplets (12 max)",projectile.transform,droplet,acid,12,false,false);
            // Runtime resources copy is the authoritative prefab, with direct references to the effects.
            GameObject projectilePrefab=SavePrefab(projectile,"AcidProjectile_Advanced",true);
            CreateTestScene(projectilePrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[AcidSpitAdvanced] Generated meshes, three prefabs, opaque URP materials, and isolated test scene. Original Boss_AcidOrb assets preserved.");
        }

        static Material Material(string name,string shader)
        {
            string path=Root+"Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s=Shader.Find(shader); if(!s) throw new System.Exception("Missing shader "+shader);
            if(!m) { m=new Material(s); AssetDatabase.CreateAsset(m,path); }
            else m.shader=s;
            m.enableInstancing=true; EditorUtility.SetDirty(m); return m;
        }
        static Mesh SaveMesh(Mesh mesh,string name)
        {
            mesh.name=name;
            string path=Root+"Meshes/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); return existing; }
            AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material mat)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>(); r.sharedMaterial=mat;
            r.shadowCastingMode=ShadowCastingMode.Off; r.receiveShadows=false;
            return go;
        }
        static GameObject SavePrefab(GameObject go,string name,bool resource)
        {
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+name+".prefab");
            if(resource)
            {
                // Keep Resources as a variant so edits to the main prefab propagate into gameplay.
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                prefab=PrefabUtility.SaveAsPrefabAsset(instance,Root+"Resources/AcidSpitAdvanced/"+name+".prefab");
                Object.DestroyImmediate(instance);
            }
            Object.DestroyImmediate(go); return prefab;
        }
        static Mesh DropMesh()
        {
            const int segments=16,rings=12;
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var tris=new List<int>();
            for(int j=0;j<=rings;j++)
            {
                float t=(float)j/rings; float z=Mathf.Cos(t*Mathf.PI);
                float radius=Mathf.Sin(t*Mathf.PI)*(0.78f+z*0.18f);
                for(int i=0;i<=segments;i++)
                {
                    float a=i*2*Mathf.PI/segments;
                    float asymmetric=1+0.09f*Mathf.Sin(a*3+t*7)+0.06f*Mathf.Cos(a*2-t*5);
                    float tail=Mathf.Max(0,-z);
                    vertices.Add(new Vector3(Mathf.Cos(a)*radius*asymmetric+0.28f*tail*tail,Mathf.Sin(a)*radius*asymmetric+tail*tail*0.2f,z*(z>0?0.9f:1.65f)));
                    uv.Add(new Vector2((float)i/segments,t));
                }
            }
            for(int j=0;j<rings;j++) for(int i=0;i<segments;i++)
            {
                int a=j*(segments+1)+i,b=a+segments+1;
                if(j>0) { tris.Add(a);tris.Add(b);tris.Add(a+1); }
                if(j<rings-1) { tris.Add(a+1);tris.Add(b);tris.Add(b+1); }
            }
            var m=new Mesh();m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();
            // Expand bounds to include shader displacement.
            var bounds=m.bounds;bounds.Expand(0.5f);m.bounds=bounds;return m;
        }
        static Mesh RadialMesh(bool crown)
        {
            const int n=64,rings=4;
            var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var tr=new List<int>();
            v.Add(new Vector3(0,crown?0.08f:0,0));uv.Add(new Vector2(0.5f,0.5f));colors.Add(new Color(0,0,0,1));
            for(int r=1;r<=rings;r++) for(int i=0;i<n;i++)
            {
                float a=i*2*Mathf.PI/n, f=(float)r/rings;
                float outline=1+0.17f*Mathf.Sin(a*5+0.8f)+0.12f*Mathf.Cos(a*9)+0.06f*Mathf.Sin(a*13);
                float radius=f*outline*(crown?1.1f:1.05f);
                float spike=Mathf.Pow(Mathf.Max(0,Mathf.Sin(a*11+1)),5);
                float y=crown? Mathf.Sin(f*Mathf.PI*0.76f)*(0.16f+spike*0.85f)*f:0;
                v.Add(new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius));
                uv.Add(new Vector2(0.5f+Mathf.Cos(a)*f*0.5f,0.5f+Mathf.Sin(a)*f*0.5f));colors.Add(new Color(f,0,0,1));
            }
            for(int i=0;i<n;i++) { tr.Add(0);tr.Add(1+(i+1)%n);tr.Add(1+i); }
            for(int r=0;r<rings-1;r++) for(int i=0;i<n;i++)
            {
                int a=1+r*n+i,b=1+r*n+(i+1)%n,c=a+n,d=b+n;
                tr.Add(a);tr.Add(b);tr.Add(c);tr.Add(b);tr.Add(d);tr.Add(c);
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static ParticleSystem Particles(string name,Transform parent,Mesh mesh,Material material,int cap,bool burst,bool vapor)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var ps=go.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false; main.loop=!burst;main.duration=burst?0.8f:3;
            main.maxParticles=cap;main.startLifetime=vapor?0.8f:(burst?0.55f:0.38f);
            main.startSpeed=vapor?0.5f:(burst?4.5f:0.8f);
            main.startSize=vapor?0.34f:(burst?0.12f:0.065f);
            main.gravityModifier=vapor?-0.05f:(burst?0.7f:0.2f);
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=new ParticleSystem.MinMaxGradient(Hex("#77FF21"),Hex("#E5FF55"));
            var emission=ps.emission;
            if(burst) { emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,12)}); }
            else emission.rateOverTime=vapor?4:22;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=burst?65:15;shape.radius=burst?0.3f:0.12f;
            go.transform.localRotation=Quaternion.Euler(vapor||burst?-90:0,0,0);
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
            var color=ps.colorOverLifetime;color.enabled=true;
            var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=g;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.renderMode=vapor?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Mesh;
            if(!vapor) renderer.mesh=mesh;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return ps;
        }
        static void CreateTestScene(GameObject prefab)
        {
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var cameraGO=new GameObject("Acid Test Gameplay Camera");var cam=cameraGO.AddComponent<Camera>();
            cameraGO.tag="MainCamera";cameraGO.transform.position=new Vector3(0,2.2f,-9);cameraGO.transform.rotation=Quaternion.Euler(5.5f,0,0);
            cam.fieldOfView=56;cam.backgroundColor=new Color(0.035f,0.045f,0.06f);cam.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-35,0);
            var asphalt=Material("TestAsphalt","Universal Render Pipeline/Lit");asphalt.SetColor("_BaseColor",new Color(0.08f,0.09f,0.105f));asphalt.SetFloat("_Smoothness",0.1f);
            var road=GameObject.CreatePrimitive(PrimitiveType.Cube);road.name="Asphalt impact surface";road.transform.position=new Vector3(0,-0.15f,12);road.transform.localScale=new Vector3(8,0.3f,60);road.GetComponent<Renderer>().sharedMaterial=asphalt;
            var lineMat=Material("TestLaneMarkings","Universal Render Pipeline/Unlit");lineMat.SetColor("_BaseColor",new Color(0.48f,0.49f,0.37f));
            for(int lane=-1;lane<=1;lane+=2) for(int z=-4;z<32;z+=5)
            {
                var line=GameObject.CreatePrimitive(PrimitiveType.Cube);line.name="Lane dash";line.transform.position=new Vector3(lane,0.006f,z);line.transform.localScale=new Vector3(0.065f,0.008f,2);line.GetComponent<Renderer>().sharedMaterial=lineMat;Object.DestroyImmediate(line.GetComponent<Collider>());
            }
            var mark=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mark.name="Visible ground impact target";mark.transform.position=new Vector3(0,0.01f,0);mark.transform.localScale=new Vector3(1.8f,0.01f,1.8f);mark.GetComponent<Renderer>().sharedMaterial=lineMat;Object.DestroyImmediate(mark.GetComponent<Collider>());
            var launcher=new GameObject("Acid Launcher - inspector controls").AddComponent<AcidTestLauncher>();launcher.transform.position=new Vector3(0,1.4f,24);launcher.projectilePrefab=prefab;launcher.target=new Vector3(0,-0.3f,0);
            EditorSceneManager.SaveScene(scene,Root+"Scenes/AcidSpit_VFX_Test.unity");
            EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);
        }
    }
}
