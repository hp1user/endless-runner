using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EndlessRunner.Audio;
using UI.DamageNumbers;
using TMPro;

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
    private VisualElement _spriteAssignmentsSection;
    
    private Label _listHeaderLabel;
    private Label _previewHeaderLabel;

    private List<UpgradeCard> _upgradeCards = new List<UpgradeCard>();
    private List<UpgradeCard> _filteredCards = new List<UpgradeCard>();
    private List<ScriptableObject> _items = new List<ScriptableObject>();
    private List<ScriptableObject> _filteredItems = new List<ScriptableObject>();
    private ScriptableObject _selectedItem;

    // Enemy Database Support
    private EnemyDatabase _enemyDatabase;
    private List<EnemyEntry> _enemyList = new List<EnemyEntry>();
    private List<EnemyEntry> _filteredEnemies = new List<EnemyEntry>();
    private EnemyEntry _selectedEnemy;

    // Audio Database Support
    private AudioDatabase _audioDatabase;

    // Damage Number Settings Support
    private DamageNumberSettings _damageNumberSettings;
    private ToolbarMenu _dataTypeMenu;

    private enum DataType { UpgradeCard, Weapon, Enemy, Audio, DamageNumbers }
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
        _spriteAssignmentsSection = rootVisualElement.Q<VisualElement>("spriteAssignmentsSection");
        
        _listHeaderLabel = rootVisualElement.Q<Label>("listHeaderLabel");
        _previewHeaderLabel = rootVisualElement.Q<Label>("previewHeaderLabel");

        var createBtn = rootVisualElement.Q<Button>("createNewButton");
        var duplicateBtn = rootVisualElement.Q<Button>("duplicateButton");
        var refreshBtn = rootVisualElement.Q<ToolbarButton>("refreshButton");
        var settingsBtn = rootVisualElement.Q<ToolbarButton>("settingsButton");
        _dataTypeMenu = rootVisualElement.Q<ToolbarMenu>("dataTypeMenu");

        // Callbacks
        createBtn.clicked += CreateNewItem;
        duplicateBtn.clicked += DuplicateSelectedItem;
        refreshBtn.clicked += RefreshList;
        settingsBtn.clicked += OpenSettings;
        
        _searchField.RegisterValueChangedCallback(evt => FilterList(evt.newValue));
        
        _dataTypeMenu.menu.AppendAction("Upgrade Card", (a) => SetDataType(DataType.UpgradeCard), (a) => _currentDataType == DataType.UpgradeCard ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        _dataTypeMenu.menu.AppendAction("Weapon", (a) => SetDataType(DataType.Weapon), (a) => _currentDataType == DataType.Weapon ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        _dataTypeMenu.menu.AppendAction("Enemy", (a) => SetDataType(DataType.Enemy), (a) => _currentDataType == DataType.Enemy ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        _dataTypeMenu.menu.AppendAction("Audio & Sound System", (a) => SetDataType(DataType.Audio), (a) => _currentDataType == DataType.Audio ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        _dataTypeMenu.menu.AppendAction("Damage Numbers", (a) => SetDataType(DataType.DamageNumbers), (a) => _currentDataType == DataType.DamageNumbers ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        
        _itemListView.makeItem = () => 
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            
            var leftBox = new VisualElement { name = "leftBox", style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, overflow = Overflow.Hidden } };
            leftBox.RegisterCallback<ClickEvent>(evt =>
            {
                if (leftBox.userData is System.Action onClick)
                {
                    onClick.Invoke();
                }
            });
            var badge = new Label { name = "itemBadge", style = { flexShrink = 0, whiteSpace = WhiteSpace.NoWrap } };
            badge.style.display = DisplayStyle.None;
            var label = new Label { name = "itemLabel" };
            label.AddToClassList("list-item");
            label.style.flexGrow = 1;
            leftBox.Add(badge);
            leftBox.Add(label);

            var btnContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            
            var renameBtn = new Button { name = "renameBtn", text = "✎", style = { width = 25, height = 20, paddingLeft = 2, paddingRight = 2 } };
            var deleteBtn = new Button { name = "deleteBtn", text = "✖", style = { width = 25, height = 20, paddingLeft = 2, paddingRight = 2, backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f)) } };
            
            btnContainer.Add(renameBtn);
            btnContainer.Add(deleteBtn);
            
            container.Add(leftBox);
            container.Add(btnContainer);
            
            return container;
        };

        _itemListView.bindItem = (element, i) =>
        {
            var leftBox = element.Q<VisualElement>("leftBox");
            var label = element.Q<Label>("itemLabel");
            var badge = element.Q<Label>("itemBadge");
            var renameBtn = element.Q<Button>("renameBtn");
            var deleteBtn = element.Q<Button>("deleteBtn");

            if (leftBox != null)
            {
                leftBox.userData = (System.Action)(() =>
                {
                    if (_currentDataType == DataType.Enemy)
                    {
                        if (i >= 0 && i < _filteredEnemies.Count)
                        {
                            _itemListView.SetSelection(i);
                            OnItemSelected(new object[] { _filteredEnemies[i] });
                        }
                    }
                    else
                    {
                        if (i >= 0 && i < _filteredItems.Count && _filteredItems[i] != null)
                        {
                            _itemListView.SetSelection(i);
                            OnItemSelected(new object[] { _filteredItems[i] });
                        }
                    }
                });
            }

            if (_currentDataType == DataType.Enemy)
            {
                if (i < _filteredEnemies.Count && _filteredEnemies[i] != null)
                {
                    var enemy = _filteredEnemies[i];
                    label.text = string.IsNullOrEmpty(enemy.enemyName) ? "Unnamed Enemy" : enemy.enemyName;
                    label.style.display = DisplayStyle.Flex;

                    badge.style.display = DisplayStyle.Flex;
                    badge.ClearClassList();
                    badge.AddToClassList("badge");
                    switch (enemy.category)
                    {
                        case EnemyCategory.Standard:
                            badge.text = "STD";
                            badge.AddToClassList("badge-standard");
                            break;
                        case EnemyCategory.Elite:
                            badge.text = "ELITE";
                            badge.AddToClassList("badge-elite");
                            break;
                        case EnemyCategory.Boss:
                            badge.text = "BOSS";
                            badge.AddToClassList("badge-boss");
                            break;
                    }

                    renameBtn.clickable = new Clickable(() => 
                    {
                        var textField = new TextField { value = enemy.enemyName, style = { flexGrow = 1, marginRight = 5 } };
                        element.Q(className: "list-item").parent.Insert(1, textField);
                        label.style.display = DisplayStyle.None;
                        
                        textField.Focus();
                        textField.SelectAll();
                        
                        void ApplyRename()
                        {
                            if (textField.parent != null)
                            {
                                Undo.RecordObject(_enemyDatabase, "Rename Enemy");
                                enemy.enemyName = textField.value;
                                EditorUtility.SetDirty(_enemyDatabase);
                                label.text = enemy.enemyName;
                                label.style.display = DisplayStyle.Flex;
                                textField.parent.Remove(textField);
                                UpdatePreview();
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
                                if (textField.parent != null) textField.parent.Remove(textField);
                            }
                        });
                    });

                    deleteBtn.clickable = new Clickable(() => DeleteEnemy(enemy));
                }
            }
            else
            {
                if (i < _filteredItems.Count && _filteredItems[i] != null)
                {
                    var item = _filteredItems[i];
                    label.text = item.name;
                    label.style.display = DisplayStyle.Flex; // ensure visible

                    if (item is UpgradeCard card)
                    {
                        badge.style.display = DisplayStyle.Flex;
                        badge.ClearClassList();
                        badge.AddToClassList("badge");
                        badge.text = card.rarity.ToString().ToUpper();
                        switch (card.rarity)
                        {
                            case CardRarity.Common:
                                badge.AddToClassList("badge-common");
                                break;
                            case CardRarity.Uncommon:
                                badge.AddToClassList("badge-uncommon");
                                break;
                            case CardRarity.Rare:
                                badge.AddToClassList("badge-rare");
                                break;
                            case CardRarity.Epic:
                                badge.AddToClassList("badge-epic");
                                break;
                            case CardRarity.Legendary:
                                badge.AddToClassList("badge-legendary");
                                break;
                            case CardRarity.Mythic:
                                badge.AddToClassList("badge-mythic");
                                break;
                            default:
                                badge.AddToClassList("badge-common");
                                break;
                        }
                    }
                    else
                    {
                        badge.style.display = DisplayStyle.None;
                    }
                    
                    renameBtn.clickable = new Clickable(() => 
                    {
                        // Inline Rename Logic
                        var textField = new TextField { value = item.name, style = { flexGrow = 1, marginRight = 5 } };
                        element.Q(className: "list-item").parent.Insert(0, textField);
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
            }
        };
        _itemListView.selectionChanged += OnItemSelected;

        // Auto Refresh Preview on Inspector Change
        Undo.undoRedoPerformed += OnUndoRedo;
        
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
    
    private void OnUndoRedo()
    {
        _itemListView?.RefreshItems();
        UpdatePreview();
    }

    private void OnDestroy()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void SetDataType(DataType newType)
    {
        _currentDataType = newType;
        _selectedItem = null;
        _selectedEnemy = null;
        _inspectorContainer.Clear();
        ClearPreview();
        if (_itemListView != null) _itemListView.ClearSelection();
        
        if (_dataTypeMenu != null)
        {
            _dataTypeMenu.text = newType switch
            {
                DataType.UpgradeCard => "Data Type: Upgrade Card",
                DataType.Weapon => "Data Type: Weapon",
                DataType.Enemy => "Data Type: Enemy",
                DataType.Audio => "Data Type: Audio",
                DataType.DamageNumbers => "Data Type: Damage Numbers",
                _ => "Data Type"
            };
        }

        if (_listHeaderLabel != null)
        {
            _listHeaderLabel.text = newType switch
            {
                DataType.UpgradeCard => "Upgrade Cards",
                DataType.Weapon => "Weapons",
                DataType.Enemy => "Enemies",
                DataType.Audio => "Audio & Sound System",
                DataType.DamageNumbers => "Damage Numbers",
                _ => "Items"
            };
        }
        
        if (_previewHeaderLabel != null)
        {
            _previewHeaderLabel.text = newType switch
            {
                DataType.UpgradeCard => "Card Preview",
                DataType.Weapon => "Weapon Preview",
                DataType.Enemy => "Enemy Preview",
                DataType.Audio => "Soundtrack & Audio Controls",
                DataType.DamageNumbers => "Damage Numbers Preview",
                _ => "Preview"
            };
        }

        if (_newItemNameField != null)
        {
            _newItemNameField.value = newType switch
            {
                DataType.UpgradeCard => "New Upgrade Card",
                DataType.Weapon => "New Weapon",
                DataType.Enemy => "New Enemy",
                DataType.Audio => "Audio System Active",
                DataType.DamageNumbers => "Damage Numbers Config",
                _ => "New Item"
            };
        }
            
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
        _selectedEnemy = null;
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

    private void DeleteEnemy(EnemyEntry enemy)
    {
        if (enemy == null || _enemyDatabase == null) return;

        if (EditorUtility.DisplayDialog("Delete Enemy", $"Are you sure you want to delete {enemy.enemyName} from database?", "Delete", "Cancel"))
        {
            Undo.RecordObject(_enemyDatabase, "Delete Enemy");
            _enemyDatabase.enemyTypes.Remove(enemy);
            EditorUtility.SetDirty(_enemyDatabase);
            AssetDatabase.SaveAssets();

            if (_selectedEnemy == enemy)
            {
                _selectedEnemy = null;
                _inspectorContainer.Clear();
                ClearPreview();
            }

            RefreshList();
        }
    }

    private void LoadOrCreateEnemyDatabase()
    {
        if (_enemyDatabase != null) return;

        string[] guids = AssetDatabase.FindAssets("t:EnemyDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _enemyDatabase = AssetDatabase.LoadAssetAtPath<EnemyDatabase>(path);
        }
        else
        {
            string dir = "Assets/3d/Enemy";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _enemyDatabase = ScriptableObject.CreateInstance<EnemyDatabase>();
            AssetDatabase.CreateAsset(_enemyDatabase, dir + "/EnemyDatabase.asset");
            AssetDatabase.SaveAssets();
        }
    }

    private void LoadOrCreateAudioDatabase()
    {
        if (_audioDatabase != null) return;

        string assetPath = "Assets/Resources/Audio/AudioDatabase.asset";
        _audioDatabase = AssetDatabase.LoadAssetAtPath<AudioDatabase>(assetPath);
        if (_audioDatabase == null)
        {
            string dir = "Assets/Resources/Audio";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _audioDatabase = ScriptableObject.CreateInstance<AudioDatabase>();
            AssetDatabase.CreateAsset(_audioDatabase, assetPath);
            AssetDatabase.SaveAssets();
        }
    }

    private void LoadOrCreateDamageNumberSettings()
    {
        if (_damageNumberSettings != null) return;

        string assetPath = "Assets/Resources/DamageNumberSettings.asset";
        _damageNumberSettings = AssetDatabase.LoadAssetAtPath<DamageNumberSettings>(assetPath);
        if (_damageNumberSettings == null)
        {
            string dir = "Assets/Resources";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _damageNumberSettings = ScriptableObject.CreateInstance<DamageNumberSettings>();
            AssetDatabase.CreateAsset(_damageNumberSettings, assetPath);
            AssetDatabase.SaveAssets();
        }
    }

    private void RefreshList()
    {
        if (_currentDataType == DataType.Enemy)
        {
            LoadOrCreateEnemyDatabase();
        }
        else if (_currentDataType == DataType.Audio)
        {
            LoadOrCreateAudioDatabase();
            _items.Clear();
            if (_audioDatabase != null) _items.Add(_audioDatabase);
        }
        else if (_currentDataType == DataType.DamageNumbers)
        {
            LoadOrCreateDamageNumberSettings();
            _items.Clear();
            if (_damageNumberSettings != null) _items.Add(_damageNumberSettings);
        }
        else
        {
            _items.Clear();
            string searchType = _currentDataType == DataType.UpgradeCard ? "t:UpgradeCard" : "t:WeaponData";
            string[] guids = AssetDatabase.FindAssets(searchType);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (item != null) _items.Add(item);
            }
        }
        
        FilterList(_searchField != null ? _searchField.value : "");
    }

    private void FilterList(string query)
    {
        if (_currentDataType == DataType.Enemy)
        {
            LoadOrCreateEnemyDatabase();
            _enemyList = _enemyDatabase != null && _enemyDatabase.enemyTypes != null ? _enemyDatabase.enemyTypes : new List<EnemyEntry>();

            if (string.IsNullOrEmpty(query))
            {
                _filteredEnemies = new List<EnemyEntry>(_enemyList);
            }
            else
            {
                _filteredEnemies = _enemyList.Where(e => !string.IsNullOrEmpty(e.enemyName) && e.enemyName.ToLower().Contains(query.ToLower())).ToList();
            }

            _itemListView.itemsSource = _filteredEnemies;
            _itemListView.Rebuild();

            if (_filteredEnemies.Count > 0)
            {
                _itemListView.ClearSelection();
                _itemListView.SetSelection(0);
                OnItemSelected(new object[] { _filteredEnemies[0] });
            }
            else
            {
                _selectedEnemy = null;
                _inspectorContainer.Clear();
                ClearPreview();
            }
            return;
        }

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
            _itemListView.ClearSelection();
            _itemListView.SetSelection(0);
            OnItemSelected(new object[] { _filteredItems[0] });
        }
        else
        {
            _selectedItem = null;
            _inspectorContainer.Clear();
            ClearPreview();
        }
    }

    private void OnItemSelected(IEnumerable<object> selection)
    {
        _inspectorContainer.Clear();
        foreach (var obj in selection)
        {
            if (_currentDataType == DataType.Audio && _audioDatabase != null)
            {
                _selectedEnemy = null;
                _selectedItem = _audioDatabase;
                BuildAudioInspector(_audioDatabase);
                UpdatePreview();
                break;
            }
            else if (_currentDataType == DataType.DamageNumbers && _damageNumberSettings != null)
            {
                _selectedEnemy = null;
                _selectedItem = _damageNumberSettings;
                BuildDamageNumberInspector(_damageNumberSettings);
                UpdatePreview();
                break;
            }
            else if (_currentDataType == DataType.Enemy && obj is EnemyEntry enemy)
            {
                _selectedEnemy = enemy;
                _selectedItem = null;
                BuildEnemyInspector(enemy);
                UpdatePreview();
                break;
            }
            else if (obj is ScriptableObject item)
            {
                _selectedEnemy = null;
                _selectedItem = item;
                
                var serializedObject = new SerializedObject(item);
                var inspectorElement = new InspectorElement(serializedObject);
                inspectorElement.Bind(serializedObject);
                
                // Track all changes to the serialized object
                inspectorElement.TrackSerializedObjectValue(serializedObject, so =>
                {
                    if (item is UpgradeCard card)
                    {
                        var rarityProp = so.FindProperty("rarity");
                        if (rarityProp != null)
                        {
                            CardRarity newRarity = (CardRarity)rarityProp.enumValueIndex;
                            if (card.rarity != newRarity)
                            {
                                card.rarity = newRarity;
                                card.ApplyRaritySettings();
                                EditorUtility.SetDirty(card);
                                so.Update();
                            }
                        }
                    }
                    _itemListView.RefreshItems();
                    UpdatePreview();
                });
                
                _inspectorContainer.Add(inspectorElement);
                UpdatePreview();
                break;
            }
        }
    }

    private void BuildEnemyInspector(EnemyEntry enemy)
    {
        _inspectorContainer.Clear();
        if (enemy == null || _enemyDatabase == null) return;

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        VisualElement CreateHeader(string title)
        {
            var h = new Label(title);
            h.AddToClassList("section-header");
            return h;
        }

        // --- GENERAL & VISUALS ---
        scroll.Add(CreateHeader("General & Visuals"));

        var nameField = new TextField("Enemy Name") { value = enemy.enemyName };
        nameField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Enemy Name");
            enemy.enemyName = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            _itemListView.Rebuild();
            UpdatePreview();
        });
        scroll.Add(nameField);

        var prefabField = new ObjectField("Prefab")
        {
            objectType = typeof(Transform),
            value = enemy.prefab
        };
        prefabField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Enemy Prefab");
            enemy.prefab = evt.newValue as Transform;
            EditorUtility.SetDirty(_enemyDatabase);
            UpdatePreview();
        });
        scroll.Add(prefabField);

        Slider dmgReductionSlider = null;

        var categoryField = new EnumField("Category", enemy.category);
        categoryField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Enemy Category");
            enemy.category = (EnemyCategory)evt.newValue;
            if (enemy.category == EnemyCategory.Boss && enemy.damageReduction == 0f)
            {
                enemy.damageReduction = 40f;
                if (dmgReductionSlider != null) dmgReductionSlider.value = 40f;
            }
            else if (enemy.category == EnemyCategory.Elite && enemy.damageReduction == 0f)
            {
                enemy.damageReduction = 20f;
                if (dmgReductionSlider != null) dmgReductionSlider.value = 20f;
            }
            EditorUtility.SetDirty(_enemyDatabase);
            _itemListView.RefreshItems();
            UpdatePreview();
        });
        scroll.Add(categoryField);

        var isGroundField = new Toggle("Is Ground Enemy") { value = enemy.isGroundEnemy };
        isGroundField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Ground Enemy");
            enemy.isGroundEnemy = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(isGroundField);

        var groundYField = new FloatField("Ground Y Position") { value = enemy.groundYPosition };
        groundYField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Ground Y");
            enemy.groundYPosition = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(groundYField);

        // --- SPAWN RULES ---
        scroll.Add(CreateHeader("Spawn Rules"));

        var minLevelField = new IntegerField("Min Spawn Level") { value = enemy.minSpawnLevel };
        minLevelField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Min Level");
            enemy.minSpawnLevel = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            UpdatePreview();
        });
        scroll.Add(minLevelField);

        var maxLevelField = new IntegerField("Max Spawn Level") { value = enemy.maxSpawnLevel };
        maxLevelField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Max Level");
            enemy.maxSpawnLevel = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            UpdatePreview();
        });
        scroll.Add(maxLevelField);

        var bossLevelField = new IntegerField("Boss Target Level") { value = enemy.bossTargetLevel };
        bossLevelField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Boss Target Level");
            enemy.bossTargetLevel = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(bossLevelField);

        // --- CHASE SETTINGS ---
        scroll.Add(CreateHeader("Chase Settings"));

        var alwaysChaseField = new Toggle("Always Chase Player") { value = enemy.alwaysChasePlayer };
        alwaysChaseField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Always Chase");
            enemy.alwaysChasePlayer = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(alwaysChaseField);

        var chaseChanceSlider = new Slider("Chase Chance (%)", 0f, 100f) { value = enemy.chaseChance, showInputField = true };
        chaseChanceSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Chase Chance");
            enemy.chaseChance = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(chaseChanceSlider);

        // --- STATS ---
        scroll.Add(CreateHeader("Combat Stats"));

        var hpField = new FloatField("Max Health") { value = enemy.maxHealth };
        hpField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Health");
            enemy.maxHealth = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            UpdatePreview();
        });
        scroll.Add(hpField);

        var speedField = new FloatField("Move Speed") { value = enemy.moveSpeed };
        speedField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Speed");
            enemy.moveSpeed = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            UpdatePreview();
        });
        scroll.Add(speedField);

        var dmgField = new FloatField("Damage") { value = enemy.damage };
        dmgField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Damage");
            enemy.damage = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            UpdatePreview();
        });
        scroll.Add(dmgField);

        dmgReductionSlider = new Slider("Damage Reduction (%)", 0f, 100f) { value = enemy.damageReduction, showInputField = true };
        dmgReductionSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Damage Reduction");
            enemy.damageReduction = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            UpdatePreview();
        });
        scroll.Add(dmgReductionSlider);

        var deathDurationField = new FloatField("Death Duration (s)") { value = enemy.deathDuration };
        deathDurationField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Death Duration");
            enemy.deathDuration = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(deathDurationField);

        // --- VFX & COMBAT EFFECTS ---
        scroll.Add(CreateHeader("VFX & Combat Effects"));

        var deathVfxField = new ObjectField("Death VFX (Splash)")
        {
            objectType = typeof(GameObject),
            value = enemy.deathVFX
        };
        deathVfxField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Death VFX");
            enemy.deathVFX = evt.newValue as GameObject;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(deathVfxField);

        var deathVariantField = new ObjectField("Death VFX Variant (Optional)")
        {
            objectType = typeof(GameObject),
            value = enemy.deathVFX_Variant
        };
        deathVariantField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Death VFX Variant");
            enemy.deathVFX_Variant = evt.newValue as GameObject;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(deathVariantField);

        var hitVfxField = new ObjectField("Hit VFX")
        {
            objectType = typeof(GameObject),
            value = enemy.hitVFX
        };
        hitVfxField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Hit VFX");
            enemy.hitVFX = evt.newValue as GameObject;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(hitVfxField);

        var deathVfxLifetimeField = new FloatField("Death VFX Lifetime (s)") { value = enemy.deathVFXLifetime > 0f ? enemy.deathVFXLifetime : 2f };
        deathVfxLifetimeField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Death VFX Lifetime");
            enemy.deathVFXLifetime = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(deathVfxLifetimeField);

        var hitVfxLifetimeField = new FloatField("Hit VFX Lifetime (s)") { value = enemy.hitVFXLifetime > 0f ? enemy.hitVFXLifetime : 1f };
        hitVfxLifetimeField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Hit VFX Lifetime");
            enemy.hitVFXLifetime = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(hitVfxLifetimeField);

        // --- BOSS MINIONS SECTION ---
        var minionsSection = new VisualElement();
        minionsSection.name = "minionsSection";
        scroll.Add(minionsSection);

        void RefreshMinionsUI()
        {
            minionsSection.Clear();
            bool isBoss = enemy.category == EnemyCategory.Boss;
            bossLevelField.style.display = isBoss ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isBoss) return;

            minionsSection.Add(CreateHeader("Boss Minions Configuration"));

            var canSpawnMinionsToggle = new Toggle("Can Spawn Minions") { value = enemy.canSpawnMinions };
            canSpawnMinionsToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(_enemyDatabase, "Toggle Can Spawn Minions");
                enemy.canSpawnMinions = evt.newValue;
                EditorUtility.SetDirty(_enemyDatabase);
            });
            minionsSection.Add(canSpawnMinionsToggle);

            var spawnIntervalField = new FloatField("Spawn Interval (s)") { value = enemy.minionSpawnInterval };
            spawnIntervalField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(_enemyDatabase, "Change Minion Interval");
                enemy.minionSpawnInterval = evt.newValue;
                EditorUtility.SetDirty(_enemyDatabase);
            });
            minionsSection.Add(spawnIntervalField);

            // Quick Minion Assigner UI
            var assignContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8, marginBottom = 8, alignItems = Align.Center } };
            
            var candidateEnemies = _enemyDatabase.enemyTypes.Where(e => e != enemy && e.category != EnemyCategory.Boss).ToList();
            if (candidateEnemies.Count == 0)
            {
                candidateEnemies = _enemyDatabase.enemyTypes.Where(e => e != enemy).ToList();
            }

            List<string> candidateNames = candidateEnemies.Select(e => string.IsNullOrEmpty(e.enemyName) ? "Unnamed" : $"{e.enemyName} [{e.category}]").ToList();
            if (candidateNames.Count == 0)
            {
                candidateNames.Add("(No available enemies)");
            }

            var enemyDropdown = new DropdownField("Add Minion From", candidateNames, 0);
            enemyDropdown.style.flexGrow = 1;
            assignContainer.Add(enemyDropdown);

            var addMinionBtn = new Button { text = "+ Add Minion", style = { height = 24, marginLeft = 6 } };
            addMinionBtn.clicked += () =>
            {
                if (candidateEnemies.Count > 0 && enemyDropdown.index >= 0 && enemyDropdown.index < candidateEnemies.Count)
                {
                    var chosen = candidateEnemies[enemyDropdown.index];
                    Undo.RecordObject(_enemyDatabase, "Add Boss Minion");
                    if (enemy.minionTypes == null) enemy.minionTypes = new List<EnemyEntry>();
                    enemy.minionTypes.Add(chosen.Clone());
                    enemy.canSpawnMinions = true;
                    canSpawnMinionsToggle.value = true;
                    EditorUtility.SetDirty(_enemyDatabase);
                    RefreshMinionsUI();
                }
            };
            assignContainer.Add(addMinionBtn);
            minionsSection.Add(assignContainer);

            // Minions Cards List
            var minionsListContainer = new VisualElement { style = { marginTop = 4 } };
            if (enemy.minionTypes != null && enemy.minionTypes.Count > 0)
            {
                for (int mIdx = 0; mIdx < enemy.minionTypes.Count; mIdx++)
                {
                    int indexCapture = mIdx;
                    var minion = enemy.minionTypes[mIdx];

                    var card = new VisualElement();
                    card.AddToClassList("minion-card");

                    var info = new VisualElement();
                    info.AddToClassList("minion-info");

                    var nameLbl = new Label($"{minion.enemyName}  [{minion.category}]");
                    nameLbl.AddToClassList("minion-name");

                    var statsLbl = new Label($"HP: {minion.maxHealth} | SPD: {minion.moveSpeed} | DMG: {minion.damage}");
                    statsLbl.AddToClassList("minion-stats");

                    info.Add(nameLbl);
                    info.Add(statsLbl);

                    var removeBtn = new Button { text = "✖ Remove", style = { height = 22, backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f)) } };
                    removeBtn.clicked += () =>
                    {
                        Undo.RecordObject(_enemyDatabase, "Remove Boss Minion");
                        enemy.minionTypes.RemoveAt(indexCapture);
                        EditorUtility.SetDirty(_enemyDatabase);
                        RefreshMinionsUI();
                    };

                    card.Add(info);
                    card.Add(removeBtn);
                    minionsListContainer.Add(card);
                }
            }
            else
            {
                var noMinionsLbl = new Label("No minions assigned yet. Use the dropdown above to add minions.") { style = { color = new StyleColor(Color.gray), unityFontStyleAndWeight = FontStyle.Italic, marginTop = 4 } };
                minionsListContainer.Add(noMinionsLbl);
            }
            minionsSection.Add(minionsListContainer);
        }

        categoryField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Category");
            enemy.category = (EnemyCategory)evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
            _itemListView.Rebuild();
            UpdatePreview();
            RefreshMinionsUI();
        });

        RefreshMinionsUI();
        _inspectorContainer.Add(scroll);
    }

    private void BuildAudioInspector(AudioDatabase audioDb)
    {
        _inspectorContainer.Clear();
        if (audioDb == null) return;

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        VisualElement CreateHeader(string title)
        {
            var h = new Label(title);
            h.AddToClassList("section-header");
            return h;
        }

        VisualElement CreateAudioClipRow(string labelText, AudioClip initialClip, System.Action<AudioClip> onClipChanged)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
            var objField = new ObjectField(labelText)
            {
                objectType = typeof(AudioClip),
                value = initialClip
            };
            objField.style.flexGrow = 1;
            objField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(audioDb, $"Change {labelText}");
                onClipChanged(evt.newValue as AudioClip);
                EditorUtility.SetDirty(audioDb);
            });
            row.Add(objField);

            var playBtn = new Button { text = "▶ Play", style = { width = 55, height = 22, marginLeft = 4, backgroundColor = new StyleColor(new Color(0.18f, 0.45f, 0.25f)) } };
            playBtn.clicked += () =>
            {
                AudioClip clip = objField.value as AudioClip;
                if (clip != null) PlayClipPreview(clip);
            };
            row.Add(playBtn);

            var stopBtn = new Button { text = "■ Stop", style = { width = 55, height = 22, marginLeft = 2, backgroundColor = new StyleColor(new Color(0.5f, 0.2f, 0.2f)) } };
            stopBtn.clicked += () => StopAllClipsPreview();
            row.Add(stopBtn);

            return row;
        }

        // --- MASTER VOLUMES ---
        scroll.Add(CreateHeader("Master Volume Mixers"));

        var masterSlider = new Slider("Master Volume", 0f, 1f) { value = audioDb.masterVolume, showInputField = true };
        masterSlider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Master Volume");
            audioDb.masterVolume = evt.newValue;
            EditorUtility.SetDirty(audioDb);
            if (Application.isPlaying && AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(evt.newValue);
            UpdatePreview();
        });
        scroll.Add(masterSlider);

        var musicSlider = new Slider("Music Master Volume", 0f, 1f) { value = audioDb.musicVolume, showInputField = true };
        musicSlider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Music Volume");
            audioDb.musicVolume = evt.newValue;
            EditorUtility.SetDirty(audioDb);
            if (Application.isPlaying && AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(evt.newValue);
            UpdatePreview();
        });
        scroll.Add(musicSlider);

        var sfxSlider = new Slider("SFX Master Volume", 0f, 1f) { value = audioDb.sfxVolume, showInputField = true };
        sfxSlider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "SFX Volume");
            audioDb.sfxVolume = evt.newValue;
            EditorUtility.SetDirty(audioDb);
            if (Application.isPlaying && AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(evt.newValue);
            UpdatePreview();
        });
        scroll.Add(sfxSlider);

        var l0Slider = new Slider("Layer 0 (Base) Volume", 0f, 1f) { value = audioDb.layer0Volume, showInputField = true };
        l0Slider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Layer 0 Volume");
            audioDb.layer0Volume = evt.newValue;
            EditorUtility.SetDirty(audioDb);
            if (Application.isPlaying && AudioManager.Instance != null) AudioManager.Instance.SetLayer0Volume(evt.newValue);
        });
        scroll.Add(l0Slider);

        var l1Slider = new Slider("Layer 1 (Battle) Volume", 0f, 1f) { value = audioDb.layer1Volume, showInputField = true };
        l1Slider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Layer 1 Volume");
            audioDb.layer1Volume = evt.newValue;
            EditorUtility.SetDirty(audioDb);
            if (Application.isPlaying && AudioManager.Instance != null) AudioManager.Instance.SetLayer1Volume(evt.newValue);
        });
        scroll.Add(l1Slider);

        // --- PERIMETER BREACH MUSIC ---
        scroll.Add(CreateHeader("Perimeter Breach Dynamic Music"));

        scroll.Add(CreateAudioClipRow("Layer 0 (Ambient Loop)", audioDb.perimeterBreachLayer0, c => audioDb.perimeterBreachLayer0 = c));
        scroll.Add(CreateAudioClipRow("Layer 1 (Battle Loop)", audioDb.perimeterBreachLayer1, c => audioDb.perimeterBreachLayer1 = c));

        var duckSlider = new Slider("Card Popup Duck Volume", 0.05f, 0.8f) { value = audioDb.cardPopupDuckVolume, showInputField = true };
        duckSlider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Duck Volume");
            audioDb.cardPopupDuckVolume = evt.newValue;
            EditorUtility.SetDirty(audioDb);
        });
        scroll.Add(duckSlider);

        var cardPitchSlider = new Slider("Card Popup Slow Pitch", 0.7f, 1.0f) { value = audioDb.cardPopupSlowPitch, showInputField = true };
        cardPitchSlider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Card Popup Pitch");
            audioDb.cardPopupSlowPitch = evt.newValue;
            EditorUtility.SetDirty(audioDb);
        });
        scroll.Add(cardPitchSlider);

        var slowPitchSlider = new Slider("Weapon Wheel Slow Pitch", 0.2f, 0.9f) { value = audioDb.weaponWheelSlowPitch, showInputField = true };
        slowPitchSlider.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Slow Pitch");
            audioDb.weaponWheelSlowPitch = evt.newValue;
            EditorUtility.SetDirty(audioDb);
        });
        scroll.Add(slowPitchSlider);

        var fadeSpeedField = new FloatField("Music Fade Speed") { value = audioDb.musicFadeSpeed };
        fadeSpeedField.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Fade Speed");
            audioDb.musicFadeSpeed = Mathf.Max(0.5f, evt.newValue);
            EditorUtility.SetDirty(audioDb);
        });
        scroll.Add(fadeSpeedField);

        var pitchSpeedField = new FloatField("Pitch Smooth Speed") { value = audioDb.pitchSmoothSpeed };
        pitchSpeedField.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Pitch Speed");
            audioDb.pitchSmoothSpeed = Mathf.Max(0.5f, evt.newValue);
            EditorUtility.SetDirty(audioDb);
        });
        scroll.Add(pitchSpeedField);

        var graceField = new FloatField("Battle Music Grace Period (s)") { value = audioDb.battleGracePeriod };
        graceField.RegisterValueChangedCallback(evt => {
            Undo.RecordObject(audioDb, "Grace Period");
            audioDb.battleGracePeriod = Mathf.Max(0f, evt.newValue);
            EditorUtility.SetDirty(audioDb);
        });
        scroll.Add(graceField);

        // --- UI SOUNDS ---
        scroll.Add(CreateHeader("UI Sound Effects"));
        scroll.Add(CreateAudioClipRow("Card Popup", audioDb.cardPopupSound, c => audioDb.cardPopupSound = c));
        scroll.Add(CreateAudioClipRow("Card Click / Select", audioDb.cardClickSound, c => audioDb.cardClickSound = c));
        scroll.Add(CreateAudioClipRow("Card Swipe Confirm", audioDb.cardSwipeConfirmSound, c => audioDb.cardSwipeConfirmSound = c));
        scroll.Add(CreateAudioClipRow("Button Click", audioDb.buttonClickSound, c => audioDb.buttonClickSound = c));
        scroll.Add(CreateAudioClipRow("Level Up Banner", audioDb.levelUpBannerSound, c => audioDb.levelUpBannerSound = c));
        scroll.Add(CreateAudioClipRow("Weapon Wheel Open", audioDb.weaponWheelOpenSound, c => audioDb.weaponWheelOpenSound = c));
        scroll.Add(CreateAudioClipRow("Weapon Wheel Close", audioDb.weaponWheelCloseSound, c => audioDb.weaponWheelCloseSound = c));
        scroll.Add(CreateAudioClipRow("Weapon Wheel Select", audioDb.weaponWheelSelectSound, c => audioDb.weaponWheelSelectSound = c));

        // --- WEAPONS SOUNDS ---
        scroll.Add(CreateHeader("Weapon Sound Profiles"));
        if (audioDb.weaponProfiles != null)
        {
            for (int w = 0; w < audioDb.weaponProfiles.Count; w++)
            {
                var profile = audioDb.weaponProfiles[w];
                if (profile == null) continue;

                var wpnBox = new VisualElement { style = { backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.22f)), paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6, marginBottom = 6, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
                var wpnHeader = new Label($"{profile.displayName} (ID: {profile.weaponID})") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
                wpnBox.Add(wpnHeader);

                AudioClip fireClip = (profile.fireClips != null && profile.fireClips.Length > 0) ? profile.fireClips[0] : null;
                wpnBox.Add(CreateAudioClipRow("Fire Sound", fireClip, c => {
                    profile.fireClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0];
                }));
                wpnBox.Add(CreateAudioClipRow("Reload Sound", profile.reloadClip, c => profile.reloadClip = c));
                wpnBox.Add(CreateAudioClipRow("Mag Out Sound", profile.magOutClip, c => profile.magOutClip = c));
                wpnBox.Add(CreateAudioClipRow("Mag In Sound", profile.magInClip, c => profile.magInClip = c));
                wpnBox.Add(CreateAudioClipRow("Empty Click Sound", profile.emptyClickClip, c => profile.emptyClickClip = c));

                var wpnVolSlider = new Slider("Weapon Volume", 0f, 1f) { value = profile.volume, showInputField = true };
                wpnVolSlider.RegisterValueChangedCallback(evt => {
                    profile.volume = evt.newValue;
                    EditorUtility.SetDirty(audioDb);
                });
                wpnBox.Add(wpnVolSlider);

                scroll.Add(wpnBox);
            }
        }

        // --- BOSS SOUNDS ---
        scroll.Add(CreateHeader("Boss Sound Effects"));
        scroll.Add(CreateAudioClipRow("Boss Roar (Spawn / Fight Start)", audioDb.bossRoarClip, c => audioDb.bossRoarClip = c));
        scroll.Add(CreateAudioClipRow("Boss Spit / Attack", audioDb.bossSpitClip, c => audioDb.bossSpitClip = c));
        scroll.Add(CreateAudioClipRow("Boss Hurt / Hit", audioDb.bossHurtClip, c => audioDb.bossHurtClip = c));
        scroll.Add(CreateAudioClipRow("Boss Death / Defeat", audioDb.bossDeathClip, c => audioDb.bossDeathClip = c));
        scroll.Add(CreateAudioClipRow("Boss Phase Change", audioDb.bossPhaseChangeClip, c => audioDb.bossPhaseChangeClip = c));

        // --- BUG & MINION SOUNDS ---
        scroll.Add(CreateHeader("Bug & Minion Sound Effects"));
        AudioClip bugHurt = (audioDb.bugHurtClips != null && audioDb.bugHurtClips.Length > 0) ? audioDb.bugHurtClips[0] : null;
        scroll.Add(CreateAudioClipRow("Bug Hurt / Hit", bugHurt, c => audioDb.bugHurtClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0]));

        AudioClip bugDeath = (audioDb.bugDeathClips != null && audioDb.bugDeathClips.Length > 0) ? audioDb.bugDeathClips[0] : null;
        scroll.Add(CreateAudioClipRow("Bug Death", bugDeath, c => audioDb.bugDeathClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0]));

        AudioClip bugAttack = (audioDb.bugAttackClips != null && audioDb.bugAttackClips.Length > 0) ? audioDb.bugAttackClips[0] : null;
        scroll.Add(CreateAudioClipRow("Bug Attack", bugAttack, c => audioDb.bugAttackClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0]));

        AudioClip bugSkitter = (audioDb.bugSkitterClips != null && audioDb.bugSkitterClips.Length > 0) ? audioDb.bugSkitterClips[0] : null;
        scroll.Add(CreateAudioClipRow("Bug Skitter / Spawn", bugSkitter, c => audioDb.bugSkitterClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0]));

        // --- IMPACT SOUNDS ---
        scroll.Add(CreateHeader("Impact & Damage Sound Effects"));
        AudioClip hitFlesh = (audioDb.bulletHitFleshClips != null && audioDb.bulletHitFleshClips.Length > 0) ? audioDb.bulletHitFleshClips[0] : null;
        scroll.Add(CreateAudioClipRow("Bullet Hit Flesh", hitFlesh, c => audioDb.bulletHitFleshClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0]));

        AudioClip hitSurface = (audioDb.bulletHitSurfaceClips != null && audioDb.bulletHitSurfaceClips.Length > 0) ? audioDb.bulletHitSurfaceClips[0] : null;
        scroll.Add(CreateAudioClipRow("Bullet Hit Surface", hitSurface, c => audioDb.bulletHitSurfaceClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0]));

        AudioClip playerHurt = (audioDb.playerHurtClips != null && audioDb.playerHurtClips.Length > 0) ? audioDb.playerHurtClips[0] : null;
        scroll.Add(CreateAudioClipRow("Player Hurt", playerHurt, c => audioDb.playerHurtClips = (c != null) ? new AudioClip[] { c } : new AudioClip[0]));

        scroll.Add(CreateAudioClipRow("Player Death", audioDb.playerDeathClip, c => audioDb.playerDeathClip = c));

        _inspectorContainer.Add(scroll);
    }

    private void BuildDamageNumberInspector(DamageNumberSettings settings)
    {
        _inspectorContainer.Clear();
        if (settings == null) return;

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        VisualElement CreateHeader(string title)
        {
            var h = new Label(title);
            h.AddToClassList("section-header");
            return h;
        }

        // --- FONT & BASE SIZE ---
        scroll.Add(CreateHeader("Font & Base Size"));

        var fontField = new ObjectField("Custom Font Asset")
        {
            objectType = typeof(TMP_FontAsset),
            value = settings.customFont
        };
        fontField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Change Font");
            settings.customFont = evt.newValue as TMP_FontAsset;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(fontField);

        var fontSlider = new Slider("Base Font Size", 2f, 12f) { value = settings.baseFontSize, showInputField = true };
        fontSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Base Font Size");
            settings.baseFontSize = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(fontSlider);

        // --- POSITION & OFFSETS ---
        scroll.Add(CreateHeader("Position & Worldspace Offsets"));

        var headOffsetSlider = new Slider("Enemy Head Height Offset", 0.2f, 4.0f) { value = settings.enemyHeadHeightOffset, showInputField = true };
        headOffsetSlider.tooltip = "Vertical height (in meters) above regular enemy / minion pivot to spawn floating damage text.";
        headOffsetSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Enemy Head Offset");
            settings.enemyHeadHeightOffset = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(headOffsetSlider);

        var bossCamOffsetSlider = new Slider("Boss Camera Distance Offset", 0.05f, 1.0f) { value = settings.bossCameraOffset, showInputField = true };
        bossCamOffsetSlider.tooltip = "Offset distance towards camera from boss hit point, preventing numbers from clipping inside 3D mesh.";
        bossCamOffsetSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Camera Offset");
            settings.bossCameraOffset = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossCamOffsetSlider);

        var bossVertOffsetSlider = new Slider("Boss Vertical Hit Lift", 0.0f, 1.0f) { value = settings.bossVerticalOffset, showInputField = true };
        bossVertOffsetSlider.tooltip = "Vertical upward lift applied to the impact point on the boss.";
        bossVertOffsetSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Vertical Offset");
            settings.bossVerticalOffset = evt.newValue;
            EditorUtility.SetDirty(settings);
        });
        scroll.Add(bossVertOffsetSlider);

        var spreadSlider = new Slider("Fanning Spread Radius", 0.0f, 0.8f) { value = settings.randomSpreadRadius, showInputField = true };
        spreadSlider.tooltip = "Random fanning spread so consecutive auto-fire bullets and shotgun pellets do not stack on top of each other.";
        spreadSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Spread Radius");
            settings.randomSpreadRadius = evt.newValue;
            EditorUtility.SetDirty(settings);
        });
        scroll.Add(spreadSlider);

        // --- SCALE MULTIPLIERS ---
        scroll.Add(CreateHeader("Scale Multipliers by Part & Target"));

        var normalScaleSlider = new Slider("Normal Enemy Hit Scale", 0.5f, 2.5f) { value = settings.normalScale, showInputField = true };
        normalScaleSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Normal Scale");
            settings.normalScale = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(normalScaleSlider);

        var bossHeadScaleSlider = new Slider("Boss Head / Crit Scale", 0.5f, 3.0f) { value = settings.bossHeadScale, showInputField = true };
        bossHeadScaleSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Head Scale");
            settings.bossHeadScale = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossHeadScaleSlider);

        var bossLegScaleSlider = new Slider("Boss Leg Hit Scale", 0.5f, 2.5f) { value = settings.bossLegScale, showInputField = true };
        bossLegScaleSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Leg Scale");
            settings.bossLegScale = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossLegScaleSlider);

        var bossGeneralScaleSlider = new Slider("Boss Body Hit Scale", 0.5f, 2.5f) { value = settings.bossGeneralScale, showInputField = true };
        bossGeneralScaleSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss General Scale");
            settings.bossGeneralScale = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossGeneralScaleSlider);

        var bossBrokenScaleSlider = new Slider("Boss Broken Part Scale", 0.5f, 2.0f) { value = settings.bossBrokenScale, showInputField = true };
        bossBrokenScaleSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Broken Scale");
            settings.bossBrokenScale = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossBrokenScaleSlider);

        // --- ANIMATION & MOTION ---
        scroll.Add(CreateHeader("Animation & Floating Motion"));

        var floatSpdSlider = new Slider("Float Upward Speed", 0.5f, 5.0f) { value = settings.floatUpSpeed, showInputField = true };
        floatSpdSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Float Speed");
            settings.floatUpSpeed = evt.newValue;
            EditorUtility.SetDirty(settings);
        });
        scroll.Add(floatSpdSlider);

        var driftSpdSlider = new Slider("Drift Speed", 0.0f, 2.0f) { value = settings.driftSpeed, showInputField = true };
        driftSpdSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Drift Speed");
            settings.driftSpeed = evt.newValue;
            EditorUtility.SetDirty(settings);
        });
        scroll.Add(driftSpdSlider);

        var durationSlider = new Slider("Lifetime Duration (s)", 0.3f, 2.0f) { value = settings.duration, showInputField = true };
        durationSlider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Duration");
            settings.duration = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(durationSlider);

        // --- COLOR PALETTE ---
        scroll.Add(CreateHeader("Color Palette"));

        var normalColorField = new ColorField("Normal Enemy Hit Color") { value = settings.normalColor };
        normalColorField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Normal Color");
            settings.normalColor = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(normalColorField);

        var bossHeadColorField = new ColorField("Boss Head / Crit Color") { value = settings.bossHeadColor };
        bossHeadColorField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Head Color");
            settings.bossHeadColor = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossHeadColorField);

        var bossLegColorField = new ColorField("Boss Leg Color") { value = settings.bossLegColor };
        bossLegColorField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Leg Color");
            settings.bossLegColor = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossLegColorField);

        var bossGeneralColorField = new ColorField("Boss Body Color") { value = settings.bossGeneralColor };
        bossGeneralColorField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss General Color");
            settings.bossGeneralColor = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossGeneralColorField);

        var bossBrokenColorField = new ColorField("Boss Broken Part Color") { value = settings.bossBrokenColor };
        bossBrokenColorField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Boss Broken Color");
            settings.bossBrokenColor = evt.newValue;
            EditorUtility.SetDirty(settings);
            UpdatePreview();
        });
        scroll.Add(bossBrokenColorField);

        _inspectorContainer.Add(scroll);
    }

    private static void PlayClipPreview(AudioClip clip)
    {
        if (clip == null) return;
        var method = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        method?.Invoke(null, new object[] { clip, 0, false });
    }

    private static void StopAllClipsPreview()
    {
        var method = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        method?.Invoke(null, null);
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
        if (_spriteAssignmentsSection != null) _spriteAssignmentsSection.style.display = DisplayStyle.None;
    }

    private void UpdatePreview()
    {
        var previewContainer = rootVisualElement.Q<VisualElement>(className: "preview-container");
        
        if (_currentDataType == DataType.Audio)
        {
            if (_audioDatabase == null) LoadOrCreateAudioDatabase();
            if (previewContainer != null) previewContainer.style.display = DisplayStyle.Flex;
            if (_spriteAssignmentsSection != null) _spriteAssignmentsSection.style.display = DisplayStyle.None;

            _previewTitle.text = "PERIMETER BREACH";
            _previewDesc.text = $"Adaptive Dual-Layer Audio System\nMaster: {Mathf.RoundToInt(_audioDatabase.masterVolume * 100)}% | Music: {Mathf.RoundToInt(_audioDatabase.musicVolume * 100)}% | SFX: {Mathf.RoundToInt(_audioDatabase.sfxVolume * 100)}%\nLayer 0: Continuous Groove | Layer 1: Combat Intensity";
            _previewValue.text = "DYNAMIC";

            _previewIcon.style.backgroundImage = null;
            _previewBackground.style.backgroundImage = null;
            _previewBackground.style.backgroundColor = new StyleColor(new Color(0.08f, 0.16f, 0.28f));
            return;
        }

        if (_currentDataType == DataType.DamageNumbers)
        {
            if (_damageNumberSettings == null) LoadOrCreateDamageNumberSettings();
            if (previewContainer != null) previewContainer.style.display = DisplayStyle.Flex;
            if (_spriteAssignmentsSection != null) _spriteAssignmentsSection.style.display = DisplayStyle.None;

            _previewTitle.text = "DAMAGE NUMBERS";
            _previewDesc.text = $"3D Worldspace Floating TextMeshPro\nFont Size: {_damageNumberSettings.baseFontSize:F1} | Head Scale: {_damageNumberSettings.bossHeadScale:F2}x | Leg: {_damageNumberSettings.bossLegScale:F2}x\nEnemy Height: +{_damageNumberSettings.enemyHeadHeightOffset:F2}m | Boss Cam Offset: {_damageNumberSettings.bossCameraOffset:F2}m\nDuration: {_damageNumberSettings.duration:F2}s | Float: {_damageNumberSettings.floatUpSpeed:F1}";
            _previewValue.text = "3D TEXT";

            _previewIcon.style.backgroundImage = null;
            _previewBackground.style.backgroundImage = null;
            _previewBackground.style.backgroundColor = new StyleColor(new Color(0.25f, 0.16f, 0.05f));
            return;
        }

        if (_currentDataType == DataType.Enemy)
        {
            if (_selectedEnemy == null)
            {
                ClearPreview();
                return;
            }

            if (previewContainer != null) previewContainer.style.display = DisplayStyle.Flex;
            if (_spriteAssignmentsSection != null) _spriteAssignmentsSection.style.display = DisplayStyle.None;

            _previewTitle.text = string.IsNullOrEmpty(_selectedEnemy.enemyName) ? "UNNAMED" : _selectedEnemy.enemyName.ToUpper();
            _previewDesc.text = $"HP: {_selectedEnemy.maxHealth} | Speed: {_selectedEnemy.moveSpeed}\nDamage: {_selectedEnemy.damage} | Dmg Reduction: {_selectedEnemy.damageReduction:F0}%\nLevels: {_selectedEnemy.minSpawnLevel} - {_selectedEnemy.maxSpawnLevel}";
            _previewValue.text = _selectedEnemy.category.ToString().ToUpper();

            Texture2D previewTex = null;
            if (_selectedEnemy.prefab != null)
            {
                previewTex = AssetPreview.GetAssetPreview(_selectedEnemy.prefab.gameObject);
                if (previewTex == null)
                {
                    previewTex = AssetPreview.GetMiniThumbnail(_selectedEnemy.prefab.gameObject);
                }
            }

            if (previewTex != null)
            {
                _previewIcon.style.backgroundImage = new StyleBackground(previewTex);
            }
            else
            {
                _previewIcon.style.backgroundImage = null;
            }

            Color bgColor = _selectedEnemy.category switch
            {
                EnemyCategory.Boss => new Color(0.35f, 0.12f, 0.12f),
                EnemyCategory.Elite => new Color(0.25f, 0.12f, 0.35f),
                _ => new Color(0.12f, 0.2f, 0.3f)
            };

            _previewBackground.style.backgroundImage = null;
            _previewBackground.style.backgroundColor = new StyleColor(bgColor);
            return;
        }

        if (_selectedItem == null)
        {
            ClearPreview();
            return;
        }

        if (_spriteAssignmentsSection != null) _spriteAssignmentsSection.style.display = DisplayStyle.Flex;

        if (_currentDataType == DataType.UpgradeCard && _selectedItem is UpgradeCard _selectedCard)
        {
            if (previewContainer != null) previewContainer.style.display = DisplayStyle.Flex;
            
            _previewTitle.text = _selectedCard.cardName;
            
            string descText = "";
            if (!string.IsNullOrEmpty(_selectedCard.category))
            {
                descText += $"<b>[{_selectedCard.category}]</b>\n";
            }
            descText += _selectedCard.description;
            if (!string.IsNullOrEmpty(_selectedCard.flavorText))
            {
                descText += $"\n\n<i>\"{_selectedCard.flavorText}\"</i>";
            }
            _previewDesc.text = descText;
            
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
            
            _quickIcon.style.backgroundImage = _selectedCard.cardIcon != null ? new StyleBackground(_selectedCard.cardIcon) : null;
            _quickBg.style.backgroundImage = _selectedCard.rarityBackgroundImage != null ? new StyleBackground(_selectedCard.rarityBackgroundImage) : null;
            _quickSel.style.backgroundImage = _selectedCard.selectedCardSprite != null ? new StyleBackground(_selectedCard.selectedCardSprite) : null;
        }
        else if (_currentDataType == DataType.Weapon && _selectedItem is WeaponData _selectedWeapon)
        {
            if (previewContainer != null) previewContainer.style.display = DisplayStyle.Flex;
            
            _previewTitle.text = _selectedWeapon.weaponName;
            _previewDesc.text = $"Damage: {_selectedWeapon.baseDamage}\nFire Rate: {_selectedWeapon.fireRate}\nMag: {_selectedWeapon.magSize}";
            _previewValue.text = _selectedWeapon.category.ToString();

            _previewIcon.style.backgroundImage = _selectedWeapon.icon != null ? new StyleBackground(_selectedWeapon.icon) : null;
            _previewBackground.style.backgroundImage = null;
            _previewBackground.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            
            _quickIcon.style.backgroundImage = _selectedWeapon.icon != null ? new StyleBackground(_selectedWeapon.icon) : null;
            _quickBg.style.backgroundImage = null;
            _quickSel.style.backgroundImage = null;
        }
        else
        {
            ClearPreview();
        }
    }

    private void CreateNewItem()
    {
        if (_currentDataType == DataType.DamageNumbers) return;

        if (_currentDataType == DataType.Enemy)
        {
            LoadOrCreateEnemyDatabase();
            if (_enemyDatabase == null) return;

            string enemyName = string.IsNullOrEmpty(_newItemNameField.value) ? "New Enemy" : _newItemNameField.value;
            EnemyEntry newEnemy = new EnemyEntry();
            newEnemy.enemyName = enemyName;

            Undo.RecordObject(_enemyDatabase, "Create Enemy");
            if (_enemyDatabase.enemyTypes == null) _enemyDatabase.enemyTypes = new List<EnemyEntry>();
            _enemyDatabase.enemyTypes.Add(newEnemy);
            EditorUtility.SetDirty(_enemyDatabase);
            AssetDatabase.SaveAssets();

            RefreshList();

            int index = _filteredEnemies.IndexOf(newEnemy);
            if (index >= 0)
            {
                _itemListView.SetSelection(index);
                _itemListView.ScrollToItem(index);
            }
            return;
        }

        if (_currentDataType == DataType.Audio)
        {
            if (_audioDatabase == null) LoadOrCreateAudioDatabase();
            Undo.RecordObject(_audioDatabase, "Add Weapon Profile");
            string profileName = string.IsNullOrEmpty(_newItemNameField.value) ? "New Weapon Profile" : _newItemNameField.value;
            if (_audioDatabase.weaponProfiles == null) _audioDatabase.weaponProfiles = new List<WeaponAudioProfile>();
            _audioDatabase.weaponProfiles.Add(new WeaponAudioProfile { displayName = profileName, weaponID = profileName.Replace(" ", "_") });
            EditorUtility.SetDirty(_audioDatabase);
            AssetDatabase.SaveAssets();
            BuildAudioInspector(_audioDatabase);
            return;
        }

        ScriptableObject newItem = null;
        string fullPath = "";
        
        if (_currentDataType == DataType.UpgradeCard)
        {
            string path = "Assets/ScriptableObjects/Cards";
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

            string assetName = string.IsNullOrEmpty(_newItemNameField.value) ? "New Upgrade Card" : _newItemNameField.value;
            fullPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{assetName}.asset");

            var newCard = ScriptableObject.CreateInstance<UpgradeCard>();
            newCard.cardName = assetName;
            newItem = newCard;
        }
        else if (_currentDataType == DataType.Weapon)
        {
            string path = "Assets/Resources/Weapons";
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

            string assetName = string.IsNullOrEmpty(_newItemNameField.value) ? "New Weapon" : _newItemNameField.value;
            fullPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{assetName}.asset");

            var newWpn = ScriptableObject.CreateInstance<WeaponData>();
            newWpn.weaponName = assetName;
            newWpn.weaponID = assetName.Replace(" ", "_");
            newItem = newWpn;
        }

        if (newItem != null)
        {
            AssetDatabase.CreateAsset(newItem, fullPath);
            AssetDatabase.SaveAssets();
        }
        
        RefreshList();
        
        int idx = _filteredItems.IndexOf(newItem);
        if (idx >= 0)
        {
            _itemListView.SetSelection(idx);
            _itemListView.ScrollToItem(idx);
        }
    }

    private void DuplicateSelectedItem()
    {
        if (_currentDataType == DataType.Audio || _currentDataType == DataType.DamageNumbers) return;

        if (_currentDataType == DataType.Enemy)
        {
            if (_selectedEnemy == null || _enemyDatabase == null) return;

            Undo.RecordObject(_enemyDatabase, "Duplicate Enemy");
            EnemyEntry duplicate = _selectedEnemy.Clone();
            duplicate.enemyName = _selectedEnemy.enemyName + " Copy";
            if (_selectedEnemy.minionTypes != null && _selectedEnemy.minionTypes.Count > 0)
            {
                duplicate.minionTypes = new List<EnemyEntry>();
                foreach (var m in _selectedEnemy.minionTypes)
                {
                    duplicate.minionTypes.Add(m.Clone());
                }
                duplicate.canSpawnMinions = _selectedEnemy.canSpawnMinions;
                duplicate.minionSpawnInterval = _selectedEnemy.minionSpawnInterval;
            }

            _enemyDatabase.enemyTypes.Add(duplicate);
            EditorUtility.SetDirty(_enemyDatabase);
            AssetDatabase.SaveAssets();

            RefreshList();
            int index = _filteredEnemies.IndexOf(duplicate);
            if (index >= 0)
            {
                _itemListView.SetSelection(index);
                _itemListView.ScrollToItem(index);
            }
            return;
        }

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
