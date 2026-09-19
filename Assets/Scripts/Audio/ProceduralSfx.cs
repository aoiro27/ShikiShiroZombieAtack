using System.Collections.Generic;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class ProceduralSfx : MonoBehaviour
    {
        private AudioSource _sfx;
        private AudioSource _voice;
        private AudioSource _bgm;
        private readonly List<AudioClip> _pistolShots = new List<AudioClip>();
        private readonly List<AudioClip> _smgShots = new List<AudioClip>();
        private readonly List<AudioClip> _shotgunShots = new List<AudioClip>();
        private readonly List<AudioClip> _booms = new List<AudioClip>();
        private readonly List<AudioClip> _impacts = new List<AudioClip>();
        private readonly List<AudioClip> _flesh = new List<AudioClip>();
        private readonly List<AudioClip> _groans = new List<AudioClip>();
        private readonly List<AudioClip> _deaths = new List<AudioClip>();
        private AudioClip _reload;
        private AudioClip _hit;
        private AudioClip _pickup;
        private AudioClip _pistolSynth;
        private AudioClip _smgSynth;
        private AudioClip _shotgunSynth;
        private AudioClip _explosionSynth;
        private float _nextGroanAt;

        public void Initialize()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            _sfx = MakeSource("SfxSource", 1f, false);
            _voice = MakeSource("VoiceSource", 0.95f, false);
            _bgm = MakeSource("BgmSource", 0.42f, true);
            Classify(Resources.LoadAll<AudioClip>("Sfx"));
            ClassifyVoices(Resources.LoadAll<AudioClip>("Voice"));
            _pistolSynth = BuildGun(140f, 0.11f, 0.7f);
            _smgSynth = BuildGun(210f, 0.06f, 0.48f);
            _shotgunSynth = BuildGun(80f, 0.16f, 0.85f);
            _reload = BuildTone(420f, 0.12f, 0.18f, false);
            _hit = BuildTone(70f, 0.08f, 0.35f, true);
            _pickup = BuildTone(660f, 0.1f, 0.22f, false);
        }

        public void StartBgm()
        {
            AudioClip loop = Resources.Load<AudioClip>("Bgm/bgm_horror");
            if (loop == null)
            {
                loop = Resources.Load<AudioClip>("Bgm/bgm_creepy");
            }

            if (loop == null || _bgm == null)
            {
                return;
            }

            _bgm.clip = loop;
            _bgm.loop = true;
            _bgm.volume = 0.42f;
            if (!_bgm.isPlaying)
            {
                _bgm.Play();
            }
        }

        public void BindSession(GameSession session)
        {
            session.StateChanged += OnState;
        }

        public void PlayShot(WeaponId id)
        {
            if (id == WeaponId.Smg)
            {
                PlayMix(_sfx, _smgShots, _smgSynth, 0.85f, 0.22f);
                return;
            }

            if (id == WeaponId.Shotgun)
            {
                PlayMix(_sfx, _shotgunShots.Count > 0 ? _shotgunShots : _booms, _shotgunSynth, 0.9f, 0.35f);
                return;
            }

            PlayMix(_sfx, _pistolShots.Count > 0 ? _pistolShots : _smgShots, _pistolSynth, 0.9f, 0.22f);
        }

        public void PlayReload() => SafeOneShot(_sfx, _reload, 0.7f);

        public void PlayHit() => SafeOneShot(_sfx, _hit, 0.85f);

        public void PlayFlesh(bool heavy)
        {
            if (_flesh.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_flesh), heavy ? 1f : 0.72f);
            }

            if (_impacts.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_impacts), heavy ? 0.55f : 0.35f);
            }
            else
            {
                SafeOneShot(_sfx, _hit, heavy ? 1f : 0.6f);
            }
        }

        public void PlayBoom()
        {
            if (_booms.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_booms), 0.95f);
            }
            else
            {
                SafeOneShot(_sfx, _shotgunSynth, 0.8f);
            }
        }

        public void PlayImpact()
        {
            if (_impacts.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_impacts), 0.55f);
            }
        }

        public void PlayRoar()
        {
            PlayGroan(true);
        }

        public void PlayGroan(bool force = false)
        {
            if (!force && Time.unscaledTime < _nextGroanAt)
            {
                return;
            }

            AudioClip clip = _groans.Count > 0 ? Pick(_groans) : (_deaths.Count > 0 ? Pick(_deaths) : _hit);
            SafeOneShot(_voice, clip, force ? 0.9f : 0.55f);
            _nextGroanAt = Time.unscaledTime + (force ? 0.2f : 0.55f);
        }

        public void PlayDeath()
        {
            AudioClip clip = _deaths.Count > 0 ? Pick(_deaths) : (_groans.Count > 0 ? Pick(_groans) : null);
            if (clip != null)
            {
                SafeOneShot(_voice, clip, 1f);
            }

            PlayBoom();
        }

        public void PlayPickup() => SafeOneShot(_sfx, _pickup, 0.8f);

        private void OnState(SessionState state)
        {
            if (_bgm == null)
            {
                return;
            }

            _bgm.volume = state == SessionState.GameOver ? 0.16f : 0.42f;
            _bgm.pitch = state == SessionState.GameOver ? 0.85f : 1f;
        }

        private AudioSource MakeSource(string name, float volume, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.loop = loop;
            source.volume = volume;
            source.priority = loop ? 64 : 128;
            return source;
        }

        private void Classify(AudioClip[] packed)
        {
            for (int i = 0; i < packed.Length; i++)
            {
                string n = packed[i].name.ToLowerInvariant();
                if (n.StartsWith("lasersmall") || n.StartsWith("laserlarge"))
                {
                    _pistolShots.Add(packed[i]);
                }
                else if (n.StartsWith("laserretro"))
                {
                    _smgShots.Add(packed[i]);
                }
                else if (n.StartsWith("lowfrequency_explosion"))
                {
                    _shotgunShots.Add(packed[i]);
                    _booms.Add(packed[i]);
                }
                else if (n.StartsWith("explosioncrunch"))
                {
                    _booms.Add(packed[i]);
                    _shotgunShots.Add(packed[i]);
                }
                else if (n.StartsWith("impact"))
                {
                    _impacts.Add(packed[i]);
                }
                else if (n.Contains("chop") || n.Contains("wood_heavy") || n.Contains("slime") || n.Contains("knifeslice"))
                {
                    _flesh.Add(packed[i]);
                }
            }
        }

        private void ClassifyVoices(AudioClip[] packed)
        {
            for (int i = 0; i < packed.Length; i++)
            {
                string n = packed[i].name.ToLowerInvariant();
                bool death = n.Contains("16") || n.Contains("17") || n.Contains("20") || n.Contains("24") || n.Contains("death");
                if (death)
                {
                    _deaths.Add(packed[i]);
                }
                else
                {
                    _groans.Add(packed[i]);
                }
            }
        }

        private static void PlayMix(AudioSource source, List<AudioClip> clips, AudioClip synth, float packedVol, float synthVol)
        {
            if (clips != null && clips.Count > 0)
            {
                SafeOneShot(source, Pick(clips), packedVol);
                SafeOneShot(source, synth, synthVol);
                return;
            }

            SafeOneShot(source, synth, 1f);
        }

        private static void SafeOneShot(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.PlayOneShot(clip, volume);
        }

        private static AudioClip Pick(List<AudioClip> clips)
        {
            return clips[Random.Range(0, clips.Count)];
        }

        private static AudioClip BuildGun(float frequency, float duration, float volume)
        {
            int hz = 22050;
            int samples = Mathf.CeilToInt(hz * duration);
            var clip = AudioClip.Create("gun", samples, 1, hz, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)hz;
                float env = Mathf.Exp(-t * 28f);
                float boom = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.45f;
                float crack = (Random.value * 2f - 1f) * 0.55f;
                data[i] = (boom + crack) * env * volume;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildTone(float frequency, float duration, float volume, bool noise)
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
            return clip;
        }
    }
}
