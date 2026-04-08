# 🛡️ Animation Event Tool - Technical Documentation

This tool is a **non-destructive** alternative to Unity's standard Animation Events. It allows you to store event markers in a central **ScriptableObject Library** instead of inside `.fbx` or `.anim` files, ensuring your data is never lost during re-imports.

---

## 🏗️ Core Components

### 1. `AnimationEventLibrary.cs` (The Database)
A `ScriptableObject` that acts as the container for all your events. 
- **Events**: A list of named triggers (e.g., "LeftFoot", "Land").
- **Clips**: Each event contains a list of animation clips where it should fire.
- **Markers**: Normalized time values (0.0 to 1.0) where the event triggers.
- **Function Name**: The string name of the function to call on your character (e.g., `OnFootstep`).

### 2. `AnimationEventWindow.cs` (The Editor)
A custom window (**Window > Animation > Event Tool**) for managing your markers.
- Supports **Drag & Drop** of animation clips.
- **Visual Scrubbing**: Slide markers left/right to time them.
- **Live Preview**: Select a character in the scene to see the animation pose update in real-time as you scroll.
- **Debugger**: Toggle "Debug Mode" to see logs during play.

### 3. `AnimationEventTrigger.cs` (The Runtime)
Attach this component to any character with an `Animator`.
- It monitors the currently playing animation.
- It detects when the animator's time crosses a marker point.
- **Compatibility**: It uses `SendMessage` to trigger functions, passing a standard `UnityEngine.AnimationEvent`.

---

## 🧠 Key Logic (The "Magic")

### Normalized Timing
Markers are stored as **0.0 to 1.0** (normalized time). 
- **Benefit**: If you change the animation's length or speed later, the markers stay relative to the character's pose.
- **Math**: `Mathf.Repeat(time, 1f)` is used to handle looping and animations playing in reverse.

### Weight Support
Because manual events don't have automatic `animatorClipInfo.weight` values from Unity:
1.  The `AnimationEventTrigger` reads the current animator weight.
2.  It passes this weight into the **`animationEvent.floatParameter`**.
3.  **Character Script Logic**: Your script should use `float weight = animationEvent.floatParameter;` if it needs to check for low-weight transitions.

### Ghost Event Prevention
By cleaning the FBX `.meta` files of internal events and using this tool, you prevent "double-firing" errors common in standard Unity development.

---

## 🚀 Setup Instructions (New Project/PC)

1.  **Copy Scripts**: Move the `Animation` script folder (Library, Trigger, and Editor window) to the new project.
2.  **Create Library**: Right-click in Project view ➡ `Create > Animation > Event Library`.
3.  **Setup Character**:
    - Attach the `AnimationEventTrigger` component to your root character.
    - Drag your new Library asset into the `Library` field on the component.
4.  **Define Events**:
    - Open `Window > Animation > Event Tool`.
    - Create a new Event (e.g., "Footstep").
    - Drag your Walk animation into the clip list.
    - Add markers and set the **Function Name** to `OnFootstep`.
5.  **Code Check**: Ensure your character script (e.g., `CharacterManager.cs`) has a function matching the name:
    ```csharp
    private void OnFootstep(AnimationEvent animationEvent) { ... }
    ```

---

## 🛠️ Troubleshooting
- **No Sound?** Check if `library.debugMode` is enabled. If you see the "Calling OnFootstep" log but hear nothing, check your ground detection or audio clips.
- **Marker Missed?** Ensure the `AnimationEventTrigger` is on the same GameObject as the `Animator`.
- **Ghosting?** Check the FBX Inspector ➡ Animation tab ➡ Events. Click **Apply** to clear baked events.

---
**Documented by Antigravity**
