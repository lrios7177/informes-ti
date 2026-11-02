# Uso de Informes TI

## Crear y consultar

Pulsa **Nuevo informe**, completa los datos y guarda con el botón o **Ctrl+S**.
Cada informe recibe un identificador estable `TI-XXXXXXXX`. Los campos obligatorios
son empresa, ubicación, título y técnico. La búsqueda del historial filtra por esos
datos y por el número de informe. Selecciona una entrada para reabrirla y editarla.

Cambiar de informe, crear uno nuevo o cerrar la ventana con cambios pendientes
ofrece guardar, descartar o cancelar. **Eliminar informe** pide confirmación y
elimina sus fotografías; no permite deshacer la operación sin un respaldo.

## Medianoche y recuperación

Las tres etapas tienen fecha y hora completas, locales al lugar del trabajo:

| Etapa | Ejemplo |
| --- | --- |
| Inicio | 30/10/2026 23:15 |
| Fin del trabajo físico | 31/10/2026 01:30 |
| Fin de recuperación | 31/10/2026 03:45 |

Este ejemplo produce 2 h 15 min de trabajo físico, 2 h 15 min de recuperación
posterior y 4 h 30 min en total. Cambia **el día**, no solo la hora, al cruzar
medianoche. El sistema rechaza etapas en orden inverso y no adivina la fecha.
Se permiten etapas simultáneas y duraciones de más de 24 horas. No se aplica
conversión de zona horaria a los horarios registrados de trabajo.

## Fotografías

En **Fotografías**, adjunta JPEG o PNG. Los originales se copian dentro de SQLite;
puedes mover los archivos de origen después de guardar. Selecciona cada fotografía,
escribe la descripción y usa **Subir/Bajar** para cambiar el orden. Pulsa Guardar.
La descripción vacía se muestra como «No registrado» en el anexo.

Límites de la primera versión: 20 MB por archivo, 50 fotografías y 250 MB por informe,
60 megapíxeles y 16.000 píxeles por lado. HEIC, GIF y otros formatos deben convertirse
a JPEG o PNG antes de adjuntarlos. Si un original depende de una orientación EXIF,
gíralo y guarda esa orientación en un editor antes de adjuntarlo para asegurar que
el PDF y la vista previa coincidan.

## Entregar

**Exportar PDF** guarda primero el informe y pide destino. Se usa A4, con encabezado,
fechas, duraciones, detalle técnico y un anexo por fotografía; descripciones largas
pueden continuar en otra página. Revisa el PDF antes de entregarlo.

**Generar correo** guarda primero el informe y genera asunto y mensaje. Usa
**Copiar asunto y mensaje** y pega en tu cliente de correo. Copia el asunto a su
campo y adjunta el PDF manualmente. La aplicación no comprueba que el PDF esté
adjunto, ni envía mensajes. Al editar el informe se borra el correo generado para
evitar copiar un resumen desactualizado.

## Respaldar y restaurar

**Exportar respaldo** incluye el historial guardado completo y las fotografías
en una copia consistente de SQLite dentro de un ZIP. Los cambios descartados o
no guardados no forman parte del respaldo.

**Importar respaldo** reemplaza todo el historial; no mezcla historiales.
Pide confirmación, valida el archivo y conserva una copia de la base anterior
como `informes.db.antes-de-importar-FECHA-ID` en la carpeta de datos. Las copias
anteriores se conservan hasta que las retires manualmente. Límite: 1 GB de base
descomprimida. Para recuperar una copia anterior, cierra la aplicación, conserva
el archivo actual y copia el respaldo anterior a `informes.db`.

No modifiques la base directamente mientras la aplicación está abierta. Solo
puede ejecutarse una instancia por sesión de Windows. La primera versión está
diseñada para un usuario local; no coloques la base activa en una carpeta compartida
ni uses sincronización en vivo entre equipos. Transporta el historial con respaldos.
