package shared.ui

import androidx.compose.animation.*
import androidx.compose.animation.core.*
import androidx.compose.foundation.*
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.launch
import shared.data.*

@Composable
fun NoiosoBackground() {
    val infiniteTransition = rememberInfiniteTransition(label = "bg")
    val bgOffset by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = 1200f,
        animationSpec = infiniteRepeatable(tween(25000, easing = LinearEasing), RepeatMode.Reverse),
        label = "bgOffset"
    )

    Box(modifier = Modifier.fillMaxSize().background(Color(0xFF0A0A0A))) {
        Box(
            modifier = Modifier.fillMaxSize().background(
                Brush.radialGradient(
                    colors = listOf(
                        MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.5f),
                        MaterialTheme.colorScheme.tertiaryContainer.copy(alpha = 0.3f),
                        Color.Transparent
                    ),
                    center = androidx.compose.ui.geometry.Offset(bgOffset, bgOffset / 4),
                    radius = 3000f
                )
            )
        )
    }
}

@Composable
fun AppRoot() {
    var screen by remember { mutableStateOf("chat") }
    var serverIp by remember { mutableStateOf("localhost") }
    val service = remember(serverIp) { OllamaService(serverIp, "11434") }

    NoiosoAITheme(darkTheme = true) {
        Box(modifier = Modifier.fillMaxSize()) {
            NoiosoBackground()
            Crossfade(targetState = screen) { current ->
                if (current == "chat") {
                    ChatScreen(service, onSettingsClick = { screen = "settings" })
                } else {
                    SettingsScreen(serverIp, onSave = { newIp -> 
                        serverIp = newIp
                        screen = "chat" 
                    }, onBack = { screen = "chat" })
                }
            }
        }
    }
}

@Composable
fun ChatScreen(service: OllamaService, onSettingsClick: () -> Unit) {
    val messages = remember { mutableStateListOf<Message>() }
    var isGenerating by remember { mutableStateOf(false) }
    var models by remember { mutableStateOf<List<String>>(emptyList()) }
    var selectedModel by remember { mutableStateOf("") }
    var errorMsg by remember { mutableStateOf<String?>(null) }
    
    val listState = rememberLazyListState()
    val scope = rememberCoroutineScope()
    var inputText by remember { mutableStateOf("") }

    fun refreshModels() {
        scope.launch {
            val fetched = service.getModels()
            if (fetched.isNotEmpty()) {
                models = fetched
                if (selectedModel !in fetched) selectedModel = fetched[0]
            }
        }
    }

    LaunchedEffect(Unit) { refreshModels() }

    Column(modifier = Modifier.fillMaxSize()) {
        // Header
        Row(
            modifier = Modifier.fillMaxWidth().padding(horizontal = 24.dp, vertical = 24.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text("NoiosoAI", style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.ExtraBold, color = Color.White)
                Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.clickable { refreshModels() }) {
                    Text(
                        text = if (selectedModel.isEmpty()) "Click to Select Model" else "Model: $selectedModel",
                        color = MaterialTheme.colorScheme.primary,
                        fontSize = 14.sp
                    )
                    var expanded by remember { mutableStateOf(false) }
                    IconButton(onClick = { expanded = true }, modifier = Modifier.size(20.dp)) {
                        Icon(Icons.Rounded.ArrowDropDown, null, tint = MaterialTheme.colorScheme.primary)
                    }
                    DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                        models.forEach { model ->
                            DropdownMenuItem(text = { Text(model) }, onClick = {
                                selectedModel = model
                                expanded = false
                            })
                        }
                    }
                }
            }
            Row {
                TopBarButton(Icons.Rounded.Delete, "Clear", { messages.clear() }, MaterialTheme.colorScheme.errorContainer)
                Spacer(Modifier.width(12.dp))
                TopBarButton(Icons.Rounded.Settings, "Settings", onSettingsClick, MaterialTheme.colorScheme.primaryContainer)
            }
        }

        // Chat Area
        Box(modifier = Modifier.weight(1f)) {
            LazyColumn(
                state = listState,
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                itemsIndexed(messages) { index, msg ->
                    val isLast = index == messages.size - 1
                    NoiosoBubble(msg, isGenerating = isLast && isGenerating && msg.role == "assistant")
                }
            }
            
            if (errorMsg != null) {
                Surface(modifier = Modifier.align(Alignment.TopCenter).padding(16.dp), color = MaterialTheme.colorScheme.errorContainer, shape = RoundedCornerShape(16.dp)) {
                    Text(errorMsg!!, Modifier.padding(16.dp), color = MaterialTheme.colorScheme.onErrorContainer)
                }
            }
        }

        // Input Bar
        ChatInputBar(inputText, { inputText = it }, {
            if (inputText.isNotBlank() && selectedModel.isNotBlank()) {
                val prompt = inputText
                messages.add(Message("user", prompt))
                inputText = ""
                isGenerating = true
                errorMsg = null
                
                scope.launch {
                    try {
                        messages.add(Message("assistant", ""))
                        val aiIdx = messages.size - 1
                        var currentText = ""
                        service.chat(selectedModel, messages.dropLast(1)).collect { chunk ->
                            currentText += chunk
                            // This update forces the list item to refresh
                            messages[aiIdx] = Message("assistant", currentText)
                            listState.animateScrollToItem(messages.size - 1)
                        }
                    } catch (e: Exception) {
                        errorMsg = "Check if Ollama is running"
                    } finally {
                        isGenerating = false
                    }
                }
            }
        }, isGenerating)
    }
}

@Composable
fun TopBarButton(icon: ImageVector, desc: String, onClick: () -> Unit, color: Color) {
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()
    val scale by animateFloatAsState(if (isPressed) 0.85f else 1f)
    IconButton(onClick = onClick, modifier = Modifier.scale(scale), interactionSource = interactionSource) {
        Surface(shape = RoundedCornerShape(14.dp), color = if (isPressed) color else color.copy(alpha = 0.2f), modifier = Modifier.size(46.dp)) {
            Box(contentAlignment = Alignment.Center) { Icon(icon, desc, tint = Color.White, modifier = Modifier.size(24.dp)) }
        }
    }
}

@Composable
fun NoiosoBubble(message: Message, isGenerating: Boolean) {
    val isUser = message.role == "user"
    val alignment = if (isUser) Alignment.End else Alignment.Start
    val shape = if (isUser) RoundedCornerShape(28.dp, 28.dp, 4.dp, 28.dp) else RoundedCornerShape(4.dp, 28.dp, 28.dp, 28.dp)

    Column(modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp), horizontalAlignment = alignment) {
        Row(verticalAlignment = Alignment.Bottom) {
            if (!isUser) { SparkleIcon(isGenerating); Spacer(modifier = Modifier.width(12.dp)) }
            Surface(
                color = if (isUser) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.secondaryContainer.copy(alpha = 0.8f),
                shape = shape,
                modifier = Modifier.widthIn(max = 500.dp).animateContentSize()
            ) {
                val textToDisplay = if (message.content.isEmpty() && isGenerating) "..." else message.content
                Text(textToDisplay, modifier = Modifier.padding(horizontal = 20.dp, vertical = 14.dp), color = Color.White)
            }
        }
    }
}

@Composable
fun SparkleIcon(isAnimating: Boolean) {
    val infiniteTransition = rememberInfiniteTransition()
    val scale by infiniteTransition.animateFloat(0.8f, 1.2f, infiniteRepeatable(tween(1000), RepeatMode.Reverse))
    val rotation by infiniteTransition.animateFloat(0f, 360f, infiniteRepeatable(tween(3000), RepeatMode.Restart))
    Box(
        modifier = Modifier.size(36.dp).scale(if (isAnimating) scale else 1f).graphicsLayer { if (isAnimating) rotationZ = rotation }
            .clip(CircleShape).background(Brush.linearGradient(listOf(MaterialTheme.colorScheme.tertiary, MaterialTheme.colorScheme.primary))),
        contentAlignment = Alignment.Center
    ) { Icon(Icons.Rounded.AutoAwesome, null, tint = Color.White, modifier = Modifier.size(20.dp)) }
}

@Composable
fun ChatInputBar(text: String, onValueChange: (String) -> Unit, onSend: () -> Unit, isGenerating: Boolean) {
    Surface(tonalElevation = 8.dp, modifier = Modifier.padding(24.dp).clip(RoundedCornerShape(32.dp)), color = MaterialTheme.colorScheme.surface.copy(alpha = 0.9f)) {
        Row(Modifier.padding(10.dp).fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            TextField(value = text, onValueChange = onValueChange, modifier = Modifier.weight(1f).clip(RoundedCornerShape(28.dp)),
                placeholder = { Text("Ask NoiosoAI...") }, colors = TextFieldDefaults.colors(focusedIndicatorColor = Color.Transparent, unfocusedIndicatorColor = Color.Transparent))
            Spacer(Modifier.width(16.dp))
            FilledIconButton(onClick = onSend, enabled = text.isNotBlank() && !isGenerating, modifier = Modifier.size(56.dp), shape = RoundedCornerShape(16.dp)) {
                Icon(Icons.Rounded.Send, null)
            }
        }
    }
}

@Composable
fun SettingsScreen(currentIp: String, onSave: (String) -> Unit, onBack: () -> Unit) {
    var ip by remember { mutableStateOf(currentIp) }
    Column(Modifier.fillMaxSize().padding(40.dp)) {
        IconButton(onClick = onBack) { Icon(Icons.Rounded.ArrowBack, null, tint = Color.White) }
        Text("Settings", style = MaterialTheme.typography.headlineLarge, color = Color.White, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(40.dp)); Text("Ollama IP Address", color = MaterialTheme.colorScheme.primary)
        OutlinedTextField(value = ip, onValueChange = { ip = it }, modifier = Modifier.fillMaxWidth().padding(top = 16.dp), shape = RoundedCornerShape(16.dp))
        Spacer(Modifier.weight(1f))
        Button(onClick = { onSave(ip) }, modifier = Modifier.fillMaxWidth().height(68.dp), shape = CircleShape) { Text("Save & Connect", fontWeight = FontWeight.Bold) }
    }
}
