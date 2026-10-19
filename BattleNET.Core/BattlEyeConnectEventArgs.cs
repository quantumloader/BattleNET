namespace BattleNET;

/// <summary>
/// Raised after a connect / login attempt completes.
/// </summary>
public sealed class BattlEyeConnectEventArgs : EventArgs
{
    public BattlEyeConnectEventArgs(BattlEyeCredentials credentials, BattlEyeConnectionResult result)
    {
        Credentials = credentials;
        Result = result;
    }

    public BattlEyeCredentials Credentials { get; }
    public BattlEyeConnectionResult Result { get; }

    public string Message => Result switch
    {
        BattlEyeConnectionResult.Success => "Connected successfully.",
        BattlEyeConnectionResult.InvalidLogin => "Connection failed: invalid login.",
        BattlEyeConnectionResult.ConnectionFailed => "Connection failed: host unreachable.",
        BattlEyeConnectionResult.Cancelled => "Connection cancelled.",
        _ => Result.ToString(),
    };
}
