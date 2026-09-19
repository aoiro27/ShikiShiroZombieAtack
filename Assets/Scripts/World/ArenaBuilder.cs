using UnityEngine;

namespace ShikiShiro
{
    public sealed class ArenaBuilder
    {
        private readonly Transform _root;
        private readonly Material _asphalt;
        private readonly Material _concrete;
        private readonly Material _brick;
        private readonly Material _metal;
        private readonly Material _lightGlow;

        public float Radius { get; } = 42f;

        public ArenaBuilder(Transform root)
        {
            _root = root;
            _asphalt = MaterialFactory.Create(new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.18f);
            _concrete = MaterialFactory.Create(new Color(0.22f, 0.21f, 0.2f), 0.05f, 0.12f);
            _brick = MaterialFactory.Create(new Color(0.28f, 0.14f, 0.1f), 0.02f, 0.08f);
            _metal = MaterialFactory.Create(new Color(0.18f, 0.2f, 0.22f), 0.55f, 0.42f);
            _lightGlow = MaterialFactory.Create(new Color(1f, 0.62f, 0.28f), 0f, 0.8f);
        }

        public void Build()
        {
            CreateGround();
            CreatePerimeter();
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

            var roadX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadX.name = "MainStreetX";
            roadX.transform.SetParent(_root, false);
            roadX.transform.localScale = new Vector3(Radius * 2.4f, 0.42f, 10f);
            roadX.transform.position = new Vector3(0f, -0.18f, 0f);
            roadX.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Create(new Color(0.16f, 0.16f, 0.17f), 0.08f, 0.22f);

            var roadZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadZ.name = "MainStreetZ";
            roadZ.transform.SetParent(_root, false);
            roadZ.transform.localScale = new Vector3(10f, 0.42f, Radius * 2.4f);
            roadZ.transform.position = new Vector3(0f, -0.18f, 0f);
            roadZ.GetComponent<MeshRenderer>().sharedMaterial = roadX.GetComponent<MeshRenderer>().sharedMaterial;
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
                float h = 6f + (i % 3) * 3.5f;
                float w = 8f + (i % 2) * 2.5f;
                float d = 7f + ((i + 1) % 2) * 2f;
                CreateBox("Building_" + i, buildings[i] + Vector3.up * (h * 0.5f), new Vector3(w, h, d), i % 2 == 0 ? _brick : _concrete);
            }
        }

        private void PlaceCoverAndProps()
        {
            Vector3[] dumpsters =
            {
                new Vector3(-6f, 0.7f, 6f),
                new Vector3(7f, 0.7f, -5f),
                new Vector3(-22f, 0.7f, -4f),
                new Vector3(21f, 0.7f, 5f),
                new Vector3(4f, 0.7f, 21f),
                new Vector3(-5f, 0.7f, -22f)
            };

            foreach (Vector3 p in dumpsters)
            {
                CreateBox("Dumpster", p, new Vector3(2.4f, 1.4f, 1.2f), _metal);
            }

            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 12f, 0.45f, Mathf.Sin(a) * 12f);
                if (Mathf.Abs(p.x) < 6f || Mathf.Abs(p.z) < 6f)
                {
                    CreateBox("Barrier", p, new Vector3(2.2f, 0.9f, 0.45f), _concrete);
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
                CreateBox("LampPost", p + new Vector3(0f, 3f, 0f), new Vector3(0.18f, 6f, 0.18f), _metal);
                var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glow.name = "LampHead";
                glow.transform.SetParent(_root, false);
                glow.transform.position = p + new Vector3(0f, 6.1f, 0f);
                glow.transform.localScale = Vector3.one * 0.45f;
                glow.GetComponent<MeshRenderer>().sharedMaterial = _lightGlow;
                Object.Destroy(glow.GetComponent<Collider>());

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
