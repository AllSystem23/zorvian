class AvailableField {
  final String field;
  final String label;
  final String type;

  const AvailableField(this.field, this.label, this.type);
}

const Map<String, List<AvailableField>> moduleFields = {
  'sales': [
    AvailableField('invoiceNumber', 'Factura #', 'string'),
    AvailableField('saleDate', 'Fecha', 'date'),
    AvailableField('total', 'Total', 'number'),
    AvailableField('subtotal', 'Subtotal', 'number'),
    AvailableField('tax', 'Impuesto', 'number'),
    AvailableField('discount', 'Descuento', 'number'),
    AvailableField('status', 'Estado', 'string'),
    AvailableField('paymentMethod', 'Método Pago', 'string'),
    AvailableField('currencyCode', 'Moneda', 'string'),
    AvailableField('clientName', 'Cliente', 'string'),
    AvailableField('employeeName', 'Empleado', 'string'),
    AvailableField('branchName', 'Sucursal', 'string'),
    AvailableField('createdAt', 'Creado', 'date'),
  ],
  'purchases': [
    AvailableField('invoiceNumber', 'Factura #', 'string'),
    AvailableField('purchaseDate', 'Fecha', 'date'),
    AvailableField('total', 'Total', 'number'),
    AvailableField('subtotal', 'Subtotal', 'number'),
    AvailableField('tax', 'Impuesto', 'number'),
    AvailableField('status', 'Estado', 'string'),
    AvailableField('currencyCode', 'Moneda', 'string'),
    AvailableField('supplierName', 'Proveedor', 'string'),
    AvailableField('employeeName', 'Empleado', 'string'),
    AvailableField('createdAt', 'Creado', 'date'),
  ],
  'products': [
    AvailableField('name', 'Nombre', 'string'),
    AvailableField('code', 'Código', 'string'),
    AvailableField('description', 'Descripción', 'string'),
    AvailableField('salePrice', 'Precio Venta', 'number'),
    AvailableField('costPrice', 'Precio Costo', 'number'),
    AvailableField('stock', 'Stock', 'number'),
    AvailableField('minStock', 'Stock Mínimo', 'number'),
    AvailableField('isActive', 'Activo', 'boolean'),
    AvailableField('categoryName', 'Categoría', 'string'),
    AvailableField('brandName', 'Marca', 'string'),
    AvailableField('createdAt', 'Creado', 'date'),
  ],
  'clients': [
    AvailableField('fullName', 'Nombre', 'string'),
    AvailableField('documentNumber', 'Documento', 'string'),
    AvailableField('phone', 'Teléfono', 'string'),
    AvailableField('email', 'Email', 'string'),
    AvailableField('isActive', 'Activo', 'boolean'),
    AvailableField('createdAt', 'Creado', 'date'),
  ],
  'suppliers': [
    AvailableField('name', 'Nombre', 'string'),
    AvailableField('contactName', 'Contacto', 'string'),
    AvailableField('phone', 'Teléfono', 'string'),
    AvailableField('email', 'Email', 'string'),
    AvailableField('isActive', 'Activo', 'boolean'),
    AvailableField('createdAt', 'Creado', 'date'),
  ],
  'employees': [
    AvailableField('firstName', 'Nombre', 'string'),
    AvailableField('lastName', 'Apellido', 'string'),
    AvailableField('employeeCode', 'Código', 'string'),
    AvailableField('email', 'Email', 'string'),
    AvailableField('phone', 'Teléfono', 'string'),
    AvailableField('position', 'Cargo', 'string'),
    AvailableField('salary', 'Salario', 'number'),
    AvailableField('isActive', 'Activo', 'boolean'),
    AvailableField('departmentName', 'Departamento', 'string'),
    AvailableField('branchName', 'Sucursal', 'string'),
    AvailableField('createdAt', 'Creado', 'date'),
  ],
};

const aggregateOptions = [
  null,
  'sum',
  'avg',
  'count',
  'min',
  'max',
];

const operatorOptions = [
  'eq',
  'neq',
  'contains',
  'gt',
  'gte',
  'lt',
  'lte',
  'isnull',
  'isnotnull',
];

String operatorLabel(String op) {
  switch (op) {
    case 'eq': return '=';
    case 'neq': return '≠';
    case 'contains': return 'Contiene';
    case 'gt': return '>';
    case 'gte': return '≥';
    case 'lt': return '<';
    case 'lte': return '≤';
    case 'isnull': return 'Es nulo';
    case 'isnotnull': return 'No es nulo';
    default: return op;
  }
}
