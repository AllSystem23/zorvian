namespace Zorvian.Application.Helpers;

/// <summary>
/// Fluid resuelve <c>{{ Company.Name }}</c> como acceso anidado (Company → Name),
/// pero los contextos de entidades y las variables del wizard llegan como claves
/// planas con punto ("Company.Name"). Sin normalizar, esas claves renderizan vacías.
/// Este helper anida las claves con punto en un diccionario anidado antes de renderizar.
/// Las claves planas sin punto (employee_name, position, salary...) se conservan tal cual.
/// </summary>
public static class LiquidModelNormalizer
{
    public static object Normalize(object model) => model switch
    {
        Dictionary<string, string> flat => Nest(ToStringObjectMap(flat)),
        Dictionary<string, object> obj => Nest(obj),
        _ => model
    };

    private static Dictionary<string, object> ToStringObjectMap(Dictionary<string, string> source)
    {
        var map = new Dictionary<string, object>(source.Count, StringComparer.Ordinal);
        foreach (var (key, value) in source)
            map[key] = value;
        return map;
    }

    private static Dictionary<string, object> Nest(Dictionary<string, object> source)
    {
        var root = new Dictionary<string, object>(source.Count, StringComparer.Ordinal);
        var flatKeys = new List<KeyValuePair<string, object>>(source.Count);

        foreach (var (key, value) in source)
        {
            if (key.Contains('.', StringComparison.Ordinal))
                Assign(root, key.Split('.'), value);
            else
                flatKeys.Add(new(key, value));
        }

        // Las claves planas solo rellenan huecos: no pisan estructuras creadas por claves con punto.
        foreach (var (key, value) in flatKeys)
        {
            if (!root.ContainsKey(key))
                root[key] = value;
        }

        return root;
    }

    private static void Assign(Dictionary<string, object> node, string[] path, object value)
    {
        for (var i = 0; i < path.Length - 1; i++)
        {
            if (!node.TryGetValue(path[i], out var next) || next is not Dictionary<string, object> child)
            {
                child = new Dictionary<string, object>(StringComparer.Ordinal);
                node[path[i]] = child;
            }
            node = child;
        }

        var leaf = path[^1];
        if (!node.TryGetValue(leaf, out var existing) || existing is not Dictionary<string, object>)
            node[leaf] = value;
    }
}
