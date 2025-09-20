using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections;
using Unity.Entities;
using System.Collections.Generic;

// public struct Car : IComponentData
// {
//     public Destination destination;
//     public IntersectionWayPoint nextWaypoint;
//     public MoveNode nextMovePoint;
//     public half speed;
//     public half turnSpeed;
// }

// public struct RoadNode : IComponentData
// {
//     public NativeHashMap<int, MovePath> pathDictionary;
//     public NativeArray<ushort> roadConnections;
//     // 0 = north 
//     // 1 = east 
//     // 2 = south 
//     // 3 = west 
// }

// public struct MovePath :  IBufferElementData
// {
//     public NativeList<MoveNode> value;
// }

// public struct MoveNode
// {
//     public half3 position;
//     public half3 orientation;
//     public byte nextIndex;
// }

public struct FuelTank : IComponentData
{
    public int fuelAmount;
    public int maxFuel;
}

public struct Road : IComponentData
{
    public int speedLimit;
}

// public struct Destination : IComponentData
// {
//     public enum Type { House, GroceryStore, GasStation, Stadium, Restaurant, Factory, Hospital}
//     public Type type;
//     public int2 gridCoord;
// }
