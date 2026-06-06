import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:zorvian/features/warranties/providers/public_warranty_provider.dart';

class PublicWarrantyTrackPage extends StatefulWidget {
  const PublicWarrantyTrackPage({super.key});

  @override
  State<PublicWarrantyTrackPage> createState() => _PublicWarrantyTrackPageState();
}

class _PublicWarrantyTrackPageState extends State<PublicWarrantyTrackPage> {
  final _warrantyController = TextEditingController();
  final _phoneController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Seguimiento de Garantía')),
      body: ChangeNotifierProvider(
        create: (_) => PublicWarrantyProvider(),
        child: Consumer<PublicWarrantyProvider>(
          builder: (context, provider, _) => Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              children: [
                TextField(controller: _warrantyController, decoration: const InputDecoration(labelText: 'Número de Garantía')),
                TextField(controller: _phoneController, decoration: const InputDecoration(labelText: 'Teléfono')),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: provider.isLoading ? null : () {
                    provider.trackWarranty(_warrantyController.text, _phoneController.text);
                  },
                  child: provider.isLoading ? const CircularProgressIndicator() : const Text('Consultar'),
                ),
                if (provider.error != null) Text(provider.error!, style: const TextStyle(color: Colors.red)),
                if (provider.warranty != null) ...[
                  Text('Producto: ${provider.warranty!.productName}'),
                  Text('Estado: ${provider.warranty!.status}'),
                ]
              ],
            ),
          ),
        ),
      ),
    );
  }
}
