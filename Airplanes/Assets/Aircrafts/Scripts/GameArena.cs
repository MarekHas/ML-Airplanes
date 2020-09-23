using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Airplanes
{
    public class GameArena : MonoBehaviour
    {
        [SerializeField] private CinemachineSmoothPath _raceRoute;
        [SerializeField] private GameObject _checkpoint;
        [SerializeField] private GameObject _finish;
        [SerializeField] private bool _isTraningMode;

        [SerializeField] private List<AirplaneAgent> _agents;
        [SerializeField] private List<GameObject> _checkpoints;
        [SerializeField] private AirplaneAcademy _academy;

        public bool IsTraningMode { get => _isTraningMode; set => _isTraningMode = value; }
        public List<AirplaneAgent> Agents { get => _agents; set => _agents = value; }
        public AirplaneAcademy Academy { get => _academy; set => _academy = value; }
        public List<GameObject> Checkpoints { get => _checkpoints; set => _checkpoints = value; }

        private void Awake()
        {
            Agents = transform.GetComponentsInChildren<AirplaneAgent>().ToList();
            Academy = FindObjectOfType<AirplaneAcademy>();
        }

        private void Start()
        {
            Checkpoints = new List<GameObject>();
            int checkpointsNumber = (int)_raceRoute.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);

            for (int i = 0; i < checkpointsNumber; i++)
            {
                // Instantiate either a checkpoint or finish line checkpoint
                GameObject checkpointPrefab;
                if (i == checkpointsNumber - 1) checkpointPrefab = Instantiate<GameObject>(_finish);
                else checkpointPrefab = Instantiate<GameObject>(_checkpoint);

                checkpointPrefab.transform.SetParent(_raceRoute.transform);
                checkpointPrefab.transform.localPosition = _raceRoute.m_Waypoints[i].position;
                checkpointPrefab.transform.rotation = _raceRoute.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                Checkpoints.Add(checkpointPrefab);
            }
        }

        public void ResetAgentPosition(AirplaneAgent agent, bool isRandom = false)
        {
            if (isRandom)
            {
                agent.NextCheckpointIndex = Random.Range(0, Checkpoints.Count);
            }

            int previousCheckpoint = agent.NextCheckpointIndex - 1;
            if (previousCheckpoint == -1) previousCheckpoint = Checkpoints.Count - 1;

            float startPosition = _raceRoute.FromPathNativeUnits(previousCheckpoint, CinemachinePathBase.PositionUnits.PathUnits);

            Vector3 position = _raceRoute.EvaluatePosition(startPosition);
            Quaternion orientation = _raceRoute.EvaluateOrientation(startPosition);
            Vector3 offset = Vector3.right * (Agents.IndexOf(agent) - Agents.Count / 2f)
                * UnityEngine.Random.Range(9f, 10f);

            agent.transform.position = position + orientation * offset;
            agent.transform.rotation = orientation;
        }
    }
}
