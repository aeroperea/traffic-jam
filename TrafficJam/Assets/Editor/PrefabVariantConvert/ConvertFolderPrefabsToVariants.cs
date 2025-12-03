using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConvertFolderPrefabsToVariants : EditorWindow
{
    // anchor: fields
    private DefaultAsset _folder;
    private GameObject _basePrefab;
    private string _baseName = "_BaseParent";
    private bool _includeSubfolders = false;
    private bool _useEmptyParentBase = false;     // false = shared-root base, true = empty-parent base
    private bool _deleteOriginalPrefabs = true;   // delete old non-variant prefabs after rebind

    [MenuItem("Tools/Prefabs/Convert Folder To Variants (Scene Rebind)…")]
    private static void Open() => GetWindow<ConvertFolderPrefabsToVariants>("Folder → Prefab Variants (Rebind)");

    private void OnGUI()
    {
        GUILayout.Label("source folder", EditorStyles.boldLabel);
        _folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", _folder, typeof(DefaultAsset), false);
        _includeSubfolders = EditorGUILayout.ToggleLeft("Include Subfolders", _includeSubfolders);

        GUILayout.Space(8);
        GUILayout.Label("mode", EditorStyles.boldLabel);
        _useEmptyParentBase = EditorGUILayout.ToggleLeft("Use Empty Parent Base (original becomes child)", _useEmptyParentBase);

        GUILayout.Space(8);
        GUILayout.Label("base prefab", EditorStyles.boldLabel);
        _basePrefab = (GameObject)EditorGUILayout.ObjectField("Base Prefab (optional)", _basePrefab, typeof(GameObject), false);
        _baseName = EditorGUILayout.TextField(new GUIContent("Base Name (if creating base)"),
            string.IsNullOrWhiteSpace(_baseName) ? "_BaseParent" : _baseName);

        GUILayout.Space(8);
        _deleteOriginalPrefabs = EditorGUILayout.ToggleLeft("Delete Original Prefabs After Rebind", _deleteOriginalPrefabs);

        GUILayout.Space(12);
        using (new EditorGUI.DisabledScope(_folder == null))
        {
            if (GUILayout.Button("Convert Prefabs To Variants (Create New Assets + Rebind Scene)"))
            {
                var modeText = _useEmptyParentBase
                    ? "create variant prefabs that use an empty parent base and rebind scene instances to them"
                    : "create variant prefabs that share a root base (no extra parent) and rebind scene instances to them";

                var deleteText = _deleteOriginalPrefabs
                    ? "\n\noriginal prefab assets will be deleted after scenes are rebound."
                    : "\n\noriginal prefab assets will be kept.";

                if (EditorUtility.DisplayDialog("Confirm Conversion",
                    $"{modeText}.{deleteText}\n\ncontinue?",
                    "Convert", "Cancel"))
                {
                    ConvertNow();
                }
            }
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "this tool does not modify existing prefab assets in-place.\n" +
            "instead it:\n" +
            "1) creates new variant prefabs (name__Variant.prefab) that share a base prefab.\n" +
            "2) walks loaded scenes and replaces instances of the originals with instances of the variants, preserving local trs.\n" +
            "3) optionally deletes the original (non-variant) prefabs once scenes are rebound.\n",
            MessageType.Info);
    }

    private bool CanRun()
    {
        return _folder != null;
    }

    private GameObject EnsureSharedRootBasePrefab(string folderPath)
    {
        if (_basePrefab != null) return _basePrefab;

        var go = new GameObject(_baseName.Trim());
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();

        var basePath = Path.Combine(folderPath, _baseName.Trim() + ".prefab").Replace('\\', '/');
        basePath = AssetDatabase.GenerateUniqueAssetPath(basePath);

        var created = PrefabUtility.SaveAsPrefabAsset(go, basePath);
        Object.DestroyImmediate(go);

        _basePrefab = created;
        AssetDatabase.Refresh();
        return _basePrefab;
    }

    private GameObject EnsureEmptyParentBasePrefab(string folderPath)
    {
        if (_basePrefab != null) return _basePrefab;

        var go = new GameObject(_baseName.Trim());

        var basePath = Path.Combine(folderPath, _baseName.Trim() + ".prefab").Replace('\\', '/');
        basePath = AssetDatabase.GenerateUniqueAssetPath(basePath);

        var created = PrefabUtility.SaveAsPrefabAsset(go, basePath);
        Object.DestroyImmediate(go);

        _basePrefab = created;
        AssetDatabase.Refresh();
        return _basePrefab;
    }

    private static string GetVariantPath(string originalPath)
    {
        var dir = Path.GetDirectoryName(originalPath).Replace('\\', '/');
        var file = Path.GetFileNameWithoutExtension(originalPath);
        var ext = Path.GetExtension(originalPath);
        var variantFile = file + "__Variant" + ext;
        var full = Path.Combine(dir, variantFile).Replace('\\', '/');
        return AssetDatabase.GenerateUniqueAssetPath(full);
    }

    private void ConvertNow()
    {
        if (!CanRun())
            return;

        var folderPath = AssetDatabase.GetAssetPath(_folder);

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        var prefabPaths = prefabGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".prefab"))
            .Where(p => _includeSubfolders || Path.GetDirectoryName(p).Replace('\\', '/') == folderPath)
            .ToList();

        if (prefabPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Nothing To Convert", "no prefabs found in the chosen location.", "OK");
            return;
        }

        GameObject baseObj = _useEmptyParentBase
            ? EnsureEmptyParentBasePrefab(folderPath)
            : EnsureSharedRootBasePrefab(folderPath);

        var basePath = AssetDatabase.GetAssetPath(baseObj);

        // step 1: create variant assets
        var variantMap = new Dictionary<string, string>(); // originalPath -> variantPath

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < prefabPaths.Count; i++)
            {
                var srcPath = prefabPaths[i];
                if (string.Equals(srcPath, basePath, System.StringComparison.Ordinal))
                    continue; // skip base itself

                EditorUtility.DisplayProgressBar("Creating Variant Prefabs", srcPath, (float)i / prefabPaths.Count);

                var stagedRoot = PrefabUtility.LoadPrefabContents(srcPath);
                if (stagedRoot == null)
                    continue;

                var variantPath = GetVariantPath(srcPath);

                if (_useEmptyParentBase)
                    CreateVariant_EmptyParent(baseObj, stagedRoot, variantPath);
                else
                    CreateVariant_SharedRoot(baseObj, stagedRoot, variantPath);

                PrefabUtility.UnloadPrefabContents(stagedRoot);

                variantMap[srcPath] = variantPath;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        if (variantMap.Count == 0)
        {
            Debug.LogWarning("no variants were created (perhaps only base prefab was in the folder).");
            return;
        }

        // step 2: rebind scene instances from original prefabs to new variant prefabs
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogError("scene rebind cancelled; user did not save scenes");
            return;
        }

        RebindSceneInstances(variantMap);

        // step 3: optionally delete original prefabs
        if (_deleteOriginalPrefabs)
        {
            DeleteOriginalPrefabs(variantMap, basePath);
        }
    }

    // anchor: create_variant_empty_parent
    private static void CreateVariant_EmptyParent(GameObject baseObj, GameObject stagedRoot, string variantPath)
    {
        var baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(baseObj);
        baseInstance.name = stagedRoot.name;

        var bt = baseInstance.transform;
        bt.localPosition = Vector3.zero;
        bt.localRotation = Quaternion.identity;
        bt.localScale = Vector3.one;

        var child = Object.Instantiate(stagedRoot);
        child.name = stagedRoot.name;
        child.transform.SetParent(bt, false);

        PrefabUtility.SaveAsPrefabAsset(baseInstance, variantPath);
        Object.DestroyImmediate(baseInstance);
    }

    // anchor: create_variant_shared_root
    private static void CreateVariant_SharedRoot(GameObject baseObj, GameObject stagedRoot, string variantPath)
    {
        var baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(baseObj);
        baseInstance.name = stagedRoot.name;

        var bt = baseInstance.transform;
        bt.localPosition = stagedRoot.transform.localPosition;
        bt.localRotation = stagedRoot.transform.localRotation;
        bt.localScale = stagedRoot.transform.localScale;

        var srcComponents = stagedRoot.GetComponents<Component>();
        foreach (var src in srcComponents)
        {
            if (src is Transform) continue;

            var dst = baseInstance.GetComponent(src.GetType());
            if (dst == null) continue;

            EditorUtility.CopySerializedIfDifferent(src, dst);
        }

        PrefabUtility.SaveAsPrefabAsset(baseInstance, variantPath);
        Object.DestroyImmediate(baseInstance);
    }

    // anchor: rebind_scene_instances
    private static void RebindSceneInstances(Dictionary<string, string> variantMap)
    {
        var variantPrefabs = new Dictionary<string, GameObject>(); // variantPath -> prefab
        foreach (var kvp in variantMap)
        {
            var variantPath = kvp.Value;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            if (prefab != null)
                variantPrefabs[variantPath] = prefab;
        }

        int replacedCount = 0;

        for (int si = 0; si < SceneManager.sceneCount; si++)
        {
            var scene = SceneManager.GetSceneAt(si);
            if (!scene.isLoaded) continue;

            var dirty = false;
            var roots = scene.GetRootGameObjects();
            var stack = new Stack<Transform>();

            foreach (var r in roots)
            {
                stack.Push(r.transform);
                while (stack.Count > 0)
                {
                    var t = stack.Pop();
                    var go = t.gameObject;

                    if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.NotAPrefab)
                        continue;

                    var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                    if (instanceRoot == null || instanceRoot != go)
                        continue;

                    var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
                    if (!variantMap.TryGetValue(assetPath, out var variantPath))
                        continue;

                    if (!variantPrefabs.TryGetValue(variantPath, out var variantPrefab))
                        continue;

                    var parent = instanceRoot.transform.parent;
                    var siblingIndex = instanceRoot.transform.GetSiblingIndex();
                    var localPos = instanceRoot.transform.localPosition;
                    var localRot = instanceRoot.transform.localRotation;
                    var localScale = instanceRoot.transform.localScale;
                    var name = instanceRoot.name;
                    var activeSelf = instanceRoot.activeSelf;

                    Object.DestroyImmediate(instanceRoot);

                    GameObject newRoot;
                    if (parent == null)
                        newRoot = (GameObject)PrefabUtility.InstantiatePrefab(variantPrefab, scene);
                    else
                        newRoot = (GameObject)PrefabUtility.InstantiatePrefab(variantPrefab, parent);

                    newRoot.name = name;
                    var tr = newRoot.transform;
                    tr.SetSiblingIndex(siblingIndex);
                    tr.localPosition = localPos;
                    tr.localRotation = localRot;
                    tr.localScale = localScale;
                    newRoot.SetActive(activeSelf);

                    replacedCount++;
                    dirty = true;
                }
            }

            if (dirty)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"rebound {replacedCount} prefab instance(s) to new variant prefabs.");
    }

    // anchor: delete_originals
    private static void DeleteOriginalPrefabs(Dictionary<string, string> variantMap, string basePath)
    {
        int deleted = 0;

        foreach (var srcPath in variantMap.Keys)
        {
            if (string.Equals(srcPath, basePath, System.StringComparison.Ordinal))
                continue;

            if (AssetDatabase.DeleteAsset(srcPath))
                deleted++;
        }

        if (deleted > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log($"deleted {deleted} original prefab asset(s) after rebind.");
        }
        else
        {
            Debug.Log("no original prefabs deleted (either none, or delete option was off).");
        }
    }
}
