// ══════════════════════════════════════════════════════════════
// Firebase Configuration Template
// ══════════════════════════════════════════════════════════════
//
// En CI/CD se genera automáticamente desde secrets de GitHub.
// Para desarrollo local:
//   dart run scripts/generate_firebase_config.dart
//
// ══════════════════════════════════════════════════════════════

import 'package:firebase_core/firebase_core.dart';

const firebaseOptions = FirebaseOptions(
  apiKey: '__FIREBASE_API_KEY__',
  authDomain: '__FIREBASE_AUTH_DOMAIN__',
  projectId: '__FIREBASE_PROJECT_ID__',
  storageBucket: '__FIREBASE_STORAGE_BUCKET__',
  messagingSenderId: '__FIREBASE_MESSAGING_SENDER_ID__',
  appId: '__FIREBASE_APP_ID__',
);
