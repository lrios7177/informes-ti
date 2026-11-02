# Arquitectura y decisiones

La aplicación separa la lógica multiplataforma (`InformesTI.Core`) de WPF
(`InformesTI.App`). Para esta primera versión, la ventana coordina los controles
y las operaciones; la persistencia, validación, composición de correo, PDF y
respaldos no dependen de la interfaz y tienen pruebas de comportamiento.

## Persistencia

SQLite mantiene dos tablas, `Reports` y `Photos`, esquema `PRAGMA user_version=1`.
Los campos del informe se serializan con `System.Text.Json`; fotografías, orden,
descripciones e identificadores están en la tabla relacionada. Guardar usa una
transacción: actualización del informe y sustitución de fotos se confirman juntas.
Las consultas usan parámetros, las claves foráneas están activadas por conexión y
el borrado usa cascada. Las conexiones duran una operación, sin pooling.

Las fechas de trabajo se tratan como tiempo local sin zona (`DateTimeKind.Unspecified`);
el sello de actualización se registra en UTC y se muestra localmente. No se infiere
medianoche a partir de las horas. La aplicación valida inicio ≤ fin físico ≤ recuperación.

Las fotos se validan por firma, tamaño y decodificación antes de aceptarse en la
interfaz y de exportarse. Se almacenan los bytes originales sin copiar rutas
absolutas del usuario. No se ejecuta ningún archivo adjunto.

## Exportación y recuperación

MigraDoc controla el flujo y la paginación; PDFsharp escribe el PDF y las imágenes.
Open Sans se incluye como recurso embebido para que el PDF no dependa de fuentes
instaladas ni de conexiones externas. Cada anexo ajusta su fotografía a un área
máxima de 17 × 18 cm y mantiene su proporción. El PDF se escribe a un temporal
adyacente al destino y se sustituye solo al completar la escritura.

Los respaldos usan la API Backup de SQLite, no una copia inconsistente de una base
abierta. La importación solo acepta dos entradas ZIP de nombre fijo; comprueba
formato, tamaño, integridad SQLite, versión, objetos SQL, relaciones y datos de
los informes/fotografías. No extrae rutas del ZIP. La base candidata se prepara
en un temporal y se copia al mismo volumen antes de sustituir la actual. Antes
de reemplazar se conserva una copia automática del historial anterior.

La importación no fusiona datos y los respaldos no están cifrados. Los límites
evitan cargas accidentales excesivas; no convierten la herramienta en un analizador
de archivos hostiles. Solo deben importarse respaldos de confianza.

## Distribución

Binarios autónomos `win-x64`, sin trimming (WPF usa reflexión) ni empaquetado
single-file. NSIS instala por usuario, crea accesos directos y registra la
desinstalación. La clave de registro, ruta y mutex son estables. Los datos viven fuera de la instalación
y no hay reglas que los eliminen al desinstalar. No se incorpora una clave de firma.

Actions tiene permiso `contents: read`; compila y prueba en Windows. Solo
`workflow_dispatch` publica binarios y construye el instalador. Los artefactos
caducan a los cinco días. La ejecución manual también comprueba instalación,
reinstalación, accesos directos y desinstalación silenciosas, con un marcador
sintético para verificar la conservación de datos. No hay pasos de Release, envío de correo ni subida de
datos de la aplicación. Debe revisarse y actualizarse periódicamente el SDK y las
dependencias, manteniendo sus archivos de bloqueo y la auditoría de NuGet.
