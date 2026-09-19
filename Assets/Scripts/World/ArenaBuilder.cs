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
        private const float RoadY = 0.02f;

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

        public ArenaBuilder(Transform root)
        {
            _root = root;
            _asphalt = MaterialFactory.Create(new Color(0.22f, 0.22f, 0.24f), 0.05f, 0.18f);
            _brick = MaterialFactory.Create(new Color(0.28f, 0.14f, 0.1f), 0.02f, 0.08f);
        }

        public void Build()
        {
            _block = BuildingsPerEdge * BuildingWidth;
            _pitch = _block + RoadWidth;
            int last = StreetCount / 2;
            Radius = last * _pitch + RoadWidth + 8f;
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

        private void CreateGround(bool colliderOnly)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Asphalt";
            int groundLayer = LayerMask.NameToLayer("Ground");
            ground.layer = groundLayer >= 0 ? groundLayer : 0;
            ground.transform.SetParent(_root, false);
            float thickness = 0.8f;
            ground.transform.localScale = new Vector3(Radius * 2.4f, thickness, Radius * 2.4f);
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
            float wallH = 6f;
            float thick = 2.2f;
            float span = Radius * 2.2f;
            Vector3 c = SpawnPoint;
            CreateBox("WallN", new Vector3(c.x, c.y + wallH * 0.5f, c.z + Radius), new Vector3(span, wallH, thick), _brick);
            CreateBox("WallS", new Vector3(c.x, c.y + wallH * 0.5f, c.z - Radius), new Vector3(span, wallH, thick), _brick);
            CreateBox("WallE", new Vector3(c.x + Radius, c.y + wallH * 0.5f, c.z), new Vector3(thick, wallH, span), _brick);
            CreateBox("WallW", new Vector3(c.x - Radius, c.y + wallH * 0.5f, c.z), new Vector3(thick, wallH, span), _brick);
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
                    pad.transform.position = new Vector3(cx, GroundTop - 0.01f, cz);
                    pad.transform.localScale = new Vector3(_block - 0.6f, 0.02f, _block - 0.6f);
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
            for (int s = -last; s <= last; s++)
            {
                float street = s * _pitch;
                for (int t = -tiles; t <= tiles; t++)
                {
                    float along = t * RoadWidth;
                    bool cross = OnStreet(along);
                    SpawnRoad(cross ? "KayKit/road_junction" : "KayKit/road_straight", new Vector3(street, RoadY, along), Quaternion.identity);
                    if (!cross)
                    {
                        SpawnRoad("KayKit/road_straight", new Vector3(along, RoadY, street), Quaternion.Euler(0f, 90f, 0f));
                    }
                }
            }
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
        }

        private void SpawnBuilding(Vector3 position, Vector3 face, float height)
        {
            string kit = _kits[_kit++ % _kits.Length];
            GameAssets.SpawnProp(kit, _root, position, Quaternion.LookRotation(face), true, height);
        }

        private void SpawnRoad(string path, Vector3 position, Quaternion rotation)
        {
            GameObject go = GameAssets.SpawnProp(path, _root, position, rotation, false);
            if (go != null)
            {
                GameAssets.FitFootprint(go, RoadWidth * 0.992f, RoadWidth * 0.992f);
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

        private void CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            go.layer = obstacleLayer >= 0 ? obstacleLayer : 0;
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
