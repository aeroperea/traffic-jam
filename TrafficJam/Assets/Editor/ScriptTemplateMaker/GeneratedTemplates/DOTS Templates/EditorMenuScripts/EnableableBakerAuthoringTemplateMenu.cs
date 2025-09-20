
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EnableableBakerAuthoringTemplateMenu
{
    private const string TemplatePath = @"Assets\Editor\ScriptTemplateMaker/GeneratedTemplates\DOTS Templates\Text Templates\EnableableBakerAuthoringTemplate.txt";
    private const string DefaultFileName = "NewEnableableBakerAuthoring.cs"; 

    [MenuItem("Assets/Create/My DOTS Scripts/EnableableBakerAuthoring", false, 80)]
    private static void CreateScript()
    {
        string targetDirectory = "Assets/Scripts/DOTS/Authoring";

        if (string.IsNullOrEmpty(targetDirectory))
        {
            return; // Error message already handled in GetSelectedPathOrFallback()
        }
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
            AssetDatabase.Refresh();
        }

        string path = Path.Combine(targetDirectory, DefaultFileName + ".cs");

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            0,
            ScriptableObject.CreateInstance<CreateScriptAction>(),
            path,
            null,
            TemplatePath
        );
    }
    
    class CreateScriptAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string path, string templatePath)
        {
            string scriptName = Path.GetFileNameWithoutExtension(path);
            
            // Ensure Authoring is appended if not already present
            if (!scriptName.EndsWith("Authoring"))
            {  
                scriptName += "Authoring";
                path = Path.Combine(Path.GetDirectoryName(path), scriptName + ".cs");
            }

            // Read template file
            string template = File.ReadAllText(templatePath);

            // Replace placeholders
            template = template.Replace("#SCRIPTNAME#", scriptName);

            
            template = template.Replace("#COMPONENTNAME#", scriptName.Replace("Authoring", ""));

            // Write file
            File.WriteAllText(path, template);
            AssetDatabase.ImportAsset(path);

            // Select the newly created file in the Project window
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}