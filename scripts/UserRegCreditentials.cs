
public partial class UserRegCreditentials
{
    public string email { get; set; }
    public string password { get; set; }
    public bool returnSecuredToken { get; set; }
    public UserRegCreditentials(string _Email, string _Password, bool _SecuredToken)
    {
        this.email = _Email;
        this.password = _Password;
        this.returnSecuredToken = _SecuredToken;
    }
}