using UnityEngine;

namespace ShikiShiro
{
    public sealed class WorldPickup : MonoBehaviour
    {
        public PickupKind Kind { get; private set; }

        private float _life = 22f;
        private Transform _player;
        private ProceduralSfx _sfx;
        private WeaponController _weapons;
        private PlayerVitality _vitality;
        private GameObject _ammoVisual;
        private GameObject _medkitVisual;

        public void BuildVisual()
        {
            _ammoVisual = CreateChild(GameAssets.AmmoCrate, new Color(0.85f, 0.7f, 0.2f));
            _medkitVisual = CreateChild(GameAssets.MedkitCrate, new Color(0.75f, 0.15f, 0.15f));
        }

        public void Setup(PickupKind kind, Vector3 position, Transform player, ProceduralSfx sfx)
        {
            Kind = kind;
            _player = player;
            _sfx = sfx;
            _weapons = player.GetComponent<WeaponController>();
            _vitality = player.GetComponent<PlayerVitality>();
            transform.position = position + Vector3.up * 0.45f;
            gameObject.SetActive(true);
            _life = 22f;
            if (_ammoVisual != null)
            {
                _ammoVisual.SetActive(kind == PickupKind.Ammo);
            }

            if (_medkitVisual != null)
            {
                _medkitVisual.SetActive(kind == PickupKind.Medkit);
            }
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
                if (Kind == PickupKind.Medkit)
                {
                    if (_vitality != null)
                    {
                        _vitality.Heal(40f);
                    }
                }
                else if (_weapons != null)
                {
                    _weapons.AddAmmo(_weapons.Current.MagazineSize * 2);
                }

                _sfx?.PlayPickup();
                gameObject.SetActive(false);
            }
        }

        private GameObject CreateChild(string path, Color fallback)
        {
            GameObject visual = GameAssets.TryInstantiate(path, transform);
            if (visual != null)
            {
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * 0.7f;
                GameAssets.BindColormap(visual, GameAssets.CityAtlas);
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>())
                {
                    Destroy(collider);
                }

                visual.SetActive(false);
                return visual;
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Create(fallback, 0.1f, 0.4f);
            Destroy(cube.GetComponent<Collider>());
            cube.SetActive(false);
            return cube;
        }
    }
}
