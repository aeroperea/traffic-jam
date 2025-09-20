using Unity.Entities;
using UnityEngine;
using static UnityEngine.Random;

class IntersectionAuthoring : MonoBehaviour
{
    public float redTimerSeconds = 60;
    public float yellowTimerSeconds = 5;

    class Baker : Baker<IntersectionAuthoring>  
    {
        public override void Bake(IntersectionAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            bool NSRed = Random.value > 0.5f;

            float timerStartVal = Random.Range(0, authoring.redTimerSeconds);

            AddComponent(entity, new IntersectionWayPoint
            {
                lightState_NS = NSRed ? IntersectionWayPoint.LightState.Red : IntersectionWayPoint.LightState.Green,
                lightState_EW = NSRed ? IntersectionWayPoint.LightState.Green : IntersectionWayPoint.LightState.Red,
                timer = timerStartVal,
                redTimerSeconds = authoring.redTimerSeconds,
                yellowTimerSeconds = authoring.yellowTimerSeconds,
            });

            AddComponent(entity, new LightsChanged());
            SetComponentEnabled<LightsChanged>(entity, false);
        }
    }
}

public struct IntersectionWayPoint : IComponentData
{
    public enum LightState { Green, Yellow, Red }
    public LightState lightState_NS;
    public LightState lightState_EW;
    public ushort gridCoords;
    public float timer;
    public float redTimerSeconds;
    public float yellowTimerSeconds;
}

public struct LightsChanged : IComponentData, IEnableableComponent
{
    public byte newStateRef;
}