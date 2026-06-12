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

/// A "HubSpot-style" advanced data table with selection, expandable rows, and bulk actions.
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

  List<ZColumn> get _visibleColumns =>
      widget.columns.where((c) => !_hiddenColumns.contains(c.id)).toList();

  bool get _allSelected => widget.rows.isNotEmpty && _selectedItems.length == widget.rows.length;
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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // ── Selection / Bulk Actions Bar ──
        if (_selectedItems.isNotEmpty && widget.bulkActions != null)
          _buildBulkActionBar(isDark)
        else
          _buildToolbar(isDark),

        // ── Table Header (Sticky if enabled) ──
        if (widget.stickyHeader && widget.rows.isNotEmpty)
          _buildHeaderRow(isDark),

        // ── Table Body ──
        Expanded(
          child: widget.isLoading
              ? const Center(child: CircularProgressIndicator())
              : widget.rows.isEmpty
                  ? _buildEmptyState()
                  : _buildRows(isDark),
        ),

        // ── Pagination ──
        if (widget.totalCount != null && widget.totalCount! > widget.pageSize)
          _buildPagination(isDark),
      ],
    );
  }

  Widget _buildToolbar(bool isDark) {
    return Padding(
      padding: const EdgeInsets.only(bottom: ZSpacing.sm),
      child: Row(
        children: [
          if (widget.onSearch != null)
            Expanded(
              child: SizedBox(
                height: 40,
                child: TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Buscar...',
                    prefixIcon: const Icon(Icons.search, size: 18),
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
                    isDense: true,
                    contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
                    filled: true,
                    fillColor: isDark ? ZColors.darkSurface : ZColors.surface,
                  ),
                  onChanged: widget.onSearch,
                ),
              ),
            ),
          if (widget.tableActions != null) ...[
            const SizedBox(width: ZSpacing.sm),
            widget.tableActions!,
          ],
          if (widget.showColumnToggle) ...[
            const SizedBox(width: ZSpacing.xs),
            _buildColumnToggle(isDark),
          ],
          if (widget.showExport) ...[
            const SizedBox(width: ZSpacing.xs),
            IconButton(
              icon: Icon(Icons.download_outlined, size: 20,
                  color: isDark ? ZColors.neutral400 : ZColors.neutral600),
              tooltip: 'Exportar',
              onPressed: widget.onExport,
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildBulkActionBar(bool isDark) {
    return Container(
      height: 48,
      margin: const EdgeInsets.only(bottom: ZSpacing.sm),
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md),
      decoration: BoxDecoration(
        color: isDark ? ZColors.brandPrimary.withAlpha(40) : ZColors.brandPrimary.withAlpha(20),
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(color: ZColors.brandPrimary.withAlpha(60)),
      ),
      child: Row(
        children: [
          Checkbox(
            value: _allSelected,
            tristate: _someSelected,
            onChanged: _toggleSelectAll,
            activeColor: ZColors.brandPrimary,
          ),
          Text(
            '${_selectedItems.length} seleccionados',
            style: const TextStyle(fontWeight: FontWeight.w600, color: ZColors.brandPrimary),
          ),
          const VerticalDivider(width: 32, indent: 12, endIndent: 12),
          Expanded(
            child: Row(
              children: widget.bulkActions!,
            ),
          ),
          IconButton(
            icon: const Icon(Icons.close, size: 18),
            onPressed: () => setState(() => _selectedItems.clear()),
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
          bottom: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
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
          if (widget.expandedRowBuilder != null)
            const SizedBox(width: 32),
          for (final col in _visibleColumns)
            Expanded(
              flex: col.width != null ? 0 : 1,
              child: SizedBox(
                width: col.width,
                child: col.header ?? Text(
                  col.label.toUpperCase(),
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 11,
                    letterSpacing: 0.5,
                    color: isDark ? ZColors.neutral400 : ZColors.neutral600,
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
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: isDark ? ZColors.neutral800.withAlpha(50) : ZColors.neutral50,
                  border: Border(
                    bottom: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
                  ),
                ),
                child: widget.expandedRowBuilder!(item),
              ),
          ],
        );
      },
    );
  }

  Widget _buildDataRow(T item, DataRow row, bool isSelected, bool isExpanded, bool isDark) {
    return InkWell(
      onTap: widget.selectionEnabled ? () => _toggleSelect(item, !isSelected) : (widget.expandedRowBuilder != null ? () => _toggleExpand(item) : null),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 16),
        decoration: BoxDecoration(
          color: isSelected ? (isDark ? ZColors.brandPrimary.withAlpha(30) : ZColors.brandPrimary.withAlpha(10)) : null,
          border: Border(
            bottom: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
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
                  icon: Icon(isExpanded ? Icons.keyboard_arrow_down : Icons.keyboard_arrow_right, size: 20),
                  onPressed: () => _toggleExpand(item),
                ),
              ),
            for (int i = 0; i < _visibleColumns.length; i++)
              Expanded(
                flex: _visibleColumns[i].width != null ? 0 : 1,
                child: SizedBox(
                  width: _visibleColumns[i].width,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    child: _buildCell(row.cells[i]),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildCell(DataCell cell) {
    return cell.child;
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
              style: TextStyle(color: ZColors.neutral500, fontSize: 16),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildColumnToggle(bool isDark) {
    return PopupMenuButton<String>(
      icon: Icon(Icons.view_column_outlined, size: 20,
          color: isDark ? ZColors.neutral400 : ZColors.neutral600),
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
              Text(col.label, style: TextStyle(
                color: isDark ? ZColors.neutral200 : ZColors.neutral700,
              )),
            ],
          ),
        );
      }).toList(),
    );
  }

  Widget _buildPagination(bool isDark) {
    final totalPages = (widget.totalCount! + widget.pageSize - 1) ~/ widget.pageSize;
    return Padding(
      padding: const EdgeInsets.only(top: ZSpacing.sm),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left, size: 20),
            onPressed: widget.currentPage > 1 && widget.onPageChanged != null
                ? () => widget.onPageChanged!(widget.currentPage - 1)
                : null,
          ),
          Text('${widget.currentPage} / $totalPages',
              style: TextStyle(fontSize: 13, color: isDark ? ZColors.neutral400 : ZColors.neutral600, fontWeight: FontWeight.bold)),
          IconButton(
            icon: const Icon(Icons.chevron_right, size: 20),
            onPressed: widget.currentPage < totalPages && widget.onPageChanged != null
                ? () => widget.onPageChanged!(widget.currentPage + 1)
                : null,
          ),
        ],
      ),
    );
  }
}
