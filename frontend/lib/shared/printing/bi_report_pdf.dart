import 'dart:math' as math;
import 'dart:typed_data';

import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import '../../core/utils/formatters.dart';
import '../../features/bi/models/bi_models.dart';

// ── Paleta de marca (Zorvian brand system) ──────────────────────────────────
const _navy = PdfColor.fromInt(0x1A0A3E); // Deep Violet-Navy (primario)
const _violet = PdfColor.fromInt(0x7C4DFF); // Medium Violet (acento)
const _cyan = PdfColor.fromInt(0x00E5FF); // Cyan eléctrico
const _teal = PdfColor.fromInt(0x2EE59D); // Teal
const _gold = PdfColor.fromInt(0xFFD54F); // Gold
const _green = PdfColor.fromInt(0x1B8A5A);
const _red = PdfColor.fromInt(0xC62828);
const _greyText = PdfColor.fromInt(0x6B6B7B);
const _greyLine = PdfColor.fromInt(0xE3E3EB);
const _greyBg = PdfColor.fromInt(0xF6F6FA);
const _violetBg = PdfColor.fromInt(0xECE7FA);

const _monthShort = ['', 'Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

/// Contexto de marca/alcance del reporte (encabezado y pie de página).
class BiReportMeta {
  final String title;
  final String companyName;
  final String branchLabel;
  final String currencyCode;
  final Uint8List? logoBytes;
  final DateTime generatedAt;

  BiReportMeta({
    required this.title,
    required this.companyName,
    required this.branchLabel,
    this.currencyCode = 'NIO',
    this.logoBytes,
  }) : generatedAt = DateTime.now();
}

// ═══════════════════════════════════════════════════════════════════════════
// Reporte Ejecutivo
// ═══════════════════════════════════════════════════════════════════════════

Future<Uint8List> buildExecutiveBiReportPdf({
  required BiExecutive executive,
  required List<BiMonthSales> trend,
  BranchComparison? comparison,
  BiArAging? aging,
  BiFinancialRatios? ratios,
  List<String> omittedSections = const [],
  required BiReportMeta meta,
}) async {
  final doc = pw.Document();

  doc.addPage(pw.MultiPage(
    pageFormat: PdfPageFormat.a4,
    margin: const pw.EdgeInsets.fromLTRB(36, 80, 36, 56),
    header: (_) => _pageHeader(meta),
    footer: (ctx) => _pageFooter(meta, ctx.pageNumber, ctx.pagesCount),
    build: (_) => [
      _titleBlock(meta, _periodLabel(trend)),
      _sectionTitle('Indicadores Clave · Ventas'),
      _kpiRow([
        _kpiCard('Ventas Hoy', _money(executive.sales.todaySales, meta), accent: _green,
            delta: executive.sales.salesChangePercent),
        _kpiCard('Ticket Promedio', _money(executive.sales.averageTicket, meta), accent: _violet),
        _kpiCard('Ventas del Mes', _money(executive.sales.monthSales, meta), accent: _cyan,
            delta: executive.sales.monthSalesChangePercent),
        _kpiCard('Transacciones Hoy', '${executive.sales.todaySalesCount}', accent: _gold),
      ]),
      _sectionTitle('Tendencia de Ventas (12 meses)'),
      _trendLineChart(trend, meta.currencyCode),
      _sectionTitle('Indicadores Clave · Créditos'),
      _kpiRow([
        _kpiCard('Cartera Total', _money(executive.credits.totalPortfolio, meta), accent: _navy),
        _kpiCard('Créditos Vencidos', '${executive.credits.overdueCredits}', accent: _red),
        _kpiCard('Recuperación del Mes', _money(executive.credits.monthlyRecovery, meta), accent: _teal),
        _kpiCard('DSO (días)', executive.credits.dsoDays.toStringAsFixed(0), accent: _violet),
      ]),
      if (aging != null) ...[
        _sectionTitle('Antigüedad de Cartera (AR)'),
        _agingBarChart(aging, meta.currencyCode),
        _agingTable(aging, meta),
        if (aging.byClient.isNotEmpty) ...[
          _subTitle('Principales clientes por saldo'),
          _clientsAgingTable(aging.byClient.take(5).toList(), meta),
        ],
      ],
      if (comparison != null && comparison.branches.isNotEmpty) ...[
        _sectionTitle('Comparativo por Sucursal (Mes en Curso)'),
        _branchBarChart(comparison, meta.currencyCode),
        _branchTable(comparison, meta),
        _note('Vista empresa: compara todas las sucursales sin importar la seleccionada en el encabezado.'),
      ],
      _sectionTitle('Indicadores Clave · Inventario'),
      _kpiRow([
        _kpiCard('Valor Inventario', _money(executive.inventory.totalStockValue, meta), accent: _gold),
        _kpiCard('Rotación', executive.inventory.turnoverRate.toStringAsFixed(1), accent: _cyan),
        _kpiCard('Agotados', '${executive.inventory.outOfStockProducts}', accent: _red),
        _kpiCard('Stock Bajo', '${executive.inventory.lowStockProducts}', accent: _violet),
      ]),
      if (ratios != null) ...[
        _sectionTitle('Ratios Financieros'),
        _ratiosBlock(ratios),
      ],
      if (executive.alerts.isNotEmpty) ...[
        _sectionTitle('Alertas'),
        _alertsBlock(executive.alerts),
      ],
      if (omittedSections.isNotEmpty) _omittedNote(omittedSections),
    ],
  ));

  return doc.save();
}

// ═══════════════════════════════════════════════════════════════════════════
// Reporte Comercial
// ═══════════════════════════════════════════════════════════════════════════

Future<Uint8List> buildCommercialBiReportPdf({
  required List<BiMonthSales> trend,
  required BiReportMeta meta,
}) async {
  final doc = pw.Document();

  final totalSales = trend.fold<double>(0, (s, m) => s + m.total);
  final totalCount = trend.fold<int>(0, (s, m) => s + m.count);
  final ticket = totalCount > 0 ? totalSales / totalCount : 0.0;
  final monthlyAvg = trend.isNotEmpty ? totalSales / trend.length : 0.0;

  doc.addPage(pw.MultiPage(
    pageFormat: PdfPageFormat.a4,
    margin: const pw.EdgeInsets.fromLTRB(36, 80, 36, 56),
    header: (_) => _pageHeader(meta),
    footer: (ctx) => _pageFooter(meta, ctx.pageNumber, ctx.pagesCount),
    build: (_) => [
      _titleBlock(meta, _periodLabel(trend)),
      _kpiRow([
        _kpiCard('Total Acumulado', _money(totalSales, meta), accent: _violet),
        _kpiCard('Total Transacciones', '$totalCount', accent: _cyan),
        _kpiCard('Ticket Promedio', _money(ticket, meta), accent: _green),
        _kpiCard('Promedio Mensual', _money(monthlyAvg, meta), accent: _gold),
      ]),
      _sectionTitle('Tendencia de Ventas (12 meses)'),
      _trendLineChart(trend, meta.currencyCode),
      _sectionTitle('Transacciones por Mes'),
      _countBarChart(trend),
      _sectionTitle('Detalle Mensual'),
      _monthlyTable(trend, meta),
    ],
  ));

  return doc.save();
}

// ═══════════════════════════════════════════════════════════════════════════
// Encabezado / pie / bloques de estructura
// ═══════════════════════════════════════════════════════════════════════════

pw.Widget _pageHeader(BiReportMeta meta) => pw.Container(
      padding: const pw.EdgeInsets.only(bottom: 8),
      decoration: const pw.BoxDecoration(
        border: pw.Border(bottom: pw.BorderSide(color: _violet, width: 1.4)),
      ),
      child: pw.Row(
        crossAxisAlignment: pw.CrossAxisAlignment.center,
        children: [
          if (meta.logoBytes != null)
            pw.Container(
              height: 26,
              width: 26,
              child: pw.Image(pw.MemoryImage(meta.logoBytes!), fit: pw.BoxFit.contain),
            )
          else
            pw.Container(
              height: 26,
              width: 26,
              decoration: const pw.BoxDecoration(
                color: _navy,
                borderRadius: pw.BorderRadius.all(pw.Radius.circular(6)),
              ),
              alignment: pw.Alignment.center,
              child: pw.Text('Z', style: pw.TextStyle(fontSize: 14, fontWeight: pw.FontWeight.bold, color: PdfColors.white)),
            ),
          pw.SizedBox(width: 8),
          pw.Expanded(
            child: pw.Column(
              crossAxisAlignment: pw.CrossAxisAlignment.start,
              children: [
                pw.Text(meta.companyName, style: pw.TextStyle(fontSize: 10.5, fontWeight: pw.FontWeight.bold, color: _navy)),
                pw.Text(meta.branchLabel, style: pw.TextStyle(fontSize: 7.5, color: _greyText)),
              ],
            ),
          ),
          pw.Column(
            crossAxisAlignment: pw.CrossAxisAlignment.end,
            children: [
              pw.Text(meta.title.toUpperCase(), style: pw.TextStyle(fontSize: 9, fontWeight: pw.FontWeight.bold, color: _violet)),
              pw.Text('Generado ${_fmtDateTime(meta.generatedAt)}', style: pw.TextStyle(fontSize: 7, color: _greyText)),
            ],
          ),
        ],
      ),
    );

pw.Widget _pageFooter(BiReportMeta meta, int page, int pages) => pw.Row(
      children: [
        pw.Expanded(
          child: pw.Text('Generado por Zorvian ERP · Confidencial', style: pw.TextStyle(fontSize: 7, color: _greyText)),
        ),
        pw.Text('Página $page de $pages', style: pw.TextStyle(fontSize: 7, color: _greyText)),
      ],
    );

pw.Widget _titleBlock(BiReportMeta meta, String period) => pw.Container(
      margin: const pw.EdgeInsets.only(bottom: 14),
      child: pw.Column(
        crossAxisAlignment: pw.CrossAxisAlignment.start,
        children: [
          pw.Text(meta.title, style: pw.TextStyle(fontSize: 20, fontWeight: pw.FontWeight.bold, color: _navy)),
          pw.SizedBox(height: 2),
          pw.Text(period, style: pw.TextStyle(fontSize: 9.5, color: _greyText)),
        ],
      ),
    );

pw.Widget _sectionTitle(String text) => pw.Container(
      margin: const pw.EdgeInsets.only(top: 12, bottom: 6),
      child: pw.Row(
        children: [
          pw.Container(width: 4, height: 13, color: _violet),
          pw.SizedBox(width: 6),
          pw.Text(text, style: pw.TextStyle(fontSize: 11.5, fontWeight: pw.FontWeight.bold, color: _navy)),
        ],
      ),
    );

pw.Widget _subTitle(String text) => pw.Container(
      margin: const pw.EdgeInsets.only(top: 8, bottom: 4),
      child: pw.Text(text, style: pw.TextStyle(fontSize: 9.5, fontWeight: pw.FontWeight.bold, color: _greyText)),
    );

pw.Widget _note(String text) => pw.Container(
      margin: const pw.EdgeInsets.only(top: 4),
      child: pw.Text(text, style: pw.TextStyle(fontSize: 7.5, fontStyle: pw.FontStyle.italic, color: _greyText)),
    );

/// Aviso de secciones que no pudieron cargarse: sin esto el reporte se lee
/// como completo cuando en realidad faltan datos del dashboard.
pw.Widget _omittedNote(List<String> sections) => pw.Container(
      margin: const pw.EdgeInsets.only(top: 14),
      padding: const pw.EdgeInsets.all(8),
      decoration: pw.BoxDecoration(
        color: _greyBg,
        border: pw.Border.all(color: _greyLine),
        borderRadius: const pw.BorderRadius.all(pw.Radius.circular(6)),
      ),
      child: pw.Text(
        'Secciones omitidas por falta de datos: ${sections.join(' · ')}. '
        'Actualice el dashboard y vuelva a generar el reporte.',
        style: pw.TextStyle(fontSize: 8, fontStyle: pw.FontStyle.italic, color: _greyText),
      ),
    );

// ═══════════════════════════════════════════════════════════════════════════
// Tarjetas KPI
// ═══════════════════════════════════════════════════════════════════════════

pw.Widget _kpiRow(List<pw.Widget> cards) => pw.Padding(
      padding: const pw.EdgeInsets.only(bottom: 6),
      child: pw.Row(children: [for (final c in cards) pw.Expanded(child: c)]),
    );

pw.Widget _kpiCard(String label, String value, {required PdfColor accent, double? delta}) => pw.Container(
      margin: const pw.EdgeInsets.symmetric(horizontal: 2),
      decoration: pw.BoxDecoration(
        border: pw.Border.all(color: _greyLine),
        borderRadius: const pw.BorderRadius.all(pw.Radius.circular(6)),
      ),
      child: pw.Column(
        crossAxisAlignment: pw.CrossAxisAlignment.start,
        children: [
          pw.Container(height: 3, decoration: pw.BoxDecoration(color: accent)),
          pw.Padding(
            padding: const pw.EdgeInsets.all(7),
            child: pw.Column(
              crossAxisAlignment: pw.CrossAxisAlignment.start,
              children: [
                pw.Text(label.toUpperCase(), style: pw.TextStyle(fontSize: 6, fontWeight: pw.FontWeight.bold, color: _greyText)),
                pw.SizedBox(height: 2),
                pw.FittedBox(
                  fit: pw.BoxFit.scaleDown,
                  alignment: pw.Alignment.centerLeft,
                  child: pw.Text(value, style: pw.TextStyle(fontSize: 11.5, fontWeight: pw.FontWeight.bold, color: _navy)),
                ),
                if (delta != null) ...[
                  pw.SizedBox(height: 1),
                  pw.Text(
                    '${delta >= 0 ? '+' : ''}${delta.toStringAsFixed(1)}%',
                    style: pw.TextStyle(fontSize: 7, fontWeight: pw.FontWeight.bold, color: delta >= 0 ? _green : _red),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );

// ═══════════════════════════════════════════════════════════════════════════
// Gráficos vectoriales
// ═══════════════════════════════════════════════════════════════════════════

pw.Widget _chartShell(pw.Widget chart, {double height = 180}) => pw.Container(
      height: height,
      width: double.infinity,
      margin: const pw.EdgeInsets.only(bottom: 6),
      padding: const pw.EdgeInsets.all(8),
      decoration: pw.BoxDecoration(
        border: pw.Border.all(color: _greyLine),
        borderRadius: const pw.BorderRadius.all(pw.Radius.circular(6)),
      ),
      child: chart,
    );

/// Línea curva con relleno suave (área) y puntos — tendencia de ventas.
pw.Widget _trendLineChart(List<BiMonthSales> trend, String currencyCode) {
  if (trend.length < 2) return _chartShell(_emptyChartLabel(), height: 90);

  final totals = trend.map((m) => m.total).toList();
  final yTicks = _niceTicks(totals.reduce(math.max));
  final labels = [for (final m in trend) _monthShort[m.month]];

  return _chartShell(
    pw.Chart(
      grid: pw.CartesianGrid(
        xAxis: pw.FixedAxis.fromStrings(
          labels,
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          marginStart: 4,
          marginEnd: 4,
        ),
        yAxis: pw.FixedAxis<double>(
          yTicks,
          format: (v) => v == 0 ? '0' : ZFormatters.currencyCompact(v, currencyCode: currencyCode),
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          divisions: true,
          divisionsColor: _greyLine,
          divisionsWidth: 0.5,
          marginEnd: 4,
        ),
      ),
      datasets: [
        pw.LineDataSet<pw.PointChartValue>(
          data: [for (var i = 0; i < totals.length; i++) pw.PointChartValue(i.toDouble(), totals[i])],
          color: _violet,
          lineColor: _violet,
          lineWidth: 2.2,
          drawPoints: true,
          pointSize: 2.6,
          pointColor: _cyan,
          drawSurface: true,
          surfaceColor: _violet,
          surfaceOpacity: 0.16,
          isCurved: true,
        ),
      ],
    ),
  );
}

/// Barras por tramo de mora, con semáforo de color (verde → rojo).
pw.Widget _agingBarChart(BiArAging aging, String currencyCode) {
  final values = [aging.current, aging.days30, aging.days60, aging.days90, aging.days90Plus];
  final maxV = values.reduce(math.max);
  if (maxV <= 0) return _chartShell(_emptyChartLabel(), height: 90);

  final colors = [_green, _teal, _gold, PdfColors.deepOrange, _red];
  final labels = ['Corriente', '1-30d', '31-60d', '61-90d', '+90d'];

  return _chartShell(
    pw.Chart(
      grid: pw.CartesianGrid(
        xAxis: pw.FixedAxis.fromStrings(
          labels,
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          marginStart: 4,
          marginEnd: 4,
        ),
        yAxis: pw.FixedAxis<double>(
          _niceTicks(maxV),
          format: (v) => v == 0 ? '0' : ZFormatters.currencyCompact(v, currencyCode: currencyCode),
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          divisions: true,
          divisionsColor: _greyLine,
          divisionsWidth: 0.5,
          marginEnd: 4,
        ),
      ),
      datasets: [
        for (var i = 0; i < values.length; i++)
          pw.BarDataSet<pw.PointChartValue>(
            data: [pw.PointChartValue(i.toDouble(), values[i])],
            color: colors[i],
            width: 16,
          ),
      ],
    ),
  );
}

/// Barras de ventas del mes por sucursal (un solo color de marca).
pw.Widget _branchBarChart(BranchComparison comparison, String currencyCode) {
  final branches = comparison.branches;
  final maxV = branches.map((b) => b.monthSales).reduce(math.max);
  if (maxV <= 0) return _chartShell(_emptyChartLabel(), height: 90);

  final yTicks = _niceTicks(maxV);

  return _chartShell(
    pw.Chart(
      grid: pw.CartesianGrid(
        xAxis: pw.FixedAxis.fromStrings(
          [for (final b in branches) b.branchName],
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          marginStart: 4,
          marginEnd: 4,
        ),
        yAxis: pw.FixedAxis<double>(
          yTicks,
          format: (v) => v == 0 ? '0' : ZFormatters.currencyCompact(v, currencyCode: currencyCode),
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          divisions: true,
          divisionsColor: _greyLine,
          divisionsWidth: 0.5,
          marginEnd: 4,
        ),
      ),
      datasets: [
        pw.BarDataSet<pw.PointChartValue>(
          data: [for (var i = 0; i < branches.length; i++) pw.PointChartValue(i.toDouble(), branches[i].monthSales)],
          color: _violet,
          width: 16,
        ),
      ],
    ),
  );
}

/// Barras de conteo de transacciones por mes.
pw.Widget _countBarChart(List<BiMonthSales> trend) {
  if (trend.isEmpty) return _chartShell(_emptyChartLabel(), height: 90);
  final counts = trend.map((m) => m.count.toDouble()).toList();
  final maxV = counts.reduce(math.max);
  if (maxV <= 0) return _chartShell(_emptyChartLabel(), height: 90);

  return _chartShell(
    pw.Chart(
      grid: pw.CartesianGrid(
        xAxis: pw.FixedAxis.fromStrings(
          [for (final m in trend) _monthShort[m.month]],
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          marginStart: 4,
          marginEnd: 4,
        ),
        yAxis: pw.FixedAxis<double>(
          _niceTicks(maxV, divisions: 4),
          format: (v) => v == 0 ? '0' : v.toStringAsFixed(0),
          textStyle: pw.TextStyle(fontSize: 6.5, color: _greyText),
          color: _greyLine,
          divisions: true,
          divisionsColor: _greyLine,
          divisionsWidth: 0.5,
          marginEnd: 4,
        ),
      ),
      datasets: [
        pw.BarDataSet<pw.PointChartValue>(
          data: [for (var i = 0; i < counts.length; i++) pw.PointChartValue(i.toDouble(), counts[i])],
          color: _cyan,
          width: 14,
        ),
      ],
    ),
  );
}

pw.Widget _emptyChartLabel() => pw.Center(
      child: pw.Text('Datos insuficientes para el gráfico', style: pw.TextStyle(fontSize: 8, color: _greyText)),
    );

// ═══════════════════════════════════════════════════════════════════════════
// Ratios (barras de progreso) y alertas
// ═══════════════════════════════════════════════════════════════════════════

pw.Widget _ratiosBlock(BiFinancialRatios r) => pw.Container(
      margin: const pw.EdgeInsets.only(bottom: 6),
      padding: const pw.EdgeInsets.all(10),
      decoration: pw.BoxDecoration(
        border: pw.Border.all(color: _greyLine),
        borderRadius: const pw.BorderRadius.all(pw.Radius.circular(6)),
      ),
      child: pw.Column(
        children: [
          _ratioBar('Liquidez (Corriente)', r.currentRatio, max: 3, ok: r.currentRatio >= 1.5),
          _ratioBar('Prueba Ácida', r.quickRatio, max: 2, ok: r.quickRatio >= 1),
          _ratioBar('Endeudamiento', r.debtRatio, max: 1, ok: r.debtRatio <= 0.5, inverse: true),
          _ratioBar('ROE', r.roe, max: 0.5, ok: r.roe >= 0.1, percentLabel: true),
        ],
      ),
    );

pw.Widget _ratioBar(String label, double value, {required double max, required bool ok, bool inverse = false, bool percentLabel = false}) {
  final filled = max > 0 ? ((value / max) * 100).clamp(0, 100).round() : 0;
  final color = ok ? _green : _red;
  final shown = percentLabel ? '${(value * 100).toStringAsFixed(1)}%' : value.toStringAsFixed(2);

  return pw.Padding(
    padding: const pw.EdgeInsets.only(bottom: 8),
    child: pw.Column(
      crossAxisAlignment: pw.CrossAxisAlignment.start,
      children: [
        pw.Row(
          children: [
            pw.Expanded(child: pw.Text(label, style: pw.TextStyle(fontSize: 8, fontWeight: pw.FontWeight.bold, color: _navy))),
            pw.Text(shown, style: pw.TextStyle(fontSize: 8, fontWeight: pw.FontWeight.bold, color: _navy)),
            pw.SizedBox(width: 6),
            pw.Text(
              ok ? 'Óptimo' : (inverse ? 'Alto' : 'Atención'),
              style: pw.TextStyle(fontSize: 7, fontWeight: pw.FontWeight.bold, color: color),
            ),
          ],
        ),
        pw.SizedBox(height: 3),
        pw.Row(
          children: [
            pw.Expanded(flex: filled, child: pw.Container(height: 6, color: color)),
            pw.Expanded(flex: 100 - filled, child: pw.Container(height: 6, color: _greyBg)),
          ],
        ),
      ],
    ),
  );
}

pw.Widget _alertsBlock(List<BiAlert> alerts) => pw.Column(
      children: [
        for (final a in alerts.take(8)) _alertRow(a),
      ],
    );

pw.Widget _alertRow(BiAlert a) {
  final color = switch (a.severity.toLowerCase()) {
    'critical' || 'high' => _red,
    'warning' || 'medium' => PdfColors.orange700,
    _ => _cyan,
  };
  return pw.Padding(
    padding: const pw.EdgeInsets.symmetric(vertical: 2),
    child: pw.Row(
      crossAxisAlignment: pw.CrossAxisAlignment.start,
      children: [
        pw.Container(width: 6, height: 6, margin: const pw.EdgeInsets.only(top: 2, right: 6), decoration: pw.BoxDecoration(color: color, borderRadius: pw.BorderRadius.all(pw.Radius.circular(3)))),
        pw.Expanded(child: pw.Text(a.message, style: pw.TextStyle(fontSize: 8, color: _navy))),
      ],
    ),
  );
}

// ═══════════════════════════════════════════════════════════════════════════
// Tablas
// ═══════════════════════════════════════════════════════════════════════════

pw.Widget _dataTable({
  required List<String> headers,
  required List<List<String>> rows,
  List<bool> rightAlign = const [],
  bool totalRow = false,
}) {
  bool right(int col) => col < rightAlign.length && rightAlign[col];

  pw.Widget cell(String text, {required bool header, bool bold = false, PdfColor? bg, int col = 0}) => pw.Expanded(
        child: pw.Container(
          padding: const pw.EdgeInsets.symmetric(horizontal: 5, vertical: 3.5),
          alignment: right(col) ? pw.Alignment.centerRight : pw.Alignment.centerLeft,
          decoration: bg != null ? pw.BoxDecoration(color: bg) : null,
          child: pw.Text(
            text,
            style: pw.TextStyle(
              fontSize: 7.5,
              fontWeight: header || bold ? pw.FontWeight.bold : pw.FontWeight.normal,
              color: header ? PdfColors.white : _navy,
            ),
          ),
        ),
      );

  return pw.Container(
    margin: const pw.EdgeInsets.only(bottom: 6),
    decoration: pw.BoxDecoration(border: pw.Border.all(color: _greyLine)),
    child: pw.Column(
      children: [
        pw.Row(children: [for (var c = 0; c < headers.length; c++) cell(headers[c], header: true, col: c, bg: _navy)]),
        for (var r = 0; r < rows.length; r++)
          pw.Row(
            children: [
              for (var c = 0; c < rows[r].length; c++)
                cell(
                  rows[r][c],
                  header: false,
                  col: c,
                  bold: totalRow && r == rows.length - 1,
                  bg: totalRow && r == rows.length - 1
                      ? _violetBg
                      : (r.isOdd ? _greyBg : null),
                ),
            ],
          ),
      ],
    ),
  );
}

pw.Widget _agingTable(BiArAging aging, BiReportMeta meta) {
  final total = aging.totalPortfolio;
  String pct(double v) => total > 0 ? '${(v / total * 100).toStringAsFixed(1)}%' : '0%';
  return _dataTable(
    headers: const ['Tramo', 'Monto', '% Cartera'],
    rightAlign: const [false, true, true],
    rows: [
      ['Corriente', _money(aging.current, meta), pct(aging.current)],
      ['1-30 días', _money(aging.days30, meta), pct(aging.days30)],
      ['31-60 días', _money(aging.days60, meta), pct(aging.days60)],
      ['61-90 días', _money(aging.days90, meta), pct(aging.days90)],
      ['+90 días', _money(aging.days90Plus, meta), pct(aging.days90Plus)],
      ['Total Cartera', _money(total, meta), '100%'],
    ],
    totalRow: true,
  );
}

pw.Widget _clientsAgingTable(List<BiClientAging> clients, BiReportMeta meta) => _dataTable(
      headers: const ['Cliente', 'Saldo', 'Corriente', 'Vencido'],
      rightAlign: const [false, true, true, true],
      rows: [
        for (final c in clients)
          [
            c.clientName,
            _money(c.balance, meta),
            _money(c.current, meta),
            _money(c.balance - c.current, meta),
          ],
      ],
    );

pw.Widget _branchTable(BranchComparison comparison, BiReportMeta meta) => _dataTable(
      headers: const ['Sucursal', 'Ventas Mes', 'Cobros', 'Cartera', 'Vencidos', 'Inventario'],
      rightAlign: const [false, true, true, true, true, true],
      rows: [
        for (final b in comparison.branches)
          [
            b.branchName,
            _money(b.monthSales, meta),
            _money(b.monthRecovery, meta),
            _money(b.portfolio, meta),
            '${b.overdueCredits}',
            _money(b.inventoryValue, meta),
          ],
        [
          'Total Empresa',
          _money(comparison.totalMonthSales, meta),
          _money(comparison.totalMonthRecovery, meta),
          _money(comparison.totalPortfolio, meta),
          '${comparison.totalOverdueCredits}',
          _money(comparison.totalInventoryValue, meta),
        ],
      ],
      totalRow: true,
    );

pw.Widget _monthlyTable(List<BiMonthSales> trend, BiReportMeta meta) {
  final totalSales = trend.fold<double>(0, (s, m) => s + m.total);
  final totalCount = trend.fold<int>(0, (s, m) => s + m.count);
  final ticket = totalCount > 0 ? totalSales / totalCount : 0.0;
  return _dataTable(
    headers: const ['Mes', 'Transacciones', 'Ventas', 'Ticket Promedio'],
    rightAlign: const [false, true, true, true],
    rows: [
      for (final m in trend)
        [
          '${_monthShort[m.month]} ${m.year}',
          '${m.count}',
          _money(m.total, meta),
          _money(m.averageTicket > 0 ? m.averageTicket : (m.count > 0 ? m.total / m.count : 0), meta),
        ],
      ['Total', '$totalCount', _money(totalSales, meta), _money(ticket, meta)],
    ],
    totalRow: true,
  );
}

// ═══════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════

String _money(double v, BiReportMeta meta) => ZFormatters.currency(v, currencyCode: meta.currencyCode);

String _periodLabel(List<BiMonthSales> trend) {
  if (trend.isEmpty) return 'Sin datos del período';
  final first = trend.first;
  final last = trend.last;
  return 'Período: ${_monthShort[first.month]} ${first.year} - ${_monthShort[last.month]} ${last.year} · Al ${_fmtDate(DateTime.now())}';
}

String _fmtDate(DateTime d) => '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

String _fmtDateTime(DateTime d) => '${_fmtDate(d)} ${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';

/// Divisiones "bonitas" del eje Y (1/2/2.5/5/10 × potencia de 10).
List<double> _niceTicks(double maxValue, {int divisions = 4}) {
  if (maxValue <= 0) return [0, 1];
  final rawStep = maxValue / divisions;
  final mag = math.pow(10, (math.log(rawStep) / math.ln10).floor()).toDouble();
  final norm = rawStep / mag;
  final step = (norm <= 1 ? 1.0 : norm <= 2 ? 2.0 : norm <= 2.5 ? 2.5 : norm <= 5 ? 5.0 : 10.0) * mag;
  final top = (maxValue / step).ceil() * step;
  return [for (double v = 0; v <= top + step / 2; v += step) double.parse(v.toStringAsFixed(10))];
}
