// ignore_for_file: deprecated_member_use, avoid_web_libraries_in_flutter
import 'dart:js' as js;
import 'dart:typed_data';

const _vendorIds = [0x0483, 0x0416, 0x04B8, 0x0FE6, 0x28E9];

Future<bool> thermalPlatformConnectUsb() async {
  try {
    final usb = js.context['navigator']['usb'];
    if (usb == null) return false;

    final filters = _vendorIds.map((id) => {'vendorId': id}).toList();
    final device = await usb.callMethod('requestDevice', [
      js.JsObject.jsify({'filters': filters})
    ]);
    if (device == null) return false;

    await device.callMethod('open');
    await device.callMethod('selectConfiguration', [1]);
    await device.callMethod('claimInterface', [0]);

    js.context['__thermalDevice'] = device;
    return true;
  } catch (_) {
    return false;
  }
}

Future<void> thermalPlatformDisconnectUsb() async {
  try {
    final device = js.context['__thermalDevice'];
    if (device != null) {
      await device.callMethod('close');
      js.context['__thermalDevice'] = null;
    }
  } catch (_) {}
}

Future<bool> thermalPlatformWriteUsb(Uint8List data) async {
  try {
    final device = js.context['__thermalDevice'];
    if (device == null) return false;

    final endpoints = device['configuration']['interfaces'][0]['alternate']['endpoints'];
    dynamic outEndpoint;
    for (var i = 0; i < endpoints.length; i++) {
      final ep = endpoints[i];
      if (ep['direction'] == 'out') {
        outEndpoint = ep;
        break;
      }
    }
    if (outEndpoint == null) return false;

    final array = js.JsObject.jsify(data);
    await device.callMethod('transferOut', [
      outEndpoint['endpointNumber'],
      array,
    ]);
    return true;
  } catch (_) {
    return false;
  }
}