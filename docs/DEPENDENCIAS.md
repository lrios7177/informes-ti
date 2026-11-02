# Dependencias y licencias

El código de la aplicación pertenece a Ing. Luis Ríos, MBA. Los componentes de
terceros mantienen sus licencias y avisos; no se relicencian como código propio.
El repositorio incluye los avisos en `docs/licenses/` y el instalador los distribuye
en `Licencias/`. Los paquetes transitivos y sus versiones exactas se fijan en
`packages.lock.json` por proyecto.

| Componente | Versión directa | Uso | Licencia |
| --- | --- | --- | --- |
| .NET / WPF | .NET 10; SDK CI 10.0.403 | Plataforma y escritorio | MIT; avisos del runtime distribuidos con los binarios autónomos |
| Microsoft.Data.Sqlite | 10.0.14 | Historial local y transacciones | MIT |
| SQLitePCLRaw (transitivo) | 2.1.13 | Enlace nativo con SQLite | Apache-2.0 |
| SQLite | Incluido por SQLitePCLRaw | Motor SQL | Dominio público (https://sqlite.org/copyright.html) |
| PDFsharp-MigraDoc / PDFsharp | 6.2.4 | Composición y generación del PDF | MIT |
| System.Security.Cryptography.Pkcs | 10.0.14 | Dependencia criptográfica transitiva actualizada explícitamente | MIT |
| Open Sans Regular y Bold | Fuentes del paquete Debian fonts-open-sans | Fuente incrustada en PDF | Apache-2.0; copyright Google 2010–2011 |
| Microsoft.NET.Test.Sdk | 18.0.1 | Ejecución de pruebas, solo desarrollo | MIT |
| xUnit | 2.9.3 | Pruebas, solo desarrollo | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.5 | Adaptador de pruebas, solo desarrollo | Apache-2.0 |
| PdfPig | 0.1.13 | Lectura independiente del PDF en pruebas | Apache-2.0 |
| NSIS | 3.11 (construcción local: paquete Debian 3.11-1) | Generación del instalador | zlib y licencias de componentes; LZMA CPL-1.0 con excepción explícita de enlace para el software instalado; avisos completos en NSIS-NOTICE.txt |
| GitHub Actions checkout/setup-dotnet/upload-artifact | v4/v5/v4 | Automatización del repositorio | MIT |

No se utiliza QuestPDF ni una API comercial. PDFsharp/MigraDoc no requiere un plan
de pago. El archivo de bloqueo fija también Microsoft.Extensions.*, System.* y
los paquetes internos del test runner; sus avisos están en el inventario incluido.

La referencia directa a Pkcs evita resolver la versión 8.0.1 de la dependencia
de PDFsharp, marcada con vulnerabilidades en la restauración inicial. No se ha
desactivado la auditoría de NuGet ni se suprimen sus avisos.

NSIS se ejecuta como herramienta de construcción; no se instala como herramienta
en el PC del usuario. Su excepción de LZMA permite distribuir el instalador sin
someter el código de la aplicación a CPL. No se ha modificado NSIS. Los avisos
del empaquetador incluyen las licencias de sus componentes y sus excepciones.
Los costes/cuotas de GitHub Actions
dependen de tu cuenta de GitHub; la aplicación instalada no necesita GitHub.
