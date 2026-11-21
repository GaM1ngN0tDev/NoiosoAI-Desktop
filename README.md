# NoiosoAI-Desktop

> [!WARNING]
> This branch is built with WinUI 3 and targets newer Windows 11 builds. Do not try it on older versions of Windows. Windows 10 is untested.

> [!NOTE]
> Work in progress. This is an experimental rewrite of the original Flutter app. The Flutter version is still available on the `main` branch.

---

## 🚀 Features
An experimental version of NoiosoAI-Desktop built with WinUI 3 and C#. It is a desktop chat client for a local [Ollama](https://ollama.com) server.

**Chat**
- Streaming replies, shown as they are generated, with a Stop button to cancel mid-reply
- Multiple chats: start a new chat, switch between them in the sidebar, rename or delete them with a right-click
- Chats are named automatically from your first message
- Multi-line input: Enter sends, Shift+Enter adds a new line
- Markdown rendering: code blocks with a Copy button, inline code, bold text, headings and lists
- Model picker, filled automatically from your Ollama server
- Personality presets: Default, Engineer, Friendly, Wise and Sarcastic
- Everything is saved between launches

**Settings**
- Theme: use the system setting, light or dark
- Editable system prompt for each personality, with a reset-to-default button
- Ollama server IP address and port

**Look and feel**
- Native Windows 11 style: Mica Alt backdrop, collapsible sidebar and a custom title bar
- Purple accent color
- Animations: sliding page transitions, fade-in message bubbles, animated typing dots while the model is thinking, a cursor while text streams in

## 📁 Project Structure
```
NoiosoAI-Desktop/
├── README.md
└── winui3/
    └── NoiosoAI-Desktop/
        ├── NoiosoAI-Desktop.slnx
        └── NoiosoAI-Desktop/
            ├── App.xaml(.cs)           # app entry point and purple accent theme
            ├── MainWindow.xaml(.cs)    # window, title bar, sidebar and chat list
            ├── ChatPage.xaml(.cs)      # chat UI, pickers, streaming and stop
            ├── SettingsPage.xaml(.cs)  # theme, server and system prompts
            ├── MarkdownView.cs         # built-in Markdown renderer
            ├── TypingDots.cs           # animated "thinking" indicator
            ├── Core.cs                 # chats, saved state and Ollama client
            ├── Assets/                 # app icons
            ├── Package.appxmanifest
            └── NoiosoAI-Desktop.csproj
```

## 🛠️ Prerequisites
- Windows 11 (recent build)
- A recent Visual Studio with the **WinUI application development** workload (the project uses the newer `.slnx` solution format)
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
Your chats, theme and custom prompts are saved in a local file in the app's own data folder on your PC. Server settings are stored in the Windows app settings. Chats are saved as plain, unencrypted text. Nothing is sent anywhere except to the Ollama server you configure.
