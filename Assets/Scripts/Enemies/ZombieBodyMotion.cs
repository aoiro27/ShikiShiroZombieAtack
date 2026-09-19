using System.Collections.Generic;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class ZombieBodyMotion
    {
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        private readonly Dictionary<Transform, Quaternion> _rest = new Dictionary<Transform, Quaternion>();
        private Animator _animator;
        private float _phase;
        private float _attackUntil;
        private bool _bound;

        public void Bind(Transform root)
        {
            if (_bound)
            {
                return;
            }

            _bones.Clear();
            _rest.Clear();
            _animator = root.GetComponentInChildren<Animator>(true);
            Collect(root);
            _bound = Has("pelvis") || Has("thigh_l") || Has("hips") || Has("leftupleg");
            _phase = Random.Range(0f, Mathf.PI * 2f);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                bool mixamo = Has("leftupleg") || root.name.IndexOf("FreeZombie", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (mixamo)
                {
                    var controller = GameAssets.Load<RuntimeAnimatorController>(GameAssets.ZombieAnimator);
                    if (controller != null)
                    {
                        _animator.runtimeAnimatorController = controller;
                    }
                }
            }
        }

        public void Tick(bool moving, bool attacking, bool dead, float moveSpeed)
        {
            if (dead)
            {
                ApplyDead();
                if (_animator != null && _animator.runtimeAnimatorController != null)
                {
                    _animator.SetTrigger("Dead");
                    _animator.SetFloat("MoveSpeed", 0f);
                }

                return;
            }

            if (attacking)
            {
                _attackUntil = Time.time + 0.45f;
                if (_animator != null && _animator.runtimeAnimatorController != null)
                {
                    _animator.SetTrigger("Attack");
                }
            }

            bool inAttack = Time.time < _attackUntil;
            float gait = moving ? 1f : 0.25f;
            _phase += Time.deltaTime * (1.35f + moveSpeed * 1.1f) * gait;
            bool useClips = _animator != null && _animator.runtimeAnimatorController != null;
            if (useClips)
            {
                _animator.SetFloat("MoveSpeed", moving ? Mathf.Clamp01(moveSpeed / 2.2f) : 0f);
                return;
            }

            if (!_bound)
            {
                return;
            }

            float limp = Mathf.Sin(_phase);
            float drag = Mathf.Sin(_phase + Mathf.PI) * 0.55f;
            float shuffle = Mathf.Abs(limp);

            Pose("pelvis", new Vector3(6f + shuffle * 5f, limp * 8f, limp * 6f));
            Pose("hips", new Vector3(6f + shuffle * 5f, limp * 8f, limp * 6f));
            Pose("spine_01", new Vector3(12f + limp * 4f, limp * 10f, limp * 5f));
            Pose("spine", new Vector3(12f + limp * 4f, limp * 10f, limp * 5f));
            Pose("spine_02", new Vector3(8f + drag * 4f, limp * 7f, 0f));
            Pose("spine_03", new Vector3(6f, limp * 5f, limp * 4f));
            Pose("neck_01", new Vector3(14f + shuffle * 8f, limp * 12f, drag * 6f));
            Pose("neck", new Vector3(14f + shuffle * 8f, limp * 12f, drag * 6f));
            Pose("head", new Vector3(16f + limp * 10f, limp * 14f, drag * 8f));
            Pose("unrealjaw_m", new Vector3(10f + shuffle * 16f, 0f, 0f));
            Pose("jaw", new Vector3(10f + shuffle * 16f, 0f, 0f));

            Pose("thigh_l", new Vector3(limp * 42f + 8f, 0f, limp * 8f));
            Pose("leftupleg", new Vector3(limp * 42f + 8f, 0f, limp * 8f));
            Pose("calf_l", new Vector3(Mathf.Max(0f, -limp) * 55f + 8f, 0f, 0f));
            Pose("leftleg", new Vector3(Mathf.Max(0f, -limp) * 55f + 8f, 0f, 0f));
            Pose("thigh_r", new Vector3(drag * 22f + 18f, -6f, -10f));
            Pose("rightupleg", new Vector3(drag * 22f + 18f, -6f, -10f));
            Pose("calf_r", new Vector3(28f + Mathf.Max(0f, -drag) * 20f, 0f, 0f));
            Pose("rightleg", new Vector3(28f + Mathf.Max(0f, -drag) * 20f, 0f, 0f));

            if (inAttack)
            {
                float a = 1f - Mathf.Clamp01((_attackUntil - Time.time) / 0.45f);
                float lunge = Mathf.Sin(a * Mathf.PI) * 70f;
                Pose("upperarm_l", new Vector3(-40f - lunge, 20f, 25f));
                Pose("upperarm_r", new Vector3(-55f - lunge, -18f, -20f));
                Pose("leftarm", new Vector3(-40f - lunge, 20f, 25f));
                Pose("rightarm", new Vector3(-55f - lunge, -18f, -20f));
                Pose("spine_01", new Vector3(22f + lunge * 0.2f, limp * 6f, 0f));
            }
            else
            {
                Pose("upperarm_l", new Vector3(18f + limp * 12f, 8f, 62f + limp * 10f));
                Pose("upperarm_r", new Vector3(22f + drag * 10f, -10f, -58f + drag * 8f));
                Pose("leftarm", new Vector3(18f + limp * 12f, 8f, 62f + limp * 10f));
                Pose("rightarm", new Vector3(22f + drag * 10f, -10f, -58f + drag * 8f));
                Pose("lowerarm_l", new Vector3(12f, 0f, 18f));
                Pose("lowerarm_r", new Vector3(18f, 0f, -14f));
                Pose("leftforearm", new Vector3(12f, 0f, 18f));
                Pose("rightforearm", new Vector3(18f, 0f, -14f));
            }
        }

        private void ApplyDead()
        {
            Pose("pelvis", new Vector3(70f, 8f, 12f));
            Pose("hips", new Vector3(70f, 8f, 12f));
            Pose("spine_01", new Vector3(28f, 0f, 16f));
            Pose("thigh_l", new Vector3(12f, 0f, 18f));
            Pose("thigh_r", new Vector3(-8f, 0f, -22f));
            Pose("upperarm_l", new Vector3(-20f, 0f, 40f));
            Pose("upperarm_r", new Vector3(-10f, 0f, -48f));
        }

        private bool Has(string name)
        {
            return _bones.ContainsKey(name);
        }

        private void Pose(string name, Vector3 euler)
        {
            if (!_bones.TryGetValue(name, out Transform bone) || !_rest.TryGetValue(bone, out Quaternion rest))
            {
                return;
            }

            bone.localRotation = rest * Quaternion.Euler(euler);
        }

        private void Collect(Transform node)
        {
            string key = node.name.ToLowerInvariant().Replace("mixamorig:", string.Empty).Replace(" ", string.Empty);
            if (!_bones.ContainsKey(key))
            {
                _bones[key] = node;
                _rest[node] = node.localRotation;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Collect(node.GetChild(i));
            }
        }
    }
}
