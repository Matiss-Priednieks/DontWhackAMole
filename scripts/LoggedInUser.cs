using Godot;
using Godot.Collections;
using System;
using System.Text.Json;
using System.Linq;

public partial class LoggedInUser : Node
{
    public HttpRequest HTTPRequest { get; private set; }
    public HttpRequest UnlocksHTTPRequest { get; private set; }
    public HttpRequest CurrencyHTTPRequest { get; private set; }
    public HttpRequest BuyContentHTTPRequest { get; private set; }
    public HttpRequest GetUnlocksHTTPRequest { get; private set; }
    public HttpRequest GetHighscoreHTTPRequest { get; private set; }

    UnlockableContentManager Unlockables = new();
    Label UsernameLabel;

    public bool LoggedIn { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public int UserHighScore { get; private set; }

    public UnlockableContent[] UnlockablesArray;
    Dictionary<int, bool> Unlockables_dict;
    int EquippedHatIndex = -1;
    public Node3D CurrentHat;

    public int AccountCurrency;

    int BuyRequestID = -1;
    Mole Player;

    public override void _Ready()
    {
        UnlockablesArray = Unlockables.GetUnlockableContent();
        Unlockables_dict = new Dictionary<int, bool>();
        PopulateUnlockablesDict();

        // if (EquippedHatIndex != -1 && Unlockables_dict.TryGetValue(EquippedHatIndex, out bool isUnlocked) && isUnlocked)
        // {
        //     CurrentHat = UnlockablesArray.FirstOrDefault(u => u.ContentID == EquippedHatIndex)?.ContentScene.Instantiate<Node3D>();
        //     GD.Print(CurrentHat != null ? "Hat equipped" : "No hat found");
        // }

        SetDefaultUser();
    }

    private void PopulateUnlockablesDict()
    {
        foreach (var unlockable in UnlockablesArray)
        {
            Unlockables_dict[unlockable.ContentID] = unlockable.IsUnlocked;
            GD.Print(unlockable.ContentID, unlockable.ContentName, unlockable.Description, unlockable.IsUnlocked);
        }
    }

    private void SetDefaultUser()
    {
        Username = "Guest";
        if (UsernameLabel != null)
        {
            UsernameLabel.Text = "Guest";
        }
        LoggedIn = false;
    }

    public void SetUnlocksDict(Dictionary<string, bool> unlockables)
    {
        UpdateUnlockablesDict(unlockables);
        UpdateShopUI();
    }

    public void PurchasedItemUpdate(Dictionary<string, bool> _unlockables)
    {
        UpdateUnlockablesDict(_unlockables);
        UpdateUnlockedContentRequest(Unlockables_dict);
    }

    private void UpdateUnlockablesDict(Dictionary<string, bool> unlockables)
    {
        foreach (var key in unlockables.Keys)
        {
            if (int.TryParse(key, out int intKey))
            {
                Unlockables_dict[intKey] = unlockables[key];
            }
        }
    }

    public Dictionary<int, bool> GetUnlocksDict()
    {
        return Unlockables_dict;
    }

    public void LoginInitialisation()
    {
        HTTPRequest = GetNode<HttpRequest>("../Node3D/UI/HighscoreRequest");
        UnlocksHTTPRequest = GetNode<HttpRequest>("../Node3D/UnlocksRequest");
        CurrencyHTTPRequest = GetNode<HttpRequest>("../Node3D/CurrencyUpdateRequest");
        BuyContentHTTPRequest = GetNode<HttpRequest>("../Node3D/BuyAttempt");
        GetUnlocksHTTPRequest = GetNode<HttpRequest>("../Node3D/UnlocksInfo");
        GetHighscoreHTTPRequest = GetNode<HttpRequest>("../Node3D/UI/GetHighscore");
        Player = GetNode<Mole>("../Node3D/Mole");

        UsernameLabel = GetNode<Label>("../Node3D/UI/Menu/Menu/AccountMenu/MarginContainer/LoggedInScreen/VBoxContainer/UserLabel");

        SetDefaultUser();
        GetUnlockedContentRequest();
        // GetHighscoreRequest();
    }

    public void SetUsername(string username)
    {
        Username = username;
        if (UsernameLabel != null)
        {
            UsernameLabel.Text = username;
        }
    }

    public void SetEmail(string email)
    {
        Email = email;
    }

    public string GetUsername()
    {
        return UsernameLabel?.Text ?? Username;
    }

    public void Logout()
    {
        SetDefaultUser();
    }

    public void Login(string username)
    {
        SetUsername(username);
        LoggedIn = true;
        GD.Print(Email);
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
        if (Unlockables_dict.TryGetValue(BuyRequestID, out bool isUnlocked) && !isUnlocked && AccountCurrency >= UnlockablesArray[BuyRequestID].ContentPrice)
        {
            Unlockables_dict[BuyRequestID] = true;
            BuyContentRequest();
            Update();
        }
        else
        {
            GD.Print("Item is already unlocked or insufficient funds.");
        }
        BuyRequestID = -1;
    }

    public void UpdateShopUI()
    {
        foreach (var key in Unlockables_dict.Keys)
        {
            UnlockablesArray[key].IsUnlocked = Unlockables_dict[key];
        }
    }

    public void EquipHat(int hatIndex)
    {
        GD.Print("Equip called");
        if (CurrentHat == null)
        {
            GD.Print("Equipping new hat");
            CurrentHat = UnlockablesArray[hatIndex].ContentScene.Instantiate<Node3D>();
            Player.GetNode<Node3D>("HatMount").AddChild(CurrentHat);
        }
        else
        {
            GD.Print("Equipping new hat");
            CurrentHat.QueueFree();
            CurrentHat = UnlockablesArray[hatIndex].ContentScene.Instantiate<Node3D>();
            Player.GetNode<Node3D>("HatMount").AddChild(CurrentHat);
        }
    }
    public void UnequipHat()
    {
        CurrentHat.QueueFree();
        CurrentHat = null;
    }

    public Error GetUnlockedContentRequest()
    {
        UserCreditentials userData = new(Username, Email, Unlockables_dict);
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = GetUnlocksHTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-unlocks", newRegHeaders, HttpClient.Method.Get, userDataJson);
        return error;
    }

    public Error BuyContentRequest()
    {
        UserCreditentials userData = new(Username, Email, Unlockables_dict);
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
    public Error GetHighscoreRequest()
    {
        UserCreditentials userData = new(Username, Email);
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = GetHighscoreHTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-highscore", newRegHeaders, HttpClient.Method.Get, userDataJson);
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
        UpdateUserCurrency(-UnlockablesArray[BuyRequestID].ContentPrice);
        UpdateUnlockedContentRequest(Unlockables_dict);
    }
}