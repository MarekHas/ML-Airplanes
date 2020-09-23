using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Airplanes
{
    public class CheckpointRotation : MonoBehaviour
    {
        [SerializeField] private Vector3 _rotationSpeed;
        [SerializeField] private bool _isStartPositionRandom = false;

        private void Start()
        {
            if (_isStartPositionRandom) transform.Rotate(_rotationSpeed.normalized * UnityEngine.Random.Range(0f, 360f));
        }

        void Update()
        {
            transform.Rotate(_rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
