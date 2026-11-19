namespace CyberDive
{
    public enum Sector { Approach, Firewall, Warden }
    public static class MissionRules
    {
        public const float Duration=120;
        public const int BossMaxHealth=360;
        public static Sector SectorAt(float elapsed) => elapsed<20?Sector.Approach:elapsed<60?Sector.Firewall:Sector.Warden;
        public static bool Expired(float elapsed) => elapsed>=Duration;
        public static int BossPhase(int health) => health>BossMaxHealth/2?1:2;
    }
}
