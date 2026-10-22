import Flutter
import UIKit

/// El plugin `app_links` se registra a sí mismo como `FlutterSceneLifeCycleDelegate`
/// (`registrar.addSceneDelegate(handler)`), de modo que recibe por sí solo los
/// eventos de escena (apertura con esquema propio `zorvian://` y Universal Links
/// `https://`) y los entrega a Dart.
///
/// Esta clase se deja igual que la plantilla de Flutter a propósito: sobrescribir
/// `scene(_:willConnectTo:options:)` sin llamar a `super` rompe el ciclo de vida
/// de la escena, y reenviar el enlace a mano (`AppLinks.shared.handleLink`) lo
/// entrega DOS veces (una por el delegate del plugin y otra manual).
class SceneDelegate: FlutterSceneDelegate {

}
