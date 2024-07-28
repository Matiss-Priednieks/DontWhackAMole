using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
public partial class LoggedInUser : Node
{
    public HttpRequest HTTPRequest { get; private set; }
    public HttpRequest UnlocksHTTPRequest { get; private set; }
    public HttpRequest CurrencyHTTPRequest { get; private set; }

    // Called when the node enters the scene tree for the first time.

    UnlockableContentManager Unlockables = new();
    Label UsernameLabel;

    public bool LoggedIn { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public int UserHighScore { get; private set; }

    UnlockableContent[] unlockables;
    Dictionary<int, bool> unlockables_dict;
    int EquippedHatIndex = -1;
    public Node3D CurrentHat;

    public override void _Ready()
    {
        unlockables = Unlockables.GetUnlockableContent();
        // unlockables_dict = new Dictionary<int, bool>();
        unlockables_dict = new Dictionary<int, bool>(){
            {0, false},
            {1, false},
            {2, false},
            {3, false}
        };
        for (int i = 0; i < unlockables.Count(); i++)
        {
            unlockables_dict[unlockables[i].ContentID] = unlockables[i].IsUnlocked;
            // unlockables_dict[EquippedHatIndex] = true;
            if (EquippedHatIndex != -1)
            {
                if (unlockables_dict[EquippedHatIndex] == true && EquippedHatIndex == unlockables[i].ContentID)
                {
                    GD.Print(i);
                    CurrentHat = unlockables[i].ContentScene.Instantiate<Node3D>();
                    GD.Print(unlockables[i].Description);
                    if (CurrentHat != null) break;
                    // break;
                }
            }
            GD.Print(unlockables[i].ContentID, unlockables[i].ContentName, unlockables[i].Description, unlockables[i].IsUnlocked);
        }
        Username = "Guest";
    }
    public void SetUnlocksDict(Dictionary<int, bool> unlockables)
    {
        unlockables_dict = unlockables;
    }
    public Dictionary<int, bool> GetUnlocksDict()
    {
        return unlockables_dict;
    }
    public void LoggedInFakeReady()
    {
        HTTPRequest = GetNode<HttpRequest>("../Node3D/UI/HighscoreRequest");
        UnlocksHTTPRequest = GetNode<HttpRequest>("../Node3D/UnlocksRequest");
        CurrencyHTTPRequest = GetNode<HttpRequest>("../Node3D/CurrencyUpdateRequest");

        UsernameLabel = GetNode<Label>("../Node3D/UI/Menu/Menu/AccountMenu/MarginContainer/LoggedInScreen/VBoxContainer/UserLabel");

        UsernameLabel.Text = "Guest";
        Username = "Guest";
        LoggedIn = false;
    }

    public void SetUsername(string username)
    {
        Username = username;
        UsernameLabel.Text = username;
    }
    public void SetEmail(string email)
    {
        Email = email;
    }
    public string GetUsername()
    {
        return UsernameLabel.Text;
    }

    public void Logout()
    {
        UsernameLabel.Text = "Guest";
        LoggedIn = false;
    }

    public void Login(string username)
    {
        SetUsername(username);
        LoggedIn = true;
    }

    public void SetHighscore(float value)
    {
        UserHighScore = (int)value;
    }
    public float GetHighscore()
    {
        return UserHighScore;
    }

    public Error GetUnlockedContentRequest()
    {
        GD.Print(Email);
        UserCreditentials userData = new(Username, Email, unlockables_dict);
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = UnlocksHTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-unlocks", newRegHeaders, HttpClient.Method.Get, userDataJson);
        return error;
    }


    public Error HighscoreUpdateRequest()
    {
        UserCreditentials userData = new(Username, Email, GetHighscore());
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = HTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/update-highscore", newRegHeaders, HttpClient.Method.Post, userDataJson);
        // GD.Print(userData.email + ", " + userData.username + ", " + userData.highscore + ".");
        return error;
    }
    public Error UpdateUnlockedContentRequest()
    {
        GD.Print(Email);

        UserCreditentials userData = new(Username, Email, unlockables_dict);
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = UnlocksHTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/update-unlocks", newRegHeaders, HttpClient.Method.Post, userDataJson);
        return error;
    }
    public Error UpdateUserCurrency(int CollectedCoins)
    {
        GD.Print("CoinsInPOSTCall: " + CollectedCoins);
        UserCreditentials userData = new(Username, Email, GetHighscore(), CollectedCoins);
        GD.Print("CoinsInUserData:" + userData.collected_coins);
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = CurrencyHTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/update-user-currency", newRegHeaders, HttpClient.Method.Post, userDataJson);
        // GD.Print(userData.email + ", " + userData.username + ", " + userData.highscore + ".");
        GD.Print(userDataJson);

        return error;
    }
}
