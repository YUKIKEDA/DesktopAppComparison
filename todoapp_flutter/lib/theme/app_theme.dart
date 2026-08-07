import 'package:flutter/material.dart';

/// Brand colors shared across light/dark palettes.
class AppColors {
  static const Color brandBlue = Color(0xFF2563EB);
  static const Color brandRed = Color(0xFFDC2626);
  static const Color lightBg = Color(0xFFF3F4F6);
  static const Color lightSurface = Color(0xFFFFFFFF);
  static const Color lightSurfaceAlt = Color(0xFFF9FAFB);
  static const Color lightBorder = Color(0xFFE5E7EB);
  static const Color darkBg = Color(0xFF111827);
  static const Color darkSurface = Color(0xFF1F2937);
  static const Color darkSurfaceAlt = Color(0xFF374151);
  static const Color darkBorder = Color(0xFF4B5563);
}

class AppTheme {
  static ThemeData light() {
    final colorScheme = ColorScheme.light(
      primary: AppColors.brandBlue,
      onPrimary: Colors.white,
      secondary: AppColors.brandBlue,
      error: AppColors.brandRed,
      onError: Colors.white,
      surface: AppColors.lightSurface,
      onSurface: const Color(0xFF111827),
      surfaceContainerHighest: AppColors.lightSurfaceAlt,
      outline: AppColors.lightBorder,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: AppColors.lightBg,
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.lightSurface,
        foregroundColor: Color(0xFF111827),
        elevation: 0,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.brandBlue,
          foregroundColor: Colors.white,
        ),
      ),
      dividerColor: AppColors.lightBorder,
      cardColor: AppColors.lightSurface,
    );
  }

  static ThemeData dark() {
    final colorScheme = ColorScheme.dark(
      primary: AppColors.brandBlue,
      onPrimary: Colors.white,
      secondary: AppColors.brandBlue,
      error: AppColors.brandRed,
      onError: Colors.white,
      surface: AppColors.darkSurface,
      onSurface: const Color(0xFFF9FAFB),
      surfaceContainerHighest: AppColors.darkSurfaceAlt,
      outline: AppColors.darkBorder,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: AppColors.darkBg,
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.darkSurface,
        foregroundColor: Color(0xFFF9FAFB),
        elevation: 0,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.brandBlue,
          foregroundColor: Colors.white,
        ),
      ),
      dividerColor: AppColors.darkBorder,
      cardColor: AppColors.darkSurface,
    );
  }
}
