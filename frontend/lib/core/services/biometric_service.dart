import 'package:flutter/services.dart';
import 'package:local_auth/local_auth.dart';

class BiometricService {
  final LocalAuthentication _auth = LocalAuthentication();

  Future<bool> isAvailable() async {
    try {
      return await _auth.canCheckBiometrics || await _auth.isDeviceSupported();
    } on PlatformException {
      return false;
    }
  }

  Future<List<BiometricType>> getAvailableBiometrics() async {
    try {
      return await _auth.getAvailableBiometrics();
    } on PlatformException {
      return [];
    }
  }

  Future<bool> authenticate({String reason = 'Acceso a Zorvian ERP'}) async {
    try {
      return await _auth.authenticate(
        localizedReason: reason,
        options: const AuthenticationOptions(
          stickyAuth: true,
          biometricOnly: false,
        ),
      );
    } on PlatformException {
      return false;
    }
  }

  String biometricTypeName(List<BiometricType> types) {
    if (types.contains(BiometricType.face)) return 'Face ID';
    if (types.contains(BiometricType.fingerprint)) return 'Huella digital';
    if (types.contains(BiometricType.iris)) return 'Iris';
    return 'Biometría';
  }
}
