
public partial class UserCreditentials
{
    public string email { get; set; }
    public string username { get; set; }

    public float highscore { get; set; }


    public UserCreditentials(string _Username, string _Email, float _Highscore)
    {
        this.email = _Email;
        this.username = _Username;
        this.highscore = _Highscore;
    }
}