import 'package:url_launcher/url_launcher.dart';

class WhatsAppUtils {
  static Future<void> launchWhatsApp({
    required String phone,
    String? message,
  }) async {
    // Clean phone number (only digits)
    final cleanPhone = phone.replaceAll(RegExp(r'\D'), '');
    
    // Add Nicaragua prefix if it looks like a local 8-digit number
    String formattedPhone = cleanPhone;
    if (cleanPhone.length == 8) {
      formattedPhone = '505$cleanPhone';
    }

    final uri = Uri.parse(
      'https://wa.me/$formattedPhone${message != null ? '?text=${Uri.encodeComponent(message)}' : ''}',
    );

    if (await canLaunchUrl(uri)) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }

  static String getLeadWelcomeMessage(String name) {
    return 'Hola $name, un gusto saludarte. Te contacto de Zorvian ERP para dar seguimiento a tu solicitud de información.';
  }

  static String getOpportunityFollowUpMessage(String name, String dealName) {
    return 'Hola $name, ¿cómo estás? Te escribo para dar seguimiento a la propuesta de "$dealName". ¿Has tenido oportunidad de revisarla?';
  }
}
