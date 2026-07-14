package shared.ui

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Shapes
import androidx.compose.ui.unit.dp

// --- Vibrant energetic color scheme from Android app ---

// Light Colors
val PrimaryLight = Color(0xFF0061A4)
val OnPrimaryLight = Color(0xFFFFFFFF)
val PrimaryContainerLight = Color(0xFFD1E4FF)
val OnPrimaryContainerLight = Color(0xFF001D36)
val SecondaryLight = Color(0xFF535F70)
val OnSecondaryLight = Color(0xFFFFFFFF)
val SecondaryContainerLight = Color(0xFFD7E3F7)
val OnSecondaryContainerLight = Color(0xFF101C2B)
val TertiaryLight = Color(0xFF6B5778)
val OnTertiaryLight = Color(0xFFFFFFFF)
val TertiaryContainerLight = Color(0xFFF2DAFF)
val OnTertiaryContainerLight = Color(0xFF251431)
val ErrorLight = Color(0xFFBA1A1A)
val OnErrorLight = Color(0xFFFFFFFF)
val ErrorContainerLight = Color(0xFFFFDAD6)
val OnErrorContainerLight = Color(0xFF410002)
val BackgroundLight = Color(0xFFF8F9FF)
val OnBackgroundLight = Color(0xFF191C20)
val SurfaceLight = Color(0xFFF8F9FF)
val OnSurfaceLight = Color(0xFF191C20)

// Dark Colors
val PrimaryDark = Color(0xFF9ECAFF)
val OnPrimaryDark = Color(0xFF003258)
val PrimaryContainerDark = Color(0xFF00497D)
val OnPrimaryContainerDark = Color(0xFFD1E4FF)
val SecondaryDark = Color(0xFFBBC7DB)
val OnSecondaryDark = Color(0xFF253140)
val SecondaryContainerDark = Color(0xFF3B4858)
val OnSecondaryContainerDark = Color(0xFFD7E3F7)
val TertiaryDark = Color(0xFFD7BEE4)
val OnTertiaryDark = Color(0xFF3B2948)
val TertiaryContainerDark = Color(0xFF523F5F)
val OnTertiaryContainerDark = Color(0xFFF2DAFF)
val ErrorDark = Color(0xFFFFB4AB)
val OnErrorDark = Color(0xFF690005)
val ErrorContainerDark = Color(0xFF93000A)
val OnErrorContainerDark = Color(0xFFFFDAD6)
val BackgroundDark = Color(0xFF111418)
val OnBackgroundDark = Color(0xFFE2E2E6)
val SurfaceDark = Color(0xFF111418)
val OnSurfaceDark = Color(0xFFE2E2E6)

private val LightColorScheme = lightColorScheme(
    primary = PrimaryLight,
    onPrimary = OnPrimaryLight,
    primaryContainer = PrimaryContainerLight,
    onPrimaryContainer = OnPrimaryContainerLight,
    secondary = SecondaryLight,
    onSecondary = OnSecondaryLight,
    secondaryContainer = SecondaryContainerLight,
    onSecondaryContainer = OnSecondaryContainerLight,
    tertiary = TertiaryLight,
    onTertiary = OnTertiaryLight,
    tertiaryContainer = TertiaryContainerLight,
    onTertiaryContainer = OnTertiaryContainerLight,
    error = ErrorLight,
    onError = OnErrorLight,
    errorContainer = ErrorContainerLight,
    onErrorContainer = OnErrorContainerLight,
    background = BackgroundLight,
    onBackground = OnBackgroundLight,
    surface = SurfaceLight,
    onSurface = OnSurfaceLight,
)

private val DarkColorScheme = darkColorScheme(
    primary = PrimaryDark,
    onPrimary = OnPrimaryDark,
    primaryContainer = PrimaryContainerDark,
    onPrimaryContainer = OnPrimaryContainerDark,
    secondary = SecondaryDark,
    onSecondary = OnSecondaryDark,
    secondaryContainer = SecondaryContainerDark,
    onSecondaryContainer = OnSecondaryContainerDark,
    tertiary = TertiaryDark,
    onTertiary = OnTertiaryDark,
    tertiaryContainer = TertiaryContainerDark,
    onTertiaryContainer = OnTertiaryContainerDark,
    error = ErrorDark,
    onError = OnErrorDark,
    errorContainer = ErrorContainerDark,
    onErrorContainer = OnErrorContainerDark,
    background = BackgroundDark,
    onBackground = OnBackgroundDark,
    surface = SurfaceDark,
    onSurface = OnSurfaceDark,
)

val AppTypography = Typography(
    titleLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 22.sp,
        lineHeight = 28.sp,
        letterSpacing = 0.sp
    ),
    bodyLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Normal,
        fontSize = 16.sp,
        lineHeight = 24.sp,
        letterSpacing = 0.5.sp
    )
)

@Composable
fun NoiosoAITheme(
    darkTheme: Boolean = true, // Default to dark for premium look
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        typography = AppTypography,
        content = content
    )
}
