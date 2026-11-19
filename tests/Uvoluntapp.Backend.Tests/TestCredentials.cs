using System;
using System.Security.Cryptography;

namespace Uvoluntapp.Backend.Tests;

// Credenciales de prueba generadas en cada ejecución: el repositorio no guarda ninguna clave fija.
public static class TestCredentials
{
    public static string Valid { get; } = NewValid();

    public static string NewValid() => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)) + "a1";

    public static string TooShort() => Convert.ToHexString(RandomNumberGenerator.GetBytes(2)).ToLowerInvariant()[..3] + "a1";
}
