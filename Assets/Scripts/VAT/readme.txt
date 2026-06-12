==========================================================================
                      VAT BAKER-WOFSTUDIOZ - DOCS & USECASE
==========================================================================

--------------------------------------------------------------------------
1. WHAT IS VERTEX ANIMATION TEXTURING (VAT)?
--------------------------------------------------------------------------
Vertex Animation Texturing is a powerful game optimization technique that 
converts rigged skeletal mesh animations (which rely on heavy CPU skinning, 
bone hierarchies, and animators) into static meshes animated entirely on the 
GPU using a texture sheet.

Instead of computing vertex deformation on the CPU, the vertex position 
offsets (displacements) and velocity values are pre-calculated for each 
frame and stored as pixel colors in a high-precision floating-point 
texture (EXR). A custom vertex shader then reads this texture at runtime and 
displaces the vertices of a static mesh.

USE CASE:
Perfect for rendering massive crowds, swarms, or secondary animated environmental 
assets (like thousands of bug enemies, fish swarms, or falling leaves) at 
extreme performance. It removes Animator and SkinnedMeshRenderer CPU cost, 
rendering all instances in a single draw call via GPU Instancing.

--------------------------------------------------------------------------
2. FOLDER STRUCTURE
--------------------------------------------------------------------------
* Assets/Scripts/VAT/
  ├── Editor/
  │   └── VATBakerWindow.cs        (The editor baking window interface)
  ├── VATInstanceController.cs     (Runtime per-instance property overrides)
  └── readme.txt                   (This documentation file)

* Assets/Shaders/
  └── VATDecoder.hlsl              (The custom HLSL function library)

--------------------------------------------------------------------------
3. HOW TO BAKE ANIMATIONS
--------------------------------------------------------------------------
1. Open the baker window: Window -> VAT Baker-WofstudioZ.
2. Setup parameters:
   * Source Rigged GO: Drag your rigged bug character prefab (it must contain 
     bone hierarchy and a SkinnedMeshRenderer).
   * Target Static GO: Drag your static bug mesh prefab (with no bones/rig, 
     but matching topology).
   * Animations to Bake: Set count and add your Animation Clips (e.g. Walk, Death).
   * Target Quadrant: Choose which corner to write into (e.g. Top Left for Bug 1).
   * Geometry Source: 
     - Set to 'TargetMesh' to preserve your static mesh's custom split normals, 
       authored tangents, and UVs.
     - Set to 'SourceMesh' to clone coordinates directly from the rigged pose.
   * Vertex Mapping Mode: Set to 'Distance Based' (handles UV cuts and reordered 
     indices safely).
   * Texture Save Format: Set to 'EXR'.
3. Set your output paths for the new Mesh.asset and Texture.exr.
4. Click "Bake Vertex Animation Texture".

HOW TO PACK 4 BUGS INTO 1 TEXTURE:
1. Bake Bug 1 into the "Top Left" quadrant.
2. For Bug 2, set its source/target, drag the same texture sheet into the 
   "Existing Texture (Optional)" slot, change the quadrant to "Top Right", 
   and click Bake.
3. Repeat for Bug 3 (Bottom Left) and Bug 4 (Bottom Right).
The baker will preserve existing quadrants and write only to the selected corner.

--------------------------------------------------------------------------
4. SHADER GRAPH CONFIGURATION
--------------------------------------------------------------------------
Create a Shader Graph (URP/HDRP) for your material using:
1. Blackboard Properties:
   * VAT Map (Texture2D)
   * Albedo Map (Texture2D)
   * Skin Tiling Offset (Vector4)
   * Quadrant Selector (Float)
   * Animation Speed (Float)
   * Animation Index (Float)
   * Total Animations (Float)
   * Total Vertices (Float)
2. Custom Function Node:
   * Type: File
   * Path: Assets/Shaders/VATDecoder.hlsl
   * Name: DecodeVAT
   * Inputs: VAT_Texture (Texture2D), Sampler_State (SamplerState), UV2 (Vector2), 
     TotalVertices (Float), CurrentFrame (Float), QuadrantID (Float), AnimID (Float), 
     TotalAnims (Float)
   * Outputs: PositionOffset (Vector3), VertexVelocity (Float)
3. Connections:
   * Create a UV node (set to UV1) -> connect to Custom Function's UV2 input.
   * Create a Time node -> multiply by Animation Speed -> connect to CurrentFrame.
   * Create a Position node (set to Object space) -> Add the output PositionOffset -> 
     connect the result to the Master Stack's Vertex Position block.
   * Drag your VAT Map and create a Sampler State node (Point Filter, Clamp Wrap) -> 
     connect both to the Custom Function inputs.

--------------------------------------------------------------------------
5. RUNTIME PER-INSTANCE CONTROLLER (MaterialPropertyBlock)
--------------------------------------------------------------------------
To render thousands of bugs using the same material (1 draw call) but with 
different quadrants, animations, and speed offsets, attach the 
VATInstanceController.cs script to each bug GameObject in the game.

The script uses MaterialPropertyBlocks to pass values like Quadrant Index (0-3), 
Animation Index, and Speed directly to the GPU shader per-instance:
* Bug 1: Quadrant = 0, Animation = 0 (Plays Bug 1's Walk cycle)
* Bug 2: Quadrant = 1, Animation = 1 (Plays Bug 2's Death cycle)

This ensures all instances are batched together by URP's GPU Instancing/SRP Batcher 
for optimal performance!
==========================================================================
