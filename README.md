# Informes TI

Aplicación de escritorio para **Windows 11 x64**, escrita en C# / .NET 10 y WPF.
**Desarrollado por Ing. Luis Ríos, MBA.**

Registra mantenimientos, conserva fotografías y genera un PDF profesional y el
texto de un correo de entrega. Funciona localmente: no necesita cuenta, suscripción,
servicios de IA ni conexión a Internet para trabajar. Es un proyecto independiente
de `hello-world` y no utiliza GitHub Pages.

## Primera versión

- Crear, editar, guardar, buscar, reabrir y eliminar informes con confirmación.
- Empresa, ubicación, título y técnico; técnico predeterminado: Ing. Luis Ríos, MBA.
- Inicio, fin del trabajo físico y fin de recuperación con **fecha y hora independientes**.
  Acepta trabajos que cruzan medianoche y duran varios días; muestra cada duración.
- Actividades, equipos, incidencias, resultados y pendientes.
- Fotografías JPEG/PNG con vista previa, descripción y botones para cambiar el orden.
- PDF A4 con fuente incluida, encabezado, número de informe, páginas numeradas y
  anexos fotográficos en el orden elegido.
- Correo en español con asunto y resumen, listo para copiar. **No envía mensajes**.
- Respaldo ZIP completo y restauración validada, con copia automática del historial anterior.
- Interfaz oscura en español, guardado con Ctrl+S y protección de cambios sin guardar.

## Descargar e instalar (sin herramientas de desarrollo)

El instalador se genera **bajo demanda**, no mediante una Release. Mientras no esté
subido el repositorio y no haya una ejecución correcta de Actions, no existe un
instalador descargable desde GitHub. Consulta el estado real en [VALIDACION.md](docs/VALIDACION.md).

Una vez alojado el código en el repositorio privado `lrios7177/informes-ti`:

1. Abre **Actions → Windows · compilar, probar e instalar → Run workflow**, rama `main`.
2. Espera a que termine correctamente la ejecución.
3. En **Artifacts**, descarga `InformesTI-0.1.0-instalador-win-x64`. Se conserva **5 días**.
4. Extrae el ZIP y ejecuta `InformesTI-Setup-0.1.0-win-x64.exe`.
5. Sigue el asistente en español y abre **Informes TI** desde Inicio o el escritorio.

No necesitas instalar .NET, ejecutar `.bat` ni instalar herramientas de desarrollo.
El paquete incluye el runtime y se instala para tu usuario. La primera versión no
tiene firma digital comercial; comprueba que el archivo procede de tu ejecución
privada de Actions y revisa su SHA-256. No se publica ninguna Release automáticamente.

Más detalles: [instalación](docs/INSTALACION.md), [manual](docs/USO.md)
y [prueba de aceptación](docs/PRUEBA-WINDOWS.md).

## Datos y privacidad

Todo el historial, incluidas las fotografías originales, se guarda en
`%LOCALAPPDATA%\InformesTI\informes.db`. Las imágenes se almacenan como BLOB en SQLite:
no dependen de la ubicación original de los archivos. La instalación está en
`%LOCALAPPDATA%\Programs\InformesTI`; las actualizaciones y la desinstalación **conservan los datos**.

No se sube información a GitHub desde la aplicación. PDF, bases, respaldos,
certificados y claves están excluidos del repositorio. Las pruebas solo usan datos
y dibujos sintéticos. La base, el PDF, el portapapeles y los respaldos no están
cifrados por la aplicación; usa las protecciones de tu cuenta de Windows y almacena
los respaldos en un lugar autorizado. La herramienta no elimina automáticamente
el historial al desinstalar.

## Estructura

```text
src/InformesTI.Core/          Modelo, validación, SQLite, PDF, correo y respaldos
src/InformesTI.App/           Aplicación WPF y recursos
tests/InformesTI.Core.Tests/  Pruebas de comportamiento multiplataforma
tests/InformesTI.WindowsSmoke/ Prueba real de controles WPF en Windows
installer/                   Instalador NSIS y prueba de instalación
docs/                        Uso, instalación, arquitectura y licencias
.github/workflows/           Compilación, pruebas e instalador bajo demanda
```

## Compilar (solo para desarrollo)

Requiere SDK **.NET 10.0.403** y, para ejecutar WPF, Windows 11 x64.
`global.json` permite bandas posteriores de .NET 10 para desarrollo;
Actions usa una versión concreta. No se necesita otro checkout ni un worktree.

```powershell
dotnet restore InformesTI.sln --locked-mode
dotnet build InformesTI.sln -c Release --no-restore
dotnet test tests/InformesTI.Core.Tests -c Release --no-build
dotnet run --project tests/InformesTI.WindowsSmoke -c Release --no-build
dotnet run --project src/InformesTI.App
```

En Linux se puede compilar WPF mediante `EnableWindowsTargeting`, ejecutar las
pruebas de Core y publicar binarios Windows; **no se puede ejecutar la interfaz WPF**.

Para preparar el instalador en Windows con NSIS 3.11:

```powershell
dotnet publish src/InformesTI.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o artifacts/publish
New-Item artifacts/installer -ItemType Directory -Force
& "${env:ProgramFiles(x86)}\NSIS\makensis.exe" /V3 installer/InformesTI.nsi
```

El instalador queda en `artifacts/installer/`. Antes de una distribución definitiva,
completa la [prueba de instalación, actualización y desinstalación](docs/PRUEBA-WINDOWS.md).

## Licencias

Código propio: copyright de Ing. Luis Ríos, MBA, todos los derechos reservados.
Dependencias abiertas con licencias compatibles MIT, Apache-2.0, zlib y otras
licencias del empaquetador con excepción para el software instalado;
consulta [DEPENDENCIAS.md](docs/DEPENDENCIAS.md) y `docs/licenses/`.
No se usa ninguna biblioteca que exija una suscripción o una licencia comercial de PDF.
