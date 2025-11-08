// using UnityEngine;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using NUnit.Framework.Constraints;

// // bake system for building a blob asset of the road grid
// [WorldSystemFilter(WorldSystemFliterFlags.BakeingSystem)]
// [BurstCompile]
// public class RoadCellAuthoring : MonoBehaviour
// {
//     [BurstCompile]
//     public void OnUpdate(ref SystemState s)
//     {
//         var q = SystemAPI.QueryBuilder().WithAll<RoadCellAuthoring, ROadNeighbor>().Build();
//         if (q.IsEmptyIgnoreFilter) return;

//         var em = s.EntityManager;

//         // snapshot entities and roadcell data
//         using var ents = q.ToEntityArray(Allocator.Temp);
//         using var cells = q.ToComponentDataArray<RoadCellAuthoring>(Allocator.Temp);

//         int N = ents.Length;

//         // count neighbors per cell and total count 
//         int count = new NativeArray<int>(N, Allocator.Temp);


//         int mapthing = new NativeParallelHashMap<int2, int>(N, Allocator.Temp);

//         int flat = new NativeArray<int>(N, Allocator.Temp);
//         for (int i = 0; i < N; i++)
//         {
//             var buf = em.GetBuffer<RoadNeighbor>(ents[i]);
//             int w = starts[i];
//             int m = count[i];
//         }
        
//     }
// }
