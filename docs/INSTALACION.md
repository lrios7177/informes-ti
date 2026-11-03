# Instalación de Informes TI

## Obtener el instalador

Si ya recibiste el EXE directamente como archivo de esta entrega, continúa en
**Instalar**. La siguiente opción permite regenerarlo cuando el código esté en GitHub.

Inicia sesión en GitHub con acceso al repositorio privado `lrios7177/informes-ti`.
En **Actions**, selecciona **Windows · compilar, probar e instalar** y pulsa
**Run workflow** sobre `main`. Solo esa ejecución manual genera instalador.

Cuando la ejecución esté verde, descarga el artefacto
`InformesTI-0.2.0-instalador-win-x64`, disponible durante cinco días. Si caducó,
ejecuta de nuevo el workflow. Los eventos push y pull request solo validan el código.

## Instalar

1. Extrae el archivo ZIP descargado.
2. Comprueba el origen privado del artefacto. El archivo `.sha256` permite
   verificar que el EXE no cambió después de generarse.
3. Ejecuta `InformesTI-Setup-0.2.0-win-x64.exe` en Windows 11 x64.
4. Sigue el asistente en español. El acceso directo del escritorio se ofrece
   activado; siempre se crea la entrada del menú Inicio.
5. Abre **Informes TI**.

El instalador incluye .NET y todas las dependencias de ejecución. No requiere
administrador, herramientas de desarrollo ni scripts `.bat`. No incluye un
certificado de firma digital: Windows puede mostrar avisos de editor desconocido;
evalúa el archivo conforme a la política de tu equipo. No desactives la protección
de Windows para instalarlo.

## Actualizar o desinstalar

Cierra Informes TI antes de actualizar. Exporta un respaldo desde la aplicación,
conserva una copia y ejecuta el nuevo instalador para el mismo usuario de Windows.
La identidad del instalador y la ruta de datos son estables entre versiones.

Para desinstalar, usa **Configuración de Windows → Aplicaciones → Aplicaciones
instaladas → Informes TI → Desinstalar**, o el acceso de desinstalación en Inicio.

La desinstalación elimina los binarios y accesos directos; conserva
`%LOCALAPPDATA%\InformesTI`. Si decides borrar definitivamente el historial,
hazlo de forma manual después de exportar un respaldo y cerrar la aplicación.

La versión 0.2.0 exige probar instalación, reapertura, actualización y desinstalación
en Windows antes de publicar una Release.
