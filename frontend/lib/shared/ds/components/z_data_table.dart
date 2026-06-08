import 'package:flutter/material.dart';
import '../ds.dart';

class ZDataTable<T> extends StatefulWidget {
  final List<DataColumn> columns;
  final List<T> rows;
  final DataRow Function(T item) rowMapper;
  final bool isLoading;
  final String? emptyMessage;
  final int? totalCount;
  final int currentPage;
  final int pageSize;
  final ValueChanged<int>? onPageChanged;
  final String? Function(T)? searchFilter;
  final void Function(String)? onSearch;

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
    this.searchFilter,
    this.onSearch,
  });

  @override
  State<ZDataTable<T>> createState() => _ZDataTableState<T>();
}

final class _ZDataTableState<T> extends State<ZDataTable<T>> {
  final _searchCtrl = TextEditingController();

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (widget.onSearch != null)
          Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.sm),
            child: TextField(
              controller: _searchCtrl,
              decoration: InputDecoration(
                hintText: 'Buscar...',
                prefixIcon: const Icon(Icons.search, size: 20),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
                isDense: true,
                contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
              ),
              onChanged: widget.onSearch,
            ),
          ),
        Expanded(
          child: widget.isLoading
              ? const Center(child: CircularProgressIndicator())
              : widget.rows.isEmpty
                  ? Center(child: Text(widget.emptyMessage ?? 'Sin datos', style: const TextStyle(color: ZColors.neutral600)))
                  : SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: DataTable(
                        headingRowColor: WidgetStateProperty.all(ZColors.neutral50),
                        columns: widget.columns,
                        rows: widget.rows.map((item) => widget.rowMapper(item)).toList(),
                      ),
                    ),
        ),
        if (widget.totalCount != null && widget.totalCount! > widget.pageSize)
          _buildPagination(),
      ],
    );
  }

  Widget _buildPagination() {
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
          Text('${widget.currentPage} / $totalPages', style: const TextStyle(fontSize: 13, color: ZColors.neutral600)),
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
