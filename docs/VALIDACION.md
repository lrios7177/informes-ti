# Estado de la primera versión 0.1.1

Validación realizada el **2 de noviembre de 2026** en Debian Linux x64.
Esta fecha no significa que se haya probado la aplicación instalada en Windows.

## Segunda revisión: 0.1.1

Se reprodujo con una prueba nueva que una base SQLite válida pero sin claves
primarias ni restricciones podía importarse y reemplazar un historial correcto.
Ahora se verifica la definición de ambas tablas antes de sustituir los datos.
El esquema de datos sigue siendo la versión 1: los respaldos normales de 0.1.0
siguen siendo compatibles. La prueba falló antes del cambio y pasa después.

La selección de fotografías comprueba además el límite acumulado de 250 MB antes
de cargar todos los archivos en memoria. Se conserva la validación posterior de
los bytes y la decodificación de cada imagen.

El usuario informa de una nueva ejecución roja de Windows. La API continúa
bloqueada y falta el mensaje concreto de esa ejecución para diagnosticarla;
no se da por resuelta ni por verificada la ejecución Windows.

## Corrección tras la primera ejecución en Windows

La ejecución de GitHub Actions compartida por el usuario compiló y ejecutó las
19 pruebas: 16 aprobadas y 3 fallidas al eliminar sus directorios temporales.
Las tres conexiones de inspección de las pruebas usaban el pooling predeterminado
de SQLite, que conservaba handles abiertos y bloqueaba el borrado en Windows.
Se corrigieron con `Pooling=false`, sin suprimir errores de limpieza ni omitir
pruebas. Después del cambio, las 19 pruebas pasan de nuevo en Linux. La corrección
todavía debe confirmarse en una nueva ejecución de Windows sobre `main`.

## Verificado en este entorno

- SDK .NET 10.0.403 descargado de Microsoft y comprobado con su SHA-512 oficial.
- Restauración de la solución con `--locked-mode` y archivos de bloqueo por proyecto.
- Compilación Release de Core, WPF, pruebas y prueba de controles Windows:
  **0 errores y 0 advertencias**. Se compilaron también los recursos XAML.
- Pruebas de Core: **20 aprobadas, 0 fallidas, 0 omitidas**.
  Incluyen persistencia con nueva instancia del repositorio, todos los campos,
  imágenes JPEG/PNG, bytes independientes del archivo original, descripciones,
  orden y eliminación de fotografías, edición, consulta, borrado en cascada,
  rollback transaccional ante conflicto, consultas parametrizadas, validación,
  cruce de medianoche y duraciones superiores a 24 horas.
- Exportación/importación del historial con imágenes y copia del historial anterior;
  importaciones corruptas o con cronología inválida rechazadas sin sustituir los datos.
- PDF abierto por un lector independiente (PdfPig), con textos españoles, acentos,
  fechas, duraciones, numeración y fotografías. Se comprobó contenido largo
  distribuido en varias páginas y que una foto inválida no sobrescribe un PDF previo.
- Correo con asunto, ambas etapas de finalización, resultados y pendientes.
- Auditoría NuGet de dependencias directas y transitivas: sin paquetes vulnerables
  reportados por las fuentes consultadas. La dependencia Pkcs 8.0.1 de la restauración
  inicial se sustituyó explícitamente por 10.0.14; no se suprimió la auditoría.
- Publicación autónoma `win-x64`: `InformesTI.exe` es un ejecutable PE x64;
  incluye `coreclr.dll`, `PresentationFramework.dll` y `e_sqlite3.dll`.
- Instalador EXE generado con NSIS 3.11-1 para el paquete Windows x64. NSIS se obtuvo
  de Debian verificando el índice firmado y el SHA-256 de sus paquetes.
- Integridad del archivo instalador comprobada con 7-Zip: extracción correcta y
  comparación SHA-256 de los 437 archivos publicados con los incluidos en el EXE.
  Esto verifica el empaquetado, no su ejecución ni la desinstalación en Windows.
- Fuente Open Sans y avisos de dependencias y runtimes incluidos en la distribución.

Los logs TRX y binarios son salidas locales en `artifacts/`, excluidas de Git.
El archivo `.sha256` adyacente al instalador identifica el binario exacto de entrega.

## Preparado, todavía no ejecutado en Windows

- La aplicación WPF en pantalla, selección de fechas, diálogos de archivos,
  vista previa, copiar al portapapeles y abrir el visor de PDF.
- `InformesTI.WindowsSmoke`: abre controles reales en un hilo STA, guarda un informe,
  vuelve a abrir la ventana, comprueba fotografía y descripción y persiste una edición.
- `installer/Test-Installer.ps1`: instalación, accesos directos, registro de
  desinstalación, reinstalación y desinstalación con conservación de un marcador
  sintético de datos. Se ejecuta en el workflow manual de Windows.
- Instalación interactiva en Windows 11 y prueba con un historial realista de prueba,
  escalado de pantalla, visor de PDF y reinstalación. Lista: `PRUEBA-WINDOWS.md`.

Compilar WPF y crear un EXE desde Linux **no equivale** a ejecutarlos en Windows.
El instalador se entrega como primera versión de evaluación, sin firma digital.

## GitHub y publicación

Tras crear el repositorio y autorizarlo, se confirmó la subida de los seis commits
iniciales a `lrios7177/informes-ti`: la rama remota `main` coincidía con
`03c6fd54533a3a075bd371ff423ccb83ddc83987`. Las capturas del usuario confirman que
Actions ejecutó la validación y detectó el problema de limpieza descrito arriba.

La API de GitHub sigue respondiendo `Forbidden` desde este entorno, aunque el acceso
Git funciona. El resultado de la siguiente ejecución deberá consultarse en Actions.
No se solicitaron ni copiaron tokens, no se modifica `hello-world` y no se ha
publicado ninguna Release. El proyecto local está en `/workspace/informes-ti`.
