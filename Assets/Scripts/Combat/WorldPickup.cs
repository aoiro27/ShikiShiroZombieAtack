using UnityEngine;

namespace ShikiShiro
{
    public sealed class WorldPickup : MonoBehaviour
    {
        public PickupKind Kind { get; private set; }

        private float _life = 22f;
        private Transform _player;

        public void Setup(PickupKind kind, Vector3 position, Transform player)
        {
            Kind = kind;
            _player = player;
            transform.position = position + Vector3.up * 0.45f;
            gameObject.SetActive(true);
            _life = 22f;
            var renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialFactory.Create(
                kind == PickupKind.Medkit ? new Color(0.75f, 0.15f, 0.15f) : new Color(0.85f, 0.7f, 0.2f),
                0.1f,
                0.4f);
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);
            transform.position = new Vector3(transform.position.x, 0.45f + Mathf.Sin(Time.time * 3f) * 0.1f, transform.position.z);
            if (_life <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            if (_player == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, _player.position) < 1.6f)
            {
                var weapons = _player.GetComponent<WeaponController>();
                var vitality = _player.GetComponent<PlayerVitality>();
                if (Kind == PickupKind.Medkit)
                {
                    vitality.Heal(40f);
                }
                else
                {
                    weapons.AddAmmo(weapons.Current.MagazineSize * 2);
                }

                FindObjectOfType<ProceduralSfx>()?.PlayPickup();
                gameObject.SetActive(false);
            }
        }
    }
}
