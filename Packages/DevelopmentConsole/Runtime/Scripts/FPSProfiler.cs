using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{
    public class FPSProfiler : MonoBehaviour
    {
        [FormerlySerializedAs("text")]
        public Text Text;

        [FormerlySerializedAs("intervalDuration")]
        public float IntervalDuration;

        int _totalFrames = 0;
        int _intervalframes;
        float _intervalRemainingTime;

        bool _isProfilingStarted = false;

        float _averageFPS;
        float _maxFPS;
        float _minFPS;
        float _fps;

        void Awake()
        {
            _intervalRemainingTime = IntervalDuration;
        }

        public void StartProfiling()
        {
            _totalFrames = 0;
            _averageFPS = 0;
            _maxFPS = 0;
            _minFPS = float.MaxValue;

            _isProfilingStarted = true;
        }

        public void StopProfiling()
        {
            _isProfilingStarted = false;

            Debug.LogFormat("Average: {0} | Min: {1} | Max: {2}", _averageFPS, _minFPS, _maxFPS);
        }

        void Update()
        {
            _intervalRemainingTime -= Time.unscaledDeltaTime;
            _intervalframes++;
            if (_intervalRemainingTime <= 0)
            {
                _fps = _intervalframes / IntervalDuration;
                _intervalRemainingTime = IntervalDuration;
                _intervalframes = 0;
            }
            Text.text = _fps.ToString();

            if (_isProfilingStarted)
            {
                ++_totalFrames;

                _averageFPS += (_fps - _averageFPS) / _totalFrames;

                _maxFPS = Mathf.Max(_maxFPS, _fps);
                _minFPS = Mathf.Min(_minFPS, _fps);
            }
        }
    }
}
