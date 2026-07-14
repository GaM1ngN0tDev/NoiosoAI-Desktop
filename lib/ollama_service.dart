import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class Message {
  final String role;
  final String content;

  Message({required this.role, required this.content});

  Map<String, dynamic> toJson() => {'role': role, 'content': content};
}

class OllamaService {
  static const String keyIp = 'ollama_ip';
  static const String keyPort = 'ollama_port';

  Future<String> getBaseUrl() async {
    final prefs = await SharedPreferences.getInstance();
    final ip = prefs.getString(keyIp) ?? 'localhost';
    final port = prefs.getString(keyPort) ?? '11434';
    
    String formattedIp = ip.trim();
    if (!formattedIp.startsWith('http://') && !formattedIp.startsWith('https://')) {
      formattedIp = 'http://$formattedIp';
    }
    return '$formattedIp:$port';
  }

  Future<List<String>> getModels() async {
    try {
      final baseUrl = await getBaseUrl();
      final res = await http.get(Uri.parse('$baseUrl/api/tags'));
      if (res.statusCode == 200) {
        final data = jsonDecode(res.body);
        if (data['models'] != null) {
          return (data['models'] as List).map((m) => m['name'] as String).toList();
        }
      }
    } catch (e) {
      print('Fetch Models Error: $e');
    }
    return [];
  }

  /// Streams the chat response while injecting the system prompt.
  Stream<String> chatStream(String model, List<Message> messages, String systemPrompt) async* {
    try {
      final baseUrl = await getBaseUrl();
      final request = http.Request('POST', Uri.parse('$baseUrl/api/chat'));
      request.headers['Content-Type'] = 'application/json';

      final List<Map<String, dynamic>> payloadMessages = [];
      if (systemPrompt.isNotEmpty) {
        payloadMessages.add({'role': 'system', 'content': systemPrompt});
      }
      payloadMessages.addAll(messages.map((m) => m.toJson()).toList());

      request.body = jsonEncode({
        'model': model,
        'messages': payloadMessages,
        'stream': true,
      });

      final response = await http.Client().send(request);
      
      await for (final line in response.stream.transform(utf8.decoder).transform(const LineSplitter())) {
        if (line.trim().isNotEmpty) {
          final data = jsonDecode(line);
          final content = data['message']?['content'] ?? '';
          if (content.isNotEmpty) {
            yield content;
          }
        }
      }
    } catch (e) {
      print('Chat Stream Error: $e');
      yield 'Error: Unable to connect to Ollama. Please check your server status.';
    }
  }
}