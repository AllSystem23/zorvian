import 'package:flutter/material.dart';
import '../ds.dart';

/// ZColumn definition with support for sorting and visibility
class ZColumn {
  final String id;
  final String label;
  final bool numeric;
  final bool visible;
  final bool sortable;
  final IconData? icon;
  final Widget? header;

  /// Ancho fijo en tablet/escritorio. En teléfono se ignora: las tarjetas
  /// usan todo el ancho disponible.
  final double? width;

  const ZColumn({
    required this.id,
    required this.label,
    this.numeric = false,
    this.visible = true,
    this.sortable = false,
    this.icon,
    this.header,
    this.width,
  });
}

/// Par (columna visible, índice de su celda en el [DataRow] original).
///
/// El índice se resuelve por posición de la columna en `widget.columns`, NO por
/// la posición en la lista filtrada: ocultar una columna ya no desalinea las
/// celdas (`row.cells[i]` apuntaba a la columna equivocada).
typedef _ColumnEntry = ({ZColumn column, int cellIndex});

/// A "HubSpot-style" advanced data table with selection, expandable rows, and bulk actions.
///
/// En teléfonos (`< ZBreakpoints.phone`) cada fila se dibuja como una **tarjeta**
/// (`título = primera columna`, resto `etiqueta → valor`), porque la tabla de
/// columnas no cabe sin scroll horizontal. Se puede reemplazar con
/// [mobileCardBuilder] cuando la entidad necesite una tarjeta a medida.
class ZDataTable<T> extends StatefulWidget {
  final List<ZColumn> columns;
  final List<T> rows;
  final DataRow Function(T item) rowMapper;
  final bool isLoading;
  final String? emptyMessage;
  final int? totalCount;
  final int currentPage;
  final int pageSize;
  final ValueChanged<int>? onPageChanged;
  final void Function(String)? onSearch;
  final bool showExport;
  final VoidCallback? onExport;
  final bool showColumnToggle;
  final Widget? tableActions;

  // Advanced Features
  final bool selectionEnabled;
  final void Function(List<T>)? onSelectionChanged;
  final List<Widget>? bulkActions;
  final Widget Function(T)? expandedRowBuilder;
  final bool stickyHeader;

  /// Tarjeta a medida para teléfono. Si es null se usa el renderer automático
  /// (título + etiqueta/valor por columna).
  final Widget Function(T item)? mobileCardBuilder;

  const ZDataTable({
    super.key,
    required this.columns,
    required this.rows,
    required this.rowMapper,
    this.isLoading = false,
    this.emptyMessage,
    this.totalCount,
    this.currentPage = 1,
    this.pageSize = 20,
    this.onPageChanged,
    this.onSearch,
    this.showExport = false,
    this.onExport,
    this.showColumnToggle = false,
    this.tableActions,
    this.selectionEnabled = false,
    this.onSelectionChanged,
    this.bulkActions,
    this.expandedRowBuilder,
    this.stickyHeader = true,
    this.mobileCardBuilder,
  });

  @override
  State<ZDataTable<T>> createState() => _ZDataTableState<T>();
}

class _ZDataTableState<T> extends State<ZDataTable<T>> {
  final _searchCtrl = TextEditingController();
  final Set<String> _hiddenColumns = {};
  final Set<T> _selectedItems = {};
  final Set<T> _expandedRows = {};

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  /// Columnas visibles con el índice real de su celda.
  List<_ColumnEntry> get _visibleEntries {
    final entries = <_ColumnEntry>[];
    for (var i = 0; i < widget.columns.length; i++) {
      final column = widget.columns[i];
      if (_hiddenColumns.contains(column.id)) continue;
      entries.add((column: column, cellIndex: i));
    }
    return entries;
  }

  bool get _allSelected =>
      widget.rows.isNotEmpty && _selectedItems.length == widget.rows.length;
  bool get _someSelected => _selectedItems.isNotEmpty && !_allSelected;

  void _toggleSelectAll(bool? selected) {
    setState(() {
      if (selected == true) {
        _selectedItems.addAll(widget.rows);
      } else {
        _selectedItems.clear();
      }
    });
    widget.onSelectionChanged?.call(_selectedItems.toList());
  }

  void _toggleSelect(T item, bool? selected) {
    setState(() {
      if (selected == true) {
        _selectedItems.add(item);
      } else {
        _selectedItems.remove(item);
      }
    });
    widget.onSelectionChanged?.call(_selectedItems.toList());
  }

  void _toggleExpand(T item) {
    setState(() {
      if (_expandedRows.contains(item)) {
        _expandedRows.remove(item);
      } else {
        _expandedRows.add(item);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        final isPhone = constraints.maxWidth < ZBreakpoints.phone;

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Selection / Bulk Actions Bar ──
            if (_selectedItems.isNotEmpty && widget.bulkActions != null)
              _buildBulkActionBar(isDark, isPhone)
            else
              _buildToolbar(isDark, isPhone),

            // ── Table Header (solo tablet/escritorio) ──
            if (!isPhone && widget.stickyHeader && widget.rows.isNotEmpty)
              _buildHeaderRow(isDark),

            // ── Body: tarjetas en teléfono, filas en el resto ──
            Expanded(
              child: widget.isLoading
                  ? const Center(child: CircularProgressIndicator())
                  : widget.rows.isEmpty
                      ? _buildEmptyState()
                      : isPhone
                          ? _buildCards(isDark)
                          : _buildRows(isDark),
            ),

            // ── Pagination ──
            if (widget.totalCount != null &&
                widget.totalCount! > widget.pageSize)
              _buildPagination(isDark, isPhone),
          ],
        );
      },
    );
  }

  Widget _buildToolbar(bool isDark, bool isPhone) {
    final search = widget.onSearch == null
        ? null
        : SizedBox(
            height: 40,
            child: TextField(
              controller: _searchCtrl,
              decoration: InputDecoration(
                hintText: 'Buscar...',
                prefixIcon: const Icon(Icons.search, size: 18),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(ZRadii.md),
                ),
                isDense: true,
                contentPadding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                filled: true,
                fillColor: isDark ? ZColors.darkSurface : ZColors.surface,
              ),
              onChanged: widget.onSearch,
            ),
          );

    final actions = <Widget>[
      if (widget.tableActions != null) widget.tableActions!,
      if (widget.showColumnToggle) _buildColumnToggle(isDark),
      if (widget.showExport)
        IconButton(
          icon: Icon(
            Icons.download_outlined,
            size: 20,
            color: isDark ? ZColors.neutral400 : ZColors.neutral600,
          ),
          tooltip: 'Exportar',
          onPressed: widget.onExport,
        ),
    ];

    return Padding(
      padding: const EdgeInsets.only(bottom: ZSpacing.sm),
      child: isPhone
          // En teléfono la barra se parte en dos líneas: buscador arriba,
          // acciones alineadas a la derecha (con `Wrap` para no desbordar si
          // la página pasa un botón ancho).
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (search != null) search,
                if (actions.isNotEmpty)
                  Padding(
                    padding: EdgeInsets.only(top: search != null ? ZSpacing.sm : 0),
                    child: Wrap(
                      alignment: WrapAlignment.end,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      spacing: ZSpacing.sm,
                      children: actions,
                    ),
                  ),
              ],
            )
          : Row(
              children: [
                if (search != null) Expanded(child: search),
                if (actions.isNotEmpty) ...[
                  const SizedBox(width: ZSpacing.sm),
                  ...actions,
                ],
              ],
            ),
    );
  }

  Widget _buildBulkActionBar(bool isDark, bool isPhone) {
    final color = isDark
        ? ZColors.brandPrimary.withAlpha(40)
        : ZColors.brandPrimary.withAlpha(20);
    final border = Border.all(color: ZColors.brandPrimary.withAlpha(60));
    final selectedLabel = Text(
      '${_selectedItems.length} seleccionados',
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
      style: const TextStyle(
        fontWeight: FontWeight.w600,
        color: ZColors.brandPrimary,
      ),
    );

    if (!isPhone) {
      return Container(
        height: 48,
        margin: const EdgeInsets.only(bottom: ZSpacing.sm),
        padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md),
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(ZRadii.md),
          border: border,
        ),
        child: Row(
          children: [
            Checkbox(
              value: _allSelected,
              tristate: _someSelected,
              onChanged: _toggleSelectAll,
              activeColor: ZColors.brandPrimary,
            ),
            selectedLabel,
            const VerticalDivider(width: 32, indent: 12, endIndent: 12),
            Expanded(
              child: Row(
                children: widget.bulkActions!,
              ),
            ),
            IconButton(
              icon: const Icon(Icons.close, size: 18),
              tooltip: 'Limpiar selección',
              onPressed: () => setState(() => _selectedItems.clear()),
            ),
          ],
        ),
      );
    }

    // Teléfono: dos líneas (selección + acciones en `Wrap`) para no desbordar.
    return Container(
      margin: const EdgeInsets.only(bottom: ZSpacing.sm),
      padding: const EdgeInsets.symmetric(
        horizontal: ZSpacing.md,
        vertical: ZSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: border,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Checkbox(
                value: _allSelected,
                tristate: _someSelected,
                onChanged: _toggleSelectAll,
                activeColor: ZColors.brandPrimary,
              ),
              Expanded(child: selectedLabel),
              IconButton(
                icon: const Icon(Icons.close, size: 18),
                tooltip: 'Limpiar selección',
                onPressed: () => setState(() => _selectedItems.clear()),
              ),
            ],
          ),
          Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.xs),
            child: Wrap(
              alignment: WrapAlignment.end,
              crossAxisAlignment: WrapCrossAlignment.center,
              spacing: ZSpacing.sm,
              runSpacing: ZSpacing.xs,
              children: widget.bulkActions!,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHeaderRow(bool isDark) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.neutral50,
        border: Border(
          bottom:
              BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
        ),
      ),
      child: Row(
        children: [
          if (widget.selectionEnabled) ...[
            SizedBox(
              width: 32,
              child: Checkbox(
                value: _allSelected,
                tristate: _someSelected,
                onChanged: _toggleSelectAll,
              ),
            ),
            const SizedBox(width: 8),
          ],
          if (widget.expandedRowBuilder != null) const SizedBox(width: 32),
          for (final entry in _visibleEntries)
            Expanded(
              flex: entry.column.width != null ? 0 : 1,
              child: SizedBox(
                width: entry.column.width,
                child: entry.column.header ??
                    Text(
                      entry.column.label.toUpperCase(),
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 11,
                        letterSpacing: 0.5,
                        color:
                            isDark ? ZColors.neutral400 : ZColors.neutral600,
                      ),
                    ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildRows(bool isDark) {
    return ListView.builder(
      itemCount: widget.rows.length,
      itemBuilder: (context, index) {
        final item = widget.rows[index];
        final isSelected = _selectedItems.contains(item);
        final isExpanded = _expandedRows.contains(item);
        final row = widget.rowMapper(item);

        return Column(
          children: [
            _buildDataRow(item, row, isSelected, isExpanded, isDark),
            if (isExpanded && widget.expandedRowBuilder != null)
              _buildExpandedPanel(item, isDark),
          ],
        );
      },
    );
  }

  Widget _buildExpandedPanel(T item, bool isDark) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: isDark ? ZColors.neutral800.withAlpha(50) : ZColors.neutral50,
        border: Border(
          bottom:
              BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
        ),
      ),
      child: widget.expandedRowBuilder!(item),
    );
  }

  Widget _buildDataRow(
    T item,
    DataRow row,
    bool isSelected,
    bool isExpanded,
    bool isDark,
  ) {
    return InkWell(
      onTap: widget.selectionEnabled
          ? () => _toggleSelect(item, !isSelected)
          : (widget.expandedRowBuilder != null
              ? () => _toggleExpand(item)
              : null),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 16),
        decoration: BoxDecoration(
          color: isSelected
              ? (isDark
                  ? ZColors.brandPrimary.withAlpha(30)
                  : ZColors.brandPrimary.withAlpha(10))
              : null,
          border: Border(
            bottom: BorderSide(
              color: isDark ? ZColors.darkBorder : ZColors.border,
            ),
          ),
        ),
        child: Row(
          children: [
            if (widget.selectionEnabled) ...[
              SizedBox(
                width: 32,
                child: Checkbox(
                  value: isSelected,
                  onChanged: (val) => _toggleSelect(item, val),
                ),
              ),
              const SizedBox(width: 8),
            ],
            if (widget.expandedRowBuilder != null)
              SizedBox(
                width: 32,
                child: IconButton(
                  icon: Icon(
                    isExpanded
                        ? Icons.keyboard_arrow_down
                        : Icons.keyboard_arrow_right,
                    size: 20,
                  ),
                  onPressed: () => _toggleExpand(item),
                ),
              ),
            for (final entry in _visibleEntries)
              Expanded(
                flex: entry.column.width != null ? 0 : 1,
                child: SizedBox(
                  width: entry.column.width,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    child: _cellAt(row, entry.cellIndex),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  // ── Teléfono ────────────────────────────────────────────────────────────

  Widget _buildCards(bool isDark) {
    return ListView.builder(
      itemCount: widget.rows.length,
      padding: const EdgeInsets.only(bottom: ZSpacing.lg),
      itemBuilder: (context, index) {
        final item = widget.rows[index];
        final isSelected = _selectedItems.contains(item);
        final isExpanded = _expandedRows.contains(item);
        final row = widget.rowMapper(item);

        return Column(
          children: [
            Padding(
              padding: const EdgeInsets.only(bottom: ZSpacing.sm),
              child: _buildCardShell(
                item: item,
                isSelected: isSelected,
                isExpanded: isExpanded,
                isDark: isDark,
                child: widget.mobileCardBuilder?.call(item) ??
                    _buildAutoCard(item, row, isSelected, isExpanded, isDark),
              ),
            ),
            if (isExpanded && widget.expandedRowBuilder != null)
              Container(
                margin: const EdgeInsets.only(bottom: ZSpacing.sm),
                decoration: BoxDecoration(
                  color: isDark ? ZColors.neutral800.withAlpha(50) : ZColors.neutral50,
                  borderRadius: BorderRadius.circular(ZRadii.lg),
                  border: Border.all(
                    color: isDark ? ZColors.darkBorder : ZColors.border,
                  ),
                ),
                child: _buildExpandedPanel(item, isDark),
              ),
          ],
        );
      },
    );
  }

  Widget _buildCardShell({
    required T item,
    required bool isSelected,
    required bool isExpanded,
    required bool isDark,
    required Widget child,
  }) {
    return Material(
      color: isSelected
          ? (isDark
              ? ZColors.brandPrimary.withAlpha(40)
              : ZColors.brandPrimary.withAlpha(10))
          : (isDark ? ZColors.darkSurface : ZColors.surface),
      borderRadius: BorderRadius.circular(ZRadii.lg),
      child: InkWell(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        onTap: widget.selectionEnabled
            ? () => _toggleSelect(item, !isSelected)
            : (widget.expandedRowBuilder != null
                ? () => _toggleExpand(item)
                : null),
        child: Container(
          width: double.infinity,
          padding: const EdgeInsets.fromLTRB(12, 8, 12, 10),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(ZRadii.lg),
            border: Border.all(
              color: isSelected
                  ? ZColors.brandPrimary.withAlpha(90)
                  : (isDark ? ZColors.darkBorder : ZColors.border),
            ),
          ),
          child: child,
        ),
      ),
    );
  }

  /// Renderer automático: primera columna visible como título y el resto como
  /// pares `ETIQUETA → valor` (los numéricos alineados a la derecha).
  Widget _buildAutoCard(
    T item,
    DataRow row,
    bool isSelected,
    bool isExpanded,
    bool isDark,
  ) {
    final entries = _visibleEntries;
    if (entries.isEmpty) return const SizedBox.shrink();

    final titleEntry = entries.first;
    final detailEntries = entries.skip(1).toList();
    final labelColor = isDark ? ZColors.neutral400 : ZColors.neutral500;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            if (widget.selectionEnabled)
              Padding(
                padding: const EdgeInsets.only(right: ZSpacing.xs),
                child: Checkbox(
                  value: isSelected,
                  onChanged: (val) => _toggleSelect(item, val),
                ),
              ),
            Expanded(
              child: DefaultTextStyle.merge(
                style: ZTypography.titleMedium.copyWith(
                  color: isDark ? ZColors.neutral100 : ZColors.neutral800,
                ),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                child: _cellAt(row, titleEntry.cellIndex),
              ),
            ),
            if (widget.expandedRowBuilder != null)
              Icon(
                isExpanded
                    ? Icons.keyboard_arrow_down
                    : Icons.keyboard_arrow_right,
                size: 20,
                color: labelColor,
              ),
          ],
        ),
        if (detailEntries.isNotEmpty)
          const SizedBox(height: ZSpacing.sm),
        for (final entry in detailEntries)
          Padding(
            padding: const EdgeInsets.only(bottom: 4),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(
                  width: 104,
                  child: Text(
                    entry.column.label.toUpperCase(),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 10.5,
                      letterSpacing: 0.4,
                      fontWeight: FontWeight.w600,
                      color: labelColor,
                    ),
                  ),
                ),
                const SizedBox(width: ZSpacing.sm),
                Expanded(
                  child: Align(
                    alignment: entry.column.numeric
                        ? Alignment.centerRight
                        : Alignment.centerLeft,
                    child: DefaultTextStyle.merge(
                      style: ZTypography.bodyMedium.copyWith(
                        color: isDark
                            ? ZColors.neutral200
                            : ZColors.neutral700,
                      ),
                      child: _cellAt(row, entry.cellIndex),
                    ),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(ZSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.inbox_outlined, size: 48, color: ZColors.neutral300),
            const SizedBox(height: ZSpacing.md),
            Text(
              widget.emptyMessage ?? 'Sin datos para mostrar',
              textAlign: TextAlign.center,
              style: TextStyle(color: ZColors.neutral500, fontSize: 16),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildColumnToggle(bool isDark) {
    return PopupMenuButton<String>(
      icon: Icon(
        Icons.view_column_outlined,
        size: 20,
        color: isDark ? ZColors.neutral400 : ZColors.neutral600,
      ),
      tooltip: 'Columnas',
      onSelected: (id) {
        setState(() {
          if (_hiddenColumns.contains(id)) {
            _hiddenColumns.remove(id);
          } else {
            _hiddenColumns.add(id);
          }
        });
      },
      itemBuilder: (_) => widget.columns.map((col) {
        final isHidden = _hiddenColumns.contains(col.id);
        return PopupMenuItem(
          value: col.id,
          child: Row(
            children: [
              Icon(
                isHidden ? Icons.check_box_outline_blank : Icons.check_box,
                size: 18,
                color: isHidden ? ZColors.neutral400 : ZColors.brandAccent,
              ),
              const SizedBox(width: ZSpacing.sm),
              Text(
                col.label,
                style: TextStyle(
                  color: isDark ? ZColors.neutral200 : ZColors.neutral700,
                ),
              ),
            ],
          ),
        );
      }).toList(),
    );
  }

  Widget _buildPagination(bool isDark, bool isPhone) {
    final total = widget.totalCount!;
    final totalPages = (total + widget.pageSize - 1) ~/ widget.pageSize;
    final from = total == 0 ? 0 : (widget.currentPage - 1) * widget.pageSize + 1;
    final to = widget.currentPage * widget.pageSize < total
        ? widget.currentPage * widget.pageSize
        : total;

    final label = isPhone
        ? '$from–$to de $total'
        : '${widget.currentPage} / $totalPages';

    return Padding(
      padding: const EdgeInsets.only(top: ZSpacing.sm),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left, size: 20),
            tooltip: 'Anterior',
            onPressed: widget.currentPage > 1 && widget.onPageChanged != null
                ? () => widget.onPageChanged!(widget.currentPage - 1)
                : null,
          ),
          Text(
            label,
            style: TextStyle(
              fontSize: 13,
              color: isDark ? ZColors.neutral400 : ZColors.neutral600,
              fontWeight: FontWeight.bold,
            ),
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right, size: 20),
            tooltip: 'Siguiente',
            onPressed: widget.currentPage < totalPages &&
                    widget.onPageChanged != null
                ? () => widget.onPageChanged!(widget.currentPage + 1)
                : null,
          ),
        ],
      ),
    );
  }

  /// Celda segura: si una página pasa menos celdas que columnas devuelve vacío
  /// en vez de lanzar `RangeError`.
  Widget _cellAt(DataRow row, int index) {
    if (index < 0 || index >= row.cells.length) return const SizedBox.shrink();
    return row.cells[index].child;
  }
}
