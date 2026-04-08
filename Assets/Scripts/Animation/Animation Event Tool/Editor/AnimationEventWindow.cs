using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AnimationTools;

namespace AnimationTools.Editor
{
    public class AnimationEventWindow : EditorWindow
    {
        private AnimationEventLibrary _library;
        private Vector2 _eventScrollPos;
        private Vector2 _detailScrollPos;
        private int _selectedEventIndex = -1;
        private string _newEventName = "";
        private GUIStyle _selectedStyle;
        private GameObject _previewTarget;
        private bool _livePreview;
        private int _dragSourceIndex = -1;
        private bool _isDragging = false;

        [MenuItem("Window/Animation/Event Tool")]
        public static void ShowWindow()
        {
            GetWindow<AnimationEventWindow>("Animation Events");
        }

        private void OnEnable()
        {
            // Auto-load if one exists in the project
            string[] guids = AssetDatabase.FindAssets("t:AnimationEventLibrary");
            if (guids.Length > 0)
            {
                _library = AssetDatabase.LoadAssetAtPath<AnimationEventLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        private void OnGUI()
        {
            _selectedStyle ??= new GUIStyle(GUI.skin.button) { normal = { textColor = Color.yellow } };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _library = (AnimationEventLibrary)EditorGUILayout.ObjectField("Library Asset", _library, typeof(AnimationEventLibrary), false);
            if (GUILayout.Button(EditorGUIUtility.IconContent("SaveAs"), GUILayout.Width(35), GUILayout.Height(20)))
            {
                if (_library != null)
                {
                    EditorUtility.SetDirty(_library);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[AnimationEventTool] Library saved successfully.");
                }
            }
            if (GUILayout.Button("New", GUILayout.Width(50))) CreateNewLibrary();
            
            GUILayout.Space(20);
            GUI.color = _library.debugMode ? Color.yellow : Color.white;
            _library.debugMode = GUILayout.Toggle(_library.debugMode, "Debug Mode", "Button", GUILayout.Width(100));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (_library == null)
            {
                EditorGUILayout.HelpBox("Please select or create an AnimationEventLibrary asset to begin.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            
            // SIDE BAR: Event List
            DrawEventList();

            // MAIN AREA: Event Details
            DrawEventDetails();

            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_library);
            }
        }

        private void DrawEventList()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250), GUILayout.ExpandHeight(true));
            GUILayout.Label("Event Types", EditorStyles.boldLabel);

            _eventScrollPos = EditorGUILayout.BeginScrollView(_eventScrollPos);
            for (int i = 0; i < _library.events.Count; i++)
            {
                bool isSelected = _selectedEventIndex == i;
                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                if (GUILayout.Button(_library.events[i].eventName))
                {
                    _selectedEventIndex = i;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _newEventName = EditorGUILayout.TextField(_newEventName);
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                if (!string.IsNullOrEmpty(_newEventName))
                {
                    _library.events.Add(new AnimationEventDefinition { eventName = _newEventName });
                    _newEventName = "";
                    _selectedEventIndex = _library.events.Count - 1;
                    EditorUtility.SetDirty(_library);
                    AssetDatabase.SaveAssets();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_selectedEventIndex >= 0 && _selectedEventIndex < _library.events.Count)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Delete Selected Event"))
                {
                    if (EditorUtility.DisplayDialog("Delete Event", $"Are you sure you want to delete '{_library.events[_selectedEventIndex].eventName}'?", "Yes", "No"))
                    {
                        _library.events.RemoveAt(_selectedEventIndex);
                        _selectedEventIndex = -1;
                        EditorUtility.SetDirty(_library);
                        AssetDatabase.SaveAssets();
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEventDetails()
        {
            if (_selectedEventIndex < 0 || _selectedEventIndex >= _library.events.Count)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.Label("Select an event type from the left to edit markers.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            var evt = _library.events[_selectedEventIndex];
            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos, GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            GUILayout.Label($"Editing Category: {evt.eventName}", EditorStyles.largeLabel);
            EditorGUILayout.BeginHorizontal();
            evt.eventName = EditorGUILayout.TextField("Event Name", evt.eventName);
            evt.functionName = EditorGUILayout.TextField("Function Name", evt.functionName);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("If Function Name is set, the trigger will call SendMessage(function, eventName) on the character.", MessageType.None);

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("LIVE PREVIEW SETTINGS", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _previewTarget = (GameObject)EditorGUILayout.ObjectField("Preview Target", _previewTarget, typeof(GameObject), true);
            _livePreview = EditorGUILayout.Toggle("Live Preview", _livePreview);
            EditorGUILayout.EndHorizontal();
            if (_livePreview && _previewTarget == null)
            {
                EditorGUILayout.HelpBox("Assign a Scene GameObject to 'Preview Target' to see animations while scrubbing.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Assigned Clips", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Expand All", EditorStyles.miniButtonLeft))
            {
                foreach (var cd in evt.clipData) cd.isExpanded = true;
            }
            if (GUILayout.Button("Collapse All", EditorStyles.miniButtonRight))
            {
                foreach (var cd in evt.clipData) cd.isExpanded = false;
            }
            EditorGUILayout.EndHorizontal();

            // Drag & Drop Area
            Rect dropRect = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "DRAG & DROP ANIMATION CLIPS HERE", EditorStyles.helpBox);
            HandleDragDrop(dropRect, evt);

            EditorGUILayout.Space(5);

            for (int i = 0; i < evt.clipData.Count; i++)
            {
                DrawClipRow(evt.clipData[i], evt, i);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawClipRow(ClipMarkerData cd, AnimationEventDefinition evt, int index)
        {
            Rect rowRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // Reorder Handle
            Rect handleRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            GUI.Label(handleRect, EditorGUIUtility.IconContent("VerticalLayoutGroup Icon"));
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);
            
            // Handle Dragging
            Event e = Event.current;
            if (e.type == EventType.MouseDown && handleRect.Contains(e.mousePosition))
            {
                _dragSourceIndex = index;
                _isDragging = true;
                e.Use();
            }

            if (_isDragging && _dragSourceIndex == index)
            {
                GUI.color = Color.cyan;
            }

            // More robust foldout
            cd.isExpanded = EditorGUILayout.Foldout(cd.isExpanded, cd.clip != null ? cd.clip.name : "Missing Clip", true);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("REMOVE CLIP", GUILayout.Width(100)))
            {
                evt.clipData.Remove(cd);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            // Detect drop/swap during drag
            if (_isDragging && _dragSourceIndex != index && rowRect.Contains(e.mousePosition))
            {
                MoveClip(evt, _dragSourceIndex, index);
                _dragSourceIndex = index;
                e.Use();
            }
            
            if (e.type == EventType.MouseUp)
            {
                _isDragging = false;
                _dragSourceIndex = -1;
            }

            if (cd.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(2);
                
                cd.clip = (AnimationClip)EditorGUILayout.ObjectField("Reference", cd.clip, typeof(AnimationClip), false);

                if (cd.clip != null)
                {
                    EditorGUILayout.LabelField("Markers (Normalized Time 0-1)");
                    
                    // Visual Timeline Bar
                    Rect timelineRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                    GUI.Box(timelineRect, "", EditorStyles.textArea);
                    
                    // Draw Markers on Bar
                    foreach (var marker in cd.markers)
                    {
                        float x = timelineRect.x + (marker * timelineRect.width);
                        GUI.Box(new Rect(x - 2, timelineRect.y, 4, timelineRect.height), "", _selectedStyle);
                    }

                for (int j = 0; j < cd.markers.Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    cd.markers[j] = EditorGUILayout.Slider($"Marker {j+1}", cd.markers[j], 0f, 1f);
                    if (EditorGUI.EndChangeCheck() && _livePreview && _previewTarget != null)
                    {
                        // Sample the animation for this marker instantly
                        cd.clip.SampleAnimation(_previewTarget, cd.markers[j] * cd.clip.length);
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        cd.markers.RemoveAt(j);
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Marker", GUILayout.Width(100)))
                {
                    cd.markers.Add(0.5f);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndVertical();
    }

        private void HandleDragDrop(Rect rect, AnimationEventDefinition evt)
        {
            Event e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    e.Use();
                }
                else if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged is AnimationClip clip)
                        {
                            if (!evt.clipData.Exists(x => x.clip == clip))
                            {
                                evt.clipData.Add(new ClipMarkerData { clip = clip });
                            }
                        }
                    }
                    e.Use();
                }
            }
        }

        private void CreateNewLibrary()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Event Library", "AnimationEventLibrary", "asset", "Save Animation Event Library Asset");
            if (!string.IsNullOrEmpty(path))
            {
                AnimationEventLibrary lib = ScriptableObject.CreateInstance<AnimationEventLibrary>();
                AssetDatabase.CreateAsset(lib, path);
                AssetDatabase.SaveAssets();
                _library = lib;
            }
        }

        private void MoveClip(AnimationEventDefinition evt, int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= evt.clipData.Count || toIndex >= evt.clipData.Count) return;

            var item = evt.clipData[fromIndex];
            evt.clipData.RemoveAt(fromIndex);
            evt.clipData.Insert(toIndex, item);
            EditorUtility.SetDirty(_library);
        }
    }
}
