using System.Threading;
using TrueGearSDK;



namespace MyTrueGear
{
    public class TrueGearMod
    {
        private static TrueGearPlayer _player = null;

        private static ManualResetEvent leftHandSparklingMRE = new ManualResetEvent(false);
        private static ManualResetEvent rightHandSparklingMRE = new ManualResetEvent(false);

        public void LeftHandSparkling()
        {
            while (true)
            {
                leftHandSparklingMRE.WaitOne();
                _player.SendPlay("LeftHandSparkling");
                Thread.Sleep(90);
            }
        }

        public void RightHandSparkling()
        {
            while (true)
            {
                rightHandSparklingMRE.WaitOne();
                _player.SendPlay("RightHandSparkling");
                Thread.Sleep(90);
            }
        }

        public void StartLeftHandSparkling()
        {
            leftHandSparklingMRE.Set();
        }

        public void StopLeftHandSparkling()
        {
            leftHandSparklingMRE.Reset();
        }

        public void StartRightHandSparkling()
        {
            rightHandSparklingMRE.Set();
        }

        public void StopRightHandSparkling()
        {
            rightHandSparklingMRE.Reset();
        }

        public TrueGearMod()
        {
            _player = new TrueGearPlayer("448280","Job Simulator");
            _player.Start();
        }

        public void Play(string Event)
        { 
            _player.SendPlay(Event);
        }



    }
}
