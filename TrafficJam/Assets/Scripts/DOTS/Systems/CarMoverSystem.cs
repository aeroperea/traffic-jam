using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

partial struct CarMoverSystem : ISystem
{
    
    //[BurstCompile]
    //public void OnCreate(ref SystemState state)
    //{

    //}

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //CarMoverJob carMoverJob = new CarMoverJob { 
                                                        // // set the job arguments here

                                                        //};

        // // schedule parallel uses job scheduler and if there is not enough jobs to split it will run on main thread
        //carMoverJob.ScheduleParallel();

        float deltaTime = SystemAPI.Time.DeltaTime;

        // Single threaded approach
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<Car> car
            )
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<Car>
            >()
            )
        {
            float3 deltaPos = car.ValueRO.nextMovePoint.position - localTransform.ValueRO.Position;
            float3 direction = math.normalize(deltaPos);
            localTransform.ValueRW.Position += direction * car.ValueRO.speed * deltaTime;

            var lookRotation = quaternion.LookRotation(direction, math.up());
            localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation,
                                                        lookRotation,
                                                        car.ValueRO.turnSpeed * deltaTime);
        }
    }
}

//[BurstCompile]
//public partial struct CarMoverJob : IJobEntity
//{
//    //in for RO ref for RW
//    public void Execute(in LocalTransform localTransform, in CarMover carMover)
//    {
        
//    }
//}