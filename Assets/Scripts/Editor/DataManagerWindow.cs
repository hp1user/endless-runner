using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DataManagerWindow : EditorWindow
{
    private ListView _itemListView;
    private VisualElement _inspectorContainer;
    private TextField _newItemNameField;
    private ToolbarSearchField _searchField;
    
    // Preview Elements
    private VisualElement _previewBackground;
    private VisualElement _previewIcon;
    private Label _previewTitle;
    private Label _previewDesc;
    private Label _previewValue; // The +20% label
    
    // Quick Sprite Previews
    private VisualElement _quickIcon;
    private VisualElement _quickBg;
    private VisualElement _quickSel;

    private List<UpgradeCard> _upgradeCards = new List<UpgradeCard>();
    private List<UpgradeCard> _filteredCards = new List<UpgradeCard>();
    private List<ScriptableObject> _items = new List<ScriptableObject>();
    private List<ScriptableObject> _filteredItems = new List<ScriptableObject>();
    private ScriptableObject _selectedItem;

    private enum DataType { UpgradeCard }
    private DataType _currentDataType = DataType.UpgradeCard;

    [MenuItem("Tools/Endless Runner/Data Manager")]
    public static void ShowExample()
    {
        DataManagerWindow wnd = GetWindow<DataManagerWindow>();
        wnd.titleContent = new GUIContent("Data Manager");
    }

    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/UI/DataManagerWindow.uxml");
        if (visualTree == null) return;
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/UI/DataManagerStyle.uss");
        visualTree.CloneTree(rootVisualElement);
        if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);

        // References
        _itemListView = rootVisualElement.Q<ListView>("itemListView");
        _inspectorContainer = rootVisualElement.Q<VisualElement>("inspectorContainer");
        _newItemNameField = rootVisualElement.Q<TextField>("newItemNameField");
        _searchField = rootVisualElement.Q<ToolbarSearchField>("searchField");
        
        _previewBackground = rootVisualElement.Q<VisualElement>("previewBackground");
        _previewIcon = rootVisualElement.Q<VisualElement>("previewIcon");
        _previewTitle = rootVisualElement.Q<Label>("previewTitle");
        _previewDesc = rootVisualElement.Q<Label>("previewDesc");
        _previewValue = rootVisualElement.Q<Label>("previewValue");
        
        _quickIcon = rootVisualElement.Q<VisualElement>("quickIcon");
        _quickBg = rootVisualElement.Q<VisualElement>("quickBg");
        _quickSel = rootVisualElement.Q<VisualElement>("quickSel");

        var createBtn = rootVisualElement.Q<Button>("createNewButton");
        var duplicateBtn = rootVisualElement.Q<Button>("duplicateButton");
        var refreshBtn = rootVisualElement.Q<ToolbarButton>("refreshButton");
        var settingsBtn = rootVisualElement.Q<ToolbarButton>("settingsButton");
        var dataTypeMenu = rootVisualElement.Q<ToolbarMenu>("dataTypeMenu");

        // Callbacks
        createBtn.clicked += CreateNewItem;
        duplicateBtn.clicked += DuplicateSelectedItem;
        refreshBtn.clicked += RefreshList;
        settingsBtn.clicked += OpenSettings;
        
        _searchField.RegisterValueChangedCallback(evt => FilterList(evt.newValue));
        
        dataTypeMenu.menu.AppendAction("Upgrade Card", (a) => SetDataType(DataType.UpgradeCard), (a) => _currentDataType == DataType.UpgradeCard ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        
        _itemListView.makeItem = () => 
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            
            var label = new Label();
            label.name = "itemLabel";
            label.AddToClassList("list-item");
            label.style.flexGrow = 1;
            
            var btnContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            
            var renameBtn = new Button { name = "renameBtn", text = "✎", style = { width = 25, height = 20, paddingLeft = 2, paddingRight = 2 } };
            var deleteBtn = new Button { name = "deleteBtn", text = "✖", style = { width = 25, height = 20, paddingLeft = 2, paddingRight = 2, backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f)) } };
            
            btnContainer.Add(renameBtn);
            btnContainer.Add(deleteBtn);
            
            container.Add(label);
            container.Add(btnContainer);
            
            return container;
        };

        _itemListView.bindItem = (element, i) =>
        {
            if (i < _filteredItems.Count && _filteredItems[i] != null)
            {
                var item = _filteredItems[i];
                var label = element.Q<Label>("itemLabel");
                label.text = item.name;
                label.style.display = DisplayStyle.Flex; // ensure visible
                
                var renameBtn = element.Q<Button>("renameBtn");
                var deleteBtn = element.Q<Button>("deleteBtn");
                
                renameBtn.clickable = new Clickable(() => 
                {
                    // Inline Rename Logic
                    var textField = new TextField { value = item.name, style = { flexGrow = 1, marginRight = 5 } };
                    element.Insert(0, textField);
                    label.style.display = DisplayStyle.None;
                    
                    textField.Focus();
                    textField.SelectAll();
                    
                    void ApplyRename()
                    {
                        if (element.Contains(textField))
                        {
                            RenameItem(item, textField.value);
                            label.style.display = DisplayStyle.Flex;
                            element.Remove(textField);
                        }
                    }
                    
                    textField.RegisterCallback<FocusOutEvent>(evt => ApplyRename());
                    textField.RegisterCallback<KeyDownEvent>(evt => 
                    {
                        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) 
                            ApplyRename();
                        else if (evt.keyCode == KeyCode.Escape)
                        {
                            label.style.display = DisplayStyle.Flex;
                            element.Remove(textField);
                        }
                    });
                });
                
                deleteBtn.clickable = new Clickable(() => DeleteItem(item));
            }
        };
        _itemListView.selectionChanged += OnItemSelected;

        // Auto Refresh Preview on Inspector Change
        Undo.undoRedoPerformed += UpdatePreview;
        
        // Dynamically scale the fixed 980x460 card to fit the available space perfectly
        var previewContainer = rootVisualElement.Q<VisualElement>(className: "preview-container");
        if (previewContainer != null)
        {
            previewContainer.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float availableWidth = evt.newRect.width;
                float availableHeight = evt.newRect.height;
                
                float scaleX = availableWidth / 980f;
                float scaleY = availableHeight / 460f;
                float scale = Mathf.Min(scaleX, scaleY) * 0.95f; // 95% to leave a tiny margin
                
                _previewBackground.transform.scale = new Vector3(scale, scale, 1f);
            });
        }
        
        RefreshList();
    }
    
    private void OnDestroy()
    {
        Undo.undoRedoPerformed -= UpdatePreview;
    }

    private void SetDataType(DataType newType)
    {
        _currentDataType = newType;
        RefreshList();
    }

    private void OpenSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:CardRarityDatabase");
        CardRarityDatabase db = null;
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            db = AssetDatabase.LoadAssetAtPath<CardRarityDatabase>(path);
        }
        else
        {
            string dir = "Assets/ScriptableObjects/Settings";
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            db = ScriptableObject.CreateInstance<CardRarityDatabase>();
            AssetDatabase.CreateAsset(db, dir + "/CardRarityDatabase.asset");
            AssetDatabase.SaveAssets();
        }
        
        _selectedItem = db;
        _itemListView.ClearSelection();
        
        _inspectorContainer.Clear();
        var serializedObject = new SerializedObject(db);
        var inspectorElement = new InspectorElement(serializedObject);
        inspectorElement.Bind(serializedObject);
        _inspectorContainer.Add(inspectorElement);
        
        ClearPreview();
    }

    private void RenameItem(ScriptableObject item, string newName)
    {
        if (item == null || string.IsNullOrEmpty(newName) || item.name == newName) return;

        string path = AssetDatabase.GetAssetPath(item);
        AssetDatabase.RenameAsset(path, newName);
        AssetDatabase.SaveAssets();
        RefreshList();
        
        // Re-select
        int index = _filteredItems.IndexOf(item);
        if (index >= 0)
        {
            _itemListView.SetSelection(index);
            _itemListView.ScrollToItem(index);
        }
    }

    private void DeleteItem(ScriptableObject item)
    {
        if (item == null) return;
        
        if (EditorUtility.DisplayDialog("Delete Asset", $"Are you sure you want to delete {item.name}?", "Delete", "Cancel"))
        {
            string path = AssetDatabase.GetAssetPath(item);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            
            if (_selectedItem == item)
            {
                _selectedItem = null;
                _inspectorContainer.Clear();
                ClearPreview();
            }
            
            RefreshList();
        }
    }

    private void RefreshList()
    {
        _items.Clear();
        string[] guids = AssetDatabase.FindAssets("t:UpgradeCard");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (item != null) _items.Add(item);
        }
        
        FilterList(_searchField.value);
    }

    private void FilterList(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            _filteredItems = new List<ScriptableObject>(_items);
        }
        else
        {
            _filteredItems = _items.Where(c => c.name.ToLower().Contains(query.ToLower())).ToList();
        }

        _itemListView.itemsSource = _filteredItems;
        _itemListView.Rebuild();
        
        if (_filteredItems.Count > 0)
        {
            _itemListView.SetSelection(0);
        }
        else
        {
            _inspectorContainer.Clear();
            ClearPreview();
        }
    }

    private void OnItemSelected(IEnumerable<object> selection)
    {
        _inspectorContainer.Clear();
        foreach (var obj in selection)
        {
            if (obj is ScriptableObject item)
            {
                _selectedItem = item;
                
                var serializedObject = new SerializedObject(item);
                var inspectorElement = new InspectorElement(serializedObject);
                inspectorElement.Bind(serializedObject);
                
                // Track all changes to the serialized object
                inspectorElement.TrackSerializedObjectValue(serializedObject, so => UpdatePreview());
                
                _inspectorContainer.Add(inspectorElement);
                UpdatePreview();
            }
        }
    }

    private void ClearPreview()
    {
        _previewTitle.text = "";
        _previewDesc.text = "";
        _previewValue.text = "";
        _previewIcon.style.backgroundImage = null;
        _previewBackground.style.backgroundImage = null;
        _previewBackground.style.backgroundColor = new StyleColor(Color.clear);
        _quickIcon.style.backgroundImage = null;
        _quickBg.style.backgroundImage = null;
        _quickSel.style.backgroundImage = null;
        
        var previewContainer = rootVisualElement.Q<VisualElement>(className: "preview-container");
        if (previewContainer != null) previewContainer.style.display = DisplayStyle.None;
    }

    private void UpdatePreview()
    {
        var previewContainer = rootVisualElement.Q<VisualElement>(className: "preview-container");
        
        if (_selectedItem == null || !(_selectedItem is UpgradeCard))
        {
            ClearPreview();
            return;
        }

        if (previewContainer != null) previewContainer.style.display = DisplayStyle.Flex;

        UpgradeCard _selectedCard = _selectedItem as UpgradeCard;
        
        _previewTitle.text = _selectedCard.cardName;
        _previewDesc.text = _selectedCard.description;
        
        _previewValue.text = "";
        if (_selectedCard.effects != null && _selectedCard.effects.Count > 0)
        {
            var firstFx = _selectedCard.effects[0];
            string suffix = firstFx.upgradeType switch
            {
                UpgradeType.DamageBoost => "%",
                UpgradeType.MaxHealth => " HP",
                UpgradeType.SpeedBoost => " SPD",
                _ => ""
            };
            _previewValue.text = $"+{firstFx.upgradeValue}{suffix}";
        }

        // Main preview sprites
        _previewIcon.style.backgroundImage = _selectedCard.cardIcon != null ? new StyleBackground(_selectedCard.cardIcon) : null;
        
        if (_selectedCard.rarityBackgroundImage != null)
        {
            _previewBackground.style.backgroundImage = new StyleBackground(_selectedCard.rarityBackgroundImage);
            _previewBackground.style.backgroundColor = new StyleColor(Color.clear);
        }
        else
        {
            _previewBackground.style.backgroundImage = null;
            _previewBackground.style.backgroundColor = new StyleColor(_selectedCard.rarityColor);
        }
        
        // Quick Previews
        _quickIcon.style.backgroundImage = _selectedCard.cardIcon != null ? new StyleBackground(_selectedCard.cardIcon) : null;
        _quickBg.style.backgroundImage = _selectedCard.rarityBackgroundImage != null ? new StyleBackground(_selectedCard.rarityBackgroundImage) : null;
        _quickSel.style.backgroundImage = _selectedCard.selectedCardSprite != null ? new StyleBackground(_selectedCard.selectedCardSprite) : null;
    }

    private void CreateNewItem()
    {
        string path = "Assets/ScriptableObjects/Cards";
        if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

        string assetName = string.IsNullOrEmpty(_newItemNameField.value) ? "New Upgrade Card" : _newItemNameField.value;
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{assetName}.asset");

        UpgradeCard newItem = ScriptableObject.CreateInstance<UpgradeCard>();
        newItem.cardName = assetName;

        AssetDatabase.CreateAsset(newItem, fullPath);
        AssetDatabase.SaveAssets();
        
        RefreshList();
        
        int index = _filteredItems.IndexOf(newItem);
        if (index >= 0)
        {
            _itemListView.SetSelection(index);
            _itemListView.ScrollToItem(index);
        }
    }

    private void DuplicateSelectedItem()
    {
        if (_selectedItem == null) return;
        
        string path = AssetDatabase.GetAssetPath(_selectedItem);
        string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
        
        if (AssetDatabase.CopyAsset(path, newPath))
        {
            AssetDatabase.SaveAssets();
            RefreshList();
            
            var newItem = AssetDatabase.LoadAssetAtPath<ScriptableObject>(newPath);
            int index = _filteredItems.IndexOf(newItem);
            if (index >= 0)
            {
                _itemListView.SetSelection(index);
                _itemListView.ScrollToItem(index);
            }
        }
    }
}
