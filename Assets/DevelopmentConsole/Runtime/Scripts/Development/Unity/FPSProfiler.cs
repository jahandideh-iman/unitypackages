using UnityEngine;
using UnityEngine.UI;

namespace Arman.Development.DevelopmentConsole.Unity
{
    public class FPSProfiler : MonoBehaviour
    {
        public Text text;
        public float intervalDuration;

        int totalFrames = 0;
        int intervalframes;
        float intervalRemainingTime;


        bool isProfilingStarted = false;


        float averageFPS;
        float maxFPS;
        float minFPS;
        float fps;


        void Awake()
        {
            intervalRemainingTime = intervalDuration;
        }

        public void StartProfiling()
        {
            totalFrames = 0;
            averageFPS = 0;
            maxFPS = 0;
            minFPS = float.MaxValue;

            isProfilingStarted = true;
        }

        public void StopProfiling()
        {
            isProfilingStarted = false;

            Debug.LogFormat("Average: {0} | Min: {1} | Max: {2}", averageFPS, minFPS, maxFPS);
        }

        void Update()
        {
            intervalRemainingTime -= Time.unscaledDeltaTime;
            intervalframes++;
            if (intervalRemainingTime <= 0)
            {
                fps = intervalframes / intervalDuration;
                intervalRemainingTime = intervalDuration;
                intervalframes = 0;
            }
            text.text = fps.ToString();

            if (isProfilingStarted)
            {

                ++totalFrames;




                averageFPS += (fps - averageFPS) / totalFrames;

                maxFPS = Mathf.Max(maxFPS, fps);
                minFPS = Mathf.Min(minFPS, fps);

            }
        }


    }
}