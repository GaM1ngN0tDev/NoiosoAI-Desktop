# NoiosoAI Desktop 🌌

NoiosoAI Desktop is a native Windows client for **Ollama**, designed as a high-fidelity replica of the original NoiosoAI Android application. Built with **Compose Multiplatform**, it brings a premium, privacy-focused AI chat experience to your desktop with a modern Material 3 "Expressive" aesthetic.

> [!CAUTION]
> **Build in Progress:** This project is currently in early development (Beta). You may encounter bugs, and features are being added frequently.

## ✨ Key Features
- **Native Performance:** Built with Kotlin and Compose Multiplatform for a smooth, hardware-accelerated experience on Windows.
- **Living Wallpaper:** An organic, animated background that flows behind the UI, replicating the "Expressive" feel of the Android version.
- **Glassmorphism UI:** A sleek, semi-transparent chat interface with modern Material 3 shapes and deep dark-mode support.
- **Streaming Responses:** Real-time, word-by-word text generation.
- **Local & Private:** Connects directly to your local Ollama instance. Your data never leaves your network.

## 🚀 Getting Started

### Prerequisites
1.  **Ollama:** Download and install from [ollama.com](https://ollama.com/).
2.  **Pull a Model:** Open your terminal and run:
    `ollama pull llama3.2:1b` (or your preferred model).

### Setup
1.  Launch **NoiosoAI Desktop**.
2.  Go to **Settings** (Gear icon ⚙️).
3.  Enter your Ollama Server IP (Default is `localhost`).
4.  Return to the chat and select your model from the dropdown menu in the header.
5.  Start chatting!

## 🛠 Tech Stack
- **UI Framework:** Compose Multiplatform (Jetpack Compose for Desktop)
- **Networking:** Ktor Client with CIO engine
- **Serialization:** Kotlinx Serialization (JSON)
- **Concurrency:** Kotlin Coroutines & Flows
- **Build System:** Gradle

## 🤝 Contributing
Since this is a build in progress, contributions are welcome! Feel free to open issues or submit pull requests to help improve the Windows experience.

## 📄 License
Distributed under the MIT License. See `LICENSE` for more information.
