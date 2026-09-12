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

    private enum DataType { UpgradeCard, Weapon, Enemy }
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
        var dataTypeMenu = rootVisualElement.Q<ToolbarMenu>("dataTypeMenu");

        // Callbacks
        createBtn.clicked += CreateNewItem;
        duplicateBtn.clicked += DuplicateSelectedItem;
        refreshBtn.clicked += RefreshList;
        settingsBtn.clicked += OpenSettings;
        
        _searchField.RegisterValueChangedCallback(evt => FilterList(evt.newValue));
        
        dataTypeMenu.menu.AppendAction("Upgrade Card", (a) => SetDataType(DataType.UpgradeCard), (a) => _currentDataType == DataType.UpgradeCard ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        dataTypeMenu.menu.AppendAction("Weapon", (a) => SetDataType(DataType.Weapon), (a) => _currentDataType == DataType.Weapon ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        dataTypeMenu.menu.AppendAction("Enemy", (a) => SetDataType(DataType.Enemy), (a) => _currentDataType == DataType.Enemy ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        
        _itemListView.makeItem = () => 
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            
            var leftBox = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };
            var badge = new Label { name = "itemBadge" };
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
            var label = element.Q<Label>("itemLabel");
            var badge = element.Q<Label>("itemBadge");
            var renameBtn = element.Q<Button>("renameBtn");
            var deleteBtn = element.Q<Button>("deleteBtn");

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
                badge.style.display = DisplayStyle.None;
                if (i < _filteredItems.Count && _filteredItems[i] != null)
                {
                    var item = _filteredItems[i];
                    label.text = item.name;
                    label.style.display = DisplayStyle.Flex; // ensure visible
                    
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
        _selectedItem = null;
        _selectedEnemy = null;
        _inspectorContainer.Clear();
        ClearPreview();
        
        if (_listHeaderLabel != null)
        {
            _listHeaderLabel.text = newType switch
            {
                DataType.UpgradeCard => "Upgrade Cards",
                DataType.Weapon => "Weapons",
                DataType.Enemy => "Enemies",
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

    private void RefreshList()
    {
        if (_currentDataType == DataType.Enemy)
        {
            LoadOrCreateEnemyDatabase();
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
                _itemListView.SetSelection(0);
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
            _itemListView.SetSelection(0);
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
            if (_currentDataType == DataType.Enemy && obj is EnemyEntry enemy)
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
                inspectorElement.TrackSerializedObjectValue(serializedObject, so => UpdatePreview());
                
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

        var categoryField = new EnumField("Category", enemy.category);
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

        var deathDurationField = new FloatField("Death Duration (s)") { value = enemy.deathDuration };
        deathDurationField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_enemyDatabase, "Change Death Duration");
            enemy.deathDuration = evt.newValue;
            EditorUtility.SetDirty(_enemyDatabase);
        });
        scroll.Add(deathDurationField);

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
            _previewDesc.text = $"HP: {_selectedEnemy.maxHealth} | Speed: {_selectedEnemy.moveSpeed}\nDamage: {_selectedEnemy.damage}\nLevels: {_selectedEnemy.minSpawnLevel} - {_selectedEnemy.maxSpawnLevel}";
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
