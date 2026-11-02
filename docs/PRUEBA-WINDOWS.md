# Prueba de aceptación antes de una Release

Esta lista debe ejecutarse en Windows 11 x64 con una cuenta de prueba y datos
sintéticos. La compilación desde Linux no sustituye esta prueba. No marques como
probado un paso que solo está configurado en Actions.

- [ ] Ejecutar el workflow manual y comprobar que compila, prueba y genera el EXE.
- [ ] Instalar sin SDK .NET, aceptar el acceso directo y abrir desde Inicio y escritorio.
- [ ] Confirmar que la interfaz se ve en español, oscura, sin recortes a 100 % y 150 % de escala.
- [ ] Confirmar autoría y técnico predeterminado «Ing. Luis Ríos, MBA».
- [ ] Crear informe ficticio con inicio 30/10/2026 23:15, fin físico 31/10/2026 01:30 y recuperación 31/10/2026 03:45.
- [ ] Verificar duraciones 2 h 15 min, 2 h 15 min y 4 h 30 min.
- [ ] Introducir una fecha inversa y comprobar que el guardado la rechaza.
- [ ] Completar todos los apartados; guardar, cerrar y reabrir desde el historial.
- [ ] Adjuntar un JPEG y un PNG, describirlos, reordenarlos y guardar.
- [ ] Mover los originales; cerrar y reabrir: imágenes y orden deben conservarse.
- [ ] Exportar PDF, abrir en Edge/Acrobat y revisar textos, acentos, encabezado,
      numeración, salto de páginas, fotografías, orientación y descripciones.
- [ ] Generar/copiar correo y pegar en Bloc de notas: asunto, etapas y pendientes completos.
- [ ] Cambiar datos y comprobar que se invalida el correo previo.
- [ ] Cancelar la eliminación y comprobar que se conserva; confirmarla en otro informe.
- [ ] Comprobar guardar/descartar/cancelar al cerrar o cambiar un informe editado.
- [ ] Exportar respaldo, crear otro informe e importar: confirmación, historial
      restaurado y copia anterior disponibles.
- [ ] Intentar importar ZIP dañado; el historial actual debe conservarse.
- [ ] Ejecutar de nuevo el instalador con la aplicación cerrada; los informes deben seguir.
- [ ] Desinstalar: accesos y binarios fuera; carpeta de datos conservada.
- [ ] Reinstalar y comprobar que se recupera el historial anterior.

Registra la versión de Windows, hash SHA-256 del instalador, resultado, fecha,
incidencias y capturas sin datos personales. Solo después de esta prueba se puede
considerar una Release, con autorización explícita del usuario.
