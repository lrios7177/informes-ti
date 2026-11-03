# Estado de la versión 0.2.0

Validación realizada el **2 de noviembre de 2026** en Debian Linux x64.
Esta fecha no significa que se haya probado la aplicación instalada en Windows.

## Diseño 0.2.0

Corrección posterior del aislamiento: la captura de la ejecución manual mostró
que la creación del EXE terminaba correctamente, pero la prueba del instalador
detectaba datos previos en el perfil. WindowsSmoke instanciaba la App de producción;
al procesar eventos para la captura también ejecutaba App.OnStartup, que abría
una ventana y una base de datos en la ubicación normal. Se movió el tema a
Themes/Dark.xaml y la prueba usa ahora una Application base con ese mismo tema.
Se comprueba que el arranque y la captura no creen ventanas adicionales ni la
carpeta de datos normal. Se mantiene el rechazo de datos preexistentes en la
prueba del instalador, con diagnóstico de qué objeto existe. Compilación y 20
pruebas aprobadas en Linux; falta confirmar esta corrección en Windows.

Nuevo PDF con cabecera gráfica, tarjetas de duración, secciones numeradas y marcos
fotográficos. Se renderizaron y revisaron la primera página y un anexo del ejemplo
sintético; las 20 pruebas pasan y la solución compila sin advertencias. La prueba
de texto largo conserva el contenido al paginar y la de fotografías conserva el orden.

La interfaz recibe un tema oscuro explícito en MainWindow, cabecera tecnológica,
tarjetas, pestañas y barras oscuras. Se añade una comprobación del fondo en
WindowsSmoke y una captura WPF como artefacto. Esta nueva interfaz no se ha
podido ejecutar en el entorno Linux: su validación Windows y la del instalador
0.2.0 siguen pendientes. Las capturas verdes anteriores validan versiones previas.
No se añaden servicios de IA ni se modifica el esquema SQLite.

## Corrección de acentos del instalador: 0.1.2

La captura de la ejecución manual #6 en verde confirma que 0.1.1 completó la
compilación, las 20 pruebas, WindowsSmoke, la creación del EXE y las pruebas
silenciosas de instalación, reinstalación y desinstalación. La captura posterior
del instalador abierto mostró textos mal decodificados en la bienvenida.

Se reprodujo el problema al preprocesar el script con CP1252: aparecía
«tÃ©cnico». Se guardaron el script y los textos del instalador con BOM UTF-8 y
se fijó `/INPUTCHARSET UTF8` en el workflow y en las instrucciones de compilación.
El mismo ensayo CP1252 ahora conserva «técnico», «Se instalará» e «Ing. Luis Ríos,
MBA». La revisión visual del nuevo asistente en Windows sigue pendiente.

## Segunda revisión: 0.1.1

Se reprodujo con una prueba nueva que una base SQLite válida pero sin claves
primarias ni restricciones podía importarse y reemplazar un historial correcto.
Ahora se verifica la definición de ambas tablas antes de sustituir los datos.
El esquema de datos sigue siendo la versión 1: los respaldos normales de 0.1.0
siguen siendo compatibles. La prueba falló antes del cambio y pasa después.

La selección de fotografías comprueba además el límite acumulado de 250 MB antes
de cargar todos los archivos en memoria. Se conserva la validación posterior de
los bytes y la decodificación de cada imagen.

La captura posterior del usuario confirma la ejecución #3 en verde (commit
`63a81a5`): compilación, las 19 pruebas originales y WindowsSmoke aprobados en
Windows. La ejecución #4 de 0.1.1 (`9e5a4f7`) figura en curso en esa captura;
su resultado y las pruebas del instalador todavía no están confirmados.

## Corrección tras la primera ejecución en Windows

La ejecución de GitHub Actions compartida por el usuario compiló y ejecutó las
19 pruebas: 16 aprobadas y 3 fallidas al eliminar sus directorios temporales.
Las tres conexiones de inspección de las pruebas usaban el pooling predeterminado
de SQLite, que conservaba handles abiertos y bloqueaba el borrado en Windows.
Se corrigieron con `Pooling=false`, sin suprimir errores de limpieza ni omitir
pruebas. Después del cambio, las 19 pruebas pasan de nuevo en Linux y la captura
de la ejecución #3 en verde confirma la validación automática en Windows.

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
- Revisión visual de un PDF sintético de tres páginas, renderizado con Poppler:
  título, encabezado, fechas, duraciones, pie numerado, imagen y descripción del
  primer anexo legibles y sin superposiciones en el ejemplo revisado.
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

## Windows: confirmado y pendiente

- Confirmado en #3: `InformesTI.WindowsSmoke` abre controles reales en un hilo STA,
  guarda un informe, vuelve a abrir la ventana, comprueba fotografía y descripción
  y persiste una edición. También pasó la suite original de 19 pruebas de Core.
- Pendiente: resultado de #4 para 0.1.1 (20 pruebas), que estaba en curso en la captura.
- Pendiente: uso manual de selección de fechas, diálogos de archivos, portapapeles,
  visor de PDF y revisión visual de la interfaz en Windows 11.
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
Git funciona. Los resultados Windows citados se basan en las capturas del usuario;
el resultado de las siguientes ejecuciones deberá consultarse en Actions.
No se solicitaron ni copiaron tokens, no se modifica `hello-world` y no se ha
publicado ninguna Release. El proyecto local está en `/workspace/informes-ti`.
