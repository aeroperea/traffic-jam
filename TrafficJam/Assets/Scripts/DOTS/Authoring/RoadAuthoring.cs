// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using UnityEngine;

// class RoadAuthoring : MonoBehaviour
// {
//     // road types are not stored within the struct only on the baker to be used for the movepoint generation process
//     public enum RoadType { Straight, Corner, Intersection}
//     public RoadType roadType;

//     class Baker : Baker<RoadAuthoring>
//     {
//         public override void Bake(RoadAuthoring authoring)
//         {
//             Entity entity = GetEntity(TransformUsageFlags.Dynamic);
//             AddComponent(entity, new Road
//             {

//             });

//             Road road = new Road();
//         }
//     }
// }

// public struct RoadPathNode : IComponentData
// {
//     public NativeHashMap<int, MovePath> pathDictionary;
//     public NativeArray<ushort> roadConnections;
//     // 0 = north 
//     // 1 = east 
//     // 2 = south 
//     // 3 = west 
// }

// //each road contains several movepaths which are made up of nodes
// // a move path describes a possible sequence of MoveNodes that a car must travel through when using the road
// // a car picks a move path based on their entry point from the previous node and the destination
// // it can also help to bake the sequences of move nodes that a car will travel through
// public struct MovePath : IBufferElementData
// {
//     public NativeList<MoveNode> value;
// }

// //a point which the car moves towards
// public struct MoveNode
// {
//     public half3 position;
//     public half3 orientation;
//     public byte nextIndex;
// }

// public struct RoadMovementMeta : IComponentData
// {
//     public int speedLimit;
// }
