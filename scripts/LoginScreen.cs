using Godot;
using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class LoginScreen : Panel
{
	// Called when the node enters the scene tree for the first time.
	private string LoginEmail, LoginPassword;
	LineEdit EmailInput, PasswordInput;
	HttpRequest HTTPRequest;

	LoggedInUser User;
	Panel ErrorPanel, LoggedInPage;
	Label ErrorMessage, UserLabel;
	Button Logout, Login, RegButton, GuestButton;
	PanelContainer MainMenu, AccountMenu;
	// AnimatedSprite2D LoadingIconRef;
	SaveManager SaveManager;


	public override void _Ready()
	{
		SaveManager = GetNode<SaveManager>("/root/SaveManager");

		MainMenu = GetNode<PanelContainer>("%MenuButtons");
		AccountMenu = GetNode<PanelContainer>("%AccountMenu");
		EmailInput = GetNode<LineEdit>("%Email");
		PasswordInput = GetNode<LineEdit>("%Password");
		HTTPRequest = GetNode<HttpRequest>("%LoginRequest");

		Logout = GetNode<Button>("%Logout");
		Login = GetNode<Button>("%LoginConfirm");

		RegButton = GetNode<Button>("%GoToRegPage");
		GuestButton = GetNode<Button>("%AsGuest");

		UserLabel = GetNode<Label>("%UserLabel");

		LoggedInPage = GetNode<Panel>("%LoggedInScreen");

		ErrorPanel = GetNode<Panel>("%ErrorPanel");
		ErrorMessage = GetNode<Label>("%ErrorMessage");
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
		// LoadingIconRef = GetNode<AnimatedSprite2D>("%LoadingIcon");

		ErrorPanel.Hide();
	}

	public override void _Process(double delta)
	{

	}
	public void _on_login_email_field_text_changed(string newText)
	{
		LoginEmail = newText;
		ErrorPanel.Hide();

	}
	public void _on_login_email_field_text_submitted(string newText)
	{
		LoginEmail = newText;
		PasswordInput.GrabFocus();
	}

	public void _on_login_password_field_text_changed(string newText)
	{
		LoginPassword = newText;
		ErrorPanel.Hide();
	}
	public void _on_login_password_field_text_submitted(string newText)
	{
		LoginPassword = newText;
		LoginRequest();
	}

	public void _on_login_button_pressed()
	{
		LoginRequest();
	}
	public async void _on_login_request_request_completed(long result, long responseCode, string[] headers, byte[] body)
	{
		var response = Json.ParseString(body.GetStringFromUtf8());
		await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		// GD.Print(responseCode);
		if (responseCode == 200)
		{
			// GD.Print(responseCode);

			Login.Disabled = false;
			ErrorPanel.Hide();
			var dict = (Godot.Collections.Dictionary)response;

			if (!String.IsNullOrWhiteSpace(dict[key: "username"].ToString()))
			{
				User.Login(dict[key: "username"].ToString());
			}
			User.SetHighscore((float)dict[key: "highscore"]);
			if (!String.IsNullOrWhiteSpace(LoginEmail))
			{
				User.SetEmail(LoginEmail);
			}
			UserLabel.Text = dict[key: "username"].ToString();
			UserLabel.Show();
			LoggedInPage.Show();
			Hide();

			EmailInput.Editable = true;
			PasswordInput.Editable = true;
			// LoadingIconRef.Hide();
			User.GetHighscoreRequest();
		}
		else
		{
			Login.Disabled = false;

			EmailInput.Show();
			PasswordInput.Show();
			Login.Show();

			Logout.Hide();
			UserLabel.Hide();
			var dict = (Godot.Collections.Dictionary)response;
			// GD.Print(dict.Keys);
			if (dict[key: "response_text"].ToString().Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
			{
				ErrorMessage.Text = "Too Many Attempts Try Again Later";
			}
			else
			{
				// GD.Print(response);
				ErrorMessage.Text = "Invalid Login";
			}
			ErrorPanel.Show();
			EmailInput.Editable = true;
			PasswordInput.Editable = true;
			// LoadingIconRef.Hide();
		}
	}

	public Error _on_logout_pressed()
	{
		Show();
		LoggedInPage.Hide();

		User.SetUsername("Guest");
		User.Email = null;
		User.LoggedIn = false;
		return Error.Ok;
	}

	public Error LoginRequest()
	{
		Login.Disabled = true;
		// LoadingIconRef.Show();

		EmailInput.Editable = false;
		PasswordInput.Editable = false;

		if (User.GetUsername() == "Guest")
		{
			string[] newRegHeaders = new string[] { "Content-Type: application/json" };
			UserRegCreditentials LoginCredentials = new(LoginEmail, LoginPassword, true);
			string JsonString = JsonSerializer.Serialize(LoginCredentials);
			var error = HTTPRequest.Request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-user/login", newRegHeaders, HttpClient.Method.Post, JsonString);
			// GD.Print(error);
			return error;
		}
		else
		{
			// GD.Print("Already Logged in");
			// User.Logout();
			return Error.Ok;
		}
	}

	public void _on_back_to_menu_pressed()
	{
		AccountMenu.Hide();
		MainMenu.Show();
	}
}
