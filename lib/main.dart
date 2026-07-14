import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'ollama_service.dart';

void main() => runApp(const NoiosoApp());

class NoiosoApp extends StatelessWidget {
  const NoiosoApp({super.key});
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorSchemeSeed: const Color(0xFFD0BCFF),
        brightness: Brightness.dark,
        textTheme: GoogleFonts.plusJakartaSansTextTheme(ThemeData.dark().textTheme),
      ),
      home: const MainLayout(),
    );
  }
}

class MainLayout extends StatefulWidget {
  const MainLayout({super.key});
  @override
  State<MainLayout> createState() => _MainLayoutState();
}

class _MainLayoutState extends State<MainLayout> with TickerProviderStateMixin {
  late AnimationController _bgController;
  int _screenIndex = 0;
  final OllamaService _service = OllamaService();

  // Chat state preserved across screen transitions
  final List<Message> _messages = [];
  List<String> _models = [];
  String? _selectedModel;

  // --- PRE-MADE ENGLISH PERSONALITIES ---
  final Map<String, String> _personalities = {
    "Default": "You are NoiosoAI, a helpful, polite, and precise AI assistant.",
    "Engineer": "You are a Senior Software Engineer. Give highly technical, code-focused, concise, and optimized answers. Avoid unnecessary small talk.",
    "Friendly": "You are a super friendly, empathetic, and warm assistant. Use an informal tone, support the user, and show enthusiasm using matching emojis.",
    "Wise": "You are a wise philosopher. Respond with deep, well-thought-out reflections, calmly analyzing pros and cons with poetic nuance.",
    "Sarcastic": "You are NoiosoAI, but today you are extremely sarcastic, witty, and slightly lazy. Tease the user playfully while still providing the actual solution.",
  };
  
  late String _selectedPersonalityKey;

  @override
  void initState() {
    super.initState();
    _selectedPersonalityKey = _personalities.keys.first;
    _bgController = AnimationController(vsync: this, duration: const Duration(seconds: 25))..repeat(reverse: true);
    _loadModels();
  }

  void _loadModels() async {
    final m = await _service.getModels();
    if (m.isNotEmpty) {
      setState(() { 
        _models = m; 
        if (_selectedModel == null || !_models.contains(_selectedModel)) {
          _selectedModel = m.first;
        }
      });
    }
  }

  @override
  void dispose() {
    _bgController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0A0A0A),
      body: Stack(
        children: [
          // Animated Background
          AnimatedBuilder(
            animation: _bgController,
            builder: (context, child) {
              return Stack(
                children: [
                  Positioned(
                    top: -200 + (150 * _bgController.value),
                    left: -100 + (100 * _bgController.value),
                    child: _Blob(color: const Color(0xFF6750A4).withValues(alpha: 0.4), size: 900),
                  ),
                  Positioned(
                    bottom: -300 + (200 * _bgController.value),
                    right: -200 - (150 * _bgController.value),
                    child: _Blob(color: const Color(0xFF381E72).withValues(alpha: 0.35), size: 1100),
                  ),
                ],
              );
            },
          ),
          // Glassmorphism Main Frame
          Center(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(42),
              child: BackdropFilter(
                filter: ImageFilter.blur(sigmaX: 50, sigmaY: 50),
                child: Container(
                  width: MediaQuery.of(context).size.width * 0.94,
                  height: MediaQuery.of(context).size.height * 0.92,
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.55),
                    borderRadius: BorderRadius.circular(42),
                    border: Border.all(color: Colors.white.withValues(alpha: 0.1), width: 1.2),
                  ),
                  child: _screenIndex == 0 
                    ? ChatView(
                        service: _service, 
                        messages: _messages,
                        models: _models,
                        selectedModel: _selectedModel,
                        personalities: _personalities.keys.toList(),
                        selectedPersonality: _selectedPersonalityKey,
                        systemPrompt: _personalities[_selectedPersonalityKey] ?? "",
                        onModelChanged: (newModel) => setState(() => _selectedModel = newModel),
                        onPersonalityChanged: (newPers) => setState(() => _selectedPersonalityKey = newPers!),
                        onSettings: () => setState(() => _screenIndex = 1),
                      )
                    : SettingsView(onBack: () {
                        setState(() => _screenIndex = 0);
                        _loadModels();
                      }),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Blob extends StatelessWidget {
  final Color color;
  final double size;
  const _Blob({required this.color, required this.size});
  @override
  Widget build(BuildContext context) {
    return Container(width: size, height: size, decoration: BoxDecoration(shape: BoxShape.circle, color: color));
  }
}

// --- CHAT VIEW ---
class ChatView extends StatefulWidget {
  final OllamaService service;
  final List<Message> messages;
  final List<String> models;
  final String? selectedModel;
  final List<String> personalities;
  final String selectedPersonality;
  final String systemPrompt;
  final ValueChanged<String?> onModelChanged;
  final ValueChanged<String?> onPersonalityChanged;
  final VoidCallback onSettings;

  const ChatView({
    required this.service, 
    required this.messages,
    required this.models,
    required this.selectedModel,
    required this.personalities,
    required this.selectedPersonality,
    required this.systemPrompt,
    required this.onModelChanged,
    required this.onPersonalityChanged,
    required this.onSettings, 
    super.key,
  });

  @override
  State<ChatView> createState() => _ChatViewState();
}

class _ChatViewState extends State<ChatView> {
  final TextEditingController _controller = TextEditingController();
  final ScrollController _scroll = ScrollController();

  void _clearChat() {
    setState(() {
      widget.messages.clear();
    });
  }

  void _send() {
    if (_controller.text.isEmpty || widget.selectedModel == null || widget.selectedModel!.isEmpty) return;
    final text = _controller.text;
    setState(() {
      widget.messages.add(Message(role: 'user', content: text));
      widget.messages.add(Message(role: 'assistant', content: ''));
      _controller.clear();
    });

    String fullResponse = "";
    
    widget.service.chatStream(
      widget.selectedModel!, 
      widget.messages.sublist(0, widget.messages.length - 1),
      widget.systemPrompt
    ).listen((chunk) {
      fullResponse += chunk;
      setState(() => widget.messages.last = Message(role: 'assistant', content: fullResponse));
      
      if (_scroll.hasClients) {
        _scroll.animateTo(
          _scroll.position.maxScrollExtent, 
          duration: const Duration(milliseconds: 300), 
          curve: Curves.easeOut
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // TOP BAR
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 20),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                "NoiosoAI", 
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.w900)
              ),
              
              // DROPDOWNS (Model + Personality)
              Row(
                children: [
                  // Models Dropdown
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.05),
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: Colors.white.withValues(alpha: 0.1)),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: widget.selectedModel,
                        hint: const Text("Select Model", style: TextStyle(fontSize: 13)),
                        icon: const Icon(Icons.keyboard_arrow_down, size: 16),
                        dropdownColor: const Color(0xFF1A1A1A),
                        borderRadius: BorderRadius.circular(16),
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.primary, 
                          fontWeight: FontWeight.bold,
                          fontSize: 13
                        ),
                        items: widget.models.map((String value) {
                          return DropdownMenuItem<String>(
                            value: value,
                            child: Text(value),
                          );
                        }).toList(),
                        onChanged: widget.onModelChanged,
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  
                  // Personalities Dropdown
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.05),
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: Colors.white.withValues(alpha: 0.1)),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: widget.selectedPersonality,
                        icon: const Icon(Icons.face, size: 16),
                        dropdownColor: const Color(0xFF1A1A1A),
                        borderRadius: BorderRadius.circular(16),
                        style: const TextStyle(
                          color: Colors.cyanAccent, 
                          fontWeight: FontWeight.bold,
                          fontSize: 13
                        ),
                        items: widget.personalities.map((String value) {
                          return DropdownMenuItem<String>(
                            value: value,
                            child: Text(value),
                          );
                        }).toList(),
                        onChanged: widget.onPersonalityChanged,
                      ),
                    ),
                  ),
                ],
              ),
              
              // TOP BAR BUTTONS
              Row(
                children: [
                  IconButton(
                    onPressed: _clearChat, 
                    icon: const Icon(Icons.delete_outline, color: Colors.redAccent),
                    tooltip: "Clear Chat",
                  ),
                  const SizedBox(width: 8),
                  IconButton(
                    onPressed: widget.onSettings, 
                    icon: const Icon(Icons.settings),
                    tooltip: "Settings",
                  ),
                ],
              ),
            ],
          ),
        ),
        const Divider(height: 1, color: Colors.white10),
        // CHAT WINDOW
        Expanded(
          child: widget.messages.isEmpty
              ? Center(
                  child: Text(
                    "How can I help you today?", 
                    style: TextStyle(fontSize: 20, color: Colors.white.withValues(alpha: 0.4))
                  ),
                )
              : ListView.builder(
                  controller: _scroll,
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
                  itemCount: widget.messages.length,
                  itemBuilder: (context, i) {
                    final isUser = widget.messages[i].role == 'user';
                    return Align(
                      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
                      child: Container(
                        margin: const EdgeInsets.only(bottom: 16),
                        padding: const EdgeInsets.all(18),
                        constraints: const BoxConstraints(maxWidth: 600),
                        decoration: BoxDecoration(
                          color: isUser 
                              ? Theme.of(context).colorScheme.primaryContainer 
                              : const Color(0xFF1E1B24),
                          borderRadius: BorderRadius.only(
                            topLeft: const Radius.circular(24), 
                            topRight: const Radius.circular(24),
                            bottomLeft: Radius.circular(isUser ? 24 : 4), 
                            bottomRight: Radius.circular(isUser ? 4 : 24),
                          ),
                        ),
                        child: Text(
                          widget.messages[i].content, 
                          style: const TextStyle(fontSize: 16, height: 1.4)
                        ),
                      ),
                    );
                  },
                ),
        ),
        // PROMPT INPUT BAR
        Padding(
          padding: const EdgeInsets.all(24),
          child: Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _controller,
                  onSubmitted: (_) => _send(),
                  decoration: InputDecoration(
                    hintText: "Ask NoiosoAI...",
                    filled: true, 
                    fillColor: Colors.white.withValues(alpha: 0.05),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(32), 
                      borderSide: BorderSide.none
                    ),
                    contentPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              FloatingActionButton(
                onPressed: _send, 
                child: const Icon(Icons.arrow_upward),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// --- SETTINGS VIEW ---
class SettingsView extends StatefulWidget {
  final VoidCallback onBack;
  const SettingsView({required this.onBack, super.key});
  @override
  State<SettingsView> createState() => _SettingsViewState();
}

class _SettingsViewState extends State<SettingsView> {
  final _ipController = TextEditingController();
  final _portController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _load();
  }

  void _load() async {
    final prefs = await SharedPreferences.getInstance();
    _ipController.text = prefs.getString(OllamaService.keyIp) ?? 'localhost';
    _portController.text = prefs.getString(OllamaService.keyPort) ?? '11434';
  }

  void _save() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(OllamaService.keyIp, _ipController.text);
    await prefs.setString(OllamaService.keyPort, _portController.text);
    widget.onBack();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(40),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          IconButton(onPressed: widget.onBack, icon: const Icon(Icons.arrow_back)),
          const Text("Server Settings", style: TextStyle(fontSize: 36, fontWeight: FontWeight.bold)),
          const SizedBox(height: 32),
          const Text("Server IP Address", style: TextStyle(fontWeight: FontWeight.bold)),
          TextField(controller: _ipController),
          const SizedBox(height: 20),
          const Text("Server Port", style: TextStyle(fontWeight: FontWeight.bold)),
          TextField(controller: _portController),
          const Spacer(),
          SizedBox(
            width: double.infinity,
            height: 60,
            child: ElevatedButton(
              onPressed: _save, 
              child: const Text("Save & Connect", style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold))
            ),
          ),
        ],
      ),
    );
  }
}