using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ConvertFolderPrefabsToVariants : EditorWindow
{
    // minimal ui state
    private DefaultAsset _folder;
    private GameObject _basePrefab;
    private bool _includeSubfolders = false;

    [MenuItem("Tools/Prefabs/Convert Folder To Variants…")]
    private static void Open() => GetWindow<ConvertFolderPrefabsToVariants>("Folder → Prefab Variants");

    private void OnGUI()
    {
        // ui
        GUILayout.Label("source", EditorStyles.boldLabel);
        _folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", _folder, typeof(DefaultAsset), false);
        _includeSubfolders = EditorGUILayout.ToggleLeft("Include Subfolders", _includeSubfolders);

        GUILayout.Space(8);
        GUILayout.Label("base", EditorStyles.boldLabel);
        _basePrefab = (GameObject)EditorGUILayout.ObjectField("Base Prefab (empty parent)", _basePrefab, typeof(GameObject), false);

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
        EditorGUILayout.HelpBox("the base prefab should be an empty transform that will become the parent of each old root.\n" +
                                "existing asset paths are preserved so references in scenes/prefabs remain valid.", MessageType.Info);
    }

    private bool CanRun()
    {
        if (_folder == null || _basePrefab == null) return false;
        var path = AssetDatabase.GetAssetPath(_basePrefab);
        return PrefabUtility.IsPartOfPrefabAsset(_basePrefab) && path.EndsWith(".prefab");
    }

    private void ConvertNow()
    {
        var basePath = AssetDatabase.GetAssetPath(_basePrefab);
        var baseGuid = AssetDatabase.AssetPathToGUID(basePath);

        var folderPath = AssetDatabase.GetAssetPath(_folder);
        var search = _includeSubfolders ? "t:Prefab" : "t:Prefab";
        var allPrefabGuids = AssetDatabase.FindAssets(search, new[] { folderPath });

        // filter: only prefabs under folder (respect subfolder toggle) and not the base itself
        var targets = allPrefabGuids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => p.EndsWith(".prefab"))
            .Where(p => _includeSubfolders || Path.GetDirectoryName(p).Replace('\\','/') == folderPath)
            .Where(p => AssetDatabase.AssetPathToGUID(p) != baseGuid)
            .ToArray();

        if (targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Nothing To Convert", "no prefabs found in the chosen location.", "OK");
            return;
        }

        try
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogError("Conversion cancelled user didnt save scene");
                return;
            }

            AssetDatabase.StartAssetEditing();

            int changed = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                var path = targets[i];
                EditorUtility.DisplayProgressBar("Converting To Variants", path, (float)i / targets.Length);

                // open the old prefab contents (editable, not an instance)
                var oldRoot = PrefabUtility.LoadPrefabContents(path);
                if (oldRoot == null)
                    continue;

                // quick skip if it already looks like a variant of base (heuristic by comparing parent prefab)
                var existingParent = PrefabUtility.GetCorrespondingObjectFromSource(oldRoot);
                // not reliable for contents root; do explicit variant test:
                bool alreadyVariant = PrefabUtility.IsAnyPrefabInstanceRoot(oldRoot) &&
                                      PrefabUtility.GetPrefabAssetType(oldRoot) == PrefabAssetType.Variant;

                // build an instance of base (keeps link so saving creates a VARIANT)
                var baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(_basePrefab);
                baseInstance.name = Path.GetFileNameWithoutExtension(path);

                // reparent the old prefab contents under base instance
                oldRoot.transform.SetParent(baseInstance.transform, false);
                oldRoot.transform.SetAsFirstSibling();
                oldRoot.name = "Model";

                // ensure zeroed child
                oldRoot.transform.localPosition = Vector3.zero;
                oldRoot.transform.localRotation = Quaternion.identity;
                oldRoot.transform.localScale = Vector3.one;

                // save the base instance as the SAME asset path -> creates/overwrites as a VARIANT
                PrefabUtility.SaveAsPrefabAsset(baseInstance, path, out bool success);

                // cleanup temp objects
                PrefabUtility.UnloadPrefabContents(oldRoot);
                DestroyImmediate(baseInstance);

                if (success) changed++;
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"converted {changed} prefab(s) to variants of '{_basePrefab.name}'.");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}
