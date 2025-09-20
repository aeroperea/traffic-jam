using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TemplateMakerEditor : EditorWindow
{
    private const string EditorPrefsKey = "TemplateMakerEditor_Properties";
    private string editorModuleFolderName = "ScriptTemplateMaker";

    private string templateName = "NewTemplate";
    private string appendToScriptName = "";
    private bool removeBeforeProcessingProperties = true;
    private string targetDirectory = "";
    private string createMenuSubFolder = "My Template Scripts";
    private string subFolder = ""; // Subfolder support
    private TextAsset existingTemplate; // Drag-and-drop TextAsset
    private List<TemplateScriptProperty> scriptProperties = new List<TemplateScriptProperty>();

    private EditorTab activeTab = EditorTab.TemplateGenerator;
    private TextAsset selectedScript; // User-selected script
    private List<ScriptElement> extractedElements = new List<ScriptElement>();

    private static string GeneratedTemplatesPath = ""; // Stores dynamically found module path

    Color generateButtonColor = new Color(0, 1, 0.2f, 1);

    [MenuItem("Tools/Script Template Generator")]
    public static void ShowWindow()
    {
        TemplateMakerEditor window = GetWindow<TemplateMakerEditor>("Script Template Generator");
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        GeneratedTemplatesPath = FindTemplateModulePath() + "/GeneratedTemplates";
        Debug.Log(GeneratedTemplatesPath);
        LoadEditorData(); // Load stored properties on enable
    }

    private void OnDisable()
    {
        SaveEditorData(); // Save properties when window closes or Unity recompiles
    }

    private void LoadEditorData()
    {
        if (EditorPrefs.HasKey(EditorPrefsKey))
        {
            string json = EditorPrefs.GetString(EditorPrefsKey);
            TemplateEditorSaveData data = JsonUtility.FromJson<TemplateEditorSaveData>(json);

            if (data != null)
            {
                templateName = data.templateName;
                appendToScriptName = data.appendToScriptName;
                removeBeforeProcessingProperties = data.removeBeforeProcessingProperties;
                targetDirectory = data.targetDirectory;
                createMenuSubFolder = data.createMenuSubFolder;
                subFolder = data.subFolder;
                scriptProperties = data.scriptProperties ?? new List<TemplateScriptProperty>();
            }
        }
    }

    private void SaveEditorData()
    {
        TemplateEditorSaveData data = new TemplateEditorSaveData()
        {
            templateName = templateName,
            appendToScriptName = appendToScriptName,
            removeBeforeProcessingProperties = removeBeforeProcessingProperties,
            targetDirectory = targetDirectory,
            createMenuSubFolder = createMenuSubFolder,
            subFolder = subFolder,
            scriptProperties = scriptProperties
        };

        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EditorPrefsKey, json);
    }

    private string FindTemplateModulePath()
    {
        string[] results = AssetDatabase.FindAssets(editorModuleFolderName);
        if (results.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(results[0]);
            return assetPath;
        }
        return "Assets"; // Fallback if not found
    }

    private void OnGUI()
    {
        GUILayout.Label("Template Generator", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Template Name", "The name of your template and generated script menu."), GUILayout.Width(210));
        templateName = EditorGUILayout.TextField(templateName, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Ensured Script Suffix", "Automatically appends this text to script names if missing."), GUILayout.Width(210));
        appendToScriptName = EditorGUILayout.TextField(appendToScriptName, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Editor Files Subfolder (FromModule)", "Where the generated script and script template will go."), GUILayout.Width(210));
        subFolder = EditorGUILayout.TextField(subFolder, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("NewCreations Target Directory", "Where new scripts made from this template will go."), GUILayout.Width(205));
        targetDirectory = EditorGUILayout.TextField(targetDirectory, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Create Menu Sub-Folder", "Where the template will be accessed in the create menu. Format it like a path."), GUILayout.Width(210));
        createMenuSubFolder = EditorGUILayout.TextField(createMenuSubFolder, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Existing Template (Optional)", EditorStyles.boldLabel);
        existingTemplate = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Template File", "Drag and drop a template text file."), existingTemplate, typeof(TextAsset), false);

        if (existingTemplate != null && GUILayout.Button("Import Template Properties"))
        {
            ImportTemplateProperties();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Template Properties", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Property Derrivitives Remove Suffix", "Removes the suffix text before processing other template properties."), GUILayout.Width(210));
        removeBeforeProcessingProperties = EditorGUILayout.Toggle(removeBeforeProcessingProperties, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Add Property", GUILayout.Height(25)))
        {
            scriptProperties.Add(new TemplateScriptProperty());
        }

        for (int i = 0; i < scriptProperties.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(new GUIContent("Property", "Placeholder name to be replaced in the template (e.g., JOBNAME -> #JOBNAME#)"), GUILayout.Width(80));
            scriptProperties[i].propertyName = EditorGUILayout.TextField(scriptProperties[i].propertyName, GUILayout.Width(100));

            scriptProperties[i].modifyType = (ModifyType)EditorGUILayout.EnumPopup(scriptProperties[i].modifyType, GUILayout.Width(100));

            if (scriptProperties[i].modifyType == ModifyType.DoNothing)
            {
                EditorGUI.BeginDisabledGroup(true); // Lock text field
                scriptProperties[i].storedTextValue = EditorGUILayout.TextField("", GUILayout.Width(100));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                scriptProperties[i].storedTextValue = EditorGUILayout.TextField(scriptProperties[i].storedTextValue, GUILayout.Width(100));
            }

            // Checkbox for CamelCase Transformation
            scriptProperties[i].useCamelCase = EditorGUILayout.ToggleLeft(new GUIContent("CamelCase", "Convert this placeholder to camelCase"), scriptProperties[i].useCamelCase, GUILayout.Width(90));

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                scriptProperties.RemoveAt(i);
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space();
        GUI.backgroundColor = generateButtonColor;
        if (GUILayout.Button("Generate Template", GUILayout.Height(30)))
        {
            GenerateTemplate();
        }



        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Clear Properties", GUILayout.Height(30)))
        {
            scriptProperties = new List<TemplateScriptProperty>();
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("ClearAll", GUILayout.Height(20), GUILayout.Width(60)))
        {
            templateName = "NewTemplate";
            appendToScriptName = "";
            removeBeforeProcessingProperties = true;
            targetDirectory = "";
            createMenuSubFolder = "My Template Scripts";
            subFolder = ""; // Subfolder support
            existingTemplate = null; // Drag-and-drop TextAsset
            scriptProperties = new List<TemplateScriptProperty>();
        }

        GUI.backgroundColor = new Color(0.75f, 0, 0.75f);
        EditorGUILayout.EndVertical();
        // Tab Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(activeTab == EditorTab.TemplateGenerator, "Template Generator", "Button"))
            activeTab = EditorTab.TemplateGenerator;
        if (GUILayout.Toggle(activeTab == EditorTab.ScriptToTemplate, "Convert C# Script", "Button"))
            activeTab = EditorTab.ScriptToTemplate;
        EditorGUILayout.EndHorizontal();

        DrawScriptConversionUI();
    }

    private void DrawScriptConversionUI()
    {
        GUILayout.Label("Convert C# Script to Template", EditorStyles.boldLabel);

        selectedScript = (TextAsset)EditorGUILayout.ObjectField("C# Script", selectedScript, typeof(TextAsset), false);

        if (selectedScript != null)
        {
            if (GUILayout.Button("Extract Components & Methods"))
            {
                ExtractScriptElements(selectedScript);
            }

            // Display extracted fields/methods for user customization
            GUILayout.Label("Customize Placeholders", EditorStyles.boldLabel);

            for (int i = 0; i < extractedElements.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                extractedElements[i].isEnabled = EditorGUILayout.ToggleLeft("", extractedElements[i].isEnabled, GUILayout.Width(20));
                extractedElements[i].originalName = EditorGUILayout.TextField(extractedElements[i].originalName, GUILayout.Width(120));
                extractedElements[i].placeholderName = EditorGUILayout.TextField(extractedElements[i].placeholderName, GUILayout.Width(120));
                extractedElements[i].useCamelCase = EditorGUILayout.ToggleLeft("CamelCase", extractedElements[i].useCamelCase, GUILayout.Width(90));

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    extractedElements.RemoveAt(i);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Generate Template"))
            {
                GenerateConvertedTemplate(selectedScript);
            }
        }
    }

    private void ImportTemplateProperties()
    {
        if (existingTemplate == null)
            return;

        string content = existingTemplate.text;
        Regex regex = new Regex(@"#([A-Z_]+)#"); // Detects uppercase placeholders inside # #
        MatchCollection matches = regex.Matches(content);

        // Extract template name from the text file name
        string fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(existingTemplate));

        // Remove "Template" suffix if present
        if (templateName == "" || templateName == "NewTemplate" && fileName.EndsWith("Template"))
        {
            templateName = fileName.Substring(0, fileName.Length - "Template".Length);
        }
        else if (templateName == "" || templateName == "NewTemplate")
        {
            templateName = fileName; // Default to the full filename if "Template" isn't found
        }

        foreach (Match match in matches)
        {
            string propertyName = match.Groups[1].Value;

            // Ignore SCRIPTNAME as it should not be imported
            if (propertyName == "SCRIPTNAME")
                continue;

            // Check if the property already exists
            bool alreadyExists = scriptProperties.Exists(prop => prop.propertyName == propertyName);

            if (!alreadyExists)
            {
                ModifyType modifyType = ModifyType.DoNothing;
                bool isCamelCase = false;
                string appendOrRemoveText = "";

                // If property contains "JOB", it will be an "Append Job"
                if (propertyName.Contains("JOB"))
                {
                    modifyType = ModifyType.Append;
                    appendOrRemoveText = "Job";
                }

                if (propertyName.Contains("CAMELCASE"))
                {
                    isCamelCase = true;
                }

                scriptProperties.Add(new TemplateScriptProperty
                {
                    propertyName = propertyName,
                    modifyType = modifyType,
                    storedTextValue = appendOrRemoveText,
                    useCamelCase = isCamelCase
                });
            }
        }

        Debug.Log($"Imported {matches.Count} properties from template. Template Name set to: {templateName}");
    }

    private void GenerateTemplate()
    {
        string folderPath = GeneratedTemplatesPath;
        if (!string.IsNullOrEmpty(subFolder))
        {
            folderPath = Path.Combine(GeneratedTemplatesPath, subFolder);
        }

        string textTemplateFolder = Path.Combine(folderPath, "Text Templates");
        string menuFolder = folderPath + "/EditorMenuScripts";

        if (!Directory.Exists(textTemplateFolder))
            Directory.CreateDirectory(textTemplateFolder);

        if (!Directory.Exists(menuFolder))
            Directory.CreateDirectory(menuFolder);

        AssetDatabase.Refresh();

        string templateFileName = templateName;
        string menuFileName = templateName;

        if (!templateName.EndsWith("Template"))
        {
            templateFileName += "Template";
            menuFileName += "TemplateMenu";
        }
        templateFileName += ".txt";
        menuFileName += ".cs";

        string templatePath = Path.Combine(textTemplateFolder, templateFileName);
        string menuPath = Path.Combine(menuFolder, menuFileName);

        if (existingTemplate != null)
        {
            File.WriteAllText(templatePath, existingTemplate.text);
        }
        else
        {
            string templateContent = "using Unity.Entities;\n\n";
            templateContent += "public struct #SCRIPTNAME# : IComponentData\n{\n}\n";
            File.WriteAllText(templatePath, templateContent);
        }

        AssetDatabase.ImportAsset(templatePath);

        string menuContent = GenerateMenuScript(templateName, templatePath, menuPath);
        File.WriteAllText(menuPath, menuContent);
        AssetDatabase.ImportAsset(menuPath);
         
        Debug.Log($"Template and Menu script generated at {folderPath}");
    }

    private string GetRelevantPropReplacement(bool usesCamelCase, string replacement)
    {
        return usesCamelCase ? char.ToLower(replacement[0]) + replacement.Substring(1) : replacement;
    }

    private string GeneratePropertyReplacements()
    {
        string replacements = "";

        foreach (var prop in scriptProperties)
        {
            string processedScriptName = "scriptName";

            // Apply removeBeforeProcessingProperties if enabled
            if (removeBeforeProcessingProperties && !string.IsNullOrEmpty(appendToScriptName))
            {
                processedScriptName = $"scriptName.Replace(\"{appendToScriptName}\", \"\")";
            }

            if (prop.modifyType == ModifyType.Append)
            {
                replacements += $@"
            template = template.Replace(""#{prop.propertyName}#"", {GetRelevantPropReplacement(prop.useCamelCase, processedScriptName)} + ""{prop.storedTextValue}"");";
            }
            else if (prop.modifyType == ModifyType.Remove)
            {
                replacements += $@"
            template = template.Replace(""#{prop.propertyName}#"", {GetRelevantPropReplacement(prop.useCamelCase, processedScriptName)}.Replace(""{prop.storedTextValue}"", """"));";
            }
            else if (prop.modifyType == ModifyType.DoNothing)
            {
                // DoNothing still replaces placeholders but does not modify scriptName
                replacements += $@"
            template = template.Replace(""#{prop.propertyName}#"", {GetRelevantPropReplacement(prop.useCamelCase, processedScriptName)});";
            }
            else if(prop.modifyType == ModifyType.ReplaceAll)
            {
                replacements += $@"
            template = template.Replace(""#{prop.propertyName}#"", ""{GetRelevantPropReplacement(prop.useCamelCase, prop.storedTextValue)}"");";
            }
        }

        return replacements;
    }

    string GeneratePathGettingFunctionIfNeeded()
    {
        if (targetDirectory != "")
        {
            return "";
        }
        else
        {
            return $@"
            
    private static string GetSelectedPathOrFallback()
    {{
        if (Selection.activeObject == null)
        {{
            EditorUtility.DisplayDialog(""Error"", ""No project window is open. Please open the Project Window and select a folder."", ""OK"");
            return null;
        }}

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {{
            EditorUtility.DisplayDialog(""Error"", ""Please select a valid folder in the Project Window."", ""OK"");
            return null;
        }}

        return path;
    }}

";
        }
    }


    private string GenerateMenuScript(string scriptName, string templatePath, string menuPath)
    {
        string menuClassName = scriptName + "TemplateMenu";
        string defaultFileName = "New" + scriptName + ".cs";

        return $@"
using System.IO;
using UnityEditor;
using UnityEngine;

public static class {menuClassName}
{{
    private const string TemplatePath = @""{templatePath}"";
    private const string DefaultFileName = ""{defaultFileName}""; 

    [MenuItem(""Assets/Create/{(!string.IsNullOrEmpty(createMenuSubFolder) ? $@"{createMenuSubFolder}/" : "Assets/")}{scriptName}"", false, 80)]
    private static void CreateScript()
    {{
        string targetDirectory = {(!string.IsNullOrEmpty(targetDirectory) ? $@"""Assets/{targetDirectory}""" : "GetSelectedPathOrFallback()")};

        if (string.IsNullOrEmpty(targetDirectory))
        {{
            return; // Error message already handled in GetSelectedPathOrFallback()
        }}
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder(targetDirectory))
        {{
            Directory.CreateDirectory(targetDirectory);
            AssetDatabase.Refresh();
        }}

        string path = Path.Combine(targetDirectory, DefaultFileName + "".cs"");

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            0,
            ScriptableObject.CreateInstance<CreateScriptAction>(),
            path,
            null,
            TemplatePath
        );
    }}
    {GeneratePathGettingFunctionIfNeeded()}
    class CreateScriptAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {{
        public override void Action(int instanceId, string path, string templatePath)
        {{
            string scriptName = Path.GetFileNameWithoutExtension(path);
            
            // Ensure {appendToScriptName} is appended if not already present
            if (!scriptName.EndsWith(""{appendToScriptName}""))
            {{  
                scriptName += ""{appendToScriptName}"";
                path = Path.Combine(Path.GetDirectoryName(path), scriptName + "".cs"");
            }}

            // Read template file
            string template = File.ReadAllText(templatePath);

            // Replace placeholders
            template = template.Replace(""#SCRIPTNAME#"", scriptName);

            {GeneratePropertyReplacements()}

            // Write file
            File.WriteAllText(path, template);
            AssetDatabase.ImportAsset(path);

            // Select the newly created file in the Project window
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }}
    }}
}}";
    }

    [System.Serializable]
    public class TemplateScriptProperty
    {
        public string propertyName = "";
        public ModifyType modifyType = ModifyType.DoNothing;
        public string storedTextValue = "";
        public bool useCamelCase = false;
    }

    public enum ModifyType
    {
        Append,
        Remove,
        DoNothing,
        ReplaceAll
    }

    [System.Serializable]
    public class TemplateEditorSaveData
    {
        public string templateName;
        public string appendToScriptName;
        public bool removeBeforeProcessingProperties;
        public string targetDirectory;
        public string createMenuSubFolder;
        public string subFolder;
        public List<TemplateScriptProperty> scriptProperties;
    }



    private enum EditorTab
    {
        TemplateGenerator,
        ScriptToTemplate
    }

    private void ExtractScriptElements(TextAsset scriptFile)
    {
        extractedElements.Clear();

        string scriptContent = scriptFile.text;

        // Extract class name
        string className = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(scriptFile));

        // Extract fields
        Regex fieldRegex = new Regex(@"(\w+)\s+(\w+);");
        MatchCollection fieldMatches = fieldRegex.Matches(scriptContent);

        foreach (Match match in fieldMatches)
        {
            string type = match.Groups[1].Value;
            string name = match.Groups[2].Value;
            extractedElements.Add(new ScriptElement
            {
                isEnabled = true,
                originalName = name,
                placeholderName = "COMPONENTADDED",
                useCamelCase = false
            });
        }

        // Extract method names
        Regex methodRegex = new Regex(@"\bvoid\s+(\w+)\(");
        MatchCollection methodMatches = methodRegex.Matches(scriptContent);

        foreach (Match match in methodMatches)
        {
            string methodName = match.Groups[1].Value;

            // Ignore built-in Unity methods
            if (methodName != "Start" && methodName != "Update")
            {
                extractedElements.Add(new ScriptElement
                {
                    isEnabled = true,
                    originalName = methodName,
                    placeholderName = methodName.ToUpper(),
                    useCamelCase = false
                });
            }
        }
    }

    private void GenerateConvertedTemplate(TextAsset scriptFile)
    {
        string scriptContent = scriptFile.text;

        // Replace class name
        scriptContent = Regex.Replace(scriptContent, @"\b" + Path.GetFileNameWithoutExtension(scriptFile.name) + @"\b", "#SCRIPTNAME#");

        foreach (var element in extractedElements)
        {
            if (element.isEnabled)
            {
                string replacement = $"#{element.placeholderName}#";
                if (element.useCamelCase)
                {
                    string camelCaseReplacement = $"#{element.placeholderName}_CAMELCASE#";
                    scriptContent = scriptContent.Replace(element.originalName, camelCaseReplacement);
                }
                else
                {
                    scriptContent = scriptContent.Replace(element.originalName, replacement);
                }
            }
        }

        // Save as a template file
        string savePath = Path.Combine(GeneratedTemplatesPath, Path.GetFileNameWithoutExtension(scriptFile.name) + "Template.txt");
        File.WriteAllText(savePath, scriptContent);
        AssetDatabase.Refresh();

        Debug.Log($"Template saved: {savePath}");
    }

    [System.Serializable]
    public class ScriptElement
    {
        public bool isEnabled = true; // Whether to include this element
        public string originalName = "";
        public string placeholderName = "";
        public bool useCamelCase = false;
    }

}