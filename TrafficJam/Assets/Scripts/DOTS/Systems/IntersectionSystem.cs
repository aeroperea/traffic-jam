using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

partial struct IntersectionSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Single threaded approach
        foreach ((
            RefRW<IntersectionWayPoint> intersection,
            EnabledRefRW<LightsChanged> lightChangedEnabled
        )
            in SystemAPI.Query<
                RefRW<IntersectionWayPoint>,
                EnabledRefRW<LightsChanged>
            >()
            )
        {
            intersection.ValueRW.timer -= deltaTime;
            if (intersection.ValueRO.timer > 0) continue;

            // make sure we know to update the lights on this intersection
            lightChangedEnabled.ValueRW = true;

            // should it be yellow or red

            // if ns = red  ew = green 
            // and timer = 0.1 go to 
            // ns = red ew = yellow, reset yellow timer

            if (intersection.ValueRO.lightState_EW == IntersectionWayPoint.LightState.Green)
            {
                intersection.ValueRW.lightState_NS = IntersectionWayPoint.LightState.Red;
                intersection.ValueRW.lightState_EW = IntersectionWayPoint.LightState.Yellow;
                intersection.ValueRW.timer = intersection.ValueRW.yellowTimerSeconds;
            }

            // NS Red to Green condition
            // EW = Red

            if (intersection.ValueRO.lightState_NS == IntersectionWayPoint.LightState.Red)
            {
                intersection.ValueRW.lightState_NS = IntersectionWayPoint.LightState.Green;
                intersection.ValueRW.lightState_EW = IntersectionWayPoint.LightState.Red;
                intersection.ValueRW.redTimerSeconds = 0;
            }

            // NS Green to Yellow condition
            // EW = Red

            if (intersection.ValueRO.lightState_NS == IntersectionWayPoint.LightState.Green)
            {
                intersection.ValueRW.lightState_NS = IntersectionWayPoint.LightState.Yellow;
                intersection.ValueRW.lightState_EW = IntersectionWayPoint.LightState.Red;
                intersection.ValueRW.yellowTimerSeconds = 0;
            }

            // NS Yellow to Red condition
            // EW = Green

            if (intersection.ValueRO.lightState_NS == IntersectionWayPoint.LightState.Yellow)
            {
                intersection.ValueRW.lightState_NS = IntersectionWayPoint.LightState.Red;
                intersection.ValueRW.lightState_EW = IntersectionWayPoint.LightState.Green;
                intersection.ValueRW.redTimerSeconds = 0;
            }
            
        }
    }
}