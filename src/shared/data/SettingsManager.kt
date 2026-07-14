package com.noiosoai.service

import java.util.prefs.Preferences

/**
 * Handles persistence for application settings (Server IP, Port).
 */
class SettingsManager {
    private val prefs: Preferences = Preferences.userRoot().node("NoiosoAIApp")

    companion object {
        const val IP_KEY = "server_ip"
        const val PORT_KEY = "port"
    }

    /**
     * Saves the current server IP address.
     */
    fun saveServerIp(ip: String) {
        prefs.put(IP_KEY, ip)
    }

    /**
     * Retrieves the saved server IP address, or a default value if none exists.
     */
    fun loadServerIp(): String = prefs.get(IP_KEY, "localhost")

    /**
     * Saves the current port number.
     */
    fun savePort(port: Int) {
        prefs.put(PORT_KEY, port.toString())
    }

    /**
     * Retrieves the saved port number, or a default value if none exists.
     */
    fun loadPort(): Int = prefs.get(PORT_KEY, "11434").toIntOrNull() ?: 11434
}