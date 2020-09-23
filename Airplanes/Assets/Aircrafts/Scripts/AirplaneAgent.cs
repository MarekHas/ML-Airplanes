using MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Airplanes
{
    public class AirplaneAgent : Agent
    {
        [Header("Agents stats")]
        public float thrustPower = 100000f;
        public float pitch = 100f;
        public float yaw = 100f;
        public float roll = 100f;
        public float boostPower = 2f;
        [Header("crash effects")]
        public GameObject meshObject;
        public GameObject explosionEffect;
        public int stepTimeout = 300;//Number of steps to time out after in training

        public int NextCheckpointIndex { get; set; }

        private GameArena _area;
        new private Rigidbody _rigidbody;
        private TrailRenderer _trail;
        private RayPerception3D _rayPerception;

        private float _nextStepTimeout;
        private bool _isFreeze = false;

        private float _pitchChange = 0f;
        private float _smoothPitchChange = 0f;
        private float _maxPitchAngle = 45f;
        private float _yawChange = 0f;
        private float _smoothYawChange = 0f;
        private float _rollChange = 0f;
        private float _smoothRollChange = 0f;
        private float _maxRollAngle = 45f;
        private bool _isBoosted;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            _area = GetComponentInParent<GameArena>();
            _rigidbody = GetComponent<Rigidbody>();
            _trail = GetComponent<TrailRenderer>();
            _rayPerception = GetComponent<RayPerception3D>();

            agentParameters.maxStep = _area.IsTraningMode ? 5000 : 0;
        }

        public override void AgentAction(float[] vectorAction, string textAction)
        {
            // Read values for pitch and yaw
            _pitchChange = vectorAction[0]; // up or none
            if (_pitchChange == 2) _pitchChange = -1f; // down
            _yawChange = vectorAction[1]; // turn right or none
            if (_yawChange == 2) _yawChange = -1f; // turn left

            // Read value for boost and enable/disable trail renderer
            _isBoosted = vectorAction[2] == 1;
            if (_isBoosted && !_trail.emitting) _trail.Clear();
            _trail.emitting = _isBoosted;

            if (_isFreeze) return;

            ProcessMovement();

            if (_area.IsTraningMode)
            {
                // Small negative reward every step
                AddReward(-1f / agentParameters.maxStep);

                // Make sure we haven't run out of time if training
                if (GetStepCount() > _nextStepTimeout)
                {
                    AddReward(-.5f);
                    Done();
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                if (localCheckpointDir.magnitude < _area.Academy.resetParameters["checkpoint_radius"])
                {
                    GotCheckpoint();
                }
            }
        }


        public override void CollectObservations()
        {
            AddVectorObs(transform.InverseTransformDirection(_rigidbody.velocity));

            AddVectorObs(VectorToNextCheckpoint());

            Vector3 nextCheckpointForward = _area.Checkpoints[NextCheckpointIndex].transform.forward;
            AddVectorObs(transform.InverseTransformDirection(nextCheckpointForward));

            string[] detectableObjects = { "Untagged", "checkpoint" };

            AddVectorObs(_rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 75f
            ));

            AddVectorObs(_rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 70f, 80f, 90f, 100f, 110f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 0f
            ));

            AddVectorObs(_rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: -75f
            ));

        }

        public override void AgentReset()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _trail.emitting = false;
            _area.ResetAgentPosition(agent: this, isRandom: _area.IsTraningMode);

            if (_area.IsTraningMode) _nextStepTimeout = GetStepCount() + stepTimeout;
        }


        public void FreezeAgent()
        {
            Debug.Assert(_area.IsTraningMode == false, "Freeze/Thaw not supported in training");
            _isFreeze = true;
            _rigidbody.Sleep();
            _trail.emitting = false;
        }


        public void ThawAgent()
        {
            Debug.Assert(_area.IsTraningMode == false, "Freeze/Thaw not supported in training");
            _isFreeze = false;
            _rigidbody.WakeUp();
        }

   
        private void GotCheckpoint()
        {

            NextCheckpointIndex = (NextCheckpointIndex + 1) % _area.Checkpoints.Count;

            if (_area.IsTraningMode)
            {
                AddReward(.5f);
                _nextStepTimeout = GetStepCount() + stepTimeout;
            }
        }

        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = _area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        private void ProcessMovement()
        {
 
            float boostModifier = _isBoosted ? boostPower : 1f;


            _rigidbody.AddForce(transform.forward * thrustPower * boostModifier, ForceMode.Force);


            Vector3 curRot = transform.rotation.eulerAngles;


            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
            if (_yawChange == 0f)
            {
 
                _rollChange = -rollAngle / _maxRollAngle;
            }
            else
            {

                _rollChange = -_yawChange;
            }

            _smoothPitchChange = Mathf.MoveTowards(_smoothPitchChange, _pitchChange, 2f * Time.fixedDeltaTime);
            _smoothYawChange = Mathf.MoveTowards(_smoothYawChange, _yawChange, 2f * Time.fixedDeltaTime);
            _smoothRollChange = Mathf.MoveTowards(_smoothRollChange, _rollChange, 2f * Time.fixedDeltaTime);

            float pitch = ClampAngle(curRot.x + _smoothPitchChange * Time.fixedDeltaTime * this.pitch,
                                        -_maxPitchAngle,
                                        _maxPitchAngle);
            float yaw = curRot.y + _smoothYawChange * Time.fixedDeltaTime * this.yaw;
            float roll = ClampAngle(curRot.z + _smoothRollChange * Time.fixedDeltaTime * this.roll,
                                    -_maxRollAngle,
                                    _maxRollAngle);

            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        private static float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360f + angle;
            if (angle > 180f) return Mathf.Max(angle, 360f + from);
            return Mathf.Min(angle, to);
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.CompareTag("checkpoint") &&
                other.gameObject == _area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {
                if (_area.IsTraningMode)
                {
                    AddReward(-1f);
                    Done();
                    return;
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }


        private IEnumerator ExplosionReset()
        {
            FreezeAgent();


            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            meshObject.SetActive(true);
            explosionEffect.SetActive(false);
            _area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }
    }
}
