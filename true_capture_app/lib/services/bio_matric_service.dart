import 'package:local_auth/local_auth.dart' as la;

import '../enum/bio_matric_enum.dart';
import '../log/app_logs.dart';

class BiometricService {
  BiometricService._();
  static final BiometricService instance = BiometricService._();

  final la.LocalAuthentication _auth = la.LocalAuthentication();

  Future<BiometricStatus> status() async {
    try {
      final supported = await _auth.isDeviceSupported();
      if (!supported) return BiometricStatus.notSupported;
      final canCheck = await _auth.canCheckBiometrics;
      if (!canCheck) return BiometricStatus.notAvailable;
      final available = await _auth.getAvailableBiometrics();
      if (available.isEmpty) return BiometricStatus.notEnrolled;
      return BiometricStatus.available;
    } catch (e, s) {
      appLogError(e, s, 'BIOMETRIC');
      return BiometricStatus.notAvailable;
    }
  }

  Future<List<BiometricType>> availableTypes() async {
    try {
      final types = await _auth.getAvailableBiometrics();
      return types.map((t) {
        if (t == la.BiometricType.face) return BiometricType.face;
        if (t == la.BiometricType.fingerprint) return BiometricType.fingerprint;
        if (t == la.BiometricType.iris) return BiometricType.iris;
        return BiometricType.none;
      }).toList();
    } catch (_) {
      return const [];
    }
  }

  Future<bool> authenticate({
    String reason = 'Please authenticate to continue',
  }) async {
    try {
      return await _auth.authenticate(
        localizedReason: reason,
        options: const la.AuthenticationOptions(
          stickyAuth: true,
          biometricOnly: true,
        ),
      );
    } catch (e, s) {
      appLogError(e, s, 'BIOMETRIC');
      return false;
    }
  }
}
