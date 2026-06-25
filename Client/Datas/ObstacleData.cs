
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class ObstacleData : TriggerData
    {
        protected ConditionalWeakTable<Collider, NavMeshObstacle> _obstacles = new();

        public void InitObstacle()
        {
            foreach (var collider in _colliders)
            {
                collider.TryGetComponent<NavMeshObstacle>(out var obstacle);
                if (obstacle == null)
                {
                    obstacle = collider.gameObject.AddComponent<NavMeshObstacle>();
                }
                obstacle.enabled = true;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.shape = NavMeshObstacleShape.Box;
                var height = 1.5f;
                var padding = 0.6f;

                if (collider is BoxCollider boxCollider)
                {
                    obstacle.center = boxCollider.center;  
                    obstacle.size = new Vector3(boxCollider.size.x + padding, Mathf.Max(height, boxCollider.size.y), boxCollider.size.z + height); 
                }
                else
                {
                    var bounds = collider.bounds;  
                    obstacle.center = collider.transform.InverseTransformPoint(bounds.center);  
                    obstacle.size = new Vector3(bounds.size.x + padding, Mathf.Max(height, bounds.size.y), bounds.size.z + padding); 
                }
                _obstacles.Add(collider, obstacle);
            }
        }

        public void SetObstacle(bool active)
        {
            foreach ((var collider, var obstacle) in _obstacles)
            {
                obstacle.carving = active;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_obstacles != null)
            {
                _obstacles.Clear();
            }
        }
    }
}