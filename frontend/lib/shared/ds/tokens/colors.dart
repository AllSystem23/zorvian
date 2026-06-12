import 'package:flutter/material.dart';

class ZColors {
  // Brand - Premium Enterprise Palette (Deep Violet-Navy)
  static const Color brandPrimary = Color(0xFF1A0A3E); // Deep Violet-Navy
  static const Color brandPrimaryLight = Color(0xFF2D1B69);
  static const Color brandPrimaryDark = Color(0xFF0E0324);
  static const Color brandSecondary = Color(0xFF00E5FF); // Cyan Eléctrico (brighter)
  static const Color brandAccent = Color(0xFF7C4DFF); // Medium Violet (CTA buttons)
  static const Color brandTeal = Color(0xFF2EE59D); // Neo Teal (secondary accent)
  static const Color brandGold = Color(0xFFFFD54F); // Gold premium accent
  
  // Impeccable Glassmorphism Tokens
  static const Color glassWhite = Color(0x0DFFFFFF);
  static const Color glassBlack = Color(0x0D000000);
  static const Color glassStroke = Color(0x1AFFFFFF);

  // Semantic — WCAG 2.1 AA Compliant (≥4.5:1 on white)
  static const Color success = Color(0xFF007A3E); // Deep Green (5.2:1 on white)
  static const Color warning = Color(0xFFA64A00); // Deep Amber (5.6:1 on white)
  static const Color danger = Color(0xFFD5001C); // Deep Red (5.5:1 on white)
  static const Color info = Color(0xFF2962FF); // Deep Blue (4.6:1 on white)

  // Module-Specific Colors (for diagrams, badges, module headers)
  static const Color moduleIa = Color(0xFFB388FF); // Z-IA Purple Aura
  static const Color moduleCrm = Color(0xFF00BCD4); // CRM Cyan
  static const Color moduleSales = Color(0xFF00E5FF); // Sales Electric
  static const Color moduleInventory = Color(0xFFFF8F00); // Inventory Amber
  static const Color modulePurchases = Color(0xFFFFB300); // Purchases Gold
  static const Color moduleFinance = Color(0xFF388E3C); // Finance Green Bosque (WCAG AA on dark)
  static const Color moduleTreasury = Color(0xFF2E7D32); // Treasury Green
  static const Color moduleHr = Color(0xFFE040FB); // HR Magenta Talento
  static const Color moduleAdmin = Color(0xFF78909C); // Admin Blue Grey (WCAG AA on dark)
  static const Color moduleSecurity = Color(0xFFEF5350); // Security Red

  // Neutral Scale (Warm Slate — more premium feel)
  static const Color neutral50 = Color(0xFFFAFAFE);
  static const Color neutral100 = Color(0xFFF4F4F9);
  static const Color neutral200 = Color(0xFFE8E8EE);
  static const Color neutral300 = Color(0xFFCDCDD6);
  static const Color neutral400 = Color(0xFF9494A0);
  static const Color neutral500 = Color(0xFF646470);
  static const Color neutral600 = Color(0xFF484854);
  static const Color neutral700 = Color(0xFF333340);
  static const Color neutral800 = Color(0xFF1E1E2E);
  static const Color neutral900 = Color(0xFF0E0E1A);

  // Dark mode (Premium Deep Space)
  static const Color darkBackground = Color(0xFF0A0E27);
  static const Color darkSurface = Color(0xFF141838);
  static const Color darkBorder = Color(0xFF1E2240);
  static const Color darkCard = Color(0xFF181C3A);

  // Light mode (Clean Professional with warmth)
  static const Color background = Color(0xFFFAFAFE);
  static const Color surface = Color(0xFFFFFFFF);
  static const Color border = Color(0xFFE8E8EE);

  // Gradient helpers
  static const List<Color> brandGradient = [
    Color(0xFF1A0A3E),
    Color(0xFF2D1B69),
    Color(0xFF7C4DFF),
  ];

  static const List<Color> accentGradient = [
    Color(0xFF00E5FF),
    Color(0xFF7C4DFF),
    Color(0xFFB388FF),
  ];
}
