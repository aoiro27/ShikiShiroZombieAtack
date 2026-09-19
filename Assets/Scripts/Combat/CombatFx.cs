using UnityEngine;

namespace ShikiShiro
{
    public sealed class CombatFx : MonoBehaviour
    {
        private Material _flash;
        private Material _blood;
        private Material _spark;

        public void Initialize()
        {
            _flash = MaterialFactory.Create(new Color(1f, 0.85f, 0.35f), 0f, 1f);
            _blood = MaterialFactory.Create(new Color(0.45f, 0.05f, 0.05f), 0f, 0.1f);
            _spark = MaterialFactory.Create(new Color(0.85f, 0.8f, 0.55f), 0.4f, 0.7f);
        }

        public void MuzzleFlash(Vector3 position, Vector3 forward)
        {
            SpawnTransient(PrimitiveType.Sphere, position + forward * 0.08f, Vector3.one * 0.18f, _flash, 0.06f);
        }

        public void Impact(Vector3 point, Vector3 normal)
        {
            SpawnTransient(PrimitiveType.Sphere, point + normal * 0.05f, Vector3.one * 0.12f, _spark, 0.18f);
        }

        public void Blood(Vector3 point)
        {
            SpawnTransient(PrimitiveType.Sphere, point, Vector3.one * 0.16f, _blood, 0.28f);
        }

        public void Tracer(Vector3 from, Vector3 to)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = _flash;
            Vector3 delta = to - from;
            go.transform.position = from + delta * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(delta.normalized);
            go.transform.localScale = new Vector3(0.03f, 0.03f, delta.magnitude);
            Destroy(go, 0.05f);
        }

        private void SpawnTransient(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, float life)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(go.GetComponent<Collider>());
            Destroy(go, life);
        }
    }
}
