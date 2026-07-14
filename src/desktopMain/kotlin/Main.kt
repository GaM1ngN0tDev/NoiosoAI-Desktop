import androidx.compose.ui.window.Window
import androidx.compose.ui.window.application
import shared.ui.AppRoot

fun main() = application {
    Window(onCloseRequest = ::exitApplication, title = "NoiosoAI Desktop") {
        AppRoot()
    }
}
