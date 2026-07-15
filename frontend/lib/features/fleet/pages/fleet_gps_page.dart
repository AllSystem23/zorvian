import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:latlong2/latlong.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_gps_provider.dart';

/// Fleet GPS page — real-time vehicle tracking with OpenStreetMap,
/// vehicle markers, route history, geofence overlays, and vehicle list.
final class FleetGpsPage extends ConsumerStatefulWidget {
  const FleetGpsPage({super.key});

  @override
  ConsumerState<FleetGpsPage> createState() => _FleetGpsPageState();
}

class _FleetGpsPageState extends ConsumerState<FleetGpsPage> {
  final MapController _mapController = MapController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(fleetGpsProvider.notifier).loadFleetPositions();
      ref.read(fleetGpsProvider.notifier).loadGeofences();
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetGpsProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: state.loading && state.vehiclePositions.isEmpty
          ? _buildSkeleton()
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : Column(
                  children: [
                    Padding(
                      padding: const EdgeInsets.fromLTRB(ZSpacing.lg, ZSpacing.lg, ZSpacing.lg, 0),
                      child: Row(
                        children: [
                          ZButton(
                            text: 'Actualizar',
                            icon: Icons.refresh,
                            onPressed: () => ref.read(fleetGpsProvider.notifier).loadFleetPositions(),
                            type: ZButtonType.secondary,
                            fullWidth: false,
                          ),
                        ],
                      ),
                    ),
                    Expanded(child: _buildContent(state, theme)),
                  ],
                ),
    );
  }

  Widget _buildSkeleton() {
    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        children: [
          ZSkeleton.statCard(),
          const SizedBox(height: ZSpacing.md),
          Expanded(
            child: Column(
              children: List.generate(4, (_) => ZSkeleton.listTile()),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildContent(FleetGpsState state, ThemeData theme) {
    final vehicles = state.vehiclePositions;

    if (vehicles.isEmpty) {
      return const ZEmptyState(
        icon: Icons.location_off_outlined,
        title: 'Sin datos GPS',
        subtitle: 'No hay posiciones GPS registradas para la flota',
      );
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        final isDesktop = constraints.maxWidth > 992;

        return Padding(
          padding: const EdgeInsets.all(ZSpacing.lg),
          child: isDesktop
              ? Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(flex: 3, child: _buildMapArea(vehicles, state, theme)),
                    const SizedBox(width: ZSpacing.lg),
                    SizedBox(width: 380, child: _buildVehicleList(vehicles, theme)),
                  ],
                )
              : Column(
                  children: [
                    SizedBox(height: 300, child: _buildMapArea(vehicles, state, theme)),
                    const SizedBox(height: ZSpacing.md),
                    Expanded(child: _buildVehicleList(vehicles, theme)),
                  ],
                ),
        );
      },
    );
  }

  // ── Real Map with flutter_map + OpenStreetMap ──

  Widget _buildMapArea(List<Map<String, dynamic>> vehicles, FleetGpsState state, ThemeData theme) {
    return ZCard(
      padding: EdgeInsets.zero,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        child: Stack(
          fit: StackFit.expand,
          children: [
            FlutterMap(
              mapController: _mapController,
              options: MapOptions(
                initialCenter: _computeCenter(vehicles),
                initialZoom: vehicles.length > 1 ? 12.0 : 14.0,
                interactionOptions: const InteractionOptions(
                  flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
                ),
              ),
              children: [
                // ── Tile Layer (OpenStreetMap) ──
                TileLayer(
                  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                  userAgentPackageName: 'com.zorvian.erp',
                ),

                // ── Geofence Circles ──
                CircleLayer(
                  circles: state.geofences
                      .where((g) => g['type']?.toString().toLowerCase() == 'circle')
                      .map((g) {
                    final lat = (g['centerLatitude'] as num?)?.toDouble() ?? 0;
                    final lng = (g['centerLongitude'] as num?)?.toDouble() ?? 0;
                    final radius = (g['radiusMeters'] as num?)?.toDouble() ?? 500;
                    return CircleMarker(
                      point: LatLng(lat, lng),
                      radius: radius,
                      useRadiusInMeter: true,
                      color: ZColors.moduleFleet.withValues(alpha: 0.1),
                      borderColor: ZColors.moduleFleet,
                      borderStrokeWidth: 2,
                    );
                  }).toList(),
                ),

                // ── Geofence Polygons ──
                PolygonLayer(
                  polygons: state.geofences
                      .where((g) => g['type']?.toString().toLowerCase() == 'polygon')
                      .map((g) {
                    final points = (g['points'] as List?)
                            ?.map((p) {
                              final pt = p as Map;
                              return LatLng(
                                (pt['latitude'] as num?)?.toDouble() ?? 0,
                                (pt['longitude'] as num?)?.toDouble() ?? 0,
                              );
                            }).toList() ??
                        [];
                    if (points.length < 3) return Polygon(points: [const LatLng(0, 0)]);
                    return Polygon(
                      points: points,
                      color: ZColors.moduleFleet.withValues(alpha: 0.08),
                      borderColor: ZColors.moduleFleet,
                      borderStrokeWidth: 2,
                    );
                  }).toList(),
                ),

                // ── Route History Polylines ──
                PolylineLayer(
                  polylines: _buildRoutePolylines(state.routeHistory),
                ),

                // ── Vehicle Markers ──
                MarkerLayer(
                  markers: vehicles.map((v) => _buildVehicleMarker(v, state)).toList(),
                ),
              ],
            ),

            // ── Vehicle count badge ──
            Positioned(
              top: ZSpacing.md,
              left: ZSpacing.md,
              child: ZBadge(
                text: '${vehicles.length} vehículos',
                type: ZBadgeType.accent,
              ),
            ),

            // ── Active/Inactive badges ──
            Positioned(
              top: ZSpacing.md,
              right: ZSpacing.md,
              child: Row(
                children: [
                  ZBadge(
                    text: '${vehicles.where((v) => (v['speed'] as num?)?.toDouble() != null && (v['speed'] as num?)!.toDouble() > 0).length} en movimiento',
                    type: ZBadgeType.success,
                  ),
                  const SizedBox(width: ZSpacing.sm),
                  ZBadge(
                    text: '${vehicles.where((v) => v['speed'] == null || (v['speed'] as num?)?.toDouble() == 0).length} detenidos',
                    type: ZBadgeType.warning,
                  ),
                ],
              ),
            ),

            // ── Zoom controls ──
            Positioned(
              bottom: ZSpacing.md,
              right: ZSpacing.md,
              child: Column(
                children: [
                  _mapButton(Icons.zoom_in, () {
                    final zoom = _mapController.camera.zoom;
                    _mapController.move(_mapController.camera.center, zoom + 1);
                  }),
                  const SizedBox(height: ZSpacing.xs),
                  _mapButton(Icons.zoom_out, () {
                    final zoom = _mapController.camera.zoom;
                    _mapController.move(_mapController.camera.center, zoom - 1);
                  }),
                  const SizedBox(height: ZSpacing.sm),
                  _mapButton(Icons.my_location, () {
                    final center = _computeCenter(vehicles);
                    _mapController.move(center, 13.0);
                  }),
                ],
              ),
            ),

            // ── Selected vehicle info panel ──
            if (state.selectedVehicle != null)
              Positioned(
                bottom: ZSpacing.md,
                left: ZSpacing.md,
                child: _buildSelectedVehiclePanel(state.selectedVehicle!, theme),
              ),
          ],
        ),
      ),
    );
  }

  Marker _buildVehicleMarker(Map<String, dynamic> vehicle, FleetGpsState state) {
    final lat = (vehicle['latitude'] as num?)?.toDouble() ?? 0;
    final lng = (vehicle['longitude'] as num?)?.toDouble() ?? 0;
    final speed = (vehicle['speed'] as num?)?.toDouble();
    final plate = vehicle['vehiclePlate']?.toString() ?? 'N/A';
    final isSelected = state.selectedVehicle?['vehicleId']?.toString() == vehicle['vehicleId']?.toString();
    final isMoving = speed != null && speed > 0;
    final heading = (vehicle['heading'] as num?)?.toDouble() ?? 0;

    return Marker(
      point: LatLng(lat, lng),
      width: 44,
      height: 56,
      child: GestureDetector(
        onTap: () {
          ref.read(fleetGpsProvider.notifier).selectVehicle(vehicle['vehicleId']?.toString() ?? '');
        },
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Vehicle icon with rotation
            Transform.rotate(
              angle: heading * 3.14159265 / 180,
              child: Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: isSelected
                      ? ZColors.moduleFleet
                      : isMoving
                          ? ZColors.success
                          : ZColors.warning,
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: Colors.white,
                    width: isSelected ? 3 : 2,
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.3),
                      blurRadius: 6,
                      offset: const Offset(0, 2),
                    ),
                  ],
                ),
                child: Icon(
                  Icons.local_shipping_outlined,
                  color: Colors.white,
                  size: 18,
                ),
              ),
            ),
            // Plate label
            Container(
              margin: const EdgeInsets.only(top: 2),
              padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
              decoration: BoxDecoration(
                color: Colors.black87,
                borderRadius: BorderRadius.circular(4),
              ),
              child: Text(
                plate,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 8,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  List<Polyline> _buildRoutePolylines(List<Map<String, dynamic>> history) {
    if (history.isEmpty) return [];
    final points = history.map((p) {
      return LatLng(
        (p['latitude'] as num?)?.toDouble() ?? 0,
        (p['longitude'] as num?)?.toDouble() ?? 0,
      );
    }).toList();
    return [
      Polyline(
        points: points,
        color: ZColors.moduleFleet,
        strokeWidth: 3,
      ),
    ];
  }

  Widget _buildSelectedVehiclePanel(Map<String, dynamic> vehicle, ThemeData theme) {
    final plate = vehicle['vehiclePlate']?.toString() ?? 'N/A';
    final brandModel = vehicle['vehicleBrandModel']?.toString() ?? '';
    final speed = (vehicle['speed'] as num?)?.toDouble();
    final fuelLevel = (vehicle['fuelLevel'] as num?)?.toDouble();
    final lastUpdate = vehicle['lastUpdate']?.toString() ?? '';

    return ZCard(
      child: SizedBox(
        width: 280,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                Icon(Icons.local_shipping_outlined, color: ZColors.moduleFleet, size: 20),
                const SizedBox(width: ZSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(plate, style: const TextStyle(fontWeight: FontWeight.w700)),
                      Text(brandModel, style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurface.withValues(alpha: 0.6),
                      )),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, size: 18),
                  onPressed: () {
                    ref.read(fleetGpsProvider.notifier).clearSelected();
                  },
                ),
              ],
            ),
            const Divider(height: ZSpacing.md),
            Row(
              children: [
                if (speed != null) ...[
                  Icon(Icons.speed_outlined, size: 16, color: ZColors.neutral500),
                  const SizedBox(width: 4),
                  Text('${speed.toStringAsFixed(0)} km/h', style: theme.textTheme.bodySmall),
                ],
                const SizedBox(width: ZSpacing.md),
                if (fuelLevel != null) ...[
                  Icon(Icons.local_gas_station_outlined, size: 16,
                    color: fuelLevel < 20 ? ZColors.danger : ZColors.neutral500),
                  const SizedBox(width: 4),
                  Text('${fuelLevel.toStringAsFixed(0)}%', style: theme.textTheme.bodySmall),
                ],
              ],
            ),
            if (lastUpdate.isNotEmpty) ...[
              const SizedBox(height: ZSpacing.xs),
              Text('Última actualización: $lastUpdate',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurface.withValues(alpha: 0.5),
                )),
            ],
          ],
        ),
      ),
    );
  }

  Widget _mapButton(IconData icon, VoidCallback onTap) {
    return Material(
      color: Colors.white,
      shape: const CircleBorder(),
      elevation: 2,
      child: InkWell(
        onTap: onTap,
        customBorder: const CircleBorder(),
        child: SizedBox(
          width: 36,
          height: 36,
          child: Icon(icon, size: 20, color: ZColors.neutral700),
        ),
      ),
    );
  }

  LatLng _computeCenter(List<Map<String, dynamic>> vehicles) {
    if (vehicles.isEmpty) return const LatLng(12.1150, -86.2362); // Managua default
    double latSum = 0, lngSum = 0;
    int count = 0;
    for (final v in vehicles) {
      final lat = (v['latitude'] as num?)?.toDouble();
      final lng = (v['longitude'] as num?)?.toDouble();
      if (lat != null && lng != null && lat != 0 && lng != 0) {
        latSum += lat;
        lngSum += lng;
        count++;
      }
    }
    if (count == 0) return const LatLng(12.1150, -86.2362);
    return LatLng(latSum / count, lngSum / count);
  }

  // ── Vehicle List ──

  Widget _buildVehicleList(List<Map<String, dynamic>> vehicles, ThemeData theme) {
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Vehículos en Flota', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),
          Expanded(
            child: ListView.separated(
              itemCount: vehicles.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) {
                final v = vehicles[index];
                final speed = (v['speed'] as num?)?.toDouble();
                final plate = v['vehiclePlate'] ?? 'N/A';
                final brandModel = v['vehicleBrandModel'] ?? '';
                final fuelLevel = (v['fuelLevel'] as num?)?.toDouble();

                return ListTile(
                  contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.sm, vertical: ZSpacing.xs),
                  leading: Container(
                    width: 8,
                    height: 40,
                    decoration: BoxDecoration(
                      color: speed != null && speed > 0 ? ZColors.success : ZColors.warning,
                      borderRadius: BorderRadius.circular(4),
                    ),
                  ),
                  title: Text(plate, style: const TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: Text(brandModel, style: theme.textTheme.bodySmall),
                  trailing: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      if (speed != null)
                        ZBadge(
                          text: '${speed.toStringAsFixed(0)} km/h',
                          type: speed > 0 ? ZBadgeType.success : ZBadgeType.neutral,
                        ),
                      if (fuelLevel != null) ...[
                        const SizedBox(height: 2),
                        Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(
                              Icons.local_gas_station_outlined,
                              size: 12,
                              color: fuelLevel < 20 ? ZColors.danger : ZColors.neutral500,
                            ),
                            const SizedBox(width: 2),
                            Text('${fuelLevel.toStringAsFixed(0)}%', style: theme.textTheme.bodySmall),
                          ],
                        ),
                      ],
                    ],
                  ),
                  onTap: () {
                    final vid = v['vehicleId']?.toString();
                    if (vid != null) {
                      ref.read(fleetGpsProvider.notifier).selectVehicle(vid);
                      // Fly map to vehicle position
                      final lat = (v['latitude'] as num?)?.toDouble();
                      final lng = (v['longitude'] as num?)?.toDouble();
                      if (lat != null && lng != null && lat != 0 && lng != 0) {
                        _mapController.move(LatLng(lat, lng), 15.0);
                      }
                    }
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
