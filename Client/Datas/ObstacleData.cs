
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class ObstacleData : TriggerData
    {
        protected ConditionalWeakTable<Collider, NavMeshObstacle> _obstacles = new();
        public bool IsActiveObstacle { get; private set; } = true;
        public bool IsCarvingApplied { get; private set; } = false;

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
                obstacle.carving = false;
                obstacle.carveOnlyStationary = true;
                obstacle.shape = NavMeshObstacleShape.Box;
                var xPadding = 0.6f;
                var yPadding = 0.1f;
                var zPadding = 0.1f;

                if (collider is BoxCollider boxCollider)
                {
                    obstacle.center = boxCollider.center;
                    obstacle.size = new Vector3(boxCollider.size.x + xPadding, boxCollider.size.y + yPadding, boxCollider.size.z + zPadding);
                }
                else
                {
                    var bounds = collider.bounds;
                    obstacle.center = collider.transform.InverseTransformPoint(bounds.center);
                    obstacle.size = new Vector3(bounds.size.x + xPadding, bounds.size.y + yPadding, bounds.size.z + zPadding);
                }
                _obstacles.Add(collider, obstacle);
            }
        }

        public void SetObstacle(bool active)
        {
            IsActiveObstacle = active;
        }

        public void ApplyCarving(bool carve)
        {
            if (IsCarvingApplied == carve)
            {
                return;
            }

            IsCarvingApplied = carve;
            foreach ((var collider, var obstacle) in _obstacles)
            {
                if (obstacle != null)
                {
                    obstacle.carving = carve;
                }
            }
        }

        public List<Bounds> GetObstacleWorldBounds()
        {
            var result = new List<Bounds>();
            if (_obstacles == null)
            {
                return result;
            }

            foreach ((var collider, var obstacle) in _obstacles)
            {
                if (obstacle == null)
                {
                    continue;
                }

                var transform = obstacle.transform;
                var halfSize = obstacle.size * 0.5f;
                var worldMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var worldMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var corner = obstacle.center + new Vector3(halfSize.x * x, halfSize.y * y, halfSize.z * z);
                            var worldCorner = transform.TransformPoint(corner);
                            worldMin = Vector3.Min(worldMin, worldCorner);
                            worldMax = Vector3.Max(worldMax, worldCorner);
                        }
                    }
                }

                var bounds = new Bounds();
                bounds.SetMinMax(worldMin, worldMax);
                result.Add(bounds);
            }
            return result;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_obstacles != null)
            {
                _obstacles.Clear();
            }
            IsCarvingApplied = false;
        }
    }
}