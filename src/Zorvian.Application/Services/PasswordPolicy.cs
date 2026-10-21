namespace Zorvian.Application.Services;

/// <summary>
/// Política de contraseñas compartida por TODOS los flujos que fijan una
/// contraseña nueva (registro, cambio y restablecimiento).
///
/// El frontend replica estas mismas reglas en
/// <c>frontend/lib/core/utils/validators.dart</c> (<c>ZValidators.password</c>):
/// al tocar una de las dos, hay que tocar la otra.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    /// <summary>
    /// Valida una contraseña contra la política. Devuelve <c>null</c> si es
    /// válida o el mensaje de error (en español, listo para el cliente).
    /// </summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "La contraseña es requerida";

        if (password.Length < MinimumLength)
            return $"La contraseña debe tener al menos {MinimumLength} caracteres";

        if (!password.Any(char.IsUpper))
            return "La contraseña debe incluir al menos una letra mayúscula";

        if (!password.Any(char.IsDigit))
            return "La contraseña debe incluir al menos un número";

        return null;
    }
}
