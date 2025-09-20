using System.IO;
using UnityEditor;
using UnityEngine;

public static class AuthoringBaker
{
    private const string TemplatePath = "Assets/Editor/ScriptTemplateMaker/GeneratedTemplates/DOTS Templates/Text Templates/BakerAuthoringTemplate.txt";
    private const string DefaultFileName = "NewAuthoringBaker.cs";

    [MenuItem("Assets/Create/My DOTS Scripts/AuthoringBaker", false, 80)] // Adds to 'Create' menu
    private static void CreateBakerScript()
    {
        string targetDirectory = "Assets/Scripts/DOTS/Authoring/";

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
            AssetDatabase.Refresh();
        }

        string path = Path.Combine(targetDirectory, DefaultFileName + ".cs");

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            0,
            ScriptableObject.CreateInstance<CreateBakerScriptAction>(),
            path,
            null,
            TemplatePath
        );
    }
}

// This handles the file creation after the user enters the script name
class CreateBakerScriptAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
{
    public override void Action(int instanceId, string path, string templatePath)
    {
        string generatedScriptName = Path.GetFileNameWithoutExtension(path);

        // Ensure "Authoring" is appended if not already present
        if (!generatedScriptName.EndsWith("Authoring"))
        {
            generatedScriptName += "Authoring";
            path = Path.Combine(Path.GetDirectoryName(path), generatedScriptName + ".cs");
        }

        // Read template
        string template = File.ReadAllText(templatePath);

        // Remove "Authoring" from name to generate component name
        string componentName = generatedScriptName.Replace("Authoring", "");

        // Replace placeholders in template
        template = template.Replace("#SCRIPTNAME#", generatedScriptName);
        template = template.Replace("#COMPONENTNAME#", componentName);

        // Write file
        File.WriteAllText(path, template);
        AssetDatabase.ImportAsset(path);

        // Select the newly created file in the Project window
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
