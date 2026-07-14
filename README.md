# NoiosoAI-Desktop

⚠️ **WORK IN PROGRESS:** This project is actively under development. Features, architecture, and documentation are subject to frequent changes as the application evolves.

A sleek, lightweight desktop AI companion built with cross-platform frameworks and integrated with local LLM capabilities via Ollama. 

---

## 🚀 Features

* **Local AI Processing:** Integrated with `OllamaService` for offline, private AI interactions.
* **Custom UI:** Native-feeling interface powered by a custom `AppTheme`.
* **Config Management:** Local persistence handling via `SettingsManager`.
* **Cross-Platform Foundation:** Structured supporting desktop environments with clean architecture.

## 📁 Project Structure

The project utilizes a hybrid structure supporting cross-platform application execution:
* `src/` - Kotlin Multiplatform source files (Desktop entry point, Shared UI/Data logic).
* `lib/` - Flutter/Dart source components.
* `windows/` - Native Windows build files and configurations.

## 🛠️ Prerequisites

Before running or building the project, ensure you have the following installed:
* [JDK 17 or higher](https://adoptium.net/) (for Kotlin/Gradle builds)
* [Ollama](https://ollama.com/) (running locally)
* [Flutter SDK](https://docs.flutter.dev/get-started/install) (if building the Flutter targets)

## 🏃 Getting Started

### Running the Desktop Application (Kotlin/Compose)
To start the application locally using the Gradle wrapper, run the following command in your terminal:

```bash
./gradlew run
