package com.example.todoappkotlinmultiplatform.ui.theme

import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color

val BrandBlue = Color(0xFF2563EB)
val BrandRed = Color(0xFFDC2626)
val LightBg = Color(0xFFF3F4F6)
val LightSurface = Color(0xFFFFFFFF)
val LightSurfaceAlt = Color(0xFFF9FAFB)
val LightBorder = Color(0xFFE5E7EB)
val DarkBg = Color(0xFF111827)
val DarkSurface = Color(0xFF1F2937)
val DarkSurfaceAlt = Color(0xFF374151)
val DarkBorder = Color(0xFF4B5563)

val LightColorScheme = lightColorScheme(
    primary = BrandBlue,
    onPrimary = Color.White,
    secondary = BrandBlue,
    onSecondary = Color.White,
    error = BrandRed,
    onError = Color.White,
    background = LightBg,
    onBackground = Color(0xFF111827),
    surface = LightSurface,
    onSurface = Color(0xFF111827),
    surfaceVariant = LightSurfaceAlt,
    onSurfaceVariant = Color(0xFF374151),
    outline = LightBorder
)

val DarkColorScheme = darkColorScheme(
    primary = BrandBlue,
    onPrimary = Color.White,
    secondary = BrandBlue,
    onSecondary = Color.White,
    error = BrandRed,
    onError = Color.White,
    background = DarkBg,
    onBackground = Color(0xFFF9FAFB),
    surface = DarkSurface,
    onSurface = Color(0xFFF9FAFB),
    surfaceVariant = DarkSurfaceAlt,
    onSurfaceVariant = Color(0xFFD1D5DB),
    outline = DarkBorder
)

enum class AppThemeMode {
    LIGHT,
    DARK;

    fun toStorage(): String = when (this) {
        LIGHT -> "light"
        DARK -> "dark"
    }

    companion object {
        fun fromStorage(value: String?): AppThemeMode =
            if (value == "dark") DARK else LIGHT
    }
}
