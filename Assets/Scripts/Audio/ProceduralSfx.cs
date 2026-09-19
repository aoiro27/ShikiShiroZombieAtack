using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class ProceduralSfx : MonoBehaviour
    {
        private AudioSource _sfx;
        private AudioSource _voice;
        private AudioSource _bgm;
        private AudioSource _sting;
        private readonly List<AudioClip> _pistolShots = new List<AudioClip>();
        private readonly List<AudioClip> _smgShots = new List<AudioClip>();
        private readonly List<AudioClip> _shotgunShots = new List<AudioClip>();
        private readonly List<AudioClip> _booms = new List<AudioClip>();
        private readonly List<AudioClip> _impacts = new List<AudioClip>();
        private readonly List<AudioClip> _flesh = new List<AudioClip>();
        private readonly List<AudioClip> _kills = new List<AudioClip>();
        private readonly List<AudioClip> _groans = new List<AudioClip>();
        private readonly List<AudioClip> _deaths = new List<AudioClip>();
        private AudioClip _reload;
        private AudioClip _hit;
        private AudioClip _bite;
        private AudioClip _pickup;
        private AudioClip _waveClear;
        private AudioClip _waveStart;
        private AudioClip[] _countdownTicks;
        private AudioClip _pistolSynth;
        private AudioClip _smgSynth;
        private AudioClip _shotgunSynth;
        private AudioClip _explosionSynth;
        private AudioClip _balloonBurst;
        private float _nextGroanAt;
        private Coroutine _clearSting;

        public void Initialize()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            _sfx = MakeSource("SfxSource", 1f, false, 128);
            _voice = MakeSource("VoiceSource", 0.95f, false, 64);
            _bgm = MakeSource("BgmSource", 0.42f, true, 200);
            _sting = MakeSource("StingSource", 1f, false, 0);
            Classify(Resources.LoadAll<AudioClip>("Sfx"));
            ClassifyVoices(Resources.LoadAll<AudioClip>("Voice"));
            _pistolSynth = BuildGun(0.12f, 0.82f, false);
            _smgSynth = BuildGun(0.07f, 0.62f, true);
            _shotgunSynth = BuildGun(0.22f, 0.95f, false);
            _explosionSynth = BuildExplosion();
            _reload = BuildTone(420f, 0.12f, 0.18f, false);
            _hit = BuildTone(70f, 0.08f, 0.35f, true);
            _bite = BuildBite();
            _pickup = BuildTone(660f, 0.1f, 0.22f, false);
            _waveClear = LoadWaveClearClip();
            if (_waveClear == null)
            {
                _waveClear = BuildWaveClear();
            }
            _waveStart = BuildWaveStart();
            _countdownTicks = new[]
            {
                BuildTone(392.0f, 0.16f, 0.42f, false),
                BuildTone(523.25f, 0.16f, 0.48f, false),
                BuildTone(659.25f, 0.28f, 0.58f, false)
            };
            _balloonBurst = Resources.Load<AudioClip>("balloon-burst");
            if (_balloonBurst != null && !_balloonBurst.preloadAudioData)
            {
                _balloonBurst.LoadAudioData();
            }
        }

        public void PlayBalloonBurst()
        {
            if (_balloonBurst == null)
            {
                _balloonBurst = Resources.Load<AudioClip>("balloon-burst");
            }

            SafeOneShot(_sfx, _balloonBurst, 0.95f);
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
                PlayPacked(_sfx, _smgShots, _smgSynth, 1f, 0.94f, 1.08f);
                return;
            }

            if (id == WeaponId.Shotgun)
            {
                PlayPacked(_sfx, _shotgunShots, _shotgunSynth, 1f, 0.92f, 1.04f);
                return;
            }

            PlayPacked(_sfx, _pistolShots.Count > 0 ? _pistolShots : _smgShots, _pistolSynth, 1f, 0.93f, 1.06f);
        }

        public void PlayReload() => SafeOneShot(_sfx, _reload, 0.7f);

        public void PlayHit() => SafeOneShot(_sfx, _hit, 0.85f);

        public void PlayBite()
        {
            SafeOneShot(_sfx, _bite, 1f);
            if (_flesh.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_flesh), 0.85f);
            }
            else
            {
                SafeOneShot(_sfx, _hit, 0.9f);
            }

            PlayGroan(true);
        }

        public void PlayFlesh(bool heavy)
        {
            if (_flesh.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_flesh), heavy ? 0.95f : 0.7f);
            }
            else if (_kills.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_kills), heavy ? 0.7f : 0.45f);
            }
            else
            {
                SafeOneShot(_sfx, _hit, heavy ? 1f : 0.6f);
            }
        }

        public void PlayKill(bool headshot)
        {
            if (_kills.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_kills), headshot ? 1f : 0.92f);
            }

            if (_flesh.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_flesh), headshot ? 0.85f : 0.7f);
            }

            PlayDeath();
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

        public void PlayExplosion()
        {
            if (_booms.Count > 0)
            {
                SafeOneShot(_sfx, Pick(_booms), 1f);
            }

            SafeOneShot(_sfx, _explosionSynth, 1f);
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
        }

        public void PlayPickup() => SafeOneShot(_sfx, _pickup, 0.8f);

        public void PlayWaveClear()
        {
            if (_bgm != null)
            {
                _bgm.volume = 0.08f;
            }

            if (_clearSting != null)
            {
                StopCoroutine(_clearSting);
            }

            _clearSting = StartCoroutine(PlayWaveClearDelayed());
        }

        private IEnumerator PlayWaveClearDelayed()
        {
            yield return new WaitForSeconds(0.18f);
            if (_sting == null || _waveClear == null)
            {
                yield break;
            }

            _sting.Stop();
            _sting.pitch = 1f;
            _sting.volume = 1f;
            if (!_waveClear.preloadAudioData)
            {
                _waveClear.LoadAudioData();
            }

            _sting.clip = _waveClear;
            _sting.Play();
            _clearSting = null;
        }

        private static AudioClip LoadWaveClearClip()
        {
            AudioClip clip = Resources.Load<AudioClip>("clear");
            if (clip == null)
            {
                clip = GameAssets.Load<AudioClip>("Assets/clear.mp3");
            }

            if (clip != null && !clip.preloadAudioData)
            {
                clip.LoadAudioData();
            }

            return clip;
        }

        public void PlayWaveStart()
        {
            SafeOneShot(_sfx, _waveStart, 0.95f);
        }

        public void PlayCountdownTick(int number)
        {
            if (_countdownTicks == null || _countdownTicks.Length == 0)
            {
                return;
            }

            int index = Mathf.Clamp(3 - number, 0, _countdownTicks.Length - 1);
            SafeOneShot(_sfx, _countdownTicks[index], number == 1 ? 1f : 0.85f);
        }

        private void OnState(SessionState state)
        {
            if (_bgm == null)
            {
                return;
            }

            if (state == SessionState.WaveClear)
            {
                _bgm.volume = 0.08f;
                _bgm.pitch = 1f;
            }
            else if (state == SessionState.GameOver)
            {
                _bgm.volume = 0.16f;
                _bgm.pitch = 0.85f;
            }
            else
            {
                _bgm.volume = 0.42f;
                _bgm.pitch = 1f;
            }
        }

        private AudioSource MakeSource(string name, float volume, bool loop, int priority)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.loop = loop;
            source.volume = volume;
            source.priority = priority;
            source.ignoreListenerPause = true;
            return source;
        }

        private void Classify(AudioClip[] packed)
        {
            for (int i = 0; i < packed.Length; i++)
            {
                string n = packed[i].name.ToLowerInvariant();
                if (n.StartsWith("laser") || n.Contains("slime"))
                {
                    continue;
                }

                if (n.StartsWith("gun_pistol"))
                {
                    _pistolShots.Add(packed[i]);
                }
                else if (n.StartsWith("gun_smg") || n.StartsWith("gun_rifle"))
                {
                    _smgShots.Add(packed[i]);
                }
                else if (n.StartsWith("gun_shotgun"))
                {
                    _shotgunShots.Add(packed[i]);
                }
                else if (n.StartsWith("kill_flesh") || n.StartsWith("wet_break"))
                {
                    _kills.Add(packed[i]);
                }
                else if (n.StartsWith("flesh_hit") || n.Contains("chop") || n.Contains("knifeslice"))
                {
                    _flesh.Add(packed[i]);
                }
                else if (n.StartsWith("explosion") || n.StartsWith("lowfrequency_explosion"))
                {
                    _booms.Add(packed[i]);
                }
                else if (n.StartsWith("impact"))
                {
                    _impacts.Add(packed[i]);
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

        private static void PlayPacked(AudioSource source, List<AudioClip> clips, AudioClip fallback, float volume, float pitchMin, float pitchMax)
        {
            AudioClip clip = clips != null && clips.Count > 0 ? Pick(clips) : fallback;
            if (source == null || clip == null)
            {
                return;
            }

            float pitch = source.pitch;
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.PlayOneShot(clip, volume);
            source.pitch = pitch;
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

        private static AudioClip BuildExplosion()
        {
            const int hz = 22050;
            const float duration = 0.58f;
            int samples = Mathf.CeilToInt(hz * duration);
            var clip = AudioClip.Create("explosion", samples, 1, hz, false);
            var data = new float[samples];
            float low = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)hz;
                float env = Mathf.Exp(-t * 5.8f);
                float thump = Mathf.Sin(2f * Mathf.PI * 36f * t) * Mathf.Exp(-t * 12f);
                float rumble = Mathf.Sin(2f * Mathf.PI * (48f + t * 22f) * t);
                float noise = Random.value * 2f - 1f;
                low = low * 0.72f + noise * 0.28f;
                float sample = (thump * 0.7f + rumble * 0.35f + low * 0.5f) * env;
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildGun(float duration, float volume, bool shortBurst)
        {
            int hz = 22050;
            int samples = Mathf.CeilToInt(hz * duration);
            var clip = AudioClip.Create("gun", samples, 1, hz, false);
            var data = new float[samples];
            float low = 0f;
            float mid = 0f;
            float crackHz = shortBurst ? 1900f : 1400f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)hz;
                float env = Mathf.Exp(-t * (shortBurst ? 48f : 22f));
                float noise = Random.value * 2f - 1f;
                low = low * 0.82f + noise * 0.18f;
                mid = mid * 0.55f + noise * 0.45f;
                float thud = Mathf.Sin(2f * Mathf.PI * (48f + t * 30f) * t) * Mathf.Exp(-t * 18f);
                float crack = Mathf.Sin(2f * Mathf.PI * crackHz * t) * Mathf.Exp(-t * 90f) * 0.18f;
                float sample = (thud * 0.55f + low * 0.7f + mid * 0.35f + crack) * env * volume;
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBite()
        {
            const int hz = 22050;
            const float duration = 0.34f;
            int samples = Mathf.CeilToInt(hz * duration);
            var clip = AudioClip.Create("bite", samples, 1, hz, false);
            var data = new float[samples];
            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)hz;
                float env = Mathf.Exp(-t * 7.5f);
                float jaw = Mathf.Sin(2f * Mathf.PI * (42f + t * 28f) * t) * Mathf.Exp(-t * 11f);
                float noise = Random.value * 2f - 1f;
                float cut = t < 0.045f ? 0.62f : 0.16f;
                lp = lp * (1f - cut) + noise * cut;
                float click = t < 0.018f ? noise * (1f - t / 0.018f) * 0.55f : 0f;
                float chomp = t > 0.04f && t < 0.09f ? Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.35f : 0f;
                float tear = lp * (t > 0.03f && t < 0.22f ? 0.85f : 0.28f);
                data[i] = Mathf.Clamp((jaw * 0.8f + tear * 0.72f + click + chomp) * env, -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildWaveClear()
        {
            const int hz = 22050;
            const float duration = 1.55f;
            int samples = Mathf.CeilToInt(hz * duration);
            var clip = AudioClip.Create("wave_clear", samples, 1, hz, false);
            var data = new float[samples];
            float[] chordA = { 261.63f, 329.63f, 392.00f, 523.25f };
            float[] chordB = { 293.66f, 369.99f, 440.00f, 587.33f };
            float[] chordC = { 329.63f, 392.00f, 493.88f, 659.25f, 783.99f };
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)hz;
                float sample = 0f;
                sample += Chord(chordA, t, 0.00f, 0.22f, 4.8f);
                sample += Chord(chordB, t, 0.22f, 0.22f, 4.2f);
                sample += Chord(chordC, t, 0.44f, 0.95f, 2.1f);
                float hit = Mathf.Sin(2f * Mathf.PI * 62f * t) * Mathf.Exp(-t * 8f) * 0.55f;
                float crash = 0f;
                if (t < 0.08f)
                {
                    crash = (Random.value * 2f - 1f) * (1f - t / 0.08f) * 0.22f;
                }

                data[i] = sample + hit + crash;
            }

            float peak = 0.0001f;
            for (int i = 0; i < samples; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            float gain = 0.94f / peak;
            for (int i = 0; i < samples; i++)
            {
                float fade = 1f - Mathf.SmoothStep(1.15f, duration, i / (float)hz);
                data[i] = Mathf.Clamp(data[i] * gain * fade, -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static float Chord(float[] notes, float t, float start, float life, float decay)
        {
            float local = t - start;
            if (local < 0f || local > life + 0.4f)
            {
                return 0f;
            }

            float env = Mathf.Clamp01(local / 0.012f) * Mathf.Exp(-local * decay);
            float sum = 0f;
            for (int n = 0; n < notes.Length; n++)
            {
                float f = notes[n];
                float saw = 2f * ((f * local) % 1f) - 1f;
                float sine = Mathf.Sin(2f * Mathf.PI * f * local);
                sum += (sine * 0.65f + saw * 0.35f) / notes.Length;
            }

            return sum * env * 1.6f;
        }

        private static AudioClip BuildWaveStart()
        {
            const int hz = 22050;
            const float duration = 0.72f;
            int samples = Mathf.CeilToInt(hz * duration);
            var clip = AudioClip.Create("wave_start", samples, 1, hz, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)hz;
                float env = Mathf.Exp(-t * 3.8f);
                float horn = Mathf.Sin(2f * Mathf.PI * 174.61f * t) * 0.45f;
                float fifth = Mathf.Sin(2f * Mathf.PI * 261.63f * t) * 0.28f;
                float rumble = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 5f) * 0.4f;
                data[i] = Mathf.Clamp((horn + fifth + rumble) * env, -1f, 1f);
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
