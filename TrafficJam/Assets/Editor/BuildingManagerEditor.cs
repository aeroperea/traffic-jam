//using UnityEngine;
//using UnityEditor;
//using System.Linq;

//[CustomEditor(typeof(BuildingManager))]
//public class BuildingManagerEditor : Editor
//{
//    public int buildingLayer = 20;

//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();

//        BuildingManager buildingManager = (BuildingManager)target;

//        if (GUILayout.Button("Prepare for getting rekt"))
//        {
//            // Set layers
//            buildingManager.gameObject.layer = buildingLayer;
//            foreach (Transform child in buildingManager.transform)
//            {
//                child.gameObject.layer = buildingLayer;
//            }

//            // Assign unifiedBuilding, add MeshCollider, and set active
//            buildingManager.unifiedBuilding = buildingManager.transform.GetChild(0).gameObject;
//            if (buildingManager.unifiedBuilding.GetComponent<MeshCollider>() == null)
//            {
//                buildingManager.unifiedBuilding.AddComponent<MeshCollider>();
//            }
//            buildingManager.unifiedBuilding.SetActive(true);

//            // Assign buildingPieces, add MeshCollider, Rigidbody, and set inactive
//            buildingManager.buildingPieces = buildingManager.transform.Cast<Transform>()
//                .Skip(1)
//                .Select(t => t.gameObject)
//                .ToArray();

//            foreach (GameObject piece in buildingManager.buildingPieces)
//            {
//                MeshCollider collider = piece.GetComponent<MeshCollider>();
//                if (collider == null)
//                {
//                    collider = piece.AddComponent<MeshCollider>();
//                }
//                collider.convex = true;

//                Rigidbody rb = piece.GetComponent<Rigidbody>();
//                if (rb == null)
//                {
//                    rb = piece.AddComponent<Rigidbody>();
//                }
//                rb.isKinematic = true;
//                rb.mass = 100f;
//                rb.angularDamping = 1f;

//                piece.SetActive(false);
//            }
//        }
//    }
//}
