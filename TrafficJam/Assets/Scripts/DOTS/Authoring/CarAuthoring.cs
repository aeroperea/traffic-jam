using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class CarAuthoring : MonoBehaviour
{
    // create authoring fields here


    class Baker : Baker<CarAuthoring>
    {
        public override void Bake(CarAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Car 
            {
                // set component values here
            });
        }
    }
}

public struct Car : IComponentData
{
    public Destination destination;
    public IntersectionWayPoint nextWaypoint;
    public MoveNode nextMovePoint;
    public half speed;
    public half turnSpeed;
}
