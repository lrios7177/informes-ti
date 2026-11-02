# Estado de la primera versión 0.1.0

Validación realizada el **2 de noviembre de 2026** en Debian Linux x64.
Esta fecha no significa que se haya probado la aplicación instalada en Windows.

## Verificado en este entorno

- SDK .NET 10.0.403 descargado de Microsoft y comprobado con su SHA-512 oficial.
- Restauración de la solución con `--locked-mode` y archivos de bloqueo por proyecto.
- Compilación Release de Core, WPF, pruebas y prueba de controles Windows:
  **0 errores y 0 advertencias**. Se compilaron también los recursos XAML.
- Pruebas de Core: **19 aprobadas, 0 fallidas, 0 omitidas**.
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

La creación automática de `lrios7177/informes-ti` devolvió `Forbidden` en la API.
La comprobación de lectura Git del repositorio nuevo devolvió `Repository not found`;
ese resultado tampoco permite distinguir entre inexistencia y falta de acceso a
un repositorio privado. No se solicitaron ni copiaron tokens.

El proyecto está en `/workspace/informes-ti`, con un remoto HTTPS preparado para
el repositorio nuevo y commits locales. No se modifica `hello-world`. No se afirma
que el código esté subido mientras el push no se confirme.
El intento de `git push --set-upstream origin main` también devolvió
`Repository not found`; no se ha subido código a otro repositorio como alternativa.

El usuario debe crear un repositorio **privado y vacío**, llamado **informes-ti**,
bajo **lrios7177**, sin README, licencia ni `.gitignore` iniciales, y permitir acceso
al repositorio desde el entorno. Después se puede subir `main` y ejecutar Actions.
No se ha ejecutado Actions ni publicado una Release.
