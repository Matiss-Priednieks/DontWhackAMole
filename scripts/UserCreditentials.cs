using Godot.Collections;
public partial class UserCreditentials
{
    public string email { get; set; }
    public string username { get; set; }

    public float highscore { get; set; }
    public Dictionary<int, bool> unlocks { get; set; }


    public UserCreditentials(string _Username, string _Email, float _Highscore)
    {
        this.email = _Email;
        this.username = _Username;
        this.highscore = _Highscore;
    }
    public UserCreditentials(string _Username, string _Email, Dictionary<int, bool> Unlocks)
    {
        this.email = _Email;
        this.username = _Username;
        this.unlocks = Unlocks;
    }
    public UserCreditentials(string _Username, string _Email, float _Highscore, Dictionary<int, bool> Unlocks)
    {
        this.email = _Email;
        this.username = _Username;
        this.highscore = _Highscore;
        this.unlocks = Unlocks;
    }
}