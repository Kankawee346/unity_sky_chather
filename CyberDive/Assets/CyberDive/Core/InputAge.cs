namespace CyberDive
{
    public static class InputAge
    {
        public static bool Fresh(double now,double requestStarted,float ageMs,double limitSeconds)
            => !float.IsNaN(ageMs) && !float.IsInfinity(ageMs) && ageMs>=0 && now>=requestStarted && now-requestStarted+ageMs/1000.0<=limitSeconds;
    }
}
