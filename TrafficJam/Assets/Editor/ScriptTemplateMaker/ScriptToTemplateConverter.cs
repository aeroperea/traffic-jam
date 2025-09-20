using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ScriptToTemplateConverter
{
    [MenuItem("Assets/Create/Convert C# Script to Template", false, 81)]
    private static void ConvertScriptToTemplate()
    {
        string path = GetSelectedScriptPath();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("No C# script selected.");
            return;
        }

        string scriptContent = File.ReadAllText(path);
        string className = Path.GetFileNameWithoutExtension(path);

        // Step 1: Extract fields and methods
        string convertedContent = ProcessScriptContent(scriptContent, className);

        // Step 2: Save to a template file
        string templateFilePath = Path.Combine(Path.GetDirectoryName(path), className + "Template.txt");
        File.WriteAllText(templateFilePath, convertedContent);
        AssetDatabase.Refresh();

        Debug.Log($"Converted script saved as template: {templateFilePath}");
    }

    private static string GetSelectedScriptPath()
    {
        Object selected = Selection.activeObject;
        if (selected == null) return null;

        string path = AssetDatabase.GetAssetPath(selected);
        return path.EndsWith(".cs") ? path : null;
    }

    private static string ProcessScriptContent(string scriptContent, string className)
    {
        // Replace class name with SCRIPTNAME placeholder
        string modifiedContent = Regex.Replace(scriptContent, @"\b" + className + @"\b", "#SCRIPTNAME#");

        // Match and replace component fields (Rigidbody rb → #COMPONENTADDED# #COMPONENTADDED_CAMELCASE#;)
        modifiedContent = Regex.Replace(modifiedContent, @"(\w+)\s+(\w+);", match =>
        {
            string componentType = match.Groups[1].Value;
            string fieldName = match.Groups[2].Value;
            string camelCaseField = char.ToLower(fieldName[0]) + fieldName.Substring(1);

            return $"#COMPONENTADDED# #COMPONENTADDED_CAMELCASE#;";
        });

        // Replace GetComponent<T>() calls
        modifiedContent = Regex.Replace(modifiedContent, @"GetComponent<(\w+)>", match =>
        {
            return "GetComponent<#COMPONENTADDED#>";
        });

        // Match and replace method names (StarterMethod → #STARTERMETHOD#)
        modifiedContent = Regex.Replace(modifiedContent, @"(\w+)\s*\(", match =>
        {
            string methodName = match.Groups[1].Value;
            if (methodName == "Start" || methodName == "Update") return match.Value; // Keep built-in methods

            return $"#{methodName.ToUpper()}#(";
        });

        return modifiedContent;
    }
}
