namespace CyberDive
{
    public sealed class PoseGate
    {
        string session;
        long lastRevision=-1;
        bool armed;
        public bool Observe(string sessionId, long revision, string gesture, bool active, bool fresh)
        {
            if(!fresh || string.IsNullOrEmpty(sessionId)){armed=false;return false;}
            if(session!=sessionId){session=sessionId;lastRevision=-1;armed=false;}
            if(revision<=lastRevision)return false;
            lastRevision=revision;
            if(gesture=="NONE" && !active){armed=true;return false;}
            bool fire=armed && gesture=="PARRY" && active;
            armed=false;return fire;
        }
        public void Reset() { armed=false; }
    }
}
