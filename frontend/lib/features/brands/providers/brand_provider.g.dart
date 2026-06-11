// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'brand_provider.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(BrandNotifier)
final brandProvider = BrandNotifierProvider._();

final class BrandNotifierProvider
    extends $AsyncNotifierProvider<BrandNotifier, List<BrandItem>> {
  BrandNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'brandProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$brandNotifierHash();

  @$internal
  @override
  BrandNotifier create() => BrandNotifier();
}

String _$brandNotifierHash() => r'4f10c1401eed371a0672399fc717bec2f4b76cca';

abstract class _$BrandNotifier extends $AsyncNotifier<List<BrandItem>> {
  FutureOr<List<BrandItem>> build();
  @$mustCallSuper
  @override
  void runBuild() {
    final ref = this.ref as $Ref<AsyncValue<List<BrandItem>>, List<BrandItem>>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<AsyncValue<List<BrandItem>>, List<BrandItem>>,
              AsyncValue<List<BrandItem>>,
              Object?,
              Object?
            >;
    element.handleCreate(ref, build);
  }
}
