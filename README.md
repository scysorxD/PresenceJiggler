# Presence Jiggler (.NET 10 / WinForms)

Aplicación Windows que detecta períodos de inactividad de entrada en tu sesión y emite un pequeño movimiento del cursor, volviendo inmediatamente a la posición original. Funciona desde la bandeja del sistema (tray) y **no tiene integración directa con Microsoft Teams**. Teams puede ignorar entradas simuladas y seguir marcando Ausente; no se garantiza el resultado. Usala respetando las políticas de tu organización.

## Requisitos para compilar

- Windows 11 o Windows 10 compatible.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) y, opcionalmente, Visual Studio con herramientas de desarrollo de escritorio .NET.
- No requiere bibliotecas NuGet externas.

## Abrir en Visual Studio

1. Abrir `PresenceJiggler.sln` con Visual Studio.
2. Compilar con **Ctrl+Shift+B** y ejecutar con **F5**.
3. Buscá el ícono junto al reloj de Windows (puede estar dentro de `^`). Doble clic para Configuración, clic derecho para Activar / Desactivar / Salir.

También podés compilar la solución desde PowerShell:

```powershell
dotnet build .\PresenceJiggler.sln -c Release
```

## Generar un EXE portable/autocontenido

En PowerShell, desde la carpeta raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\publish.ps1
```

El ejecutable queda en `publish\win-x64\PresenceJiggler.exe`.

Para Windows ARM64, agregá `-Runtime win-arm64` al comando anterior. El ejecutable queda en `publish\win-arm64\PresenceJiggler.exe`.

Alternativamente, sin ejecutar ningún script:

```powershell
dotnet publish .\PresenceJiggler\PresenceJiggler.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded -o .\publish\win-x64
```

Luego podés ejecutar `PresenceJiggler.exe` sin instalar .NET Runtime en la PC destino. Para que el inicio automático funcione, **dejá el EXE en una ubicación fija** antes de activar «Iniciar con Windows».

## Configuración

- **Activar / Desactivar**: desde el menú del tray.
- **Inactividad**: de 1 a 30 minutos; 3 minutos por defecto.
- **Iniciar con Windows**: optativo, solo para el usuario actual (registro `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).
- Los ajustes se guardan en `%LOCALAPPDATA%\PresenceJiggler\settings.json`.
- No requiere privilegios de administrador en un Windows sin restricciones empresariales.
- No usa red ni recopila actividad del usuario.

## Cómo funciona

1. Cada segundo consulta `GetLastInputInfo` de Win32 (solo sesión actual).
2. Si el tiempo inactivo alcanza el umbral, emite con `SendInput` dos movimientos relativos de 1 píxel en direcciones opuestas.
3. Evita mover el mouse si algún botón está presionado.
4. La siguiente entrada (real o simulada) reinicia el contador de inactividad de Windows; si se mantiene inactivo, intentará de nuevo tras el próximo período configurado.
5. Espera al menos 30 segundos entre intentos para no insistir si Windows rechaza los eventos.

**Limitaciones:** No funciona en otras sesiones/VDIs sin ejecutarse allí; la sesión puede estar bloqueada o desconectada; Teams puede seguir mostrando Away; protecciones de Windows, políticas empresariales y software de seguridad pueden restringir ejecutables o eventos sintéticos. No altera el estado de Teams mediante API, no evita el bloqueo del equipo ni modifica configuraciones de seguridad.

## Organización para Git

El proyecto ya incluye `.gitignore`. Podés inicializar un repositorio local con:

```powershell
git init
git add .
git commit -m "Initial Presence Jiggler WinForms app"
```

## Pruebas recomendadas

- Compilar `Release` sin advertencias ni errores.
- Ajustar a 1 minuto y verificar movimiento muy breve cuando no tocás mouse/teclado.
- Verificar activación y desactivación desde la bandeja.
- Confirmar que no interfiere mientras el mouse está en uso.
- Abrir dos veces: la segunda instancia debe avisar que ya está abierta.
- Probar «Iniciar con Windows» **solo después de mover el EXE a su ruta definitiva**.
- Comprobar directamente si Teams refleja alguna diferencia (comportamiento no garantizado).
