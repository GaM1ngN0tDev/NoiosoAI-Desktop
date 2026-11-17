# NoiosoAI-Desktop

> [!WARNING]
> This branch is built with WinUI 3 and targets newer Windows 11 builds. Do not try it on older versions of Windows. Windows 10 is untested.

> [!NOTE]
> Work in progress. This is an experimental rewrite of the original Flutter app. The Flutter version is still available on the `main` branch.

---

## 🚀 Features
An experimental version of NoiosoAI-Desktop built with WinUI 3 and C#. It is a desktop chat client for a local [Ollama](https://ollama.com) server.

- Native Windows 11 look: Mica backdrop, sidebar navigation and a custom title bar
- Streaming replies, shown word by word as they are generated
- Stop button to cancel a reply mid-stream
- Model picker, filled automatically from your Ollama server
- Personality presets: Default, Engineer, Friendly, Wise and Sarcastic
- Markdown rendering: code blocks with a Copy button, inline code, bold text, headings and lists
- Chat history saved between launches
- Server settings page for the Ollama IP address and port
- Purple accent color

## 📁 Project Structure
```
NoiosoAI-Desktop/
├── README.md
└── winui3/
    └── NoiosoAI-Desktop/
        ├── NoiosoAI-Desktop.slnx
        └── NoiosoAI-Desktop/
            ├── App.xaml(.cs)           # app entry point and purple accent theme
            ├── MainWindow.xaml(.cs)    # window, title bar and sidebar navigation
            ├── ChatPage.xaml(.cs)      # chat UI, pickers, streaming and stop
            ├── SettingsPage.xaml(.cs)  # Ollama server IP and port
            ├── MarkdownView.cs         # built-in Markdown renderer
            ├── Core.cs                 # chat messages, saved state and Ollama client
            ├── Assets/                 # app icons
            ├── Package.appxmanifest
            └── NoiosoAI-Desktop.csproj
```

## 🛠️ Prerequisites
- Windows 11 (recent build)
- Visual Studio 2022 or newer with the **WinUI application development** workload
- .NET SDK (the version that comes with Visual Studio)
- Windows **Developer Mode** turned on (Settings → System → For developers)
- [Ollama](https://ollama.com) running with at least one model pulled, for example `ollama pull llama3`

## ▶️ Build and Run
1. Clone the repo and switch to this branch:
   ```
   git clone https://github.com/GaM1ngN0tDev/NoiosoAI-Desktop
   cd NoiosoAI-Desktop
   git checkout winui3-experiment
   ```
2. Open `winui3/NoiosoAI-Desktop/NoiosoAI-Desktop.slnx` in Visual Studio.
3. Set the platform to **x64** and press **F5**.
4. If Ollama is not on the same PC, open **Settings** in the app and enter its IP address and port (the default is `localhost:11434`).

## 💾 Where Data Is Stored
Chat history and server settings are stored locally in the app's own data folder on your PC. Nothing is sent anywhere except to the Ollama server you configure.




