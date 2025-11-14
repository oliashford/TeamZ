using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct UnitMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
                RefRW<LocalTransform> localTransform, 
                RefRO<MoveSpeed> moveSpeed,
                RefRW<PhysicsVelocity> physicsVelocity
            )
            in SystemAPI.Query<
                RefRW<LocalTransform>, 
                RefRO<MoveSpeed>,
                RefRW<PhysicsVelocity>
            >())
        {
            /*
            //float3 targetPosition = localTransform.ValueRO.Position + new float3(10, 0, 0);

            float3 targetPosition = MouseWorldPosition.Instance.GetPosition();
            
            float3 moveDirection = targetPosition - localTransform.ValueRO.Position;
            
            moveDirection = math.normalize(moveDirection);
            
            //localTransform.ValueRW.Rotation = quaternion.LookRotation(moveDirection, math.up());
            
            float rotationSpeed = 10f;

            localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, quaternion.LookRotation(moveDirection, math.up()), SystemAPI.Time.DeltaTime * rotationSpeed);
            
            // set physics velocity
            physicsVelocity.ValueRW.Linear = moveDirection * moveSpeed.ValueRO.value;
            
            // temp to avoid collisions
            physicsVelocity.ValueRW.Angular = float3.zero;
            
            //localTransform.ValueRW.Position += moveDirection * moveSpeed.ValueRO.value * SystemAPI.Time.DeltaTime;
            */
        }
    }
}
