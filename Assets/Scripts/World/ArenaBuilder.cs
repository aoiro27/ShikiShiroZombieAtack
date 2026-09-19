using UnityEngine;

namespace ShikiShiro
{
    public sealed class ArenaBuilder
    {
        private readonly Transform _root;
        private readonly Material _asphalt;
        private readonly Material _brick;

        public float Radius { get; } = 42f;

        public ArenaBuilder(Transform root)
        {
            _root = root;
            _asphalt = MaterialFactory.Create(new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.18f);
            _brick = MaterialFactory.Create(new Color(0.28f, 0.14f, 0.1f), 0.02f, 0.08f);
        }

        public void Build()
        {
            CreateGround();
            CreatePerimeter();
            PlaceRoads();
            PlaceBlocks();
            PlaceCoverAndProps();
            PlaceLights();
        }

        public Vector3 RandomSpawnOnRing(float minDistanceFromCenter, float maxDistanceFromCenter)
        {
            for (int i = 0; i < 24; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(minDistanceFromCenter, maxDistanceFromCenter);
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (Physics.CheckCapsule(p + Vector3.up * 0.5f, p + Vector3.up * 1.6f, 0.45f, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                return p;
            }

            return new Vector3(Radius - 6f, 0f, 0f);
        }

        private void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Asphalt";
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.SetParent(_root, false);
            ground.transform.localScale = new Vector3(Radius * 2.4f, 0.4f, Radius * 2.4f);
            ground.transform.position = new Vector3(0f, -0.2f, 0f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = _asphalt;
        }

        private void CreatePerimeter()
        {
            float wallH = 6f;
            float thick = 2.2f;
            float span = Radius * 2.2f;
            CreateBox("WallN", new Vector3(0f, wallH * 0.5f, Radius), new Vector3(span, wallH, thick), _brick);
            CreateBox("WallS", new Vector3(0f, wallH * 0.5f, -Radius), new Vector3(span, wallH, thick), _brick);
            CreateBox("WallE", new Vector3(Radius, wallH * 0.5f, 0f), new Vector3(thick, wallH, span), _brick);
            CreateBox("WallW", new Vector3(-Radius, wallH * 0.5f, 0f), new Vector3(thick, wallH, span), _brick);
        }

        private void PlaceRoads()
        {
            Vector3 tile = GameAssets.Measure("Kenney/City/road-straight");
            float step = Mathf.Max(1.5f, tile.z > 0.2f ? tile.z : tile.x);
            GameAssets.SpawnProp("Kenney/City/road-crossroad-line", _root, Vector3.zero, Quaternion.identity, false);

            for (int i = -8; i <= 8; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                float offset = i * step;
                GameAssets.SpawnProp("Kenney/City/road-straight", _root, new Vector3(offset, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), false);
                GameAssets.SpawnProp("Kenney/City/road-straight", _root, new Vector3(0f, 0f, offset), Quaternion.identity, false);
            }

            GameAssets.SpawnProp("Kenney/City/road-sign-stop", _root, new Vector3(4.5f, 0f, 4.5f), Quaternion.Euler(0f, -45f, 0f), false);
        }

        private void PlaceBlocks()
        {
            Vector3[] buildings =
            {
                new Vector3(-16f, 0f, 16f),
                new Vector3(16f, 0f, 16f),
                new Vector3(-16f, 0f, -16f),
                new Vector3(16f, 0f, -16f),
                new Vector3(-28f, 0f, 8f),
                new Vector3(28f, 0f, -8f),
                new Vector3(-8f, 0f, 28f),
                new Vector3(8f, 0f, -28f)
            };

            for (int i = 0; i < buildings.Length; i++)
            {
                string path = GameAssets.Buildings[i % GameAssets.Buildings.Length];
                GameObject model = GameAssets.SpawnProp(path, _root, buildings[i], Quaternion.Euler(0f, i * 45f, 0f), true);
                if (model == null)
                {
                    float h = 6f + (i % 3) * 3.5f;
                    float w = 8f + (i % 2) * 2.5f;
                    float d = 7f + ((i + 1) % 2) * 2f;
                    CreateBox("Building_" + i, buildings[i] + Vector3.up * (h * 0.5f), new Vector3(w, h, d), _brick);
                }
            }

            GameAssets.SpawnProp("Kenney/Buildings/water-tower", _root, new Vector3(-24f, 0f, -24f), Quaternion.identity, true);
            GameAssets.SpawnProp("Kenney/Buildings/chimney-large", _root, new Vector3(24f, 0f, 24f), Quaternion.identity, true);
            GameAssets.SpawnProp("Kenney/Buildings/detail-tank-large", _root, new Vector3(22f, 0f, -22f), Quaternion.identity, true);
        }

        private void PlaceCoverAndProps()
        {
            Vector3[] dumpsters =
            {
                new Vector3(-6f, 0f, 6f),
                new Vector3(7f, 0f, -5f),
                new Vector3(-22f, 0f, -4f),
                new Vector3(21f, 0f, 5f),
                new Vector3(4f, 0f, 21f),
                new Vector3(-5f, 0f, -22f)
            };

            foreach (Vector3 p in dumpsters)
            {
                if (GameAssets.SpawnProp("Kenney/City/dumpster", _root, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), true) == null)
                {
                    CreateBox("Dumpster", p + Vector3.up * 0.7f, new Vector3(2.4f, 1.4f, 1.2f), MaterialFactory.Create(new Color(0.18f, 0.2f, 0.22f), 0.55f, 0.42f));
                }
            }

            string[] containers =
            {
                "Kenney/Buildings/shipping-container-a",
                "Kenney/Buildings/shipping-container-b",
                "Kenney/Buildings/shipping-container-c"
            };
            Vector3[] containerPos =
            {
                new Vector3(-10f, 0f, 10f),
                new Vector3(12f, 0f, 9f),
                new Vector3(-11f, 0f, -12f)
            };
            for (int i = 0; i < containerPos.Length; i++)
            {
                GameAssets.SpawnProp(containers[i], _root, containerPos[i], Quaternion.Euler(0f, 90f * i, 0f), true);
            }

            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 12f, 0f, Mathf.Sin(a) * 12f);
                if (Mathf.Abs(p.x) < 6f || Mathf.Abs(p.z) < 6f)
                {
                    Quaternion rot = Quaternion.LookRotation(new Vector3(-p.z, 0f, p.x));
                    if (GameAssets.SpawnProp("Kenney/City/construction-barrier", _root, p, rot, true) == null)
                    {
                        CreateBox("Barrier", p + Vector3.up * 0.45f, new Vector3(2.2f, 0.9f, 0.45f), MaterialFactory.Create(new Color(0.22f, 0.21f, 0.2f), 0.05f, 0.12f));
                    }
                }

                if (i % 3 == 0)
                {
                    GameAssets.SpawnProp("Kenney/City/construction-cone", _root, p * 0.55f, Quaternion.identity, false);
                }
            }
        }

        private void PlaceLights()
        {
            Vector3[] posts =
            {
                new Vector3(-11f, 0f, -11f),
                new Vector3(11f, 0f, 11f),
                new Vector3(-11f, 0f, 11f),
                new Vector3(11f, 0f, -11f),
                new Vector3(0f, 0f, 18f),
                new Vector3(0f, 0f, -18f)
            };

            foreach (Vector3 p in posts)
            {
                GameObject lamp = GameAssets.SpawnProp("Kenney/City/light-curved", _root, p, Quaternion.LookRotation(Vector3.zero - p), false);
                if (lamp == null)
                {
                    GameAssets.SpawnProp("Kenney/City/electricity-pole", _root, p, Quaternion.identity, false);
                }

                var lightGo = new GameObject("StreetLight");
                lightGo.transform.SetParent(_root, false);
                lightGo.transform.position = p + new Vector3(0f, 5.8f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.68f, 0.38f);
                light.intensity = 2.4f;
                light.range = 16f;
                light.shadows = LightShadows.Soft;
            }
        }

        private void CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = LayerMask.NameToLayer("Obstacle");
            if (go.layer < 0)
            {
                go.layer = 0;
            }

            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
