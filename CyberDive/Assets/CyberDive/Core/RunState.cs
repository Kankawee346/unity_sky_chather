namespace CyberDive
{
    public enum RunPhase { Ready, Playing, Paused, Won, Lost }
    public sealed class RunState
    {
        public int Hull { get; private set; }
        public int Score { get; private set; }
        public int Multiplier { get; private set; }
        public int Sync { get; private set; }
        public int Parries { get; private set; }
        public int Hits { get; private set; }
        public RunPhase Phase { get; private set; }
        public bool ShieldActive { get; private set; }
        public bool OverdriveActive { get; private set; }
        public double CooldownRemaining { get; private set; }
        double shieldUntil, readyAt, immuneUntil, driveUntil, pausedAt;
        public void Start(double now)
        {
            Hull=3; Score=Sync=Parries=Hits=0; Multiplier=1; Phase=RunPhase.Playing;
            shieldUntil=immuneUntil=driveUntil=now; readyAt=now;
            ShieldActive=OverdriveActive=false; CooldownRemaining=0;
        }
        public void Tick(double now)
        {
            if(Phase==RunPhase.Paused)return;
            ShieldActive=Phase==RunPhase.Playing && now<shieldUntil;
            OverdriveActive=Phase==RunPhase.Playing && now<driveUntil;
            CooldownRemaining=System.Math.Max(0,readyAt-now);
        }
        public bool TryParry(double now)
        {
            Tick(now); if(Phase!=RunPhase.Playing || now<readyAt)return false;
            shieldUntil=now+.6;readyAt=now+2;Tick(now);return true;
        }
        public bool Damage(double now)
        {
            if(Phase!=RunPhase.Playing || now<immuneUntil)return false;
            Hull--;Hits++;Multiplier=1;Sync=0;immuneUntil=now+1.2;
            if(Hull<=0){Phase=RunPhase.Lost;ShieldActive=OverdriveActive=false;}return true;
        }
        public void AwardKill(int points) { if(Phase==RunPhase.Playing)Score+=System.Math.Max(0,points)*Multiplier; }
        public bool Reflect(double now)
        {
            Tick(now);if(!ShieldActive)return false;
            Parries++;Multiplier=System.Math.Min(8,Multiplier+1);Score+=100*Multiplier;
            Sync+=25;if(Sync>=100){Sync=0;driveUntil=now+4;Tick(now);}return true;
        }
        public void DefeatBoss() { if(Phase==RunPhase.Playing){Phase=RunPhase.Won;ShieldActive=OverdriveActive=false;} }
        public void Fail() { if(Phase==RunPhase.Playing){Phase=RunPhase.Lost;ShieldActive=OverdriveActive=false;} }
        public void Pause(bool paused, double now)
        {
            if(paused && Phase==RunPhase.Playing){pausedAt=now;Phase=RunPhase.Paused;}
            else if(!paused && Phase==RunPhase.Paused){double gap=now-pausedAt;shieldUntil+=gap;readyAt+=gap;immuneUntil+=gap;driveUntil+=gap;Phase=RunPhase.Playing;Tick(now);}
        }
    }
}
