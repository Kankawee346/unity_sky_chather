using NUnit.Framework;
namespace CyberDive.Tests
{
    public class RulesTests
    {
        [Test] public void DifficultyScalesEnemySpeedAndAttackFrequency()
        {
            Assert.That(DifficultyRules.SpeedFactor(CampaignDifficulty.Easy),Is.EqualTo(.8f));
            Assert.That(DifficultyRules.SpeedFactor(CampaignDifficulty.Normal),Is.EqualTo(1));
            Assert.That(DifficultyRules.SpeedFactor(CampaignDifficulty.Hard),Is.EqualTo(1.2f));
            Assert.That(DifficultyRules.VolleyBeats(CampaignDifficulty.Easy,false),Is.EqualTo(8));
            Assert.That(DifficultyRules.VolleyBeats(CampaignDifficulty.Easy,true),Is.EqualTo(6));
            Assert.That(DifficultyRules.VolleyBeats(CampaignDifficulty.Normal,false),Is.EqualTo(6));
            Assert.That(DifficultyRules.VolleyBeats(CampaignDifficulty.Normal,true),Is.EqualTo(4));
            Assert.That(DifficultyRules.VolleyBeats(CampaignDifficulty.Hard,false),Is.EqualTo(4));
            Assert.That(DifficultyRules.VolleyBeats(CampaignDifficulty.Hard,true),Is.EqualTo(3));
            Assert.That(DifficultyRules.FromSaved(-1),Is.EqualTo(CampaignDifficulty.Normal));
            Assert.That(DifficultyRules.FromSaved(2),Is.EqualTo(CampaignDifficulty.Hard));
        }
        [Test] public void MissionMovesFromApproachToFirewallToBossAndExpires()
        {
            Assert.That(MissionRules.SectorAt(0),Is.EqualTo(Sector.Approach));
            Assert.That(MissionRules.SectorAt(19.99f),Is.EqualTo(Sector.Approach));
            Assert.That(MissionRules.SectorAt(20),Is.EqualTo(Sector.Firewall));
            Assert.That(MissionRules.SectorAt(59.9f),Is.EqualTo(Sector.Firewall));
            Assert.That(MissionRules.SectorAt(60),Is.EqualTo(Sector.Warden));
            Assert.That(MissionRules.Expired(119.9f),Is.False);
            Assert.That(MissionRules.Expired(120),Is.True);
            Assert.That(MissionRules.BossPhase(181),Is.EqualTo(1));
            Assert.That(MissionRules.BossPhase(180),Is.EqualTo(2));
        }
        [Test] public void TransitDelayAndSnapshotAgeBothCountTowardExpiry()
        {
            Assert.That(InputAge.Fresh(10.2,10,100,.5),Is.True);
            Assert.That(InputAge.Fresh(10.6,10,0,.5),Is.False);
            Assert.That(InputAge.Fresh(10.15,10,100,.2),Is.False);
            Assert.That(InputAge.Fresh(10.1,10,float.NaN,.5),Is.False);
        }
        [Test] public void ParryHasWindowAndCooldown()
        {
            var s = new RunState(); s.Start(0);
            Assert.That(s.Hull, Is.EqualTo(3));
            Assert.That(s.TryParry(1), Is.True);
            Assert.That(s.TryParry(1.2), Is.False);
            s.Tick(1.61); Assert.That(s.ShieldActive, Is.False);
            Assert.That(s.TryParry(3), Is.True);
        }
        [Test] public void DamageInvulnerabilityAndTerminalState()
        {
            var s = new RunState(); s.Start(0);
            Assert.That(s.Damage(1), Is.True);
            Assert.That(s.Damage(1.1), Is.False);
            s.Damage(3); s.Damage(5);
            Assert.That(s.Phase, Is.EqualTo(RunPhase.Lost));
            s.AwardKill(100); s.DefeatBoss();
            Assert.That(s.Score, Is.Zero);
            Assert.That(s.Phase, Is.EqualTo(RunPhase.Lost));
            Assert.That(s.TryParry(9), Is.False);
            s.Start(10); Assert.That(s.Hull, Is.EqualTo(3));
            Assert.That(s.TryParry(10), Is.True);
        }
        [Test] public void PauseFreezesCooldownAndRejectsCombat()
        {
            var s = new RunState(); s.Start(0); s.TryParry(1); s.Pause(true, 1.1);
            Assert.That(s.Damage(2), Is.False); Assert.That(s.TryParry(3), Is.False);
            s.Pause(false, 11.1); s.Tick(11.2);
            Assert.That(s.ShieldActive, Is.True);
            Assert.That(s.TryParry(11.3), Is.False);
            Assert.That(s.TryParry(13), Is.True);
        }
        [Test] public void ReflectionScoresAndMultiplierCaps()
        {
            var s = new RunState(); s.Start(0);
            Assert.That(s.Reflect(0), Is.False);
            for (int i=0;i<20;i++) { s.TryParry(i*3); Assert.That(s.Reflect(i*3+0.1), Is.True); }
            Assert.That(s.Multiplier, Is.EqualTo(8)); Assert.That(s.Parries, Is.EqualTo(20));
            s.DefeatBoss(); Assert.That(s.Phase, Is.EqualTo(RunPhase.Won));
            int score=s.Score; s.AwardKill(10); Assert.That(s.Score, Is.EqualTo(score));
        }
        [Test] public void PoseRequiresNeutralAndNeverRepeatsHeldGesture()
        {
            var g=new PoseGate();
            Assert.That(g.Observe("a",1,"PARRY",true,true), Is.False);
            Assert.That(g.Observe("a",2,"NONE",false,true), Is.False);
            Assert.That(g.Observe("a",3,"PARRY",true,true), Is.True);
            Assert.That(g.Observe("a",3,"PARRY",true,true), Is.False);
            Assert.That(g.Observe("a",4,"PARRY",true,true), Is.False);
            Assert.That(g.Observe("b",5,"PARRY",true,true), Is.False);
            g.Observe("b",6,"NONE",false,true);
            Assert.That(g.Observe("b",7,"PARRY",true,true), Is.True);
        }
        [Test] public void LostTrackingAndPauseRequireNewNeutral()
        {
            var g=new PoseGate(); g.Observe("a",1,"NONE",false,true);
            g.Observe("a",2,"NONE",false,false);
            Assert.That(g.Observe("a",3,"PARRY",true,true), Is.False);
            g.Observe("a",4,"NONE",false,true); g.Reset();
            Assert.That(g.Observe("a",5,"PARRY",true,true), Is.False);
            g.Observe("a",6,"NONE",false,true);
            Assert.That(g.Observe("a",7,"PARRY",true,true), Is.True);
        }
    }
}
