package shared.data

import io.ktor.client.*
import io.ktor.client.call.*
import io.ktor.client.plugins.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
import io.ktor.utils.io.*
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.serialization.json.Json
import kotlinx.serialization.Serializable

@Serializable
data class Message(val role: String, val content: String)

@Serializable
data class ChatRequest(val model: String, val messages: List<Message>, val stream: Boolean = true)

@Serializable
data class ChatResponse(
    val message: Message? = null,
    val done: Boolean? = false
)

@Serializable
data class ModelResponse(val models: List<OllamaModel> = emptyList())

@Serializable
data class OllamaModel(val name: String)

class OllamaService(private val host: String, private val port: String) {
    // This is the key fix: creating a lenient JSON instance for the manual parsing
    private val json = Json { 
        ignoreUnknownKeys = true 
        isLenient = true 
        coerceInputValues = true
    }

    private val client = HttpClient {
        install(ContentNegotiation) { 
            json(json) 
        }
        install(HttpTimeout) {
            requestTimeoutMillis = 60000
        }
    }

    private val baseUrl = "http://$host:$port"

    suspend fun getModels(): List<String> = try {
        val res: ModelResponse = client.get("$baseUrl/api/tags").body()
        res.models.map { it.name }
    } catch (e: Exception) { 
        emptyList() 
    }

    fun chat(model: String, messages: List<Message>): Flow<String> = flow {
        try {
            client.preparePost("$baseUrl/api/chat") {
                contentType(ContentType.Application.Json)
                setBody(ChatRequest(model, messages))
            }.execute { response ->
                val channel = response.bodyAsChannel()
                while (!channel.isClosedForRead) {
                    val line = channel.readUTF8Line() ?: break
                    if (line.isBlank()) continue
                    
                    try {
                        // Use the lenient json instance here!
                        val content = json.decodeFromString<ChatResponse>(line).message?.content ?: ""
                        if (content.isNotEmpty()) {
                            emit(content)
                        }
                    } catch (e: Exception) {
                        // This was where it was failing before
                    }
                }
            }
        } catch (e: Exception) {
            emit("Error: ${e.localizedMessage}")
        }
    }
}
