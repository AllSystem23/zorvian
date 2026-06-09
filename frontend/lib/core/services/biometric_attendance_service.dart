import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';
import 'package:local_auth/local_auth.dart';
import 'package:camera/camera.dart';

/// Mobile native enhancements service (P4.4)
/// Biometric authentication, GPS check-in, camera integration
class BiometricAttendanceService {
  final LocalAuthentication _auth = LocalAuthentication();
  CameraController? _cameraController;
  List<CameraDescription>? _cameras;

  /// Check if device has biometric hardware
  Future<bool> isBiometricAvailable() async {
    try {
      final supported = await _auth.isDeviceSupported();
      final canCheck = await _auth.canCheckBiometrics;
      return supported && canCheck;
    } catch (e) {
      return false;
    }
  }

  /// Get available biometric types
  Future<List<BiometricType>> getAvailableBiometrics() async {
    try {
      return await _auth.getAvailableBiometrics();
    } catch (e) {
      return [];
    }
  }

  /// Authenticate user with biometrics
  Future<BiometricResult> authenticate({
    String reason = 'Verificar identidad para registrar asistencia',
  }) async {
    try {
      final authenticated = await _auth.authenticate(
        localizedReason: reason,
      );
      return BiometricResult(
        success: authenticated,
        timestamp: DateTime.now(),
      );
    } catch (e) {
      return BiometricResult(
        success: false,
        error: e.toString(),
        timestamp: DateTime.now(),
      );
    }
  }

  /// Check if location services are enabled
  Future<bool> isLocationServiceEnabled() async {
    return await Geolocator.isLocationServiceEnabled();
  }

  /// Request location permission
  Future<bool> requestLocationPermission() async {
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    return permission == LocationPermission.always ||
        permission == LocationPermission.whileInUse;
  }

  /// Get current location with high accuracy
  Future<LocationData?> getCurrentLocation() async {
    try {
      if (!await isLocationServiceEnabled()) return null;
      if (!await requestLocationPermission()) return null;

      final position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 10),
        ),
      );

      return LocationData(
        latitude: position.latitude,
        longitude: position.longitude,
        accuracy: position.accuracy,
        altitude: position.altitude,
        speed: position.speed,
        heading: position.heading,
        timestamp: position.timestamp,
      );
    } catch (e) {
      if (kDebugMode) {
        print('[GPS] Error getting location: $e');
      }
      return null;
    }
  }

  /// Check if employee is within office geofence
  Future<bool> isWithinGeofence({
    required double officeLat,
    required double officeLng,
    required double radiusMeters,
  }) async {
    final location = await getCurrentLocation();
    if (location == null) return false;

    final distance = Geolocator.distanceBetween(
      officeLat,
      officeLng,
      location.latitude,
      location.longitude,
    );

    return distance <= radiusMeters;
  }

  /// Initialize camera for photo check-in
  Future<bool> initializeCamera({CameraLensDirection lens = CameraLensDirection.front}) async {
    try {
      _cameras = await availableCameras();
      if (_cameras == null || _cameras!.isEmpty) return false;

      final camera = _cameras!.firstWhere(
        (c) => c.lensDirection == lens,
        orElse: () => _cameras!.first,
      );

      _cameraController = CameraController(
        camera,
        ResolutionPreset.medium,
        enableAudio: false,
      );

      await _cameraController!.initialize();
      return true;
    } catch (e) {
      return false;
    }
  }

  /// Capture photo for attendance
  Future<String?> captureAttendancePhoto() async {
    try {
      if (_cameraController == null || !_cameraController!.value.isInitialized) {
        return null;
      }
      final image = await _cameraController!.takePicture();
      return image.path;
    } catch (e) {
      return null;
    }
  }

  /// Cleanup camera
  Future<void> disposeCamera() async {
    await _cameraController?.dispose();
    _cameraController = null;
  }

  CameraController? get cameraController => _cameraController;
}

class BiometricResult {
  final bool success;
  final String? error;
  final DateTime timestamp;

  const BiometricResult({
    required this.success,
    this.error,
    required this.timestamp,
  });
}

class LocationData {
  final double latitude;
  final double longitude;
  final double accuracy;
  final double altitude;
  final double speed;
  final double heading;
  final DateTime timestamp;

  const LocationData({
    required this.latitude,
    required this.longitude,
    required this.accuracy,
    this.altitude = 0,
    this.speed = 0,
    this.heading = 0,
    required this.timestamp,
  });

  /// Calculate distance to another point in meters
  double distanceTo(double otherLat, double otherLng) {
    return Geolocator.distanceBetween(latitude, longitude, otherLat, otherLng);
  }

  /// Format as Google Maps link
  String toGoogleMapsUrl() {
    return 'https://www.google.com/maps?q=$latitude,$longitude';
  }
}

/// Attendance record with biometric + GPS data
class AttendanceRecordRequest {
  final String? employeeId;
  final String type; // "check-in", "check-out", "break-start", "break-end"
  final LocationData? location;
  final String? photoPath;
  final String? notes;
  final DateTime timestamp;

  const AttendanceRecordRequest({
    required this.employeeId,
    required this.type,
    this.location,
    this.photoPath,
    this.notes,
    required this.timestamp,
  });

  Map<String, dynamic> toJson() => {
    'employeeId': employeeId?.toString(),
    'type': type,
    'location': location != null
        ? {
            'latitude': location!.latitude,
            'longitude': location!.longitude,
            'accuracy': location!.accuracy,
            'timestamp': location!.timestamp.toIso8601String(),
          }
        : null,
    'photoPath': photoPath,
    'notes': notes,
    'timestamp': timestamp.toIso8601String(),
  };
}
