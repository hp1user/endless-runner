using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class VATBakerWindow : EditorWindow
{
    public enum Quadrant
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum TexturePrecision
    {
        Half,
        Float
    }

    public enum AlphaMode
    {
        DisplacementMagnitude,
        VelocityScale
    }

    public enum MappingMode
    {
        IndexBased,
        DistanceBased
    }

    public enum SaveFormat
    {
        EXR,
        Asset
    }

    public enum GeometrySource
    {
        TargetMesh,
        SourceMesh
    }

    // Input fields
    private GameObject sourceGO;
    private GameObject targetGO;
    private List<AnimationClip> animationClips = new List<AnimationClip>();
    private Quadrant targetQuadrant = Quadrant.BottomLeft;
    private TexturePrecision precision = TexturePrecision.Half;
    private AlphaMode alphaMode = AlphaMode.DisplacementMagnitude;
    private MappingMode mappingMode = MappingMode.IndexBased;
    private SaveFormat saveFormat = SaveFormat.EXR;
    private GeometrySource geometrySource = GeometrySource.TargetMesh;

    private Texture2D targetTexture;
    private string outputMeshPath = "Assets/VATBaker_Mesh.asset";
    private string outputTexturePath = "Assets/VATBaker_Texture.exr";

    // Auto-detected components
    private Animator[] detectedAnimators = new Animator[0];
    private SkinnedMeshRenderer[] detectedSMRs = new SkinnedMeshRenderer[0];
    private int selectedAnimatorIndex = 0;
    private int selectedSMRIndex = 0;

    private Vector2 animListScroll = Vector2.zero;
    private Material wireframeMat;

    [MenuItem("Window/VAT Baker-WofstudioZ")]
    public static void ShowWindow()
    {
        VATBakerWindow window = GetWindow<VATBakerWindow>("VAT Baker-WofstudioZ");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (wireframeMat != null)
        {
            DestroyImmediate(wireframeMat);
            wireframeMat = null;
        }
    }

    private void OnGUI()
    {
        // Custom styling for premium look
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            margin = new RectOffset(0, 0, 10, 15)
        };
        titleStyle.normal.textColor = new Color(0.1f, 0.8f, 1f);

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            margin = new RectOffset(0, 0, 10, 5)
        };

        EditorGUILayout.LabelField("VAT Baker-WofstudioZ", titleStyle);

        // --- SECTION 1: SOURCE DESIGN & AUTO-DETECTION ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Source Components", headerStyle);

        EditorGUI.BeginChangeCheck();
        sourceGO = (GameObject)EditorGUILayout.ObjectField("Source Rigged GO", sourceGO, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck())
        {
            AutoDetectComponents();
        }

        if (sourceGO != null)
        {
            // Animator Dropdown
            if (detectedAnimators.Length > 0)
            {
                string[] animatorNames = new string[detectedAnimators.Length];
                for (int i = 0; i < detectedAnimators.Length; i++)
                {
                    animatorNames[i] = $"{detectedAnimators[i].name} (Animator)";
                }
                selectedAnimatorIndex = EditorGUILayout.Popup("Animator", selectedAnimatorIndex, animatorNames);
            }
            else
            {
                EditorGUILayout.HelpBox("No Animator component found in children.", MessageType.Warning);
            }

            // SkinnedMeshRenderer Dropdown
            if (detectedSMRs.Length > 0)
            {
                string[] smrNames = new string[detectedSMRs.Length];
                for (int i = 0; i < detectedSMRs.Length; i++)
                {
                    smrNames[i] = $"{detectedSMRs[i].name} (SkinnedMeshRenderer)";
                }
                selectedSMRIndex = EditorGUILayout.Popup("Skinned Mesh", selectedSMRIndex, smrNames);
            }
            else
            {
                EditorGUILayout.HelpBox("No SkinnedMeshRenderer found in children.", MessageType.Error);
            }

            // Validation check
            if (detectedAnimators.Length > 0 && detectedSMRs.Length > 0)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✔ Validation Passed: Ready for baking.", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign a Source Rigged GameObject to detect components.", MessageType.Info);
        }
        EditorGUILayout.EndVertical();

        // --- SECTION 2: TARGET MESH CONFIGURATION ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Target Static Mesh", headerStyle);

        targetGO = (GameObject)EditorGUILayout.ObjectField("Target Static GO", targetGO, typeof(GameObject), true);
        if (targetGO != null)
        {
            MeshFilter mf = targetGO.GetComponentInChildren<MeshFilter>();
            if (mf == null)
            {
                EditorGUILayout.HelpBox("Target GameObject has no MeshFilter in children.", MessageType.Error);
            }
            else if (mf.sharedMesh == null)
            {
                EditorGUILayout.HelpBox("Target MeshFilter has no Mesh assigned.", MessageType.Error);
            }
            else
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField($"✔ Target Mesh detected: {mf.sharedMesh.name} ({mf.sharedMesh.vertexCount} vertices)", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a Target GameObject (unrigged static mesh) to receive UV2/bake data.", MessageType.Info);
        }
        EditorGUILayout.EndVertical();

        // --- SECTION 3: ANIMATIONS LIST ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Animations to Bake", headerStyle);

        int listSize = EditorGUILayout.IntField("Animations Count", animationClips.Count);
        while (animationClips.Count < listSize) animationClips.Add(null);
        while (animationClips.Count > listSize) animationClips.RemoveAt(animationClips.Count - 1);

        animListScroll = EditorGUILayout.BeginScrollView(animListScroll, GUILayout.Height(100));
        for (int i = 0; i < animationClips.Count; i++)
        {
            animationClips[i] = (AnimationClip)EditorGUILayout.ObjectField($"Anim Clip {i}", animationClips[i], typeof(AnimationClip), false);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // --- SECTION 4: SETTINGS ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Bake & Packing Settings", headerStyle);

        targetQuadrant = (Quadrant)EditorGUILayout.EnumPopup("Target Quadrant", targetQuadrant);
        precision = (TexturePrecision)EditorGUILayout.EnumPopup("Texture Precision", precision);
        alphaMode = (AlphaMode)EditorGUILayout.EnumPopup("Alpha Channel Encoding", alphaMode);
        mappingMode = (MappingMode)EditorGUILayout.EnumPopup("Vertex Mapping Mode", mappingMode);
        geometrySource = (GeometrySource)EditorGUILayout.EnumPopup("Geometry Source", geometrySource);
        saveFormat = (SaveFormat)EditorGUILayout.EnumPopup("Texture Save Format", saveFormat);

        EditorGUILayout.Space();

        // Auto-fill output paths if empty
        if (string.IsNullOrEmpty(outputMeshPath))
        {
            outputMeshPath = "Assets/VATBaker_Mesh.asset";
        }
        if (string.IsNullOrEmpty(outputTexturePath))
        {
            outputTexturePath = "Assets/VATBaker_Texture" + (saveFormat == SaveFormat.EXR ? ".exr" : ".asset");
        }

        // Handle path extensions depending on save format
        if (saveFormat == SaveFormat.EXR && !outputTexturePath.EndsWith(".exr", StringComparison.OrdinalIgnoreCase))
        {
            outputTexturePath = Path.ChangeExtension(outputTexturePath, ".exr");
        }
        else if (saveFormat == SaveFormat.Asset && !outputTexturePath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
        {
            outputTexturePath = Path.ChangeExtension(outputTexturePath, ".asset");
        }

        EditorGUILayout.BeginHorizontal();
        outputMeshPath = EditorGUILayout.TextField("Output Mesh Path", outputMeshPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string dir = string.IsNullOrEmpty(outputMeshPath) ? "Assets" : Path.GetDirectoryName(outputMeshPath);
            string file = string.IsNullOrEmpty(outputMeshPath) ? "VATBaker_Mesh" : Path.GetFileNameWithoutExtension(outputMeshPath);
            string path = EditorUtility.SaveFilePanelInProject("Save Mesh Asset", file, "asset", "Choose where to save the baked Mesh asset", dir);
            if (!string.IsNullOrEmpty(path))
            {
                outputMeshPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        outputTexturePath = EditorGUILayout.TextField("Output Texture Path", outputTexturePath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string dir = string.IsNullOrEmpty(outputTexturePath) ? "Assets" : Path.GetDirectoryName(outputTexturePath);
            string file = string.IsNullOrEmpty(outputTexturePath) ? "VATBaker_Texture" : Path.GetFileNameWithoutExtension(outputTexturePath);
            string ext = saveFormat == SaveFormat.EXR ? "exr" : "asset";
            string path = EditorUtility.SaveFilePanelInProject("Save Texture Asset", file, ext, "Choose where to save the baked Texture asset", dir);
            if (!string.IsNullOrEmpty(path))
            {
                outputTexturePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        targetTexture = (Texture2D)EditorGUILayout.ObjectField("Existing Texture (Optional)", targetTexture, typeof(Texture2D), false);

        EditorGUILayout.EndVertical();

        // --- SECTION 5: BAKE BUTTON ---
        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(0.1f, 0.7f, 1f);
        if (GUILayout.Button("Bake Vertex Animation Texture", GUILayout.Height(40)))
        {
            BakeVAT();
        }
        GUI.backgroundColor = Color.white;
    }

    private void AutoDetectComponents()
    {
        if (sourceGO == null)
        {
            detectedAnimators = new Animator[0];
            detectedSMRs = new SkinnedMeshRenderer[0];
            selectedAnimatorIndex = 0;
            selectedSMRIndex = 0;
            return;
        }

        detectedAnimators = sourceGO.GetComponentsInChildren<Animator>();
        detectedSMRs = sourceGO.GetComponentsInChildren<SkinnedMeshRenderer>();

        selectedAnimatorIndex = 0;
        selectedSMRIndex = 0;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Draw the wireframe preview of source and target meshes
        if (sourceGO != null && detectedSMRs.Length > selectedSMRIndex)
        {
            SkinnedMeshRenderer smr = detectedSMRs[selectedSMRIndex];
            if (smr != null && smr.sharedMesh != null)
            {
                // Source Skinned Mesh - Cyan wireframe
                DrawWireframeMesh(smr.sharedMesh, smr.transform.position, smr.transform.rotation, smr.transform.lossyScale, new Color(0f, 1f, 1f, 0.4f));
            }
        }

        if (targetGO != null)
        {
            MeshFilter mf = targetGO.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                // Target Static Mesh - Yellow wireframe
                DrawWireframeMesh(mf.sharedMesh, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale, new Color(1f, 0.92f, 0.016f, 0.4f));
            }
        }
    }

    private void DrawWireframeMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        if (mesh == null) return;

        if (wireframeMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                wireframeMat = new Material(shader);
            }
        }

        if (wireframeMat != null)
        {
            wireframeMat.SetColor("_Color", color);
            wireframeMat.SetPass(0);

            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);

            GL.PushMatrix();
            GL.MultMatrix(matrix);

            bool prevWireframe = GL.wireframe;
            GL.wireframe = true;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity, i);
            }

            GL.wireframe = prevWireframe;
            GL.PopMatrix();
        }
    }

    private void BakeVAT()
    {
        // Validation Checks
        if (sourceGO == null || targetGO == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign both Source and Target GameObjects.", "OK");
            return;
        }

        if (detectedSMRs.Length <= selectedSMRIndex)
        {
            EditorUtility.DisplayDialog("Error", "No valid SkinnedMeshRenderer found on the Source GameObject.", "OK");
            return;
        }

        SkinnedMeshRenderer smr = detectedSMRs[selectedSMRIndex];
        MeshFilter targetMF = targetGO.GetComponentInChildren<MeshFilter>();

        if (targetMF == null || targetMF.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("Error", "Target GameObject must have a MeshFilter with a valid Mesh.", "OK");
            return;
        }

        // Filter null animation clips
        List<AnimationClip> activeClips = new List<AnimationClip>();
        foreach (var clip in animationClips)
        {
            if (clip != null) activeClips.Add(clip);
        }

        if (activeClips.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please assign at least one non-null AnimationClip to bake.", "OK");
            return;
        }

        Mesh sourceMesh = smr.sharedMesh;
        Mesh targetMesh = targetMF.sharedMesh;

        int sourceCount = sourceMesh.vertexCount;
        int targetCount = targetMesh.vertexCount;

        if (mappingMode == MappingMode.IndexBased && sourceCount != targetCount)
        {
            EditorUtility.DisplayDialog("Error", $"Vertex counts do not match ({sourceCount} vs {targetCount}). Cannot use Index-Based mapping. Please choose Distance-Based or match topologies.", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("VAT Baker-WofstudioZ", "Mapping vertices...", 0.1f);

        // 1. Vertex Mapping
        Vector3[] sourceBindPoseVerts = sourceMesh.vertices;
        Vector3[] targetVerts = targetMesh.vertices;
        int[] vertexMap = new int[targetCount];

        if (mappingMode == MappingMode.IndexBased)
        {
            for (int i = 0; i < targetCount; i++)
            {
                vertexMap[i] = i;
            }
        }
        else
        {
            // Distance-based closest match
            for (int i = 0; i < targetCount; i++)
            {
                Vector3 targetPos = targetVerts[i];
                float minD = float.MaxValue;
                int bestIdx = 0;
                for (int j = 0; j < sourceCount; j++)
                {
                    float d = Vector3.Distance(targetPos, sourceBindPoseVerts[j]);
                    if (d < minD)
                    {
                        minD = d;
                        bestIdx = j;
                    }
                }
                vertexMap[i] = bestIdx;
            }
        }

        // 2. Setup Animation Stacking in 512x512 sub-section
        int totalFrames = 512;
        int framesPerClip = totalFrames / activeClips.Count;
        int remainingFrames = totalFrames - (framesPerClip * activeClips.Count);

        // Preallocate data
        Vector3[,] displacements = new Vector3[targetCount, totalFrames];
        float[,] alphas = new float[targetCount, totalFrames];

        // Save transform state of rigged components to avoid modifications in the editor
        var savedStates = SaveTransformStates(sourceGO);

        Mesh tempMesh = new Mesh();
        int frameIndex = 0;

        try
        {
            for (int clipIdx = 0; clipIdx < activeClips.Count; clipIdx++)
            {
                AnimationClip clip = activeClips[clipIdx];
                int clipFrames = framesPerClip + (clipIdx == activeClips.Count - 1 ? remainingFrames : 0);

                for (int f = 0; f < clipFrames; f++)
                {
                    float progress = (float)frameIndex / totalFrames;
                    EditorUtility.DisplayProgressBar("VAT Baker-WofstudioZ", $"Baking clip: {clip.name} (Frame {f}/{clipFrames})", 0.1f + 0.7f * progress);

                    // Compute normalized time
                    float time = 0f;
                    if (clipFrames > 1)
                    {
                        time = ((float)f / (clipFrames - 1)) * clip.length;
                    }

                    // Sample animation in editor
                    clip.SampleAnimation(sourceGO, time);

                    // Bake deformed skinned mesh in SMR local space
                    smr.BakeMesh(tempMesh);
                    Vector3[] bakedVerts = tempMesh.vertices;

                    // Write vertex position offsets
                    for (int targetIdx = 0; targetIdx < targetCount; targetIdx++)
                    {
                        int mappedSourceIdx = vertexMap[targetIdx];
                        Vector3 currentPos = bakedVerts[mappedSourceIdx];
                        
                        // Select base position based on geometry source reference
                        Vector3 basePos = geometrySource == GeometrySource.TargetMesh ? 
                            targetVerts[targetIdx] : 
                            sourceBindPoseVerts[mappedSourceIdx];

                        Vector3 disp = currentPos - basePos;
                        displacements[targetIdx, frameIndex] = disp;

                        if (alphaMode == AlphaMode.DisplacementMagnitude)
                        {
                            alphas[targetIdx, frameIndex] = disp.magnitude;
                        }
                        else
                        {
                            // Velocity scale (relative displacement difference from previous frame)
                            if (f == 0)
                            {
                                alphas[targetIdx, frameIndex] = 0f;
                            }
                            else
                            {
                                Vector3 prevDisp = displacements[targetIdx, frameIndex - 1];
                                alphas[targetIdx, frameIndex] = (disp - prevDisp).magnitude;
                            }
                        }
                    }

                    frameIndex++;
                }
            }
        }
        finally
        {
            // Restore original transforms
            RestoreTransformStates(savedStates);
            DestroyImmediate(tempMesh);
            EditorUtility.ClearProgressBar();
        }

        // Normalize Alpha Channel
        float maxAlpha = 0f;
        for (int i = 0; i < targetCount; i++)
        {
            for (int f = 0; f < totalFrames; f++)
            {
                if (alphas[i, f] > maxAlpha) maxAlpha = alphas[i, f];
            }
        }

        for (int i = 0; i < targetCount; i++)
        {
            for (int f = 0; f < totalFrames; f++)
            {
                alphas[i, f] = maxAlpha > 0.0001f ? alphas[i, f] / maxAlpha : 0f;
            }
        }

        // 3. Create or Load 1024x1024 Texture
        Texture2D tex = null;
        TextureFormat format = precision == TexturePrecision.Half ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat;

        if (targetTexture != null)
        {
            tex = targetTexture;
            string texPath = AssetDatabase.GetAssetPath(tex);
            if (!string.IsNullOrEmpty(texPath))
            {
                ConfigureTextureImportSettings(texPath);
            }
        }
        else if (File.Exists(outputTexturePath))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(outputTexturePath);
            if (tex != null)
            {
                ConfigureTextureImportSettings(outputTexturePath);
            }
        }

        // Check if we need to create a new texture or reformat
        if (tex == null)
        {
            tex = new Texture2D(1024, 1024, format, false, true);
            Color[] clearPixels = new Color[1024 * 1024];
            for (int k = 0; k < clearPixels.Length; k++)
            {
                clearPixels[k] = new Color(0, 0, 0, 0);
            }
            tex.SetPixels(clearPixels);
            tex.Apply();
        }
        else if (tex.width != 1024 || tex.height != 1024 || tex.format != format)
        {
            // Reformat while preserving existing pixels
            Texture2D newTex = new Texture2D(1024, 1024, format, false, true);
            try
            {
                Color[] oldPixels = tex.GetPixels();
                newTex.SetPixels(oldPixels);
            }
            catch (System.Exception)
            {
                // If it fails (e.g. read-only before reimport), fill with clear
                Color[] clearPixels = new Color[1024 * 1024];
                for (int k = 0; k < clearPixels.Length; k++)
                {
                    clearPixels[k] = new Color(0, 0, 0, 0);
                }
                newTex.SetPixels(clearPixels);
            }
            newTex.Apply();
            tex = newTex;
        }

        // Get pixels and write to the target quadrant
        Color[] pixels = tex.GetPixels();

        int offsetX = 0;
        int offsetY = 0;

        switch (targetQuadrant)
        {
            case Quadrant.TopLeft:
                offsetX = 0;
                offsetY = 512;
                break;
            case Quadrant.TopRight:
                offsetX = 512;
                offsetY = 512;
                break;
            case Quadrant.BottomLeft:
                offsetX = 0;
                offsetY = 0;
                break;
            case Quadrant.BottomRight:
                offsetX = 512;
                offsetY = 0;
                break;
        }

        // Write baked displacements into the 512x512 sub-section
        for (int localY = 0; localY < 512; localY++)
        {
            for (int localX = 0; localX < 512; localX++)
            {
                int globalX = offsetX + localX;
                int globalY = offsetY + localY;
                int pixelIndex = globalY * 1024 + globalX;

                if (localX < targetCount && localY < totalFrames)
                {
                    Vector3 disp = displacements[localX, localY];
                    float alphaVal = alphas[localX, localY];
                    pixels[pixelIndex] = new Color(disp.x, disp.y, disp.z, alphaVal);
                }
                else
                {
                    // Pad unused pixels in the quadrant to prevent bleeding
                    pixels[pixelIndex] = new Color(0, 0, 0, 0);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // Save texture based on format
        if (saveFormat == SaveFormat.EXR)
        {
            Texture2D.EXRFlags exrFlags = precision == TexturePrecision.Half ? Texture2D.EXRFlags.None : Texture2D.EXRFlags.OutputAsFloat;
            byte[] bytes = tex.EncodeToEXR(exrFlags);
            File.WriteAllBytes(outputTexturePath, bytes);
            AssetDatabase.ImportAsset(outputTexturePath);
            ConfigureTextureImportSettings(outputTexturePath);
        }
        else
        {
            if (AssetDatabase.Contains(tex))
            {
                EditorUtility.SetDirty(tex);
            }
            else
            {
                AssetDatabase.CreateAsset(tex, outputTexturePath);
            }
            AssetDatabase.SaveAssets();
        }

        // 4. Generate Target Mesh asset with lighting and normal copying
        Mesh newMesh = new Mesh();
        newMesh.name = Path.GetFileNameWithoutExtension(outputMeshPath);
        
        // Select the reference mesh based on geometrySource setting (preserves Target Mesh custom normals/UVs)
        Mesh refMesh = geometrySource == GeometrySource.TargetMesh ? targetMesh : sourceMesh;

        // Copy original geometry and lighting channels directly from reference geometry
        newMesh.vertices = refMesh.vertices;
        newMesh.normals = refMesh.normals;
        newMesh.tangents = refMesh.tangents;
        newMesh.uv = refMesh.uv;
        
        newMesh.subMeshCount = refMesh.subMeshCount;
        for (int s = 0; s < refMesh.subMeshCount; s++)
        {
            newMesh.SetTriangles(refMesh.GetTriangles(s), s);
        }

        // Generate unique UV2 channel
        Vector2[] uv2 = new Vector2[targetCount];
        for (int i = 0; i < targetCount; i++)
        {
            float u = (float)i / (targetCount > 1 ? targetCount : 1);
            float v = 0f; // Baseline normalized frame coordinate
            uv2[i] = new Vector2(u, v);
        }
        newMesh.uv2 = uv2;

        // Save/Update the mesh asset
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(outputMeshPath);
        if (existingMesh != null)
        {
            existingMesh.Clear();
            existingMesh.name = newMesh.name;
            existingMesh.vertices = newMesh.vertices;
            existingMesh.normals = newMesh.normals;
            existingMesh.tangents = newMesh.tangents;
            existingMesh.uv = newMesh.uv;
            existingMesh.uv2 = newMesh.uv2;
            existingMesh.triangles = newMesh.triangles;
            existingMesh.subMeshCount = newMesh.subMeshCount;
            for (int s = 0; s < newMesh.subMeshCount; s++)
            {
                existingMesh.SetTriangles(newMesh.GetTriangles(s), s);
            }
            EditorUtility.SetDirty(existingMesh);
            AssetDatabase.SaveAssets();
            newMesh = existingMesh;
        }
        else
        {
            AssetDatabase.CreateAsset(newMesh, outputMeshPath);
            AssetDatabase.SaveAssets();
        }

        // Force a reimport to ensure everything is synced up
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("VAT Baker-WofstudioZ", $"VAT Baked Successfully!\n\nMesh Saved to: {outputMeshPath}\nTexture Saved to: {outputTexturePath}", "OK");
    }

    private void ConfigureTextureImportSettings(string path)
    {
        AssetDatabase.Refresh();
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = false; // Important: Treat as linear data, not color
            
            // Force floating point format on Default platform
            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            defaultSettings.overridden = true;
            defaultSettings.format = precision == TexturePrecision.Half ? TextureImporterFormat.RGBAHalf : TextureImporterFormat.RGBAFloat;
            defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(defaultSettings);

            // Force floating point format on Standalone platform
            TextureImporterPlatformSettings standaloneSettings = importer.GetPlatformTextureSettings("Standalone");
            standaloneSettings.overridden = true;
            standaloneSettings.format = precision == TexturePrecision.Half ? TextureImporterFormat.RGBAHalf : TextureImporterFormat.RGBAFloat;
            standaloneSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(standaloneSettings);
            
            importer.SaveAndReimport();
        }
    }

    private struct TransformState
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    private List<TransformState> SaveTransformStates(GameObject root)
    {
        List<TransformState> states = new List<TransformState>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            states.Add(new TransformState
            {
                transform = t,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale
            });
        }
        return states;
    }

    private void RestoreTransformStates(List<TransformState> states)
    {
        foreach (var state in states)
        {
            if (state.transform != null)
            {
                state.transform.localPosition = state.localPosition;
                state.transform.localRotation = state.localRotation;
                state.transform.localScale = state.localScale;
            }
        }
    }
}
