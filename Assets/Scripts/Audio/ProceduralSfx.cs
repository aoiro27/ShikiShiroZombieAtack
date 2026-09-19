using UnityEngine;

namespace ShikiShiro
{
    public sealed class ProceduralSfx : MonoBehaviour
    {
        private AudioSource _source;

        public void Initialize()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public void PlayShot(WeaponId id)
        {
            float freq = id switch
            {
                WeaponId.Shotgun => 90f,
                WeaponId.Smg => 220f,
                _ => 160f
            };

            PlayTone(freq, 0.07f, 0.45f, true);
        }

        public void PlayReload()
        {
            PlayTone(420f, 0.12f, 0.18f, false);
        }

        public void PlayHit()
        {
            PlayTone(70f, 0.08f, 0.35f, true);
        }

        public void PlayRoar()
        {
            PlayTone(55f, 0.22f, 0.25f, true);
        }

        public void PlayPickup()
        {
            PlayTone(660f, 0.1f, 0.22f, false);
        }

        private void PlayTone(float frequency, float duration, float volume, bool noise)
        {
            int hz = 22050;
            int samples = Mathf.CeilToInt(hz * duration);
            var clip = AudioClip.Create("sfx", samples, 1, hz, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)hz;
                float env = 1f - (i / (float)samples);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
                if (noise)
                {
                    wave = wave * 0.35f + (Random.value * 2f - 1f) * 0.65f;
                }

                data[i] = wave * env * volume;
            }

            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }
    }
}
