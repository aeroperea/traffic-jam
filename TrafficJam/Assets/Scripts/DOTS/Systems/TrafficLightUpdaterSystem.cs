// using Unity.Entities;
// using UnityEngine;

// public partial class TrafficLightUpdater : SystemBase
// {
//     EntityQuery queryChangedLight;

//     protected override void OnCreate()
//     {
//         // build a reusable query for all intersections whose lights changed
//         queryChangedLight = GetEntityQuery(
//             ComponentType.ReadOnly<IntersectionWayPoint>(),
//             ComponentType.ReadOnly<LinkedEntityGroup>(),
//             ComponentType.ReadOnly<LightsChanged>()
//         );
//     }

//     // protected override void OnUpdate()
//     // {
//     //     // iterate all matching entities and their linked‑entity buffers on the main thread
//     //     Entities
//     //         .WithName("TrafficLightUpdater")
//     //         .WithStoreEntityQueryInField(ref queryChangedLight)
//     //         .ForEach((in IntersectionWayPoint intersection, in DynamicBuffer<LinkedEntityGroup> linkedGroup) =>
//     //         {
//     //             // linkedGroup[0] is always the root entity
//     //             for (int i = 1; i < linkedGroup.Length; i++)
//     //             {
//     //                 var childEntity = linkedGroup[i].Value;
//     //                 var renderer = SystemAPI.GetComponentObject<MeshRenderer>(childEntity);
//     //                 // modify your renderer here...
//     //             }
//     //         })
//     //         .Run(); // .Run() executes immediately on the main thread, no burst, no allocations
//     // }
// }
