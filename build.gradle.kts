plugins {
    kotlin("multiplatform") version "1.9.20"
    id("org.jetbrains.compose") version "1.5.11"
    kotlin("plugin.serialization") version "1.9.20"
}

repositories {
    google()
    mavenCentral()
    maven("https://maven.pkg.jetbrains.space/public/p/compose/public")
}

kotlin {
    jvm("desktop")
    sourceSets {
        val desktopMain by getting {
            kotlin.srcDirs("src/shared", "src/desktopMain/kotlin")
            dependencies {
                implementation(compose.desktop.currentOs)
                implementation(compose.material3)
                implementation(compose.materialIconsExtended)
                // Ktor for networking
                implementation("io.ktor:ktor-client-core:2.3.7")
                implementation("io.ktor:ktor-client-cio:2.3.7")
                implementation("io.ktor:ktor-client-content-negotiation:2.3.7")
                implementation("io.ktor:ktor-serialization-kotlinx-json:2.3.7")
                // Coroutines
                implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.7.3")
                // Logging to fix SLF4J warning
                implementation("org.slf4j:slf4j-simple:2.0.9")
            }
        }
    }
}

compose.desktop {
    application {
        mainClass = "MainKt"
        nativeDistributions {
            targetFormats(org.jetbrains.compose.desktop.application.dsl.TargetFormat.Exe)
            packageName = "NoiosoAI"
        }
    }
}
