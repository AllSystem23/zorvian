import 'package:flutter/material.dart';

class ZShadows {
  static const BoxShadow sm = BoxShadow(
    color: Color(0x0A000000),
    blurRadius: 2,
    offset: Offset(0, 1),
  );

  static const BoxShadow md = BoxShadow(
    color: Color(0x14000000),
    blurRadius: 6,
    offset: Offset(0, 2),
  );

  static const BoxShadow lg = BoxShadow(
    color: Color(0x1A000000),
    blurRadius: 16,
    offset: Offset(0, 4),
  );

  static const BoxShadow xl = BoxShadow(
    color: Color(0x24000000),
    blurRadius: 24,
    offset: Offset(0, 8),
  );

  static List<BoxShadow> get small => [sm];
  static List<BoxShadow> get medium => [md];
  static List<BoxShadow> get large => [lg];
  static List<BoxShadow> get extraLarge => [xl];
}
