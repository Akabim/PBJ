namespace PBJ.Networking
{
    // A1: server-authoritative session lifecycle, minimum scope.
    // Replicated via NetworkVariable<SessionState> on SessionStateController;
    // the server (host) is the only writer. Extending this enum is a later
    // task - do not add gameplay semantics here.
    public enum SessionState
    {
        Waiting = 0,
        Playing = 1,
    }
}