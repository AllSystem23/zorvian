import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import '../ds.dart';

/// Document viewer supporting images, PDFs and other files
class ZDocumentViewer extends StatelessWidget {
  final String url;
  final String? fileName;
  final String? mimeType;
  final double maxHeight;
  final double maxWidth;

  const ZDocumentViewer({
    super.key,
    required this.url,
    this.fileName,
    this.mimeType,
    this.maxHeight = 600,
    this.maxWidth = double.infinity,
  });

  @override
  Widget build(BuildContext context) {
    return ConstrainedBox(
      constraints: BoxConstraints(maxWidth: maxWidth, maxHeight: maxHeight),
      child: Container(
        decoration: BoxDecoration(
          color: ZColors.neutral50,
          borderRadius: BorderRadius.circular(ZRadii.md),
          border: Border.all(color: ZColors.neutral200),
        ),
        child: _buildContent(),
      ),
    );
  }

  Widget _buildContent() {
    if (url.isEmpty) {
      return _buildEmpty('No hay documento');
    }

    final lowerUrl = url.toLowerCase();
    if (_isImage(lowerUrl)) {
      return _buildImage();
    } else if (_isPdf(lowerUrl)) {
      return _buildPdfPlaceholder();
    } else {
      return _buildGenericFile();
    }
  }

  bool _isImage(String url) {
    return url.endsWith('.png') || url.endsWith('.jpg') || url.endsWith('.jpeg') ||
        url.endsWith('.gif') || url.endsWith('.webp') || url.endsWith('.bmp');
  }

  bool _isPdf(String url) {
    return url.endsWith('.pdf');
  }

  Widget _buildImage() {
    return ClipRRect(
      borderRadius: BorderRadius.circular(ZRadii.md),
      child: kIsWeb
          ? Image.network(url, fit: BoxFit.contain, errorBuilder: (_, _, _) => _buildError())
          : Image.file(File(url), fit: BoxFit.contain, errorBuilder: (_, _, _) => _buildError()),
    );
  }

  Widget _buildPdfPlaceholder() {
    return Container(
      padding: const EdgeInsets.all(ZSpacing.xl),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.picture_as_pdf, size: 80, color: ZColors.danger),
          const SizedBox(height: ZSpacing.md),
          Text(fileName ?? 'Documento PDF', style: const TextStyle(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.lg),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              FilledButton.icon(
                onPressed: () => _openInBrowser(),
                icon: const Icon(Icons.open_in_new, size: 16),
                label: const Text('Abrir'),
              ),
              const SizedBox(width: ZSpacing.sm),
              OutlinedButton.icon(
                onPressed: () => _downloadFile(),
                icon: const Icon(Icons.download, size: 16),
                label: const Text('Descargar'),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildGenericFile() {
    return Container(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.insert_drive_file, size: 64, color: ZColors.neutral400),
          const SizedBox(height: ZSpacing.md),
          Text(fileName ?? 'Archivo', style: const TextStyle(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.lg),
          OutlinedButton.icon(
            onPressed: () => _downloadFile(),
            icon: const Icon(Icons.download, size: 16),
            label: const Text('Descargar'),
          ),
        ],
      ),
    );
  }

  Widget _buildEmpty(String message) {
    return Container(
      padding: const EdgeInsets.all(ZSpacing.xl),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.description_outlined, size: 64, color: ZColors.neutral300),
          const SizedBox(height: ZSpacing.md),
          Text(message, style: const TextStyle(color: ZColors.neutral500)),
        ],
      ),
    );
  }

  Widget _buildError() {
    return Container(
      padding: const EdgeInsets.all(ZSpacing.xl),
      child: const Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.broken_image, size: 64, color: ZColors.neutral300),
          SizedBox(height: ZSpacing.md),
          Text('No se pudo cargar el documento', style: TextStyle(color: ZColors.neutral500)),
        ],
      ),
    );
  }

  void _openInBrowser() {
    // In production: use url_launcher
  }

  void _downloadFile() {
    // In production: trigger download
  }
}

/// File picker card
class ZFilePickerCard extends StatelessWidget {
  final String? fileName;
  final String? fileUrl;
  final VoidCallback onTap;
  final VoidCallback? onRemove;
  final String label;
  final IconData icon;
  final String? acceptTypes;

  const ZFilePickerCard({
    super.key,
    this.fileName,
    this.fileUrl,
    required this.onTap,
    this.onRemove,
    this.label = 'Subir archivo',
    this.icon = Icons.cloud_upload_outlined,
    this.acceptTypes,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hasFile = fileName != null || fileUrl != null;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(ZRadii.md),
      child: Container(
        padding: const EdgeInsets.all(ZSpacing.lg),
        decoration: BoxDecoration(
          color: hasFile ? theme.colorScheme.primaryContainer.withValues(alpha: 0.2) : ZColors.neutral50,
          borderRadius: BorderRadius.circular(ZRadii.md),
          border: Border.all(
            color: hasFile ? theme.colorScheme.primary : ZColors.neutral200,
            style: hasFile ? BorderStyle.solid : BorderStyle.solid,
          ),
        ),
        child: Row(
          children: [
            Icon(hasFile ? Icons.check_circle : icon, size: 32, color: hasFile ? theme.colorScheme.primary : ZColors.neutral400),
            const SizedBox(width: ZSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    fileName ?? label,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      fontWeight: FontWeight.w500,
                      color: hasFile ? ZColors.neutral800 : ZColors.neutral500,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (acceptTypes != null)
                    Text(acceptTypes!, style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral400)),
                ],
              ),
            ),
            if (hasFile && onRemove != null)
              IconButton(icon: const Icon(Icons.close, size: 20), onPressed: onRemove, tooltip: 'Quitar'),
          ],
        ),
      ),
    );
  }
}
