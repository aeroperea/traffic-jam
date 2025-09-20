using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConvertFolderPrefabsToVariants : EditorWindow
{
    private DefaultAsset _folder;
    private GameObject _basePrefab;           // optional; created if null
    private string _baseName = "_BaseParent";
    private bool _includeSubfolders = false;

    // snapshot of scene instances to restore after prefab overwrite
    private class InstanceSnapshot
{
    public GameObject go;
    public Scene scene;
    public Vector3 lpos;
    public Quaternion lrot;
    public Vector3 lscale;
}

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

    private static IEnumerable<GameObject> EnumerateSceneObjects(Scene scene)
    {
        if (!scene.isLoaded) yield break;
        var roots = scene.GetRootGameObjects();
        var stack = new Stack<Transform>();
        foreach (var r in roots)
        {
            stack.Push(r.transform);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                yield return t.gameObject;
                for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
            }
        }
    }

    private static List<InstanceSnapshot> CaptureInstancesForPath(string prefabAssetPath)
{
    var list = new List<InstanceSnapshot>();
    var seenRoots = new HashSet<GameObject>();

    for (int si = 0; si < EditorSceneManager.sceneCount; si++)
    {
        var scene = EditorSceneManager.GetSceneAt(si);
        foreach (var go in EnumerateSceneObjects(scene))
        {
            if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.NotAPrefab)
                continue;

            var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (root == null || seenRoots.Contains(root)) continue;

            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (string.Equals(assetPath, prefabAssetPath, System.StringComparison.Ordinal))
            {
                seenRoots.Add(root);
                var tr = root.transform;

                list.Add(new InstanceSnapshot
                {
                    go = root,
                    scene = scene,
                    lpos = tr.localPosition,
                    lrot = tr.localRotation,
                    lscale = tr.localScale
                });
            }
        }
    }
    return list;
}

    private static void RestoreInstanceTransforms(IEnumerable<InstanceSnapshot> snaps)
    {
        var dirtyScenes = new HashSet<Scene>();
        foreach (var s in snaps)
        {
            if (s.go == null) continue;

            var tr = s.go.transform;
            Undo.RecordObject(tr, "Restore TRS");
            tr.localPosition = s.lpos;
            tr.localRotation = s.lrot;
            tr.localScale = s.lscale;
            EditorUtility.SetDirty(tr);
            dirtyScenes.Add(s.scene);
        }
        foreach (var sc in dirtyScenes)
        {
            if (sc.IsValid() && sc.isLoaded)
                EditorSceneManager.MarkSceneDirty(sc);
        }
    }
    private void ConvertNow()
    {
        var folderPath = AssetDatabase.GetAssetPath(_folder);

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

            var allSnaps = new List<InstanceSnapshot>();

            AssetDatabase.StartAssetEditing();

            int changed = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                var path = targets[i];
                EditorUtility.DisplayProgressBar("Converting To Variants", path, (float)i / targets.Length);

                // capture instance trs before asset swap
                var snaps = CaptureInstancesForPath(path);
                allSnaps.AddRange(snaps);

                // stage: open prefab contents, clone to scene, then unload the stage
                var stagedRoot = PrefabUtility.LoadPrefabContents(path);
                if (stagedRoot == null)
                    continue;

                var cloned = (GameObject)Object.Instantiate(stagedRoot); // keep original local trs
                cloned.name = stagedRoot.name;                            // keep original name
                PrefabUtility.UnloadPrefabContents(stagedRoot);

                // create base instance and parent the clone (preserve local trs)
                var baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(baseObj);
                baseInstance.name = Path.GetFileNameWithoutExtension(path);

                cloned.transform.SetParent(baseInstance.transform, false); // false = keep local trs

                // save variant over the same path
                PrefabUtility.SaveAsPrefabAsset(baseInstance, path, out bool success);

                // cleanup
                Object.DestroyImmediate(baseInstance);
                if (cloned != null) Object.DestroyImmediate(cloned);

                if (success) changed++;
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"converted {changed} prefab(s) to variants of '{baseObj.name}'.");

            // delay restore until after import finishes
            var snapsCopy = new List<InstanceSnapshot>(allSnaps);
            EditorApplication.delayCall += () =>
            {
                RestoreInstanceTransforms(snapsCopy);
                // extra tick just in case further imports run
                EditorApplication.delayCall += () => RestoreInstanceTransforms(snapsCopy);
            };
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}