using System.IO;
using UnityEditor;
using UnityEngine;

public static class ISystemTemplateMenu
{
    private const string TemplatePath = "Assets/Editor/ScriptTemplateMaker/GeneratedTemplates/DOTS Templates/Text Templates/ISystemTemplate.txt";
    private const string DefaultFileName = "NewSystem.cs";

    [MenuItem("Assets/Create/My DOTS Scripts/ISystem", false, 80)] // Adds to 'Create' menu
    private static void CreateISystemScript()
    {
        string targetDirectory = "Assets/Scripts/DOTS/Systems/";

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
            AssetDatabase.Refresh();
        }

        string path = Path.Combine(targetDirectory, DefaultFileName + ".cs");

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            0,
            ScriptableObject.CreateInstance<CreateISystemScriptAction>(),
            path,
            null,
            TemplatePath
        );
    }
}

// Handles file creation after user enters script name
class CreateISystemScriptAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
{
    public override void Action(int instanceId, string path, string templatePath)
    {
        string scriptName = Path.GetFileNameWithoutExtension(path);

        // Ensure "System" is appended if not already present
        if (!scriptName.EndsWith("System"))
        {
            scriptName += "System";
            path = Path.Combine(Path.GetDirectoryName(path), scriptName + ".cs");
        }

        // Determine component name (remove "System")
        string possibleComponentName = scriptName.Replace("System", "");
        string possibleComponentNameCamelCase = char.ToLower(possibleComponentName[0]) + possibleComponentName.Substring(1);

        // Determine job name
        string jobName = possibleComponentName + "Job";
        string jobNameCamelCase = char.ToLower(jobName[0]) + jobName.Substring(1);

        // Read template
        string template = File.ReadAllText(templatePath);

        // Replace placeholders in template
        template = template.Replace("#SCRIPTNAME#", scriptName);
        template = template.Replace("#POSSIBLECOMPONENTNAME#", possibleComponentName);
        template = template.Replace("#POSSIBLECOMPONENTNAME_CAMELCASE#", possibleComponentNameCamelCase);
        template = template.Replace("#JOBNAME#", jobName);
        template = template.Replace("#JOBNAME_CAMELCASE#", jobNameCamelCase);

        // Write file
        File.WriteAllText(path, template);
        AssetDatabase.ImportAsset(path);

        // Select the newly created file in the Project window
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
