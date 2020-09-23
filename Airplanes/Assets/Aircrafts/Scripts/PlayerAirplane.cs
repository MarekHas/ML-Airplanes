using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Airplanes
{
    public class PlayerAirplane : AirplaneAgent
    {
        [Header("Input Bindings")]
        [SerializeField] private InputAction _pitch;
        [SerializeField] private InputAction _yaw;
        [SerializeField] private InputAction _boost;
        [SerializeField] private InputAction _pause;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            _pitch.Enable();
            _yaw.Enable();
            _boost.Enable();
            _pause.Enable();
        }

        public override float[] Heuristic()
        {
            float pitch = Mathf.Round(_pitch.ReadValue<float>());

            float yaw = Mathf.Round(_yaw.ReadValue<float>());
            float boost = Mathf.Round(_boost.ReadValue<float>());

            if (pitch == -1f) pitch = 2f;

            if (yaw == -1f) yaw = 2f;

            return new float[] { pitch, yaw, boost };
        }

        private void OnDestroy()
        {
            _pitch.Disable();
            _yaw.Disable();
            _boost.Disable();
            _pause.Disable();
        }
    }
}
