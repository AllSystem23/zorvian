package com.nexora.nexora

import io.flutter.embedding.android.FlutterFragmentActivity

/**
 * `local_auth` (BiometricPrompt) necesita una FragmentActivity: con
 * `FlutterActivity` la autenticación biométrica falla con
 * `PlatformException(no_fragment_activity)` y la marcación de asistencia por
 * huella/Face ID no funciona en Android.
 */
class MainActivity : FlutterFragmentActivity()
