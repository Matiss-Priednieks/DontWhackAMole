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
    public HttpRequest BuyContentHTTPRequest { get; private set; }
    public HttpRequest GetUnlocksHTTPRequest { get; private set; }

    UnlockableContentManager Unlockables = new();
    Label UsernameLabel;

    public bool LoggedIn { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public int UserHighScore { get; private set; }

    public UnlockableContent[] unlockables;
    Dictionary<int, bool> unlockables_dict;
    int EquippedHatIndex = -1;
    public Node3D CurrentHat;

    public int AccountCurrency;

    int BuyRequestID = -1;

    public override void _Ready()
    {
        unlockables = Unlockables.GetUnlockableContent();
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
    public void SetUnlocksDict(Dictionary<string, bool> unlockables)
    {
        GD.Print("Unlocks in logged in user setunlocks function pre translation: " + unlockables);
        foreach (string key in unlockables.Keys)
        {
            if (int.TryParse(key, out int intKey))
            {
                unlockables_dict[intKey] = (bool)unlockables[key];
            }
        }
        GD.Print("Unlocks in logged in user setunlocks function POST translation: " + unlockables_dict);
        UpdateShopUI();
    }
    public Dictionary<int, bool> GetUnlocksDict()
    {
        return unlockables_dict;
    }
    public void LoginInitialisation()
    {
        HTTPRequest = GetNode<HttpRequest>("../Node3D/UI/HighscoreRequest");
        UnlocksHTTPRequest = GetNode<HttpRequest>("../Node3D/UnlocksRequest");
        CurrencyHTTPRequest = GetNode<HttpRequest>("../Node3D/CurrencyUpdateRequest");
        BuyContentHTTPRequest = GetNode<HttpRequest>("../Node3D/BuyAttempt");
        GetUnlocksHTTPRequest = GetNode<HttpRequest>("../Node3D/UnlocksInfo");



        UsernameLabel = GetNode<Label>("../Node3D/UI/Menu/Menu/AccountMenu/MarginContainer/LoggedInScreen/VBoxContainer/UserLabel");

        UsernameLabel.Text = "Guest";
        Username = "Guest";
        LoggedIn = false;
        GetUnlockedContentRequest();
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
    public void CheckUnlockedContent(int contentBuyID)
    {
        BuyRequestID = contentBuyID;
        GetUnlockedContentRequest();
    }
    public void FinalBuyCheck()
    {
        GD.Print("Buy request 3");
        if (unlockables_dict[BuyRequestID] == false && AccountCurrency >= unlockables[BuyRequestID].ContentPrice)
        {
            unlockables_dict[BuyRequestID] = true;
            BuyContentRequest();
            Update();
            BuyRequestID = -1;
        }
        else
        {
            BuyRequestID = -1;
            unlockables_dict[BuyRequestID] = false;
            GD.Print("error");
            GD.Print("Item is already unlocked: " + (unlockables_dict[BuyRequestID] == false) + "\n" + "or cant afford: " + AccountCurrency + "vs price: " + unlockables[BuyRequestID].ContentPrice);
            //show buy error/disable buy button
        }
    }
    public void PurchasedItemUpdate(Dictionary<string, bool> _unlockables)
    {
        foreach (string key in _unlockables.Keys)
        {
            if (int.TryParse(key, out int intKey))
            {
                unlockables_dict[intKey] = (bool)_unlockables[key];
            }
        }

        UpdateUnlockedContentRequest(unlockables_dict);
    }
    public void UpdateShopUI()
    {
        foreach (int key in unlockables_dict.Keys)
        {
            unlockables[key].IsUnlocked = unlockables_dict[key];
            // GD.Print("Unlocks in UI update function: ", unlockables_dict[key]);
        }
    }

    public Error GetUnlockedContentRequest()
    {
        UserCreditentials userData = new(Username, Email, unlockables_dict);
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = GetUnlocksHTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-unlocks", newRegHeaders, HttpClient.Method.Get, userDataJson);
        return error;
    }
    public Error BuyContentRequest()
    {
        UserCreditentials userData = new(Username, Email, unlockables_dict);
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = BuyContentHTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/buy-attempt", newRegHeaders, HttpClient.Method.Get, userDataJson);
        return error;
    }


    public Error HighscoreUpdateRequest()
    {
        UserCreditentials userData = new(Username, Email, GetHighscore());
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = HTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/update-highscore", newRegHeaders, HttpClient.Method.Post, userDataJson);
        return error;
    }
    public Error UpdateUnlockedContentRequest(Dictionary<int, bool> _unlockables_dict)
    {
        GD.Print(_unlockables_dict);

        UserCreditentials userData = new(Username, Email, _unlockables_dict);
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
        GD.Print(userDataJson);

        return error;
    }
    public void Update()
    {
        UpdateUserCurrency(-unlockables[BuyRequestID].ContentPrice);
        UpdateUnlockedContentRequest(unlockables_dict);
    }
}
