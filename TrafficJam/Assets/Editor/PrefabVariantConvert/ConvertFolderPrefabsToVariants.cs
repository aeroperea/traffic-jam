using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ConvertFolderPrefabsToVariants : EditorWindow
{
    private DefaultAsset _folder;
    private GameObject _basePrefab;              // optional; created if null
    private string _baseName = "_BaseParent";    // name when creating
    private bool _includeSubfolders = false;

    [MenuItem("Tools/Prefabs/Convert Folder To Variants…")]
    private static void Open() => GetWindow<ConvertFolderPrefabsToVariants>("Folder → Prefab Variants");

    private void OnGUI()
    {
        GUILayout.Label("source", EditorStyles.boldLabel);
        _folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", _folder, typeof(DefaultAsset), false);
        _includeSubfolders = EditorGUILayout.ToggleLeft("Include Subfolders", _includeSubfolders);

        GUILayout.Space(8);
        GUILayout.Label("base", EditorStyles.boldLabel);
        _basePrefab = (GameObject)EditorGUILayout.ObjectField("Base Prefab (empty parent)", _basePrefab, typeof(GameObject), false);
        _baseName = EditorGUILayout.TextField(new GUIContent("Base Name (if creating)"), string.IsNullOrWhiteSpace(_baseName) ? "_BaseParent" : _baseName);

        GUILayout.Space(12);
        using (new EditorGUI.DisabledScope(!CanRun()))
        {
            if (GUILayout.Button("Convert Prefabs To Variants"))
            {
                if (EditorUtility.DisplayDialog("Confirm Conversion",
                    "This will overwrite each prefab with a variant of the base prefab while keeping references.\n\nContinue?",
                    "Convert", "Cancel"))
                {
                    ConvertNow();
                }
            }
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox("if no base prefab is supplied, an empty one will be created in the selected folder using the name above.", MessageType.Info);
    }

    private bool CanRun()
    {
        if (_folder == null) return false;
        if (_basePrefab != null)
        {
            var path = AssetDatabase.GetAssetPath(_basePrefab);
            return PrefabUtility.IsPartOfPrefabAsset(_basePrefab) && path.EndsWith(".prefab");
        }
        return !string.IsNullOrWhiteSpace(_baseName);
    }

    private GameObject EnsureBasePrefab(string folderPath)
    {
        if (_basePrefab != null) return _basePrefab;

        var desiredPath = Path.Combine(folderPath, _baseName.Trim() + ".prefab").Replace('\\', '/');
        desiredPath = AssetDatabase.GenerateUniqueAssetPath(desiredPath);

        var temp = new GameObject(_baseName.Trim());
        var created = PrefabUtility.SaveAsPrefabAsset(temp, desiredPath);
        Object.DestroyImmediate(temp);
        _basePrefab = created;
        AssetDatabase.Refresh();
        return _basePrefab;
    }

    private void ConvertNow()
    {
        var folderPath = AssetDatabase.GetAssetPath(_folder);

        // create base if missing
        var baseObj = EnsureBasePrefab(folderPath);
        var basePath = AssetDatabase.GetAssetPath(baseObj);
        var baseGuid = AssetDatabase.AssetPathToGUID(basePath);

        var allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        var targets = allPrefabGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".prefab"))
            .Where(p => _includeSubfolders || Path.GetDirectoryName(p).Replace('\\', '/') == folderPath)
            .Where(p => AssetDatabase.AssetPathToGUID(p) != baseGuid)
            .ToArray();

        if (targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Nothing To Convert", "no prefabs found in the chosen location.", "OK");
            return;
        }

        try
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogError("conversion cancelled; user did not save scenes");
                return;
            }

            AssetDatabase.StartAssetEditing();

            int changed = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                var path = targets[i];
                EditorUtility.DisplayProgressBar("Converting To Variants", path, (float)i / targets.Length);

                // stage: open prefab contents (isolated editing stage)
                var stagedRoot = PrefabUtility.LoadPrefabContents(path);
                if (stagedRoot == null)
                    continue;

                // clone the staged root into the SCENE (not the contents stage)
                var cloned = (GameObject)Object.Instantiate(stagedRoot);
                cloned.name = "Model";

                // IMPORTANT: unload the staged contents BEFORE parenting/overwriting
                PrefabUtility.UnloadPrefabContents(stagedRoot);

                // build an instance of base (keeps link so saving creates a VARIANT)
                var baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(baseObj);
                baseInstance.name = Path.GetFileNameWithoutExtension(path);

                // parent the clone under base
                cloned.transform.SetParent(baseInstance.transform, false);
                cloned.transform.SetAsFirstSibling();
                cloned.transform.localPosition = Vector3.zero;
                cloned.transform.localRotation = Quaternion.identity;
                cloned.transform.localScale = Vector3.one;

                // save the base instance as the SAME asset path -> creates/overwrites as a VARIANT
                PrefabUtility.SaveAsPrefabAsset(baseInstance, path, out bool success);

                // cleanup temp scene objects
                Object.DestroyImmediate(baseInstance);
                if (cloned != null) Object.DestroyImmediate(cloned);

                if (success) changed++;
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"converted {changed} prefab(s) to variants of '{baseObj.name}'.");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}
