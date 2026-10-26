import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/admin/pages/super_admin_companies_page.dart';
import 'package:zorvian/features/admin/widgets/company_logo_picker.dart';
import 'package:zorvian/shared/ds/ds.dart';

/// `SecureStorage` de prueba: en `flutter test` el canal del plugin nunca
/// responde, así que el token se sirve desde memoria (si no, el interceptor de
/// `DioClient` deja el request colgado).
class _TestStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => 'test-token';

  @override
  Future<String?> getRefreshToken() async => null;
}

class _FakeAdapter implements HttpClientAdapter {
  _FakeAdapter(this._handle);

  final Future<ResponseBody> Function(RequestOptions options) _handle;
  final List<RequestOptions> requests = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    return _handle(options);
  }

  @override
  void close({bool force = false}) {}
}

/// PNG 1x1 transparente: `Image.memory` debe poder decodificarlo en los tests.
final Uint8List _tinyPng = Uint8List.fromList(const [
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
  0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
  0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
  0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
  0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
  0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
]);

ResponseBody _jsonBody(Object? data, [int status = 200]) =>
    ResponseBody.fromString(
      data == null ? '' : jsonEncode(data),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

/// Red falsa de los diálogos de empresa: `GET companies/all`, `POST companies`,
/// `POST companies/{id}/logo` y `PUT companies/{id}`.
class _FakeCompanyApi {
  _FakeCompanyApi({this.logoFails = false});

  final bool logoFails;

  late final _FakeAdapter adapter = _FakeAdapter(_handle);

  List<RequestOptions> get requests => adapter.requests;

  List<RequestOptions> get logoRequests =>
      requests.where((r) => r.path.endsWith('/logo')).toList();

  static const companyId = 'c1';

  static final Map<String, Object?> company = {
    'id': companyId,
    'tenantId': 't1',
    'name': 'Tienda Brizuela',
    'legalName': 'Tienda Brizuela S.A.',
    'email': 'admin@brizuela.test',
    'phone': '8888-0000',
    'country': 'Nicaragua',
    'currency': 'NIO',
    'subscriptionPlan': 'starter',
    'isActive': true,
    'logoUrl': 'https://signed.test/companies/c1/logo.png',
  };

  Future<ResponseBody> _handle(RequestOptions options) async {
    final path = options.path;
    if (path == 'companies/all') return _jsonBody([company]);
    if (path == 'companies/subscription-plans') return _jsonBody(const []);
    if (options.method == 'POST' && path == 'companies') {
      return _jsonBody({
        'id': 'c-nueva',
        'tenantId': 't-nueva',
        'name': 'Nueva Empresa S.A.',
      }, 201);
    }
    if (path.endsWith('/logo')) {
      if (options.method == 'DELETE') return _jsonBody(null, 204);
      if (logoFails) {
        return _jsonBody({
          'error': 'No se pudo guardar el logo en el almacenamiento. '
              'Verifique la configuración de Firebase Storage del servidor.',
        }, 502);
      }
      return _jsonBody({'logoUrl': 'https://signed.test/$path'});
    }
    if (options.method == 'PUT') return _jsonBody(const {});
    return _jsonBody({'error': 'no encontrado'}, 404);
  }
}

void main() {
  group('readPickedFileBytes', () {
    test('lee los bytes desde el path cuando el picker no precargó bytes',
        () async {
      // Regresión: en Windows/Android/iOS `withData` es false por defecto, así
      // que `PlatformFile.bytes` es null y `readAsBytes()` lanza StateError →
      // el logo nunca se subía.
      final dir = await Directory.systemTemp.createTemp('zorvian_logo_');
      addTearDown(() => dir.delete(recursive: true));
      final file = File('${dir.path}/logo.png');
      final payload = Uint8List.fromList([137, 80, 78, 71, 13, 10, 26, 10]);
      await file.writeAsBytes(payload);

      final platformFile = PlatformFile(
        name: 'logo.png',
        size: payload.length,
        path: file.path,
      );

      // Evidencia del bug original: sin bytes precargados, readAsBytes lanza.
      await expectLater(platformFile.readAsBytes(), throwsStateError);

      final bytes = await readPickedFileBytes(platformFile);
      expect(bytes, payload);
    });

    test('devuelve un buffer vacío si el archivo no tiene contenido', () async {
      final dir = await Directory.systemTemp.createTemp('zorvian_logo_empty_');
      addTearDown(() => dir.delete(recursive: true));
      final file = File('${dir.path}/vacio.png');
      await file.writeAsBytes(const <int>[]);

      final bytes = await readPickedFileBytes(
        PlatformFile(name: 'vacio.png', size: 0, path: file.path),
      );

      expect(bytes, isEmpty);
    });
  });

  group('logoContentType', () {
    test('mapea las extensiones aceptadas por el API', () {
      expect(logoContentType('logo.png'), 'image/png');
      expect(logoContentType('LOGO.PNG'), 'image/png');
      expect(logoContentType('foto.jpg'), 'image/jpeg');
      expect(logoContentType('foto.JPEG'), 'image/jpeg');
      expect(logoContentType('foto.webp'), 'image/webp');
    });

    test('cae a image/png para extensiones desconocidas', () {
      expect(logoContentType('logo.gif'), 'image/png');
      expect(logoContentType('sin-extension'), 'image/png');
    });
  });

  group('uploadCompanyLogo', () {
    test('hace POST multipart a companies/{id}/logo y devuelve la URL firmada',
        () async {
      final adapter = _FakeAdapter((options) async {
        return ResponseBody.fromString(
          '{"logoUrl":"https://signed.test/companies/c1/logo.png"}',
          200,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );
      });
      final client = DioClient(_TestStorage(), adapter: adapter);

      final url = await uploadCompanyLogo(
        client,
        'c1',
        bytes: Uint8List.fromList([1, 2, 3]),
        fileName: 'logo.png',
      );

      expect(url, 'https://signed.test/companies/c1/logo.png');
      final request = adapter.requests.single;
      expect(request.method, 'POST');
      expect(request.path, 'companies/c1/logo');
      expect(request.headers['Authorization'], 'Bearer test-token');

      final formData = request.data as FormData;
      expect(formData.files.single.key, 'file');
      expect(formData.files.single.value.filename, 'logo.png');
      expect(formData.files.single.value.contentType?.mimeType, 'image/png');
      // El error ya se muestra en el diálogo: no duplicar el toast global.
      expect(request.extra['suppressGlobalError'], isTrue);
    });

    test('devuelve null cuando el API responde sin logoUrl', () async {
      final adapter = _FakeAdapter((options) async => ResponseBody.fromString(
            '{"logoUrl":""}',
            200,
            headers: {
              Headers.contentTypeHeader: [Headers.jsonContentType],
            },
          ));
      final client = DioClient(_TestStorage(), adapter: adapter);

      final url = await uploadCompanyLogo(
        client,
        'c1',
        bytes: Uint8List.fromList([1]),
        fileName: 'logo.jpg',
      );

      expect(url, isNull);
      final formData = adapter.requests.single.data as FormData;
      expect(formData.files.single.value.contentType?.mimeType, 'image/jpeg');
    });
  });

  group('removeCompanyLogo', () {
    test('hace DELETE companies/{id}/logo sin duplicar el toast global',
        () async {
      final adapter = _FakeAdapter(
        (options) async => _jsonBody(null, 204),
      );
      final client = DioClient(_TestStorage(), adapter: adapter);

      await removeCompanyLogo(client, 'c1');

      final request = adapter.requests.single;
      expect(request.method, 'DELETE');
      expect(request.path, 'companies/c1/logo');
      expect(request.extra['suppressGlobalError'], isTrue);
      expect(request.headers['Authorization'], 'Bearer test-token');
    });
  });

  group('companyLogoErrorMessage', () {
    DioException errorWith(Object? data, int status) => DioException(
          requestOptions: RequestOptions(path: 'companies/c1/logo'),
          response: Response<Object?>(
            requestOptions: RequestOptions(path: 'companies/c1/logo'),
            statusCode: status,
            data: data,
          ),
          type: DioExceptionType.badResponse,
        );

    test('muestra el mensaje del backend ({error})', () {
      expect(
        companyLogoErrorMessage(errorWith({
          'error': 'No se pudo guardar el logo en el almacenamiento. '
              'Verifique la configuración de Firebase Storage del servidor.',
        }, 502)),
        contains('Firebase Storage'),
      );
    });

    test('acepta {message} y listas de validación', () {
      expect(
        companyLogoErrorMessage(errorWith({'message': 'Solo PNG'}, 400)),
        'Solo PNG',
      );
      expect(
        companyLogoErrorMessage(errorWith({
          'title': ['El archivo es demasiado grande'],
        }, 400)),
        'El archivo es demasiado grande',
      );
    });

    test('un 404 sin cuerpo da un mensaje accionable', () {
      expect(
        companyLogoErrorMessage(errorWith(null, 404)),
        contains('No se encontró la empresa'),
      );
    });

    test('las excepciones de selección conservan su mensaje', () {
      expect(
        companyLogoErrorMessage(const LogoPickException('El logo no puede superar 5 MB')),
        'El logo no puede superar 5 MB',
      );
    });
  });

  group('CompanyLogoField', () {
    Future<void> pumpField(
      WidgetTester tester, {
      Uint8List? pendingBytes,
      String? currentLogoUrl,
      bool uploading = false,
      VoidCallback? onPick,
      VoidCallback? onRemove,
    }) =>
        tester.pumpWidget(MaterialApp(
          home: Scaffold(
            body: CompanyLogoField(
              pendingBytes: pendingBytes,
              fileName: pendingBytes == null ? null : 'logo.png',
              currentLogoUrl: currentLogoUrl,
              uploading: uploading,
              onPick: onPick ?? () {},
              onRemove: onRemove,
            ),
          ),
        ));

    testWidgets('ofrece subir el logo y notifica el toque', (tester) async {
      var taps = 0;
      await pumpField(tester, onPick: () => taps++);

      expect(find.text('Subir logo'), findsOneWidget);
      await tester.tap(find.text('Subir logo'));
      await tester.pump();

      expect(taps, 1);
    });

    testWidgets('muestra la vista previa del logo elegido', (tester) async {
      await pumpField(tester, pendingBytes: _tinyPng);

      expect(find.text('Cambiar logo'), findsOneWidget);
      expect(find.byType(Image), findsOneWidget);
    });

    testWidgets('deshabilita la selección mientras se sube', (tester) async {
      var taps = 0;
      await pumpField(tester, uploading: true, onPick: () => taps++);

      await tester.tap(find.text('Subir logo'), warnIfMissed: false);
      await tester.pump();

      expect(taps, 0);
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('sin logo no ofrece quitar', (tester) async {
      await pumpField(tester, onRemove: () {});

      expect(find.text('Quitar logo'), findsNothing);
    });

    testWidgets('con logo guardado ofrece quitarlo', (tester) async {
      var removed = 0;
      await pumpField(
        tester,
        currentLogoUrl: 'https://signed.test/companies/c1/logo.png',
        onRemove: () => removed++,
      );

      expect(find.text('Cambiar logo'), findsOneWidget);
      await tester.tap(find.text('Quitar logo'));
      await tester.pump();

      expect(removed, 1);
    });
  });

  group('Diálogos de empresa', () {
    void setView(WidgetTester tester, {double width = 1000, double height = 1200}) {
      tester.view.physicalSize = Size(width, height);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);
    }

    Future<void> pumpDialogHost(
      WidgetTester tester,
      _FakeCompanyApi api,
      Widget Function() dialog,
    ) async {
      setView(tester);

      final client = DioClient(_TestStorage(), adapter: api.adapter);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            dioClientProvider.overrideWithValue(client),
            // El selector nativo del SO no existe en `flutter test`.
            logoPickerProvider.overrideWithValue(
              () async => PickedLogo(bytes: _tinyPng, fileName: 'logo.png'),
            ),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: Builder(
                builder: (context) => TextButton(
                  onPressed: () => showZAdaptiveDialog(
                    context,
                    builder: (_) => dialog(),
                  ),
                  child: const Text('Abrir'),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();
    }

    testWidgets('crear empresa sube el logo DESPUÉS de crearla', (tester) async {
      // Regresión: el diálogo de creación no tenía selector de logo, así que
      // una empresa nueva nunca podía quedar con logo.
      final api = _FakeCompanyApi();
      await pumpDialogHost(tester, api, () => CompanyCreateDialog(onCreated: () {}));

      expect(find.text('Subir logo'), findsOneWidget);
      await tester.tap(find.text('Subir logo'));
      await tester.pumpAndSettle();
      expect(find.text('Cambiar logo'), findsOneWidget);

      await tester.enterText(find.byType(TextField).first, 'Nueva Empresa S.A.');
      await tester.tap(find.text('Crear Empresa'));
      await tester.pumpAndSettle();

      final posts = api.requests.where((r) => r.method == 'POST').toList();
      expect(posts.map((r) => r.path).toList(),
          ['companies', 'companies/c-nueva/logo']);
      expect((posts.first.data as Map)['name'], 'Nueva Empresa S.A.');

      final logoForm = posts.last.data as FormData;
      expect(logoForm.files.single.key, 'file');
      expect(logoForm.files.single.value.filename, 'logo.png');
      expect(logoForm.files.single.value.contentType?.mimeType, 'image/png');

      expect(find.text('Empresa creada correctamente'), findsOneWidget);
    });

    testWidgets(
        'crear empresa sin logo solo hace el POST de la empresa', (tester) async {
      final api = _FakeCompanyApi();
      await pumpDialogHost(tester, api, () => CompanyCreateDialog(onCreated: () {}));

      await tester.tap(find.text('Crear Empresa'));
      await tester.pumpAndSettle();

      expect(api.logoRequests, isEmpty);
      expect(api.requests.where((r) => r.method == 'POST').single.path,
          'companies');
    });

    testWidgets('si el logo falla al crear, la empresa queda creada y se avisa',
        (tester) async {
      final api = _FakeCompanyApi(logoFails: true);
      await pumpDialogHost(tester, api, () => CompanyCreateDialog(onCreated: () {}));

      await tester.tap(find.text('Subir logo'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Crear Empresa'));
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Empresa creada, pero el logo no se pudo subir'),
        findsOneWidget,
      );
      expect(find.textContaining('Firebase Storage'), findsOneWidget);
    });

    testWidgets('editar empresa sube el logo antes de guardar los cambios',
        (tester) async {
      final api = _FakeCompanyApi();
      setView(tester, width: 1600, height: 1200);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            dioClientProvider.overrideWithValue(
              DioClient(_TestStorage(), adapter: api.adapter),
            ),
            logoPickerProvider.overrideWithValue(
              () async => PickedLogo(bytes: _tinyPng, fileName: 'logo.png'),
            ),
          ],
          child: const MaterialApp(home: SuperAdminCompaniesPage()),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Editar empresa'));
      await tester.pumpAndSettle();

      // El diálogo precarga el logo guardado → la acción dice "Cambiar logo".
      await tester.tap(find.text('Cambiar logo'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Guardar Cambios'));
      await tester.pumpAndSettle();

      final methods = api.requests.map((r) => '${r.method} ${r.path}').toList();
      expect(methods, containsAllInOrder([
        'GET companies/all',
        'POST companies/c1/logo',
        'PUT companies/c1',
      ]));
    });

    testWidgets('editar empresa puede quitar el logo guardado', (tester) async {
      // El borrado se aplica en el API al guardar: se limpia la vista previa y
      // el DELETE viaja antes del PUT de los datos.
      final api = _FakeCompanyApi();
      setView(tester, width: 1600, height: 1200);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            dioClientProvider.overrideWithValue(
              DioClient(_TestStorage(), adapter: api.adapter),
            ),
          ],
          child: const MaterialApp(home: SuperAdminCompaniesPage()),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Editar empresa'));
      await tester.pumpAndSettle();
      expect(find.text('Quitar logo'), findsOneWidget);

      await tester.tap(find.text('Quitar logo'));
      await tester.pumpAndSettle();
      expect(find.text('Subir logo'), findsOneWidget);
      expect(find.text('Quitar logo'), findsNothing);

      await tester.tap(find.text('Guardar Cambios'));
      await tester.pumpAndSettle();

      final methods = api.requests.map((r) => '${r.method} ${r.path}').toList();
      expect(methods, containsAllInOrder([
        'GET companies/all',
        'DELETE companies/c1/logo',
        'PUT companies/c1',
      ]));
    });

    testWidgets('editar sin tocar el logo no sube ni borra nada',
        (tester) async {
      final api = _FakeCompanyApi();
      setView(tester, width: 1600, height: 1200);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            dioClientProvider.overrideWithValue(
              DioClient(_TestStorage(), adapter: api.adapter),
            ),
          ],
          child: const MaterialApp(home: SuperAdminCompaniesPage()),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Editar empresa'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Guardar Cambios'));
      await tester.pumpAndSettle();

      expect(api.logoRequests, isEmpty);
    });

    testWidgets(
        'si el logo falla al editar, no se guarda nada y el botón se rehabilita',
        (tester) async {
      final api = _FakeCompanyApi(logoFails: true);
      setView(tester, width: 1600, height: 1200);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            dioClientProvider.overrideWithValue(
              DioClient(_TestStorage(), adapter: api.adapter),
            ),
            logoPickerProvider.overrideWithValue(
              () async => PickedLogo(bytes: _tinyPng, fileName: 'logo.png'),
            ),
          ],
          child: const MaterialApp(home: SuperAdminCompaniesPage()),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip('Editar empresa'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cambiar logo'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Guardar Cambios'));
      await tester.pumpAndSettle();

      // Regresión: el botón quedaba girando para siempre tras el fallo.
      expect(find.text('Guardar Cambios'), findsOneWidget);
      expect(
        find.textContaining('No se pudo guardar el logo'),
        findsOneWidget,
      );
      expect(
        api.requests.where((r) => r.method == 'PUT'),
        isEmpty,
        reason: 'el formulario no debe guardarse si el logo falló',
      );
    });
  });
}
