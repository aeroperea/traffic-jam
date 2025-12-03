using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class DestinationAuthoring : MonoBehaviour
{
    // create authoring fields here
    public Destination.Type type;
    // goap given res later

    class Baker : Baker<DestinationAuthoring>
    {
        public override void Bake(DestinationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Destination
            {
                type = authoring.type
            });
        }
    }
}

public struct Destination : IComponentData
{
    public enum Type
    {
        House, GroceryStore, 
        GasStation, Stadium,
        Restaurant, Factory, 
        Hospital, Auto_Service, 
        Shop,
    }
    public Type type;
    public int2 gridCoord;
}
