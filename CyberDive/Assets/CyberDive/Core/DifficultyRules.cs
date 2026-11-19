namespace CyberDive
{
    public enum CampaignDifficulty { Easy, Normal, Hard }
    public static class DifficultyRules
    {
        public static float SpeedFactor(CampaignDifficulty difficulty) => difficulty==CampaignDifficulty.Easy?.8f:difficulty==CampaignDifficulty.Hard?1.2f:1f;
        public static int VolleyBeats(CampaignDifficulty difficulty,bool enraged) => difficulty==CampaignDifficulty.Easy?(enraged?6:8):difficulty==CampaignDifficulty.Hard?(enraged?3:4):(enraged?4:6);
        public static CampaignDifficulty FromSaved(int value) => value>=0 && value<=2?(CampaignDifficulty)value:CampaignDifficulty.Normal;
    }
}
