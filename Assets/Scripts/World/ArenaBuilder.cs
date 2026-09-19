using System.Collections.Generic;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class ArenaBuilder
    {
        private const float RoadWidth = 8f;
        private const float BuildingWidth = 7.4f;
        private const float BuildingDepth = 7.4f;
        private const int BuildingsPerEdge = 3;
        private const int StreetCount = 5;
        private const float GroundTop = 0f;
        private const float RoadY = 0.08f;

        private readonly Transform _root;
        private readonly Material _asphalt;
        private readonly Material _brick;
        private readonly string[] _kits =
        {
            "KayKit/building_A",
            "KayKit/building_B",
            "KayKit/building_C",
            "KayKit/building_D",
            "KayKit/building_E",
            "KayKit/building_F",
            "KayKit/building_G",
            "KayKit/building_H"
        };

        private float _block;
        private float _pitch;
        private int _kit;

        public Vector3 SpawnPoint { get; private set; } = new Vector3(0f, 0.2f, 0f);
        public float Radius { get; private set; } = 72f;
        public float PlayHalf { get; private set; } = 72f;

        public ArenaBuilder(Transform root)
        {
            _root = root;
            _asphalt = MaterialFactory.Create(new Color(0.22f, 0.22f, 0.24f), 0.05f, 0.18f);
            _brick = MaterialFactory.Create(new Color(0.28f, 0.14f, 0.1f), 0.02f, 0.08f);
        }

        public void Build()
        {
            int last = StreetCount / 2;
            _block = BuildingsPerEdge * BuildingWidth;
            _pitch = _block + RoadWidth;
            PlayHalf = last * _pitch + RoadWidth * 0.5f + 1.15f;
            Radius = PlayHalf;
            SpawnPoint = new Vector3(0f, GroundTop + 0.12f, 0f);
            CreateGround(true);
            CreatePerimeter();
            PlaceCourtyards();
            PlaceRoads();
            PlaceDistrict();
            PlaceCoverAndProps();
            PlaceLights();
        }

        public Vector3 RandomSpawnOnRing(float minDistanceFromCenter, float maxDistanceFromCenter)
        {
            int mask = LayerMask.GetMask("Obstacle");
            if (mask == 0)
            {
                mask = ~0;
            }

            Vector3 c = SpawnPoint;
            for (int i = 0; i < 32; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float maxR = Mathf.Min(maxDistanceFromCenter, Radius * 0.88f);
                float minR = Mathf.Min(minDistanceFromCenter, Mathf.Max(8f, maxR - 8f));
                float radius = Random.Range(minR, maxR);
                Vector3 p = new Vector3(c.x + Mathf.Cos(angle) * radius, SpawnPoint.y, c.z + Mathf.Sin(angle) * radius);
                if (Physics.CheckCapsule(p + Vector3.up * 0.6f, p + Vector3.up * 1.6f, 0.4f, mask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                return p;
            }

            return SpawnPoint + new Vector3(0f, 0f, -8f);
        }

        public bool Contains(Vector3 point, float padding = 1f)
        {
            float h = Mathf.Max(4f, PlayHalf - padding);
            return Mathf.Abs(point.x - SpawnPoint.x) <= h && Mathf.Abs(point.z - SpawnPoint.z) <= h
                && point.y > SpawnPoint.y - 2f && point.y < SpawnPoint.y + 10f;
        }

        public Vector3 ClampInside(Vector3 point, float padding = 1.4f)
        {
            float h = Mathf.Max(4f, PlayHalf - padding);
            point.x = Mathf.Clamp(point.x, SpawnPoint.x - h, SpawnPoint.x + h);
            point.z = Mathf.Clamp(point.z, SpawnPoint.z - h, SpawnPoint.z + h);
            if (point.y < SpawnPoint.y - 0.2f || point.y > SpawnPoint.y + 6f)
            {
                point.y = SpawnPoint.y;
            }

            return SnapToStreet(point);
        }

        private void CreateGround(bool colliderOnly)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Asphalt";
            int groundLayer = LayerMask.NameToLayer("Ground");
            ground.layer = groundLayer >= 0 ? groundLayer : 0;
            ground.transform.SetParent(_root, false);
            float thickness = 0.8f;
            ground.transform.localScale = new Vector3(PlayHalf * 2f + 2f, thickness, PlayHalf * 2f + 2f);
            ground.transform.position = new Vector3(SpawnPoint.x, GroundTop - thickness * 0.5f, SpawnPoint.z);
            var renderer = ground.GetComponent<MeshRenderer>();
            renderer.enabled = !colliderOnly;
            if (renderer.enabled)
            {
                renderer.sharedMaterial = _asphalt;
            }
        }

        private void CreatePerimeter()
        {
            float wallH = 22f;
            float thick = 4.2f;
            float inner = PlayHalf;
            float center = inner + thick * 0.5f;
            float span = (inner + thick) * 2f;
            Vector3 c = SpawnPoint;
            var wallMat = MaterialFactory.Create(new Color(0.16f, 0.12f, 0.11f), 0.02f, 0.08f);
            CreateBox("WallN", new Vector3(c.x, wallH * 0.5f, c.z + center), new Vector3(span, wallH, thick), wallMat, true);
            CreateBox("WallS", new Vector3(c.x, wallH * 0.5f, c.z - center), new Vector3(span, wallH, thick), wallMat, true);
            CreateBox("WallE", new Vector3(c.x + center, wallH * 0.5f, c.z), new Vector3(thick, wallH, span), wallMat, true);
            CreateBox("WallW", new Vector3(c.x - center, wallH * 0.5f, c.z), new Vector3(thick, wallH, span), wallMat, true);
        }

        private void PlaceCourtyards()
        {
            int last = StreetCount / 2;
            for (int i = -last; i < last; i++)
            {
                for (int j = -last; j < last; j++)
                {
                    float cx = (i + 0.5f) * _pitch;
                    float cz = (j + 0.5f) * _pitch;
                    var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pad.name = "Lot";
                    pad.transform.SetParent(_root, false);
                    pad.transform.position = new Vector3(cx, GroundTop - 0.06f, cz);
                    pad.transform.localScale = new Vector3(_block - 0.8f, 0.02f, _block - 0.8f);
                    Object.Destroy(pad.GetComponent<Collider>());
                    pad.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Create(new Color(0.42f, 0.4f, 0.36f), GameAssets.LoadCityAtlas(), 0.02f, 0.12f);
                }
            }
        }

        private void PlaceRoads()
        {
            int last = StreetCount / 2;
            float extent = last * _pitch + RoadWidth * 0.5f;
            int tiles = Mathf.CeilToInt(extent / RoadWidth);
            var used = new HashSet<Vector2Int>();
            for (int s = -last; s <= last; s++)
            {
                float street = s * _pitch;
                for (int t = -tiles; t <= tiles; t++)
                {
                    float along = t * RoadWidth;
                    TrySpawnRoadCell(street, along, used);
                    TrySpawnRoadCell(along, street, used);
                }
            }
        }

        private void TrySpawnRoadCell(float x, float z, HashSet<Vector2Int> used)
        {
            bool ns = OnStreet(x);
            bool ew = OnStreet(z);
            if (!ns && !ew)
            {
                return;
            }

            var cell = new Vector2Int(Mathf.RoundToInt(x / RoadWidth), Mathf.RoundToInt(z / RoadWidth));
            if (!used.Add(cell))
            {
                return;
            }

            string path = ns && ew ? "KayKit/road_junction" : "KayKit/road_straight";
            Quaternion rot = !ns && ew ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            SpawnRoad(path, new Vector3(x, RoadY, z), rot);
        }

        private void PlaceDistrict()
        {
            int last = StreetCount / 2;
            for (int i = -last; i < last; i++)
            {
                for (int j = -last; j < last; j++)
                {
                    float cx = (i + 0.5f) * _pitch;
                    float cz = (j + 0.5f) * _pitch;
                    PlaceBlock(cx, cz);
                }
            }
        }

        private void PlaceBlock(float cx, float cz)
        {
            float inner = _block * 0.5f;
            float front = inner - BuildingDepth * 0.5f;
            for (int k = 0; k < BuildingsPerEdge; k++)
            {
                float along = -inner + BuildingWidth * 0.5f + k * BuildingWidth;
                float h = 8f + (k % 3);
                SpawnBuilding(new Vector3(cx + along, 0f, cz - front), Vector3.back, h);
                SpawnBuilding(new Vector3(cx + along, 0f, cz + front), Vector3.forward, h + 0.6f);
                if (k == 0 || k == BuildingsPerEdge - 1)
                {
                    continue;
                }

                SpawnBuilding(new Vector3(cx - front, 0f, cz + along), Vector3.left, h + 0.3f);
                SpawnBuilding(new Vector3(cx + front, 0f, cz + along), Vector3.right, h + 0.9f);
            }

            SealBlock(cx, cz, front);
        }

        private void SealBlock(float cx, float cz, float front)
        {
            float h = 14f;
            float thick = BuildingDepth * 0.9f;
            float span = _block;
            CreateBox("SealS", new Vector3(cx, h * 0.5f, cz - front), new Vector3(span, h, thick), _brick, false);
            CreateBox("SealN", new Vector3(cx, h * 0.5f, cz + front), new Vector3(span, h, thick), _brick, false);
            CreateBox("SealW", new Vector3(cx - front, h * 0.5f, cz), new Vector3(thick, h, span), _brick, false);
            CreateBox("SealE", new Vector3(cx + front, h * 0.5f, cz), new Vector3(thick, h, span), _brick, false);
        }

        public bool IsStreet(Vector3 point)
        {
            return StreetOffset(point) <= RoadWidth * 0.46f;
        }

        public Vector3 SnapToStreet(Vector3 point)
        {
            NearestStreet(point, out float nx, out float nz, out float dx, out float dz);
            if (dx <= dz)
            {
                point.x = nx;
            }
            else
            {
                point.z = nz;
            }

            return point;
        }

        private float StreetOffset(Vector3 point)
        {
            NearestStreet(point, out _, out _, out float dx, out float dz);
            return Mathf.Min(dx, dz);
        }

        private void NearestStreet(Vector3 point, out float nearestX, out float nearestZ, out float dx, out float dz)
        {
            int last = StreetCount / 2;
            nearestX = 0f;
            nearestZ = 0f;
            dx = float.MaxValue;
            dz = float.MaxValue;
            for (int s = -last; s <= last; s++)
            {
                float street = s * _pitch;
                float xDist = Mathf.Abs(point.x - street);
                if (xDist < dx)
                {
                    dx = xDist;
                    nearestX = street;
                }

                float zDist = Mathf.Abs(point.z - street);
                if (zDist < dz)
                {
                    dz = zDist;
                    nearestZ = street;
                }
            }
        }

        private void SpawnBuilding(Vector3 position, Vector3 face, float height)
        {
            string kit = _kits[_kit++ % _kits.Length];
            GameObject go = GameAssets.SpawnProp(kit, _root, position, Quaternion.LookRotation(face), false, height);
            if (go == null)
            {
                return;
            }

            GameAssets.FitFootprint(go, BuildingWidth + 0.55f, BuildingDepth + 0.2f);
            GameAssets.MakeObstacle(go, 18f);
        }

        private void SpawnRoad(string path, Vector3 position, Quaternion rotation)
        {
            GameObject go = GameAssets.SpawnProp(path, _root, position, rotation, false);
            if (go != null)
            {
                GameAssets.FitFootprint(go, RoadWidth * 0.999f, RoadWidth * 0.999f);
                foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        private bool OnStreet(float coord)
        {
            int last = StreetCount / 2;
            for (int s = -last; s <= last; s++)
            {
                if (Mathf.Abs(coord - s * _pitch) < RoadWidth * 0.51f)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlaceCoverAndProps()
        {
            int last = StreetCount / 2;
            int n = 0;
            for (int s = -last; s <= last; s++)
            {
                float street = s * _pitch;
                for (int t = -last; t < last; t++)
                {
                    float mid = (t + 0.5f) * _pitch;
                    if (Mathf.Abs(street) < 0.1f && Mathf.Abs(mid) < _pitch)
                    {
                        continue;
                    }

                    float curb = RoadWidth * 0.5f + 1.1f;
                    Quaternion carRot = Quaternion.Euler(0f, s % 2 == 0 ? 90f : 0f, 0f);
                    GameAssets.SpawnProp("KayKit/car_sedan", _root, new Vector3(street + curb, 0f, mid), carRot, true);
                    if (n++ % 2 == 0)
                    {
                        GameAssets.SpawnProp("KayKit/dumpster", _root, new Vector3(mid, 0f, street - curb), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), true);
                    }

                    GameAssets.SpawnProp("KayKit/firehydrant", _root, new Vector3(street - curb, GroundTop, mid - BuildingWidth), Quaternion.identity, false, 0.72f);
                }
            }

            GameAssets.SpawnProp("KayKit/trafficlight_A", _root, new Vector3(RoadWidth * 0.45f, 0f, RoadWidth * 0.45f), Quaternion.Euler(0f, -45f, 0f), false);
        }

        private void PlaceLights()
        {
            int last = StreetCount / 2;
            for (int s = -last; s <= last; s++)
            {
                float street = s * _pitch;
                for (int t = -last; t <= last; t++)
                {
                    if ((s + t) % 2 != 0)
                    {
                        continue;
                    }

                    float along = t * _pitch;
                    Vector3 p = new Vector3(street + RoadWidth * 0.42f, 0f, along + RoadWidth * 0.42f);
                    GameAssets.SpawnProp("KayKit/streetlight", _root, p, Quaternion.LookRotation(new Vector3(-1f, 0f, -1f)), false);
                    var lightGo = new GameObject("StreetLight");
                    lightGo.transform.SetParent(_root, false);
                    lightGo.transform.position = p + new Vector3(0f, 5.8f, 0f);
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.92f, 0.75f);
                    light.intensity = 0.55f;
                    light.range = 22f;
                    light.shadows = LightShadows.None;
                }
            }
        }

        private void CreateBox(string name, Vector3 position, Vector3 scale, Material material, bool visible)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            go.layer = obstacleLayer >= 0 ? obstacleLayer : 0;
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.enabled = visible;
            if (visible && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
