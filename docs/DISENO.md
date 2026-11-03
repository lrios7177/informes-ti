# Diseño de Informes TI 0.2.0

## Aplicación

Paleta azul profundo y cian, con superficies en degradado, marca TI, indicador de
datos locales, panel de historial, barra de acciones en tarjeta y pestañas
redondeadas. La barra de título solicita el modo oscuro de Windows 11 mediante
DWM. Los campos conservan etiquetas, foco de teclado y las mismas funciones.
La apariencia tecnológica no implica servicios de IA ni conexiones nuevas.

`MainWindow` recibe explícitamente `AppWindowStyle`: un estilo implícito para
`Window` no se aplica por herencia a una clase derivada. Esto corrige el fondo
blanco y el texto negro de la captura del usuario. WindowsSmoke verifica que la
ventana realmente tenga fondo oscuro; no se limita a comprobar que exista el recurso.

Los estilos se comparten en `Themes/Dark.xaml`. La prueba usa una Application base
con ese diccionario, sin instanciar la App de producción: procesar eventos para
renderizar una captura no debe ejecutar el arranque normal ni crear datos en el
perfil real. Se verifican estas condiciones antes y después de la captura.

El workflow solicita una imagen de la ventana WPF creada en la prueba con datos
exclusivamente sintéticos. Se conserva cinco días como artefacto de vista de la
interfaz. La imagen captura el contenido WPF; no sustituye la inspección de la
barra nativa ni una prueba manual de accesibilidad y escalado en el equipo final.

## PDF

- A4 con márgenes de 17 mm, fuente Open Sans incrustada y texto seleccionable.
- Franja superior y cabecera azul/cian con número de informe en todas las páginas.
- Título y datos del cliente con jerarquía tipográfica.
- Tres etapas con fecha/hora completas y tres tarjetas de duración.
- Apartados numerados, separadores finos y contenido largo con flujo entre páginas.
- Anexos fotográficos en el orden guardado, con marco, número, archivo y descripción.
- Pie con autoría y número de página sobre el total.

Los colores dejan la mayor parte del papel en blanco para facilitar impresión y
lectura. No se incluyen logotipos de clientes ni imágenes reales de ejemplo.
El ejemplo de revisión contiene únicamente una empresa ficticia y dibujos de prueba.

## Validación

La compilación y las 20 pruebas de Core comprueban contenido, tiempos, fotografías,
respaldos, paginación de texto largo y protección de archivos previos cuando la
exportación falla. Se revisan además las páginas renderizadas del PDF sintético.
La nueva interfaz y la prueba de contraste deben ejecutarse en Windows; compilar
XAML desde Linux no prueba la apariencia final. El historial, la ubicación de datos
y el esquema SQLite versión 1 permanecen compatibles.
