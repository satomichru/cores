﻿using cores;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.ApplicationSettings;

namespace Cores;

public sealed partial class MainWindow : Window {
	private readonly DispatcherTimer APIRefresher;

	[DllImport("lib.dll")]
	private static extern string dialog(string name);

	[DllImport("lib.dll")]
	private static extern void setSettings(Settings settings);

	[DllImport("lib.dll")]
	private static extern void autoLaunch(string exe);

	public MainWindow() {
		InitializeComponent();

		// Set titlebar
		if (AppWindowTitleBar.IsCustomizationSupported()) {
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);
		} else {
			AppTitleBar.Visibility = Visibility.Collapsed;
		}

		// Set Mica
		var mica = new MicaBackrop(this);
		mica.TrySetSystemBackdrop();

		// Webview setup
		Init();

		APIRefresher = new DispatcherTimer();
		APIRefresher.Tick += RefreshAPI;
		APIRefresher.Interval = new TimeSpan(0, 0, App.GlobalSettings.interval);
		APIRefresher.Start();
	}

	// Refresh API
	public void RefreshAPI(object sender, object e) {
		System.Threading.Tasks.Task.Run(() => {
			App.GlobalHardwareInfo.Refresh();
		});

		SendAPI();
	}

	// Start webview
	private async void Init() {
		await webView.EnsureCoreWebView2Async();
		webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

		if (!Debugger.IsAttached) {
			webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
		}

		webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
			"app.example", "assets", CoreWebView2HostResourceAccessKind.Allow);

		if (!Debugger.IsAttached) {
			webView.Source = new Uri("http://app.example/assets/index.html");
		}

		webView.CoreWebView2.DOMContentLoaded += EventHandler;
		webView.WebMessageReceived += WebView_WebMessageReceived;
		webView.CoreWebView2.NewWindowRequested += WebView_NewWindowRequested;
	}

	// Navigation view navigation
	private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args) {
		webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new Message() {
			Name = "navigation",
			Content = args.InvokedItem.ToString().ToLower()
		}, App.SerializerOptions));
	}

	// Send settings to the interface
	public void EventHandler(object target, CoreWebView2DOMContentLoadedEventArgs arg) {
		SendAPI();

		var message = new Message() {
			Name = "settings",
			Content = JsonSerializer.Serialize(App.GlobalSettings, App.SerializerOptions)
		};

		webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, App.SerializerOptions));
	}

	// Open links in browser
	public void WebView_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs args) {
		Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
		args.Handled = true;
	}

	// WebView events
	public async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args) {
		var content = JsonSerializer.Deserialize<Message>(args.WebMessageAsJson, App.SerializerOptions);
		Debug.WriteLine($"Message: {content.Name}");

		switch (content.Name) {
			case "about":
				aboutDialogText.Text = content.Content;

				var dialogResult = await aboutDialog.ShowAsync();

				if (dialogResult.ToString() == "Primary") {
					var data = new DataPackage();
					data.SetText(content.Content);

					Clipboard.SetContent(data);
				}
				break;

			case "newSettings":
				App.GlobalSettings = JsonSerializer.Deserialize<Settings>(content.Content, App.SerializerOptions);

				APIRefresher.Interval = new TimeSpan(0, 0, App.GlobalSettings.interval);

				setSettings(App.GlobalSettings);
				break;

			case "debug":

				var path = dialog("cores-debug");

				var contents = $"{content.Content}\n{App.GlobalHardwareInfo.computer.GetReport()}";

				// write to file
				System.IO.File.WriteAllText(path, contents);
				break;

			case "launchOnStartup":
				var exe = AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/") + "Cores.exe";

				autoLaunch(exe);
				break;
		}
	}

	// Send API info to the interface
	public void SendAPI() {
		var appVersion = Assembly.GetExecutingAssembly().GetName().Version;

		App.GlobalHardwareInfo.API.System.OS.WebView = webView.CoreWebView2.Environment.BrowserVersionString;
		App.GlobalHardwareInfo.API.System.OS.App = $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}";
		App.GlobalHardwareInfo.API.System.OS.Runtime = "1.3.230724000";

		var message = new Message() {
			Name = "api",
			Content = JsonSerializer.Serialize(App.GlobalHardwareInfo.API, App.SerializerOptions)
		};

		webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, App.SerializerOptions));
	}
}
