using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MelonLoader;
using OWOHaptic;
//using MyOWOSensations;

namespace MyOwoVest
{
    public class TactsuitVR
    {
        /* A class that contains the basic functions for the bhaptics Tactsuit, like:
         * - A Heartbeat function that can be turned on/off
         * - A function to read in and register all .tact patterns in the bHaptics subfolder
         * - A logging hook to output to the Melonloader log
         * - 
         * */
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        // Event to start and stop the rain thread
        private static ManualResetEvent Rain_mrse = new ManualResetEvent(false);
        private static Random rainRandom = new Random();
        private readonly int rainDropPause = 800;
        private readonly int rainDropIntensity = 100;
        private int randomMuscleNumber = 0;

        public Dictionary<String, ISensation> FeedbackMap = new Dictionary<String, ISensation>();
        public Dictionary<String, ISensation> FeedbackMapWithoutMuscles = new Dictionary<String, ISensation>();

        public Muscle getRandomMuscleRain()
        {
            randomMuscleNumber = rainRandom.Next(20);
            if (randomMuscleNumber >= 16) return Muscle.Arm_L;
            if (randomMuscleNumber >= 12) return Muscle.Arm_R;
            if (randomMuscleNumber >= 10) return Muscle.Pectoral_L;
            if (randomMuscleNumber >= 8) return Muscle.Pectoral_R;
            if (randomMuscleNumber >= 6) return Muscle.Dorsal_L;
            if (randomMuscleNumber >= 4) return Muscle.Dorsal_R;
            if (randomMuscleNumber >= 3) return Muscle.Lumbar_L;
            if (randomMuscleNumber >= 2) return Muscle.Lumbar_R;
            if (randomMuscleNumber >= 1) return Muscle.Abdominal_L;
            return Muscle.Abdominal_R;
        }

        public void RainFunc()
        {
            while (true)
            {
                Rain_mrse.WaitOne();
                OWO.Send(FeedbackMapWithoutMuscles["Raindrop"], getRandomMuscleRain().WithIntensity(rainRandom.Next(rainDropIntensity)));
                Thread.Sleep(rainRandom.Next(rainDropPause) + 200);
            }
        }


        public TactsuitVR()
        {
            LOG("Initializing suit");
            OWO.OnConnected.AddListener(InitializeOWO);
            OWO.AutoConnect();
            LOG("Starting rain thread.");
            Thread RainThread = new Thread(RainFunc);
            RainThread.Start();
        }

        private void InitializeOWO()
        {
            suitDisabled = false;
            LOG("OWO suit connected.");
            RegisterAllTactFiles();
        }

        ~TactsuitVR()
        {
            LOG("Destructor called");
            DisconnectOwo();
        }


        public void DisconnectOwo()
        {
            LOG("Disconnecting Owo skin.");
            OWO.Disconnect();
        }

        public void LOG(string logStr)
        {
#pragma warning disable CS0618 // remove warning that the logger is deprecated
            MelonLogger.Msg(logStr);
#pragma warning restore CS0618
        }

        void RegisterAllTactFiles()
        {

            string configPath = Directory.GetCurrentDirectory() + "\\Mods\\OWO";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.owo", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                // LOG("Trying to register: " + prefix + " " + fullName);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                string tactFileStrWithoutMuscles = DetachFromMuscles(tactFileStr);
                try
                {
                    ISensation test = Sensation.FromCode(tactFileStr);
                    ISensation testNoMuscles = Sensation.FromCode(tactFileStrWithoutMuscles);
                    //bHaptics.RegisterFeedback(prefix, tactFileStr);
                    LOG("Pattern registered: " + prefix);
                    FeedbackMap.Add(prefix, test);
                    FeedbackMapWithoutMuscles.Add(prefix, testNoMuscles);
                }
                catch (Exception e) { LOG(e.ToString()); }

            }

            systemInitialized = true;
        }

        public string DetachFromMuscles(string pattern)
        {
            return System.Text.RegularExpressions.Regex.Replace(pattern, "\\|([0-9]%[0-9]+(,)*)+", "");
        }

        public void StartRain()
        {
            Rain_mrse.Set();
        }

        public void StopRain()
        {
            Rain_mrse.Reset();
        }

        public void StopThreads()
        {
            Rain_mrse.Reset();
            OWO.StopSensation();
        }

        public void PlayBackHit(string pattern, float xzAngle, float yShift, float intensity = 1.0f)
        {
            if (FeedbackMap.ContainsKey(pattern))
            {
                ISensation sensation = FeedbackMapWithoutMuscles[pattern];
                Muscle myMuscle = Muscle.Pectoral_R;
                int intensityPercentage = (int)(intensity * 100f);
                // two parameters can be given to the pattern to move it on the vest:
                // 1. An angle in degrees [0, 360] to turn the pattern to the left
                // 2. A shift [-0.5, 0.5] in y-direction (up and down) to move it up or down
                if ((xzAngle < 90f))
                {
                    if (yShift >= 0f) myMuscle = Muscle.Pectoral_L;
                    else myMuscle = Muscle.Abdominal_L;
                }
                if ((xzAngle > 90f) && (xzAngle < 180f))
                {
                    if (yShift >= 0f) myMuscle = Muscle.Dorsal_L;
                    else myMuscle = Muscle.Lumbar_L;
                }
                if ((xzAngle > 180f) && (xzAngle < 270f))
                {
                    if (yShift >= 0f) myMuscle = Muscle.Dorsal_R;
                    else myMuscle = Muscle.Lumbar_R;
                }
                if ((xzAngle > 270f))
                {
                    if (yShift >= 0f) myMuscle = Muscle.Pectoral_R;
                    else myMuscle = Muscle.Abdominal_R;
                }
                OWO.Send(sensation, myMuscle.WithIntensity(intensityPercentage));
            }
            else
            {
                LOG("Feedback not registered: " + pattern);
                return;
            }

        }



        public void Recoil(bool isRightHand)
        {
            if (isRightHand) PlayBackFeedback("Recoil_R");
            else PlayBackFeedback("Recoil_L");
        }

        public void PlayBackFeedback(string feedback, float intensity = 1.0f)
        {
            if (FeedbackMap.ContainsKey(feedback))
            {
                OWO.Send(FeedbackMap[feedback]);
            }
            else LOG("Feedback not registered: " + feedback);
        }

    }
}
