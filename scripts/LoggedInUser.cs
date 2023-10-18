using Godot;
using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class LoggedInUser : Node
{
    public HttpRequest HTTPRequest { get; private set; }

    // Called when the node enters the scene tree for the first time.

    Label UsernameLabel;

    public bool LoggedIn { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public int UserHighScore { get; private set; }
    public override void _Ready()
    {
        HTTPRequest = GetNode<HttpRequest>("../Node3D/CanvasLayer/HighscoreRequest");
        UsernameLabel = GetNode<Label>("../Node3D/CanvasLayer/UIContainer/UsernamePanel/MarginContainer/Username");
        UsernameLabel.Text = "Guest";
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


    //TODO
    public Error HighscoreUpdateRequest()
    {
        UserCreditentials userData = new(Username, Email, GetHighscore());
        string userDataJson = JsonSerializer.Serialize(userData);
        string[] newRegHeaders = new string[] { "Content-Type: application/json" };
        var error = HTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/forwardvector/update-highscore", newRegHeaders, HttpClient.Method.Post, userDataJson);
        GD.Print(userData.email + ", " + userData.username + ", " + userData.highscore + ".");
        return error;
    }
}
